using System.Text;

namespace ValveKeyValue.Test.BinaryKV2
{
    /// <summary>
    /// Files written by Hammer store the prefix attributes twice in binary version 9: once in the
    /// container before the string table, and once more as an element right after the root that
    /// nothing references. Only that second copy carries the container's identifier.
    /// </summary>
    class KV2PrefixDuplicateTestCase
    {
        static KVSerializer KV2Binary => KVSerializer.Create(KVSerializationFormat.KeyValues2Binary);

        const string DuplicateClassName = "DmElement";

        static readonly Guid PrefixId = Guid.Parse("44444444-4444-4444-4444-444444444444");
        static readonly Guid RootId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        static readonly Guid ChildId = Guid.Parse("22222222-2222-2222-2222-222222222222");
        static readonly Guid GrandChildId = Guid.Parse("33333333-3333-3333-3333-333333333333");

        static readonly string[] AssetReferences = ["a.vmdl", "b.vmat"];
        static readonly string[] SingleAssetReference = ["a.vmdl"];

        const byte TypeElement = 1;
        const byte TypeInt = 2;
        const byte TypeFloat = 3;
        const byte TypeString = 5;
        const byte TypeTime = 7;
        const byte TypeStringArray = 37;

        /// <summary>A document with a prefix container and a two level element graph below the root.</summary>
        static KV2Document MakeDocument(Guid prefixId)
        {
            var grandChild = new KV2Element("DmElement", "grandChild", GrandChildId);
            grandChild.Add("value", new KVObject(7));

            var child = new KV2Element("DmElement", "child", ChildId);
            child.Add("grandChild", grandChild);

            var root = new KV2Element("CMapRootElement", "root", RootId);
            root.Add("child", child);

            var prefix = new KV2Element(KV2Element.PrefixElementClassName, string.Empty, prefixId);
            prefix.Add("map_asset_references", KVObject.TypedArray(new List<string>(AssetReferences)));

            return new KV2Document(null, null, root, prefix);
        }

        static byte[] Serialize(KVDocument doc)
        {
            using var stream = new MemoryStream();
            KV2Binary.Serialize(stream, doc);

            return stream.ToArray();
        }

        static KV2Document Deserialize(byte[] document)
        {
            using var stream = new MemoryStream(document, writable: false);

            return (KV2Document)KV2Binary.Deserialize(stream);
        }

        [Test]
        public void PrefixWithAnIdIsAlsoWrittenAsAnElement()
        {
            var index = ReadElementIndex(Serialize(MakeDocument(PrefixId)));

            Assert.That(index, Has.Count.EqualTo(4));

            Assert.Multiple(() =>
            {
                Assert.That(index[0].Id, Is.EqualTo(RootId));
                Assert.That(index[1].ClassName, Is.EqualTo(DuplicateClassName));
                Assert.That(index[1].Name, Is.Empty);
                Assert.That(index[1].Id, Is.EqualTo(PrefixId));
            });
        }

        /// <summary>
        /// The identifier is the only thing the copy adds, so a container without one is written
        /// as the container alone, the way Valve's other tools write it.
        /// </summary>
        [Test]
        public void PrefixWithoutAnIdIsWrittenOnlyOnce()
        {
            var index = ReadElementIndex(Serialize(MakeDocument(Guid.Empty)));

            Assert.That(index, Has.Count.EqualTo(3));

            Assert.Multiple(() =>
            {
                Assert.That(index[0].Id, Is.EqualTo(RootId));
                Assert.That(index[1].Id, Is.EqualTo(ChildId));
                Assert.That(index[2].Id, Is.EqualTo(GrandChildId));
            });
        }

        [Test]
        public void PrefixIdSurvivesARoundTrip()
        {
            var doc = Deserialize(Serialize(MakeDocument(PrefixId)));

            Assert.That(doc.PrefixElement, Is.Not.Null);

            Assert.Multiple(() =>
            {
                Assert.That(doc.PrefixElement!.ElementId, Is.EqualTo(PrefixId));
                Assert.That(doc.PrefixElement.ClassName, Is.EqualTo(KV2Element.PrefixElementClassName));
                Assert.That(doc.PrefixElement["map_asset_references"].GetArray<string>(), Is.EqualTo(AssetReferences));
            });
        }

        /// <summary>
        /// Element references are indices into the element list, so the extra entry moves every
        /// element below the root down one slot.
        /// </summary>
        [Test]
        public void ElementReferencesResolvePastTheDuplicate()
        {
            var doc = Deserialize(Serialize(MakeDocument(PrefixId)));

            var child = (KV2Element)doc.Root["child"];
            var grandChild = (KV2Element)child["grandChild"];

            Assert.Multiple(() =>
            {
                Assert.That(child.ElementId, Is.EqualTo(ChildId));
                Assert.That(grandChild.ElementId, Is.EqualTo(GrandChildId));
                Assert.That((int)grandChild["value"], Is.EqualTo(7));
            });
        }

        [Test]
        public void ReadingAHammerShapedDocumentAdoptsTheDuplicatesId()
        {
            var doc = (KV2Document)KV2Binary.Deserialize(HammerShapedDocument());
            var root = (KV2Element)doc.Root;

            Assert.That(doc.PrefixElement, Is.Not.Null);

            Assert.Multiple(() =>
            {
                Assert.That(doc.PrefixElement!.ElementId, Is.EqualTo(PrefixId));
                Assert.That(doc.PrefixElement["map_asset_references"].GetArray<string>(), Is.EqualTo(SingleAssetReference));

                // The root keeps its own identity and does not become the copy.
                Assert.That(root.ClassName, Is.EqualTo("CMapRootElement"));
                Assert.That(root.ElementId, Is.EqualTo(RootId));

                // The reference in the root is index 2, which is the child rather than the copy.
                Assert.That(((KV2Element)root["child"]).ElementId, Is.EqualTo(ChildId));
                Assert.That(root.ContainsKey("map_asset_references"), Is.False);
            });
        }

        /// <summary>
        /// A file that has no copy leaves the container without an identifier, and it must not
        /// grow one on the way out.
        /// </summary>
        [Test]
        public void DocumentWithoutTheDuplicateKeepsAnEmptyPrefixId()
        {
            using var resource = TestDataHelper.OpenResource("Binary.overboss_run.dmx");
            var original = (KV2Document)KV2Binary.Deserialize(resource);

            var index = ReadElementIndex(Serialize(original));

            Assert.That(original.PrefixElement, Is.Not.Null);

            Assert.Multiple(() =>
            {
                Assert.That(original.PrefixElement!.ElementId, Is.EqualTo(Guid.Empty));
                Assert.That(index[1].Id, Is.Not.EqualTo(Guid.Empty), "the container must not be written as an element");
            });
        }

        /// <summary>
        /// The text encoding always writes the container an id, so reading one back must not turn
        /// an absent id into a real one and start producing a copy the source never had.
        /// </summary>
        [Test]
        public void AnEmptyPrefixIdSurvivesADetourThroughText()
        {
            using var resource = TestDataHelper.OpenResource("Binary.overboss_run.dmx");
            var original = KV2Binary.Deserialize(resource);

            var text = KVSerializer.Create(KVSerializationFormat.KeyValues2Text);

            using var asText = new MemoryStream();
            text.Serialize(asText, original);
            asText.Position = 0;

            var reloaded = (KV2Document)text.Deserialize(asText);

            Assert.That(reloaded.PrefixElement, Is.Not.Null);

            Assert.Multiple(() =>
            {
                Assert.That(reloaded.PrefixElement!.ElementId, Is.EqualTo(Guid.Empty));
                Assert.That(ReadElementIndex(Serialize(reloaded))[1].Id, Is.Not.EqualTo(Guid.Empty));
            });
        }

        /// <summary>
        /// The container has no name member, so an attribute called "name" is an ordinary attribute
        /// there and has to be written into the copy rather than into its name slot.
        /// </summary>
        [Test]
        public void PrefixAttributeCalledNameStaysInTheDuplicate()
        {
            var doc = MakeDocument(PrefixId);
            doc.PrefixElement!.Add("name", new KVObject("importantValue"));

            var written = Serialize(doc);
            var index = ReadElementIndex(written);

            Assert.That(index[1].Name, Is.Empty);
            Assert.That((string)Deserialize(written).PrefixElement!["name"], Is.EqualTo("importantValue"));
        }

        /// <summary>
        /// A version 9 document laid out the way Hammer writes one: the prefix container, then the
        /// root, then the copy of the container, then the elements the root refers to.
        /// </summary>
        static MemoryStream HammerShapedDocument()
        {
            const int ClassRoot = 0;
            const int NameRoot = 1;
            const int ClassDmElement = 2;
            const int NameEmpty = 3;
            const int NameChild = 4;
            const int AttrAssetReferences = 5;

            return new DmxBinaryBuilder()
                .Header("binary", 9, "vmap", 40)
                .Int(1) // prefix containers
                .Int(1) // prefix attributes
                .Str("map_asset_references").U8(TypeStringArray).Int(1).Str("a.vmdl")
                .Int(6) // string table
                .Str("CMapRootElement").Str("root").Str("DmElement").Str(string.Empty).Str("child").Str("map_asset_references")
                .Int(3) // element index
                .Int(ClassRoot).Int(NameRoot).Id(RootId)
                .Int(ClassDmElement).Int(NameEmpty).Id(PrefixId)
                .Int(ClassDmElement).Int(NameChild).Id(ChildId)
                .Int(1).Int(NameChild).U8(TypeElement).Int(2) // root body, referring to the child
                .Int(1).Int(AttrAssetReferences).U8(TypeStringArray).Int(1).Str("a.vmdl") // the copy
                .Int(0) // child body
                .ToStream();
        }

        sealed record IndexEntry(string ClassName, string Name, Guid Id);

        /// <summary>
        /// Reads the element index out of a version 9 document, so that a test can assert on what
        /// was written rather than only on what reading it back produces.
        /// </summary>
        static List<IndexEntry> ReadElementIndex(byte[] document)
        {
            using var stream = new MemoryStream(document, writable: false);
            using var reader = new BinaryReader(stream, Encoding.UTF8);

            while (reader.ReadByte() != 0)
            {
                // The header runs up to its null terminator.
            }

            for (var containers = reader.ReadInt32(); containers > 0; containers--)
            {
                for (var attributes = reader.ReadInt32(); attributes > 0; attributes--)
                {
                    ReadInlineString(reader);
                    SkipPrefixValue(reader, reader.ReadByte());
                }
            }

            var strings = new string[reader.ReadInt32()];

            for (var i = 0; i < strings.Length; i++)
            {
                strings[i] = ReadInlineString(reader);
            }

            var entries = new List<IndexEntry>();

            for (var remaining = reader.ReadInt32(); remaining > 0; remaining--)
            {
                entries.Add(new IndexEntry(strings[reader.ReadInt32()], strings[reader.ReadInt32()], new Guid(reader.ReadBytes(16))));
            }

            return entries;
        }

        static string ReadInlineString(BinaryReader reader)
        {
            var bytes = new List<byte>();

            for (var b = reader.ReadByte(); b != 0; b = reader.ReadByte())
            {
                bytes.Add(b);
            }

            return Encoding.UTF8.GetString([.. bytes]);
        }

        /// <summary>Handles the attribute types the documents in this fixture put in a container.</summary>
        static void SkipPrefixValue(BinaryReader reader, byte type)
        {
            switch (type)
            {
                case TypeInt:
                case TypeFloat:
                case TypeTime:
                    reader.ReadInt32();
                    break;

                case TypeString:
                    ReadInlineString(reader);
                    break;

                case TypeStringArray:
                    for (var items = reader.ReadInt32(); items > 0; items--)
                    {
                        ReadInlineString(reader);
                    }

                    break;

                default:
                    throw new NotSupportedException($"Prefix attribute type {type} is not handled by this test.");
            }
        }
    }
}
