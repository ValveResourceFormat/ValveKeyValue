using System.Text;

namespace ValveKeyValue.Test.BinaryKV2
{
    /// <summary>
    /// Cases that would otherwise end the process rather than raise an error: unbounded recursion
    /// and a blob that runs off the end of the stream.
    /// </summary>
    class KV2LimitsTestCase
    {
        static KVSerializer KV2Text => KVSerializer.Create(KVSerializationFormat.KeyValues2Text);
        static KVSerializer KV2Binary => KVSerializer.Create(KVSerializationFormat.KeyValues2Binary);

        const int TooDeep = 400;

        [Test]
        public void TextReaderRejectsExcessiveNesting()
        {
            var text = new StringBuilder("<!-- dmx encoding keyvalues2 1 format dmx 1 -->\n\"DmElement\"\n{\n");

            for (var i = 0; i < TooDeep; i++)
            {
                text.Append("\"child\" \"DmeChild\"\n{\n");
            }

            for (var i = 0; i < TooDeep; i++)
            {
                text.Append("}\n");
            }

            text.Append("}\n");

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(text.ToString()));

            Assert.That(() => KV2Text.Deserialize(stream), Throws.InstanceOf<KeyValueException>());
        }

        [TestCase(KVSerializationFormat.KeyValues2Text)]
        [TestCase(KVSerializationFormat.KeyValues2Binary)]
        public void WritersRejectExcessiveNesting(KVSerializationFormat format)
        {
            var root = new KV2Element("DmElement", "root", Guid.NewGuid());
            var current = root;

            for (var i = 0; i < TooDeep; i++)
            {
                var child = new KV2Element("DmeChild", "child", Guid.NewGuid());
                current.Add("child", child);
                current = child;
            }

            using var stream = new MemoryStream();

            Assert.That(
                () => KVSerializer.Create(format).Serialize(stream, new KVDocument(null, null, root)),
                Throws.InstanceOf<KeyValueException>());
        }

        /// <summary>
        /// A cyclic element graph has no depth, so the writers stop on the visited set rather than
        /// the depth limit, and the cycle becomes an id reference.
        /// </summary>
        [TestCase(KVSerializationFormat.KeyValues2Text)]
        [TestCase(KVSerializationFormat.KeyValues2Binary)]
        public void WritersHandleCycles(KVSerializationFormat format)
        {
            var serializer = KVSerializer.Create(format);

            var root = new KV2Element("DmElement", "root", Guid.NewGuid());
            var child = new KV2Element("DmeChild", "child", Guid.NewGuid());
            root.Add("child", child);
            child.Add("parent", root);

            using var stream = new MemoryStream();
            serializer.Serialize(stream, new KVDocument(null, null, root));
            stream.Position = 0;

            var root2 = (KV2Element)serializer.Deserialize(stream).Root;
            var child2 = (KV2Element)root2["child"];

            Assert.That(child2["parent"], Is.SameAs(root2));
        }

        /// <summary>
        /// A cyclic graph read into a typed object cannot terminate, so it has to be reported
        /// rather than overflowing the stack.
        /// </summary>
        [Test]
        public void TypedDeserializationRejectsCycles()
        {
            var root = new KV2Element("DmElement", "root", Guid.NewGuid());
            var child = new KV2Element("DmeChild", "child", Guid.NewGuid());
            root.Add("child", child);
            child.Add("parent", root);

            using var stream = new MemoryStream();
            KV2Text.Serialize(stream, new KVDocument(null, null, root));
            stream.Position = 0;

            Assert.That(() => KV2Text.Deserialize<CyclicNode>(stream), Throws.InstanceOf<KeyValueException>());
        }

        class CyclicNode
        {
            public CyclicNode? Child { get; set; }
            public CyclicNode? Parent { get; set; }
        }

        [Test]
        public void RejectsBlobThatRunsPastEndOfStream()
        {
            using var stream = new DmxBinaryBuilder()
                .Header("binary", 5)
                .Int(2).Str("DmElement").Str("blob")
                .Int(1)
                .Int(0).Int(0).Id(Guid.NewGuid())
                .Int(1)
                .Int(1).U8(6).Int(64) // a 64 byte blob, but only four bytes follow
                .Raw(1, 2, 3, 4)
                .ToStream();

            Assert.That(() => KV2Binary.Deserialize(stream), Throws.InstanceOf<KeyValueException>());
        }
    }
}
