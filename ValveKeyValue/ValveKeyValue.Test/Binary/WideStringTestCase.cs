namespace ValveKeyValue.Test
{
    class WideStringTestCase
    {
        [Test]
        public void DeserializationThrowsException()
        {
            var data = new byte[]
            {
                0x00, // object: TestObject
                    0x54, 0x65, 0x73, 0x74, 0x4F, 0x62, 0x6A, 0x65, 0x63, 0x74, 0x00,
                    0x05, // wstring: key = value. I'm guessing here what the value would look like. Doesn't really matter.
                        0x6B, 0x65, 0x79, 0x00,
                        0x76, 0x00, 0x61, 0x00, 0x6C, 0x00, 0x75, 0x00, 0x65, 0x00, 0x00, 0x00,
                    0x08, // end object
                0x08, // end document
            };

            Assert.That(
                () => KVSerializer.Create(KVSerializationFormat.KeyValues1Binary).Deserialize(data),
                Throws.Exception.InstanceOf<KeyValueException>().With.InnerException.TypeOf<NotSupportedException>());
        }
    }
}
