namespace ValveKeyValue.Test
{
    class StringTableFromScratchTestCase
    {
        private static readonly string[] ExpectedStrings = ["root", "key", "child"];

        [Test]
        public void PopulatesStringTableDuringSerialization()
        {
            var child = KVObject.ListCollection();
            child.Add("key", 123);
            var kv = KVObject.ListCollection();
            kv.Add("key", "value");
            kv.Add("child", child);
            var doc = new KVDocument(null!, "root", kv);

            var stringTable = new StringTable();

            var serializer = KVSerializer.Create(KVSerializationFormat.KeyValues1Binary);

            using var ms = new MemoryStream();
            serializer.Serialize(ms, doc, new KVSerializerOptions { StringTable = stringTable });

            var strings = stringTable.ToArray();
            Assert.That(strings, Is.EqualTo(ExpectedStrings));
        }
    }
}
