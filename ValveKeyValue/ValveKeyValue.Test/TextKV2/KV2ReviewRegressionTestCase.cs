using System.Numerics;

namespace ValveKeyValue.Test.TextKV2
{
    /// <summary>
    /// Cases that were wrong once and must not come back.
    /// </summary>
    class KV2ReviewRegressionTestCase
    {
        static KVSerializer KV2Text => KVSerializer.Create(KVSerializationFormat.KeyValues2Text);
        static KVSerializer KV2Binary => KVSerializer.Create(KVSerializationFormat.KeyValues2Binary);

        static readonly KVSerializationFormat[] TreeFormats =
        [
            KVSerializationFormat.KeyValues1Text,
            KVSerializationFormat.KeyValues1Binary,
            KVSerializationFormat.KeyValues3Text,
        ];

        static readonly KVSerializationFormat[] Kv2Formats =
        [
            KVSerializationFormat.KeyValues2Text,
            KVSerializationFormat.KeyValues2Binary,
        ];

        static KV2Element Cycle()
        {
            var a = new KV2Element("A", "a", Guid.NewGuid());
            var b = new KV2Element("B", "b", Guid.NewGuid());
            a.Add("b", b);
            b.Add("a", a);
            return a;
        }

        /// <summary>
        /// A DMX graph can contain cycles, and the tree serializers walk it recursively. Without a
        /// depth limit this ends the process rather than raising an error.
        /// </summary>
        [Test]
        public void CyclicGraphIsRejectedByTreeFormats([ValueSource(nameof(TreeFormats))] KVSerializationFormat format)
        {
            var options = format == KVSerializationFormat.KeyValues1Binary
                ? new KVSerializerOptions { StringTable = new StringTable() }
                : null;

            using var stream = new MemoryStream();

            Assert.That(
                () => KVSerializer.Create(format).Serialize(stream, (KVObject)Cycle(), "root", options),
                Throws.InstanceOf<KeyValueException>());
        }

        /// <summary>
        /// A KVObject subclass binds to the generic overload, which must still serialize it as a
        /// KeyValues object rather than reflecting over its properties.
        /// </summary>
        [Test]
        public void ElementPassedToTypedOverloadIsWrittenAsKeyValues()
        {
            var element = new KV2Element("A", "a", Guid.NewGuid());
            element.Add("v", new KVObject(1));

            using var stream = new MemoryStream();
            KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Serialize(stream, element, "root");

            var text = System.Text.Encoding.UTF8.GetString(stream.ToArray());

            Assert.Multiple(() =>
            {
                Assert.That(text, Does.Contain("\"v\""));
                Assert.That(text, Does.Not.Contain("Key"));
                Assert.That(text, Does.Not.Contain("Value"));
            });
        }

        [Test]
        public void ElementPassedToTypedOverloadRoundTripsAsDmx()
        {
            var element = new KV2Element("DmElement", "root", Guid.NewGuid());
            element.Add("v", new KVObject(1));

            using var stream = new MemoryStream();
            KV2Text.Serialize(stream, element, "root");
            stream.Position = 0;

            Assert.That((int)KV2Text.Deserialize(stream).Root["v"], Is.EqualTo(1));
        }

        /// <summary>
        /// Byte and UInt64 live in the inline scalar slot rather than the reference slot.
        /// </summary>
        [Test]
        public void GetValueReadsScalarBackedTypes()
        {
            Assert.Multiple(() =>
            {
                Assert.That(KVObject.Byte(200).GetValue<byte>(), Is.EqualTo(200));
                Assert.That(new KVObject(ulong.MaxValue).GetValue<ulong>(), Is.EqualTo(ulong.MaxValue));
                Assert.That(new KVObject(new Vector3(1, 2, 3)).GetValue<Vector3>(), Is.EqualTo(new Vector3(1, 2, 3)));
            });
        }

        [Test]
        public void ByteConvertsToUnsignedTypesAndBoolean()
        {
            var value = KVObject.Byte(200);

            Assert.Multiple(() =>
            {
                Assert.That(value.ToUInt64(null), Is.EqualTo(200UL));
                Assert.That(value.ToUInt32(null), Is.EqualTo(200U));
                Assert.That(value.ToUInt16(null), Is.EqualTo((ushort)200));
                Assert.That(value.ToBoolean(null), Is.True);
                Assert.That(KVObject.Byte(0).ToBoolean(null), Is.False);
            });
        }

        /// <summary>
        /// A header carried over from another format has a version number rather than a GUID, and
        /// would otherwise format into a KeyValues3 header nothing can parse.
        /// </summary>
        [Test]
        public void ForeignHeaderDoesNotCorruptKeyValues3Output()
        {
            using var source = TestDataHelper.OpenResource("Binary.overboss_run.dmx");
            var header = KV2Binary.Deserialize(source).Header;

            var root = KVObject.Collection();
            root.Add("value", new KVObject(1));

            using var stream = new MemoryStream();
            KVSerializer.Create(KVSerializationFormat.KeyValues3Text).Serialize(stream, new KVDocument(header, "root", root));
            stream.Position = 0;

            var doc = KVSerializer.Create(KVSerializationFormat.KeyValues3Text).Deserialize(stream);

            Assert.That((int)doc.Root["value"], Is.EqualTo(1));
        }

        /// <summary>
        /// Guid.Empty is the null reference sentinel, so an element carrying data without an id
        /// would be written away silently.
        /// </summary>
        [Test]
        public void ChildElementWithoutAnIdIsRejected([ValueSource(nameof(Kv2Formats))] KVSerializationFormat format)
        {
            var child = new KV2Element("DmeChild", "child", default);
            child.Add("value", new KVObject(42));

            var root = new KV2Element("DmElement", "root", Guid.NewGuid());
            root.Add("child", child);

            using var stream = new MemoryStream();

            Assert.That(
                () => KVSerializer.Create(format).Serialize(stream, new KVDocument(null, null, root)),
                Throws.InstanceOf<KeyValueException>());
        }

        [Test]
        public void ElementInsideAnArrayWithoutAnIdIsRejected([ValueSource(nameof(Kv2Formats))] KVSerializationFormat format)
        {
            var root = new KV2Element("DmElement", "root", Guid.NewGuid());
            root.Add("items", KVObject.TypedArray(new List<KV2Element> { new("DmeItem", "item", default) }));

            using var stream = new MemoryStream();

            Assert.That(
                () => KVSerializer.Create(format).Serialize(stream, new KVDocument(null, null, root)),
                Throws.InstanceOf<KeyValueException>());
        }

        /// <summary>
        /// The prefix container precedes the element index, so neither encoding can express a
        /// reference from it.
        /// </summary>
        [Test]
        public void PrefixElementReferencingAnElementIsRejected([ValueSource(nameof(Kv2Formats))] KVSerializationFormat format)
        {
            var shared = new KV2Element("DmeShared", "shared", Guid.NewGuid());
            var prefix = new KV2Element(KV2Element.PrefixElementClassName, string.Empty, Guid.NewGuid());
            prefix.Add("a", shared);

            var root = new KV2Element("DmElement", "root", Guid.NewGuid());

            using var stream = new MemoryStream();

            Assert.That(
                () => KVSerializer.Create(format).Serialize(stream, new KV2Document(null, null, root, prefix)),
                Throws.InstanceOf<KeyValueException>());
        }

        /// <summary>
        /// A DMX element attribute has to be a KV2Element; a plain collection has no class name
        /// or id, and must be reported before anything reaches the stream.
        /// </summary>
        [Test]
        public void PlainCollectionAttributeIsRejectedBeforeWriting([ValueSource(nameof(Kv2Formats))] KVSerializationFormat format)
        {
            var root = new KV2Element("DmElement", "root", Guid.NewGuid());
            root.Add("child", KVObject.Collection());

            using var stream = new MemoryStream();

            Assert.That(
                () => KVSerializer.Create(format).Serialize(stream, new KVDocument(null, null, root)),
                Throws.InstanceOf<KeyValueException>());
        }

        [Test]
        public void ElementArrayItemTagIsCaseInsensitive()
        {
            const string Text = """
                <!-- dmx encoding keyvalues2 1 format dmx 1 -->
                "DmElement"
                {
                	"id" "elementid" "11111111-1111-1111-1111-111111111111"
                	"items" "element_array"
                	[
                		"Element" "22222222-2222-2222-2222-222222222222"
                	]
                }
                """;

            using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(Text));
            var items = KV2Text.Deserialize(stream).Root["items"].GetArray<KV2Element>();

            Assert.That(items, Has.Count.EqualTo(1));
            Assert.That(items[0].ElementId, Is.EqualTo(Guid.Parse("22222222-2222-2222-2222-222222222222")));
        }

        [TestCase("NaN")]
        [TestCase("Infinity")]
        [TestCase("-Infinity")]
        public void NonFiniteTimeIsRejected(string value)
        {
            var text = $$"""
                <!-- dmx encoding keyvalues2 1 format dmx 1 -->
                "DmElement"
                {
                	"id" "elementid" "11111111-1111-1111-1111-111111111111"
                	"t" "time" "{{value}}"
                }
                """;

            using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(text));

            Assert.That(() => KV2Text.Deserialize(stream), Throws.InstanceOf<KeyValueException>());
        }

        /// <summary>
        /// A count read from the file is not trusted until the items behind it exist, so a small
        /// stream claiming a huge array must fail rather than allocate for it.
        /// </summary>
        [Test]
        public void HugeDeclaredArrayCountDoesNotAllocate()
        {
            using var stream = new BinaryKV2.DmxBinaryBuilder()
                .Header("binary", 5)
                .Int(2).Str("DmElement").Str("values")
                .Int(1)
                .Int(0).Int(0).Id(Guid.NewGuid())
                .Int(1)
                .Int(1).U8(46).Int(int.MaxValue) // matrix array claiming int.MaxValue items
                .ToStream();

            Assert.That(() => KV2Binary.Deserialize(stream), Throws.InstanceOf<KeyValueException>());
        }

        [Test]
        public void HugeDeclaredBlobLengthDoesNotAllocate()
        {
            using var stream = new BinaryKV2.DmxBinaryBuilder()
                .Header("binary", 5)
                .Int(2).Str("DmElement").Str("blob")
                .Int(1)
                .Int(0).Int(0).Id(Guid.NewGuid())
                .Int(1)
                .Int(1).U8(6).Int(int.MaxValue) // blob claiming int.MaxValue bytes
                .Raw(1, 2, 3, 4)
                .ToStream();

            Assert.That(() => KV2Binary.Deserialize(stream), Throws.InstanceOf<KeyValueException>());
        }

        /// <summary>
        /// A rejected attribute must be caught before anything reaches the stream, so a failed
        /// write does not leave a truncated document behind.
        /// </summary>
        [Test]
        public void RejectedAttributeLeavesNothingWritten()
        {
            var root = new KV2Element("DmElement", "root", Guid.NewGuid());
            root.Add("child", KVObject.Collection());

            using var stream = new MemoryStream();

            Assert.That(
                () => KV2Text.Serialize(stream, new KVDocument(null, null, root)),
                Throws.InstanceOf<KeyValueException>());

            // The attribute name and an "element" type token must not have been emitted.
            var text = System.Text.Encoding.UTF8.GetString(stream.ToArray());
            Assert.That(text, Does.Not.Contain("child"));
        }
    }
}
