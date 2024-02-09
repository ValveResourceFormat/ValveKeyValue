namespace ValveKeyValue.Test
{
    class BinaryObjectConsecutiveDeserializationTestCase
    {
        FirstObject _firstObject;
        SecondObject _secondObject;

        [Test]
        public void TestFirstObject()
            => Assert.That(_firstObject.StringValue, Is.EqualTo("firstvalue"));

        [Test]
        public void TestSecondObject()
            => Assert.That(_secondObject.StringValue, Is.EqualTo("secondvalue"));

        [OneTimeSetUp]
        public void SetUp()
        {
            var data = new byte[]
            {
                0x00, // object: FirstObject
                0x46, 0x69, 0x72, 0x73, 0x74, 0x4f, 0x62, 0x6a, 0x65, 0x63, 0x74, 0x00,
                0x01, // string: firstkey = firstvalue
                0x66, 0x69, 0x72, 0x73, 0x74, 0x6b, 0x65, 0x79, 0x00,
                0x66, 0x69, 0x72, 0x73, 0x74, 0x76, 0x61, 0x6c, 0x75, 0x65, 0x00,
                0x08, // end object
                0x08, // end document

                0x00, // object: SecondObject
                0x53, 0x65, 0x63, 0x6f, 0x6e, 0x64, 0x4f, 0x62, 0x6a, 0x65, 0x63, 0x74, 0x00,
                0x01, // string: secondkey = secondvalue
                0x73, 0x65, 0x63, 0x6f, 0x6e, 0x64, 0x6b, 0x65, 0x79, 0x00,
                0x73, 0x65, 0x63, 0x6f, 0x6e, 0x64, 0x76, 0x61, 0x6c, 0x75, 0x65, 0x00,
                0x08, // end object
                0x08, // end document
            };

            using var stream = new MemoryStream();
            stream.Write(data, 0, data.Length);
            stream.Seek(0, SeekOrigin.Begin);
            _firstObject = KVSerializer.Create(KVSerializationFormat.KeyValues1Binary).Deserialize<FirstObject>(stream);
            Assert.That(stream.Position, Is.EqualTo(36)); // ensure we read exactly 36 bytes
            _secondObject = KVSerializer.Create(KVSerializationFormat.KeyValues1Binary).Deserialize<SecondObject>(stream);
            Assert.That(stream.Position, Is.EqualTo(75)); // ensure we read exactly 39 bytes
        }

        class FirstObject
        {
            [KVProperty("firstkey")]
            public string StringValue { get; set; }
        }

        class SecondObject
        {
            [KVProperty("secondkey")]
            public string StringValue { get; set; }
        }
    }
}
