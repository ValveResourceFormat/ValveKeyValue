using System.Globalization;

namespace ValveKeyValue.Test.BinaryKV2
{
    /// <summary>
    /// Header parsing and the encoding variants that change how a document is written.
    /// </summary>
    class KV2HeaderAndEncodingTestCase
    {
        static KVSerializer KV2Binary => KVSerializer.Create(KVSerializationFormat.KeyValues2Binary);
        static KVSerializer KV2Text => KVSerializer.Create(KVSerializationFormat.KeyValues2Text);

        static readonly Guid RootId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        static readonly Guid ChildId = Guid.Parse("22222222-2222-2222-2222-222222222222");

        static MemoryStream MinimalDocument(string header)
            => new DmxBinaryBuilder()
                .RawHeader(header)
                .Int(1).Str("DmElement") // string table
                .Int(1) // element count
                .Int(0).Int(0).Id(RootId)
                .Int(0) // attribute count
                .ToStream();

        [TestCase("<!-- dmx encoding binary 5 format dmx 1 -->")]
        [TestCase("<!-- dmx\tencoding   binary  5   format  dmx  1 -->")]
        [TestCase("<!-- DMX ENCODING binary 5 FORMAT dmx 1 -->")]
        public void ParsesHeaderVariants(string header)
        {
            using var stream = MinimalDocument(header);
            var doc = KV2Binary.Deserialize(stream);

            Assert.Multiple(() =>
            {
                Assert.That(doc.Header!.Encoding.Name, Is.EqualTo("binary"));
                Assert.That(doc.Header.Encoding.Version, Is.EqualTo(5));
                Assert.That(doc.Header.Format.Name, Is.EqualTo("dmx"));
                Assert.That(doc.Header.Format.Version, Is.EqualTo(1));
            });
        }

        /// <summary>
        /// The legacy header form is encoding version 0, which has the same layout as version 1.
        /// </summary>
        [Test]
        public void ParsesLegacyHeader()
        {
            using var stream = new DmxBinaryBuilder()
                .RawHeader("<!-- DMXVersion binary_v2 -->")
                .Int(1)
                .Str("DmElement").Str("root").Id(RootId)
                .Int(0)
                .ToStream();

            var doc = KV2Binary.Deserialize(stream);

            Assert.Multiple(() =>
            {
                Assert.That(doc.Header!.Encoding.Name, Is.EqualTo("binary"));
                Assert.That(doc.Header.Encoding.Version, Is.Zero);
                Assert.That(doc.Header.Format.Version, Is.EqualTo(2));
                Assert.That(((KV2Element)doc.Root).ClassName, Is.EqualTo("DmElement"));
            });
        }

        /// <summary>A legacy document is written back out with a modern header.</summary>
        [Test]
        public void WritesLegacyDocumentAsVersionOne()
        {
            using var stream = new DmxBinaryBuilder()
                .RawHeader("<!-- DMXVersion binary_v2 -->")
                .Int(1)
                .Str("DmElement").Str("root").Id(RootId)
                .Int(0)
                .ToStream();

            var doc = KV2Binary.Deserialize(stream);

            using var output = new MemoryStream();
            KV2Binary.Serialize(output, doc);
            output.Position = 0;

            Assert.That(KV2Binary.Deserialize(output).Header!.Encoding.Version, Is.EqualTo(1));
        }

        [TestCase("<!-- dmx encoding binary 5 -->")]
        [TestCase("<!-- dmx encoding binary five format dmx 1 -->")]
        [TestCase("dmx encoding binary 5 format dmx 1")]
        public void RejectsMalformedHeader(string header)
        {
            using var stream = MinimalDocument(header);

            Assert.That(() => KV2Binary.Deserialize(stream), Throws.InstanceOf<KeyValueException>());
        }

        [Test]
        public void RejectsHeaderWithoutTerminator()
        {
            using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(new string('x', 400)));

            Assert.That(() => KV2Binary.Deserialize(stream), Throws.InstanceOf<KeyValueException>());
        }

        [Test]
        public void RejectsOverlongEncodingNameOnWrite()
        {
            var root = new KV2Element("DmElement", "root", RootId);
            var header = new KVHeader
            {
                Encoding = new KeyValues3.KV3ID("binary", Version: 5),
                Format = new KeyValues3.KV3ID(new string('f', 64), Version: 1),
            };

            using var stream = new MemoryStream();

            Assert.That(
                () => KV2Binary.Serialize(stream, new KVDocument(header, null, root)),
                Throws.InstanceOf<KeyValueException>());
        }

        [Test]
        public void SequentialIdsEncodingNumbersElementsInOrder()
        {
            var child = new KV2Element("DmeChild", "child", ChildId);
            var root = new KV2Element("DmElement", "root", RootId);
            root.Add("child", child);

            var header = new KVHeader
            {
                Encoding = new KeyValues3.KV3ID("binary_seqids", Version: 9),
                Format = new KeyValues3.KV3ID("dmx", Version: 1),
            };

            using var stream = new MemoryStream();
            KV2Binary.Serialize(stream, new KVDocument(header, null, root));
            stream.Position = 0;

            var doc = KV2Binary.Deserialize(stream);
            var root2 = (KV2Element)doc.Root;

            Assert.Multiple(() =>
            {
                Assert.That(doc.Header!.Encoding.Name, Is.EqualTo("binary_seqids"));
                Assert.That(root2.ElementId, Is.EqualTo(new Guid(1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0)));
                Assert.That(((KV2Element)root2["child"]).ElementId, Is.EqualTo(new Guid(2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0)));
            });
        }

        [Test]
        public void SequentialIdsProduceIdenticalBytesOnEveryWrite()
        {
            var root = new KV2Element("DmElement", "root", Guid.NewGuid());
            root.Add("child", new KV2Element("DmeChild", "child", Guid.NewGuid()));

            var header = new KVHeader
            {
                Encoding = new KeyValues3.KV3ID("binary_seqids", Version: 9),
                Format = new KeyValues3.KV3ID("dmx", Version: 1),
            };

            var doc = new KVDocument(header, null, root);

            using var first = new MemoryStream();
            using var second = new MemoryStream();
            KV2Binary.Serialize(first, doc);
            KV2Binary.Serialize(second, doc);

            Assert.That(second.ToArray(), Is.EqualTo(first.ToArray()));
        }

        [Test]
        public void SelectsVersionNineForSourceTwoTypes()
        {
            var root = new KV2Element("DmElement", "root", RootId);
            root.Add("handle", new KVObject(1UL));
            root.Add("small", KVObject.Byte(2));

            using var stream = new MemoryStream();
            KV2Binary.Serialize(stream, new KVDocument(null, null, root));
            stream.Position = 0;

            var doc = KV2Binary.Deserialize(stream);

            Assert.Multiple(() =>
            {
                Assert.That(doc.Header!.Encoding.Version, Is.EqualTo(9));
                Assert.That((ulong)doc.Root["handle"], Is.EqualTo(1UL));
                Assert.That((byte)doc.Root["small"], Is.EqualTo(2));
            });
        }

        /// <summary>
        /// Valve reads string table indices as signed shorts before version 5, so a table that
        /// does not fit has to be reported rather than silently wrapped.
        /// </summary>
        [Test]
        public void RejectsAStringTableThatDoesNotFitInAShort()
        {
            var root = new KV2Element("DmElement", "root", RootId);

            for (var i = 0; i < 40000; i++)
            {
                root.Add(i.ToString(CultureInfo.InvariantCulture), new KVObject(i));
            }

            var header = new KVHeader
            {
                Encoding = new KeyValues3.KV3ID("binary", Version: 4),
                Format = new KeyValues3.KV3ID("dmx", Version: 1),
            };

            using var stream = new MemoryStream();

            Assert.That(
                () => KV2Binary.Serialize(stream, new KVDocument(header, null, root)),
                Throws.InstanceOf<KeyValueException>());
        }

        [Test]
        public void DeserializeReturnsAKeyValues2Document()
        {
            using var stream = TestDataHelper.OpenResource("TextKV2.basic.dmx");
            var doc = KV2Text.Deserialize(stream);

            Assert.Multiple(() =>
            {
                Assert.That(doc, Is.InstanceOf<KV2Document>());
                Assert.That(doc.Root, Is.InstanceOf<KV2Element>());
            });
        }

        /// <summary>
        /// Valve keeps the element it saw first when two share an id, and points every reference
        /// at it.
        /// </summary>
        [Test]
        public void DuplicateElementIdsKeepTheFirstElement()
        {
            const string text = """
                <!-- dmx encoding keyvalues2 1 format dmx 1 -->
                "DmElement"
                {
                	"id" "elementid" "11111111-1111-1111-1111-111111111111"
                	"name" "string" "root"
                	"first" "DmeChild"
                	{
                		"id" "elementid" "22222222-2222-2222-2222-222222222222"
                		"name" "string" "original"
                	}
                	"second" "DmeChild"
                	{
                		"id" "elementid" "22222222-2222-2222-2222-222222222222"
                		"name" "string" "duplicate"
                	}
                }
                """;

            using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(text));
            var root = KV2Text.Deserialize(stream).Root;

            Assert.Multiple(() =>
            {
                Assert.That(((KV2Element)root["first"]).Name, Is.EqualTo("original"));
                Assert.That(root["second"], Is.SameAs(root["first"]));
            });
        }

        [Test]
        public void RejectsADocumentWithoutElements()
        {
            using var stream = new DmxBinaryBuilder()
                .Header("binary", 5)
                .Int(0)
                .Int(0)
                .ToStream();

            Assert.That(() => KV2Binary.Deserialize(stream), Throws.InstanceOf<KeyValueException>());
        }
    }
}
