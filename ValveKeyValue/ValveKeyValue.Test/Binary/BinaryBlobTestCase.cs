namespace ValveKeyValue.Test
{
    class BinaryBlobTestCase
    {
        [Test]
        public void DeserializesBlob()
        {
            var data = new byte[]
            {
                0x00, // object: TestObject
                    0x54, 0x65, 0x73, 0x74, 0x4F, 0x62, 0x6A, 0x65, 0x63, 0x74, 0x00,
                    0x09, // binary blob: blob = 00 01 FF, uint32 length
                        0x62, 0x6C, 0x6F, 0x62, 0x00,
                        0x03, 0x00, 0x00, 0x00,
                        0x00, 0x01, 0xFF,
                    0x09, // binary blob: empty
                        0x65, 0x6D, 0x70, 0x74, 0x79, 0x00,
                        0x00, 0x00, 0x00, 0x00,
                    0x08, // end object
                0x08, // end document
            };

            var obj = KVSerializer.Create(KVSerializationFormat.KeyValues1Binary).Deserialize(data);

            Assert.Multiple(() =>
            {
                Assert.That(obj["blob"].ValueType, Is.EqualTo(KVValueType.BinaryBlob));
                Assert.That(obj["blob"].AsBlob(), Is.EqualTo(new byte[] { 0x00, 0x01, 0xFF }));
                Assert.That(obj["empty"].AsBlob(), Is.Empty);
            });
        }

        [Test]
        public void TruncatedBlobThrows()
        {
            var data = new byte[]
            {
                0x00,
                    0x54, 0x65, 0x73, 0x74, 0x4F, 0x62, 0x6A, 0x65, 0x63, 0x74, 0x00,
                    0x09,
                        0x62, 0x6C, 0x6F, 0x62, 0x00,
                        0x10, 0x00, 0x00, 0x00,
                        0x00, 0x01,
            };

            Assert.That(
                () => KVSerializer.Create(KVSerializationFormat.KeyValues1Binary).Deserialize(data),
                Throws.Exception.InstanceOf<KeyValueException>().With.InnerException.TypeOf<EndOfStreamException>());
        }
    }
}
