namespace ValveKeyValue.Test
{
    class KVSerializerUtf8NoBomTestCase
    {
        [TestCase]
        public void TextSerializerDoesNotProduceUtf8Preamble()
        {
            var dataObject = new DataObject
            {
                Name = "First"
            };

            byte[] output;
            using (var ms = new MemoryStream())
            {
                KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Serialize(ms, dataObject, "test");

                ms.Seek(0, SeekOrigin.Begin);

                output = ms.ToArray();
            }

            Assert.That(output[0], Is.EqualTo((byte)'"'));
        }

        class DataObject
        {
            public string Name { get; set; }
        }
    }
}
