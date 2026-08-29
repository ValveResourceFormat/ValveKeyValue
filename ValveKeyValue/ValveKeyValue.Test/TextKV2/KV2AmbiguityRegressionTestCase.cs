namespace ValveKeyValue.Test.TextKV2
{
    /// <summary>
    /// Documents whose own content could be mistaken for syntax, and values one encoding can
    /// carry but the other cannot.
    /// </summary>
    class KV2AmbiguityRegressionTestCase
    {
        static KVSerializer KV2Text => KVSerializer.Create(KVSerializationFormat.KeyValues2Text);
        static KVSerializer KV2Binary => KVSerializer.Create(KVSerializationFormat.KeyValues2Binary);

        static readonly KVSerializationFormat[] Kv2Formats =
        [
            KVSerializationFormat.KeyValues2Text,
            KVSerializationFormat.KeyValues2Binary,
        ];

        static KVDocument RoundTrip(KVSerializer serializer, KVDocument doc)
        {
            using var stream = new MemoryStream();
            serializer.Serialize(stream, doc);
            stream.Position = 0;

            return serializer.Deserialize(stream);
        }

        /// <summary>
        /// An inline element writes its class name where a type token would otherwise go, so a
        /// class name that is also a type name has to be written as a top level block instead.
        /// </summary>
        [TestCase("int")]
        [TestCase("float")]
        [TestCase("string")]
        [TestCase("element")]
        [TestCase("Element")]
        [TestCase("vector3")]
        [TestCase("element_array")]
        [TestCase("unknown")]
        [TestCase("elementid")]
        public void ClassNameThatLooksLikeATypeTokenRoundTrips(string className)
        {
            var child = new KV2Element(className, "child", Guid.NewGuid());
            child.Add("value", new KVObject(42));

            var root = new KV2Element("DmElement", "root", Guid.NewGuid());
            root.Add("child", child);

            var child2 = (KV2Element)RoundTrip(KV2Text, new KVDocument(null, null, root)).Root["child"];

            Assert.Multiple(() =>
            {
                Assert.That(child2.ClassName, Is.EqualTo(className));
                Assert.That((int)child2["value"], Is.EqualTo(42));
            });
        }

        [Test]
        public void ClassNameThatLooksLikeATypeTokenRoundTripsInsideAnArray()
        {
            var item = new KV2Element("int", "item", Guid.NewGuid());
            item.Add("value", new KVObject(7));

            var root = new KV2Element("DmElement", "root", Guid.NewGuid());
            root.Add("items", KVObject.TypedArray(new List<KV2Element> { item }));

            var items = RoundTrip(KV2Text, new KVDocument(null, null, root)).Root["items"].GetArray<KV2Element>();

            Assert.That(items, Has.Count.EqualTo(1));
            Assert.Multiple(() =>
            {
                Assert.That(items[0].ClassName, Is.EqualTo("int"));
                Assert.That((int)items[0]["value"], Is.EqualTo(7));
            });
        }

        /// <summary>
        /// The container's class name is how a reader tells it from the root, so it is written as
        /// the sentinel whatever the caller set.
        /// </summary>
        [Test]
        public void PrefixElementWithAnotherClassNameDoesNotBecomeTheRoot([ValueSource(nameof(Kv2Formats))] KVSerializationFormat format)
        {
            var prefix = new KV2Element("MyPrefixClass", string.Empty, Guid.NewGuid());
            prefix.Add("a", new KVObject(1));

            var root = new KV2Element("CMapRootElement", "root", Guid.NewGuid());
            root.Add("keep", new KVObject(2));

            var doc = (KV2Document)RoundTrip(KVSerializer.Create(format), new KV2Document(null, null, root, prefix));

            Assert.That(doc.PrefixElement, Is.Not.Null);

            Assert.Multiple(() =>
            {
                Assert.That(((KV2Element)doc.Root).ClassName, Is.EqualTo("CMapRootElement"));
                Assert.That((int)doc.Root["keep"], Is.EqualTo(2));
                Assert.That(doc.PrefixElement!.ClassName, Is.EqualTo(KV2Element.PrefixElementClassName));
                Assert.That((int)doc.PrefixElement["a"], Is.EqualTo(1));
            });
        }

        /// <summary>
        /// The container has no name member, so an attribute called "name" is an ordinary
        /// attribute there and must survive in both encodings.
        /// </summary>
        [Test]
        public void PrefixAttributeCalledNameSurvives([ValueSource(nameof(Kv2Formats))] KVSerializationFormat format)
        {
            var prefix = new KV2Element(KV2Element.PrefixElementClassName, string.Empty, Guid.NewGuid());
            prefix.Add("name", new KVObject("importantValue"));

            var root = new KV2Element("DmElement", "root", Guid.NewGuid());

            var doc = (KV2Document)RoundTrip(KVSerializer.Create(format), new KV2Document(null, null, root, prefix));

            Assert.That(doc.PrefixElement, Is.Not.Null);
            Assert.That((string)doc.PrefixElement!["name"], Is.EqualTo("importantValue"));
        }

        /// <summary>
        /// Binary strings are null terminated, so an embedded null would end the field early and
        /// desync the rest of the document.
        /// </summary>
        [Test]
        public void EmbeddedNullInAStringIsRejectedByBinary()
        {
            var root = new KV2Element("DmElement", "root", Guid.NewGuid());
            root.Add("v", new KVObject("a\0b"));

            using var stream = new MemoryStream();

            Assert.That(
                () => KV2Binary.Serialize(stream, new KVDocument(null, null, root)),
                Throws.InstanceOf<KeyValueException>());
        }

        [Test]
        public void EmbeddedNullInAnAttributeNameIsRejectedByBinary()
        {
            var root = new KV2Element("DmElement", "root", Guid.NewGuid());
            root.Add("a\0b", new KVObject(1));

            using var stream = new MemoryStream();

            Assert.That(
                () => KV2Binary.Serialize(stream, new KVDocument(null, null, root)),
                Throws.InstanceOf<KeyValueException>());
        }
    }
}
