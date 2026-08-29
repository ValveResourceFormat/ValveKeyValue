namespace ValveKeyValue.Test.BinaryKV2
{
    /// <summary>
    /// Covers the binary layouts that differ between encoding versions, using synthetic documents
    /// because there are no real world fixtures for the older versions.
    /// </summary>
    class KV2BinaryLayoutTestCase
    {
        static KVSerializer KV2Binary => KVSerializer.Create(KVSerializationFormat.KeyValues2Binary);

        static readonly Guid RootId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        static readonly Guid ChildId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        static readonly Guid ExternalId = Guid.Parse("33333333-3333-3333-3333-333333333333");

        static readonly string[] ExpectedReferences = ["a.vmdl", "b.vmat"];
        static readonly int[] ExpectedNumbers = [1, 2, 3];

        // IDv1 (versions 1 and 2) and IDv2 (versions 3 to 5) share their layout except for slot 7.
        const byte TypeElementV1 = 1;
        const byte TypeStringV1 = 5;
        const byte TypeObjectIdV1 = 7;
        const byte TypeIntV1 = 2;

        // IDv3 (version 9).
        const byte TypeStringV3 = 5;
        const byte TypeStringArrayV3 = 37;

        [Test]
        public void ReadsVersion1WithoutStringTable()
        {
            // Version 1 has no string table at all, every string is inline.
            using var stream = new DmxBinaryBuilder()
                .Header("binary", 1)
                .Int(1) // element count
                .Str("DmElement").Str("root").Id(RootId)
                .Int(1) // attribute count
                .Str("greeting").U8(TypeStringV1).Str("hello")
                .ToStream();

            var root = (KV2Element)KV2Binary.Deserialize(stream).Root;

            Assert.Multiple(() =>
            {
                Assert.That(root.ClassName, Is.EqualTo("DmElement"));
                Assert.That(root.Name, Is.EqualTo("root"));
                Assert.That((string)root["greeting"], Is.EqualTo("hello"));
            });
        }

        [Test]
        public void ReadsVersion2WithInlineNamesAndStringValues()
        {
            // Version 2 and 3 only put class names and attribute names in the table. Element
            // names and string attribute values stay inline.
            using var stream = new DmxBinaryBuilder()
                .Header("binary", 2)
                .Short(2).Str("DmElement").Str("greeting") // string table
                .Int(1)
                .Short(0) // class name index
                .Str("root") // element name, inline
                .Id(RootId)
                .Int(1)
                .Short(1).U8(TypeStringV1).Str("hello") // string value, inline
                .ToStream();

            var root = (KV2Element)KV2Binary.Deserialize(stream).Root;

            Assert.Multiple(() =>
            {
                Assert.That(root.ClassName, Is.EqualTo("DmElement"));
                Assert.That(root.Name, Is.EqualTo("root"));
                Assert.That((string)root["greeting"], Is.EqualTo("hello"));
            });
        }

        [Test]
        public void SkipsObjectIdBeforeVersion3()
        {
            // Slot 7 is AT_OBJECTID before version 3, which is dropped on load.
            using var stream = new DmxBinaryBuilder()
                .Header("binary", 2)
                .Short(3).Str("DmElement").Str("legacyId").Str("after")
                .Int(1)
                .Short(0).Str("root").Id(RootId)
                .Int(2)
                .Short(1).U8(TypeObjectIdV1).Id(ExternalId)
                .Short(2).U8(TypeIntV1).Int(7)
                .ToStream();

            var root = (KV2Element)KV2Binary.Deserialize(stream).Root;

            Assert.Multiple(() =>
            {
                Assert.That(root.ContainsKey("legacyId"), Is.False);
                Assert.That((int)root["after"], Is.EqualTo(7));
            });
        }

        [Test]
        public void ReadsExternalElementReferenceAsStub()
        {
            // An index of -2 is followed by the target id as a null terminated string.
            using var stream = new DmxBinaryBuilder()
                .Header("binary", 2)
                .Short(3).Str("DmElement").Str("external").Str("after")
                .Int(1)
                .Short(0).Str("root").Id(RootId)
                .Int(2)
                .Short(1).U8(TypeElementV1).Int(-2).Str(ExternalId.ToString())
                .Short(2).U8(TypeIntV1).Int(42)
                .ToStream();

            var root = (KV2Element)KV2Binary.Deserialize(stream).Root;
            var external = (KV2Element)root["external"];

            Assert.Multiple(() =>
            {
                Assert.That(external.IsStub, Is.True);
                Assert.That(external.ElementId, Is.EqualTo(ExternalId));
                Assert.That(external.Count, Is.Zero);

                // The attribute after the reference only lines up if the id string was consumed.
                Assert.That((int)root["after"], Is.EqualTo(42));
            });
        }

        [Test]
        public void ExternalElementReferenceSurvivesRoundTrip()
        {
            using var stream = new DmxBinaryBuilder()
                .Header("binary", 5)
                .Int(2).Str("DmElement").Str("external")
                .Int(1)
                .Int(0).Int(0).Id(RootId)
                .Int(1)
                .Int(1).U8(TypeElementV1).Int(-2).Str(ExternalId.ToString())
                .ToStream();

            var doc = KV2Binary.Deserialize(stream);

            using var output = new MemoryStream();
            KV2Binary.Serialize(output, doc);
            output.Position = 0;

            var external = (KV2Element)KV2Binary.Deserialize(output).Root["external"];

            Assert.Multiple(() =>
            {
                Assert.That(external.IsStub, Is.True);
                Assert.That(external.ElementId, Is.EqualTo(ExternalId));
            });
        }

        [Test]
        public void ReadsVersion9PrefixAttributesWithInlineStrings()
        {
            // Prefix attributes come before the string table, so their strings are always inline.
            using var stream = new DmxBinaryBuilder()
                .Header("binary", 9)
                .Int(1) // one prefix container
                .Int(2) // two attributes
                .Str("thumbnail_format").U8(TypeStringV3).Str("jpg")
                .Str("references").U8(TypeStringArrayV3).Int(2).Str("a.vmdl").Str("b.vmat")
                .Int(1).Str("DmElement") // string table
                .Int(1)
                .Int(0).Int(0).Id(RootId)
                .Int(0)
                .ToStream();

            var doc = (KV2Document)KV2Binary.Deserialize(stream);

            Assert.That(doc.PrefixElement, Is.Not.Null);

            Assert.Multiple(() =>
            {
                Assert.That(doc.PrefixElement!.ClassName, Is.EqualTo(KV2Element.PrefixElementClassName));
                Assert.That((string)doc.PrefixElement["thumbnail_format"], Is.EqualTo("jpg"));
                Assert.That(doc.PrefixElement["references"].GetArray<string>(), Is.EqualTo(ExpectedReferences));

                // Prefix attributes are never merged into the root.
                Assert.That(doc.Root.ContainsKey("thumbnail_format"), Is.False);
                Assert.That(((KV2Element)doc.Root).ElementId, Is.EqualTo(RootId));
            });
        }

        [Test]
        public void MergesNameAttributeIntoElementName()
        {
            using var stream = new DmxBinaryBuilder()
                .Header("binary", 5)
                .Int(2).Str("DmElement").Str("name")
                .Int(1)
                .Int(0).Int(1).Id(RootId) // element name is the empty... index 1 is "name"
                .Int(1)
                .Int(1).U8(TypeStringV3).Int(0) // "name" = "DmElement" (table index 0)
                .ToStream();

            var root = (KV2Element)KV2Binary.Deserialize(stream).Root;

            Assert.Multiple(() =>
            {
                Assert.That(root.ContainsKey("name"), Is.False);
                Assert.That(root.Name, Is.EqualTo("DmElement"));
            });
        }

        [TestCase(6)]
        [TestCase(7)]
        [TestCase(8)]
        [TestCase(10)]
        public void RejectsUnknownEncodingVersion(int version)
        {
            using var stream = new DmxBinaryBuilder().Header("binary", version).Int(0).ToStream();

            Assert.That(() => KV2Binary.Deserialize(stream), Throws.InstanceOf<KeyValueException>());
        }

        [Test]
        public void RejectsNegativeElementCount()
        {
            using var stream = new DmxBinaryBuilder()
                .Header("binary", 5)
                .Int(0) // string table
                .Int(-3) // element count
                .ToStream();

            Assert.That(() => KV2Binary.Deserialize(stream), Throws.InstanceOf<KeyValueException>());
        }

        [Test]
        public void RejectsNegativeArrayCount()
        {
            using var stream = new DmxBinaryBuilder()
                .Header("binary", 5)
                .Int(2).Str("DmElement").Str("values")
                .Int(1)
                .Int(0).Int(0).Id(RootId)
                .Int(1)
                .Int(1).U8(34).Int(-1) // int_array with a negative count
                .ToStream();

            Assert.That(() => KV2Binary.Deserialize(stream), Throws.InstanceOf<KeyValueException>());
        }

        [Test]
        public void RejectsTruncatedStream()
        {
            using var stream = new DmxBinaryBuilder()
                .Header("binary", 5)
                .Int(2).Str("DmElement").Str("values")
                .Int(4) // says four elements, but none follow
                .ToStream();

            Assert.That(() => KV2Binary.Deserialize(stream), Throws.InstanceOf<KeyValueException>());
        }

        [Test]
        public void RejectsStringIndexOutOfRange()
        {
            using var stream = new DmxBinaryBuilder()
                .Header("binary", 5)
                .Int(1).Str("DmElement")
                .Int(1)
                .Int(9).Int(0).Id(RootId)
                .ToStream();

            Assert.That(() => KV2Binary.Deserialize(stream), Throws.InstanceOf<KeyValueException>());
        }

        [Test]
        public void WritesAndReadsBackEveryVersion([Values(1, 2, 3, 4, 5, 9)] int version)
        {
            var child = new KV2Element("DmeChild", "child", ChildId);
            child.Add("value", new KVObject("nested"));

            var root = new KV2Element("DmElement", "root", RootId);
            root.Add("greeting", new KVObject("hello"));
            root.Add("child", child);
            root.Add("numbers", KVObject.TypedArray(new List<int> { 1, 2, 3 }));

            var header = new KVHeader
            {
                Encoding = new KeyValues3.KV3ID("binary", Version: version),
                Format = new KeyValues3.KV3ID("dmx", Version: 1),
            };

            using var stream = new MemoryStream();
            KV2Binary.Serialize(stream, new KVDocument(header, null, root));
            stream.Position = 0;

            var doc = KV2Binary.Deserialize(stream);
            var root2 = (KV2Element)doc.Root;

            Assert.Multiple(() =>
            {
                Assert.That(doc.Header!.Encoding.Name, Is.EqualTo("binary"));
                Assert.That(doc.Header.Encoding.Version, Is.EqualTo(version));
                Assert.That(root2.Name, Is.EqualTo("root"));
                Assert.That((string)root2["greeting"], Is.EqualTo("hello"));
                Assert.That(((KV2Element)root2["child"]).Name, Is.EqualTo("child"));
                Assert.That((string)((KV2Element)root2["child"])["value"], Is.EqualTo("nested"));
                Assert.That(root2["numbers"].GetArray<int>(), Is.EqualTo(ExpectedNumbers));
            });
        }

        /// <summary>
        /// Versions 2 and 3 put class names and attribute names in the string table, but keep
        /// element names and string values inline.
        /// </summary>
        [Test]
        public void WritesVersion2ByteForByte()
        {
            var root = new KV2Element("DmElement", "root", RootId);
            root.Add("greeting", new KVObject("hello"));

            var header = new KVHeader
            {
                Encoding = new KeyValues3.KV3ID("binary", Version: 2),
                Format = new KeyValues3.KV3ID("dmx", Version: 1),
            };

            using var stream = new MemoryStream();
            KV2Binary.Serialize(stream, new KVDocument(header, null, root));

            var expected = new DmxBinaryBuilder()
                .Header("binary", 2)
                .Short(2).Str("DmElement").Str("greeting")
                .Int(1)
                .Short(0).Str("root").Id(RootId)
                .Int(1)
                .Short(1).U8(TypeStringV1).Str("hello")
                .ToArray();

            Assert.That(stream.ToArray(), Is.EqualTo(expected));
        }

        /// <summary>
        /// Version 3 has the same layout as version 2, but slot 7 is a time rather than an
        /// object id.
        /// </summary>
        [Test]
        public void WritesVersion3ByteForByte()
        {
            var root = new KV2Element("DmElement", "root", RootId);
            root.Add("duration", new KVObject(new DmxTime(15000)));

            var header = new KVHeader
            {
                Encoding = new KeyValues3.KV3ID("binary", Version: 3),
                Format = new KeyValues3.KV3ID("dmx", Version: 1),
            };

            using var stream = new MemoryStream();
            KV2Binary.Serialize(stream, new KVDocument(header, null, root));

            var expected = new DmxBinaryBuilder()
                .Header("binary", 3)
                .Short(2).Str("DmElement").Str("duration")
                .Int(1)
                .Short(0).Str("root").Id(RootId)
                .Int(1)
                .Short(1).U8(7).Int(15000)
                .ToArray();

            Assert.That(stream.ToArray(), Is.EqualTo(expected));
        }

        [Test]
        public void PreservesAttributeOrderAcrossRoundTrip()
        {
            using var stream = TestDataHelper.OpenResource("Binary.overboss_run.dmx");
            var doc = KV2Binary.Deserialize(stream);

            using var output = new MemoryStream();
            KV2Binary.Serialize(output, doc);
            output.Position = 0;

            var doc2 = KV2Binary.Deserialize(output);

            Assert.That(KeysOf(doc2.Root), Is.EqualTo(KeysOf(doc.Root)));
        }

        static List<string> KeysOf(KVObject element)
        {
            var keys = new List<string>();

            foreach (var key in element.Keys)
            {
                keys.Add(key);
            }

            return keys;
        }
    }
}
