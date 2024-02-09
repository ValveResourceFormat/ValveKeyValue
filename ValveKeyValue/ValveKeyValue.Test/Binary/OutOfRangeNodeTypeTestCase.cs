namespace ValveKeyValue.Test
{
    class OutOfRangeNodeTypeTestCase
    {
        [Test]
        public void ThrowsKeyValueException()
        {
            var data = new byte[]
            {
                0x00,
                    0x54, 0x65, 0x73, 0x74, 0x4F, 0x62, 0x6A, 0x65, 0x63, 0x74, 0x00,
                    0xA0, // Way out of range
                        0x6B, 0x65, 0x79, 0x00,
                        0x00, 0x00, 0x00, 0x00,
                    0x08,
                0x08
            };

            Assert.That(
                () => KVSerializer.Create(KVSerializationFormat.KeyValues1Binary).Deserialize(data),
                Throws.Exception.InstanceOf<KeyValueException>().With.InnerException.TypeOf<ArgumentOutOfRangeException>());
        }
    }
}
