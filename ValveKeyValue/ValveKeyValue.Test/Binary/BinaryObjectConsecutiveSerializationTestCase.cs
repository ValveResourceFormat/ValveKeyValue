namespace ValveKeyValue.Test
{
    class BinaryObjectConsecutiveSerializationTestCase
    {
        [Test]
        public void SerializesToBinaryStructure()
        {
            var first = new KVObject("FirstObject",
            [
                new KVObject("firstkey", "firstvalue")
            ]);

            var second = new KVObject("SecondObject",
            [
                new KVObject("secondkey", "secondvalue")
            ]);

            var expectedData = new byte[]
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
            KVSerializer.Create(KVSerializationFormat.KeyValues1Binary).Serialize(stream, first);
            KVSerializer.Create(KVSerializationFormat.KeyValues1Binary).Serialize(stream, second);
            Assert.That(stream.ToArray(), Is.EqualTo(expectedData));
            Assert.That(stream.CanRead, Is.True);
        }
    }
}
