namespace ValveKeyValue.Test.TextKV2
{
    /// <summary>
    /// Cases where a document cannot be written faithfully, and the writers have to say so rather
    /// than emit something that cannot be read back.
    /// </summary>
    class KV2WriterGuardTestCase
    {
        static KVSerializer Serializer(KVSerializationFormat format) => KVSerializer.Create(format);

        /// <summary>
        /// <see cref="Guid.Empty"/> is the null reference sentinel, so a root carrying it would be
        /// skipped by the element walk and produce a document with no elements.
        /// </summary>
        [TestCase(KVSerializationFormat.KeyValues2Text)]
        [TestCase(KVSerializationFormat.KeyValues2Binary)]
        public void RejectsRootWithoutAnElementId(KVSerializationFormat format)
        {
            var root = new KV2Element("DmElement", "root", Guid.Empty);

            using var stream = new MemoryStream();

            Assert.That(
                () => Serializer(format).Serialize(stream, new KVDocument(null, null, root)),
                Throws.InstanceOf<KeyValueException>());
        }

        [TestCase(KVSerializationFormat.KeyValues2Text)]
        [TestCase(KVSerializationFormat.KeyValues2Binary)]
        public void RejectsNullRoot(KVSerializationFormat format)
        {
            using var stream = new MemoryStream();

            Assert.That(
                () => Serializer(format).Serialize(stream, new KVDocument(null, null, KV2Element.Null)),
                Throws.InstanceOf<KeyValueException>());
        }

        [TestCase(KVSerializationFormat.KeyValues2Text)]
        [TestCase(KVSerializationFormat.KeyValues2Binary)]
        public void RejectsStubRoot(KVSerializationFormat format)
        {
            using var stream = new MemoryStream();

            Assert.That(
                () => Serializer(format).Serialize(stream, new KVDocument(null, null, KV2Element.Stub(Guid.NewGuid()))),
                Throws.InstanceOf<KeyValueException>());
        }

        [TestCase(KVSerializationFormat.KeyValues2Text)]
        [TestCase(KVSerializationFormat.KeyValues2Binary)]
        public void WritesNullStringArrayItemsAsEmptyStrings(KVSerializationFormat format)
        {
            var root = new KV2Element("DmElement", "root", Guid.NewGuid());
            root.Add("strings", KVObject.TypedArray(new List<string> { "a", null!, "c" }));

            using var stream = new MemoryStream();
            Serializer(format).Serialize(stream, new KVDocument(null, null, root));
            stream.Position = 0;

            var strings = Serializer(format).Deserialize(stream).Root["strings"].GetArray<string>();

            Assert.That(strings, Is.EqualTo(new[] { "a", string.Empty, "c" }));
        }

        /// <summary>
        /// An arbitrary object has no class name or element id, so the typed overload cannot
        /// produce DMX. The check runs before the object is converted, so this is what the caller
        /// sees even for an object the converter would reject.
        /// </summary>
        [TestCase(KVSerializationFormat.KeyValues2Text)]
        [TestCase(KVSerializationFormat.KeyValues2Binary)]
        public void TypedSerializeReportsThatDmxNeedsElements(KVSerializationFormat format)
        {
            var selfReferencing = new Node();
            selfReferencing.Next = selfReferencing;

            using var stream = new MemoryStream();

            Assert.That(
                () => Serializer(format).Serialize(stream, selfReferencing, "root"),
                Throws.InstanceOf<InvalidOperationException>().With.Message.Contains(nameof(KV2Element)));
        }

        class Node
        {
            public Node? Next { get; set; }
        }
    }
}
