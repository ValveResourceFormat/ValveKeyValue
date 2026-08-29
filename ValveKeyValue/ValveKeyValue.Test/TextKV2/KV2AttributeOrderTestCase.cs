namespace ValveKeyValue.Test.TextKV2
{
    /// <summary>
    /// DMX preserves the order attributes appear in the file, so the collection backing a
    /// <see cref="KV2Element"/> has to preserve insertion order even across removals.
    /// </summary>
    class KV2AttributeOrderTestCase
    {
        static readonly string[] Abc = ["a", "b", "c"];
        static readonly string[] Acd = ["a", "c", "d"];
        static readonly string[] RoundTripped = ["first", "third", "fourth"];

        static List<string> KeysOf(KVObject element)
        {
            var keys = new List<string>();

            foreach (var key in element.Keys)
            {
                keys.Add(key);
            }

            return keys;
        }

        [Test]
        public void PreservesInsertionOrder()
        {
            var element = new KV2Element("C", "n", Guid.NewGuid());
            element.Add("a", new KVObject(1));
            element.Add("b", new KVObject(2));
            element.Add("c", new KVObject(3));

            Assert.That(KeysOf(element), Is.EqualTo(Abc));
        }

        /// <summary>
        /// A plain dictionary reuses the freed slot here and enumerates a, d, c.
        /// </summary>
        [Test]
        public void PreservesOrderAfterRemoveThenAdd()
        {
            var element = new KV2Element("C", "n", Guid.NewGuid());
            element.Add("a", new KVObject(1));
            element.Add("b", new KVObject(2));
            element.Add("c", new KVObject(3));
            element.Remove("b");
            element.Add("d", new KVObject(4));

            Assert.That(KeysOf(element), Is.EqualTo(Acd));
        }

        [Test]
        public void OverwritingAValueKeepsItsPosition()
        {
            var element = new KV2Element("C", "n", Guid.NewGuid());
            element.Add("a", new KVObject(1));
            element.Add("b", new KVObject(2));
            element.Add("c", new KVObject(3));
            element["b"] = new KVObject(20);

            Assert.Multiple(() =>
            {
                Assert.That(KeysOf(element), Is.EqualTo(Abc));
                Assert.That((int)element["b"], Is.EqualTo(20));
            });
        }

        [TestCase(KVSerializationFormat.KeyValues2Text)]
        [TestCase(KVSerializationFormat.KeyValues2Binary)]
        public void MutatedElementRoundTripsInOrder(KVSerializationFormat format)
        {
            var serializer = KVSerializer.Create(format);

            var root = new KV2Element("DmElement", "root", Guid.NewGuid());
            root.Add("first", new KVObject(1));
            root.Add("doomed", new KVObject(2));
            root.Add("third", new KVObject(3));
            root.Remove("doomed");
            root.Add("fourth", new KVObject(4));

            using var stream = new MemoryStream();
            serializer.Serialize(stream, new KVDocument(null, null, root));
            stream.Position = 0;

            Assert.That(KeysOf(serializer.Deserialize(stream).Root), Is.EqualTo(RoundTripped));
        }
    }
}
