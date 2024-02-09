namespace ValveKeyValue.Test
{
    class TruncatedDataTestCase
    {
        [Test]
        public void WhileReadingEmptyData()
        {
            var data = Array.Empty<byte>();

            Assert.That(
                () => serializer.Deserialize(data),
                Throws.Exception.InstanceOf<KeyValueException>().With.InnerException.TypeOf<EndOfStreamException>());
        }

        [Test]
        public void WhileReadingObject()
        {
            var data = new byte[]
            {
                0x00,
            };

            Assert.That(
                () => serializer.Deserialize(data),
                Throws.Exception.InstanceOf<KeyValueException>().With.InnerException.TypeOf<EndOfStreamException>());
        }

        [Test]
        public void WhileReadingObjectName()
        {
            var data = new byte[]
            {
                0x00,
                    0x54, 0x65, 0x73, 0x74, 0x4F, 0x62, 0x6A, 0x65, 0x63, 0x74
            };

            Assert.That(
                () => serializer.Deserialize(data),
                Throws.Exception.InstanceOf<KeyValueException>().With.InnerException.TypeOf<EndOfStreamException>());
        }

        [Test]
        public void WhileReadingValueType()
        {
            var data = new byte[]
            {
                0x00,
                    0x54, 0x65, 0x73, 0x74, 0x4F, 0x62, 0x6A, 0x65, 0x63, 0x74, 0x00,
            };

            Assert.That(
                () => serializer.Deserialize(data),
                Throws.Exception.InstanceOf<KeyValueException>().With.InnerException.TypeOf<EndOfStreamException>());
        }

        [Test]
        public void WhileReadingValueName()
        {
            var data = new byte[]
            {
                0x00,
                    0x54, 0x65, 0x73, 0x74, 0x4F, 0x62, 0x6A, 0x65, 0x63, 0x74, 0x00,
                    0x02,
                        0x6B, 0x65, 0x79
            };

            Assert.That(
                () => serializer.Deserialize(data),
                Throws.Exception.InstanceOf<KeyValueException>().With.InnerException.TypeOf<EndOfStreamException>());
        }

        [Test]
        public void WhileReadingValue()
        {
            var data = new byte[]
            {
                0x00,
                    0x54, 0x65, 0x73, 0x74, 0x4F, 0x62, 0x6A, 0x65, 0x63, 0x74, 0x00,
                    0x02,
                        0x6B, 0x65, 0x79, 0x00,
                        0x01
            };

            Assert.That(
                () => serializer.Deserialize(data),
                Throws.Exception.InstanceOf<KeyValueException>().With.InnerException.TypeOf<EndOfStreamException>());
        }

        KVSerializer serializer;

        [OneTimeSetUp]
        public void SetUpSerializer()
        {
            serializer = KVSerializer.Create(KVSerializationFormat.KeyValues1Binary);
        }
    }
}
