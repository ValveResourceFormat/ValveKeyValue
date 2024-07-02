using System.Linq;

namespace ValveKeyValue.Test
{
    class StringTableFromScratchTestCase
    {
        [Test]
        public void PopulatesStringTableDuringSerialization()
        {
            var kv = new KVObject("root",
            [
                new KVObject("key", "value"),
                new KVObject("child", [
                    new KVObject("key", 123),
                ]),
            ]);

            var stringTable = new StringTable();

            var serializer = KVSerializer.Create(KVSerializationFormat.KeyValues1Binary);

            using var ms = new MemoryStream();
            serializer.Serialize(ms, kv, new KVSerializerOptions { StringTable = stringTable });

            var strings = stringTable.ToArray();
            Assert.That(strings, Is.EqualTo(new[] { "root", "key", "child" }));
        }
    }
}
