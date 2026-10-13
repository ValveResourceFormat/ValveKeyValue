namespace ValveKeyValue.Test
{
    class WideStringTestCase
    {
        [Test]
        public void DeserializesSteamWideString()
        {
            var data = new byte[]
            {
                0x00, // object: TestObject
                    0x54, 0x65, 0x73, 0x74, 0x4F, 0x62, 0x6A, 0x65, 0x63, 0x74, 0x00,
                    0x05, // wide string: key = value, int32 length including the terminator
                        0x6B, 0x65, 0x79, 0x00,
                        0x06, 0x00, 0x00, 0x00,
                        0x76, 0x00, 0x61, 0x00, 0x6C, 0x00, 0x75, 0x00, 0x65, 0x00, 0x00, 0x00,
                    0x05, // wide string: empty = ""
                        0x65, 0x6D, 0x70, 0x74, 0x79, 0x00,
                        0x01, 0x00, 0x00, 0x00,
                        0x00, 0x00,
                    0x08, // end object
                0x08, // end document
            };

            var obj = KVSerializer.Create(KVSerializationFormat.KeyValues1Binary).Deserialize(data);

            Assert.Multiple(() =>
            {
                Assert.That((string)obj["key"], Is.EqualTo("value"));
                Assert.That((string)obj["empty"], Is.Empty);
            });
        }

        [Test]
        public void NegativeLengthThrows()
        {
            var data = new byte[]
            {
                0x00,
                    0x54, 0x65, 0x73, 0x74, 0x4F, 0x62, 0x6A, 0x65, 0x63, 0x74, 0x00,
                    0x05,
                        0x6B, 0x65, 0x79, 0x00,
                        0xFF, 0xFF, 0xFF, 0xFF,
            };

            Assert.That(
                () => KVSerializer.Create(KVSerializationFormat.KeyValues1Binary).Deserialize(data),
                Throws.Exception.InstanceOf<KeyValueException>());
        }
    }
}
