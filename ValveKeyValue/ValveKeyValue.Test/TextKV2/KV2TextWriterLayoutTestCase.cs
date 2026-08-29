using System.Numerics;

namespace ValveKeyValue.Test.TextKV2
{
    /// <summary>
    /// The shape of the text the writer produces, which follows what Valve's own serializer emits.
    /// </summary>
    class KV2TextWriterLayoutTestCase
    {
        static KVSerializer KV2Text => KVSerializer.Create(KVSerializationFormat.KeyValues2Text);
        static KVSerializer KV2Binary => KVSerializer.Create(KVSerializationFormat.KeyValues2Binary);

        static readonly Guid RootId = Guid.Parse("00000000-0000-0000-0000-00000000000a");
        static readonly Guid SharedId = Guid.Parse("00000000-0000-0000-0000-00000000000b");
        static readonly Guid ItemId = Guid.Parse("00000000-0000-0000-0000-00000000000c");

        static string Serialize(KVDocument doc, KVSerializer? serializer = null)
        {
            using var stream = new MemoryStream();
            (serializer ?? KV2Text).Serialize(stream, doc);

            return System.Text.Encoding.UTF8.GetString(stream.ToArray());
        }

        static string SerializeResource(string name, KVSerializer? serializer = null)
        {
            using var stream = TestDataHelper.OpenResource(name);

            return Serialize(KV2Text.Deserialize(stream), serializer);
        }

        [Test]
        public void WritesMultiReferencedElementsAsTopLevelBlocks()
        {
            var text = SerializeResource("TextKV2.shared_refs.dmx");
            var lines = text.Split('\n');

            // The shared element is referenced three times, so it becomes its own block after the
            // root rather than being inlined at the first usage site.
            Assert.Multiple(() =>
            {
                Assert.That(lines, Does.Contain("\"DmeShared\""));
                Assert.That(text, Does.Contain("\"sharedElement\" \"element\" \"00000000-0000-0000-0000-000000000002\""));
                Assert.That(text, Does.Contain("\"refToShared\" \"element\" \"00000000-0000-0000-0000-000000000002\""));
            });
        }

        [Test]
        public void SeparatesTopLevelBlocksWithABlankLine()
        {
            var text = SerializeResource("TextKV2.shared_refs.dmx");

            Assert.That(text, Does.Contain("}\n\n"));
            Assert.That(text, Does.EndWith("}\n\n"));
        }

        [Test]
        public void LeavesASpaceAfterATypeTokenWhoseValueStartsOnTheNextLine()
        {
            var text = SerializeResource("TextKV2.basic.dmx");

            Assert.Multiple(() =>
            {
                Assert.That(text, Does.Contain("\"intArray\" \"int_array\" \n"));
                Assert.That(text, Does.Contain("\"elements\" \"element_array\" \n"));
            });
        }

        [Test]
        public void PutsCommasAfterElementArrayItemsAndNotAfterTheLastOne()
        {
            var root = new KV2Element("DmElement", "root", RootId);
            var item = new KV2Element("DmeItem", "item", ItemId);
            root.Add("items", KVObject.TypedArray(new List<KV2Element> { item, KV2Element.Null }));

            var text = Serialize(new KVDocument(null, null, root));

            Assert.Multiple(() =>
            {
                Assert.That(text, Does.Contain("\t\t},\n"));
                Assert.That(text, Does.Contain("\t\t\"element\" \"\"\n"));
                Assert.That(text, Does.Not.Contain("\n\t\t,"));
            });
        }

        [Test]
        public void WritesTimeAsSecondsWithFourDecimals()
        {
            var root = new KV2Element("DmElement", "root", RootId);
            root.Add("seconds", new KVObject(new DmxTime(15000)));
            root.Add("zero", new KVObject(new DmxTime(0)));
            root.Add("negative", new KVObject(new DmxTime(-1)));

            var text = Serialize(new KVDocument(null, null, root));

            Assert.Multiple(() =>
            {
                Assert.That(text, Does.Contain("\"seconds\" \"time\" \"1.5000\""));
                Assert.That(text, Does.Contain("\"zero\" \"time\" \"0.0000\""));
                Assert.That(text, Does.Contain("\"negative\" \"time\" \"-0.0001\""));
            });
        }

        [Test]
        public void WritesUInt64AsHex()
        {
            var root = new KV2Element("DmElement", "root", RootId);
            root.Add("handle", new KVObject(0xdf41645d6af564aUL));
            root.Add("zero", new KVObject(0UL));

            var text = Serialize(new KVDocument(null, null, root));

            Assert.Multiple(() =>
            {
                Assert.That(text, Does.Contain("\"handle\" \"uint64\" \"0xdf41645d6af564a\""));
                Assert.That(text, Does.Contain("\"zero\" \"uint64\" \"0x0\""));
            });
        }

        [Test]
        public void WritesFloatsWithoutExponentNotation()
        {
            var root = new KV2Element("DmElement", "root", RootId);
            root.Add("small", new KVObject(0.1f));
            root.Add("whole", new KVObject(1f));
            root.Add("half", new KVObject(0.5f));

            var text = Serialize(new KVDocument(null, null, root));

            Assert.Multiple(() =>
            {
                Assert.That(text, Does.Contain("\"small\" \"float\" \"0.1000000015\""));
                Assert.That(text, Does.Contain("\"whole\" \"float\" \"1\""));
                Assert.That(text, Does.Contain("\"half\" \"float\" \"0.5\""));
            });
        }

        /// <summary>
        /// The same values Datamodel.NET pins its float formatting to, so that both libraries can
        /// be checked against Valve's <c>%.10f</c> output rather than only against each other.
        /// </summary>
        [Test]
        public void WritesTheSameFloatSpellingAsTheReferenceSerializer()
        {
            var root = new KV2Element("DmElement", "root", RootId);
            root.Add("position", new KVObject(new Vector3(-270.11304f, -233.07538f, 562.09106f)));
            root.Add("whole", new KVObject(40f));
            root.Add("negative", new KVObject(-1f));

            var text = Serialize(new KVDocument(null, null, root));

            Assert.Multiple(() =>
            {
                Assert.That(text, Does.Contain("\"position\" \"vector3\" \"-270.1130371094 -233.075378418 562.0910644531\""));
                Assert.That(text, Does.Contain("\"whole\" \"float\" \"40\""));
                Assert.That(text, Does.Contain("\"negative\" \"float\" \"-1\""));
            });
        }

        /// <summary>
        /// A null reference is written as the "element" type with an empty id, and an empty array
        /// still gets its brackets on their own lines.
        /// </summary>
        [Test]
        public void WritesNullReferencesAndEmptyArrays()
        {
            var root = new KV2Element("DmElement", "root", RootId);
            root.Add("nothing", KV2Element.Null);
            root.Add("empty", KVObject.TypedArray(new List<int>()));

            var text = Serialize(new KVDocument(null, null, root));

            Assert.Multiple(() =>
            {
                Assert.That(text, Does.Contain("\t\"nothing\" \"element\" \"\"\n"));
                Assert.That(text, Does.Contain("\t\"empty\" \"int_array\" \n\t[\n\t]\n"));
            });
        }

        [Test]
        public void WritesMatrixAndBinaryAcrossSeveralLines()
        {
            var root = new KV2Element("DmElement", "root", RootId);
            root.Add("transform", new KVObject(Matrix4x4.Identity));
            root.Add("blob", KVObject.Blob([0xDE, 0xAD, 0xBE, 0xEF]));

            var text = Serialize(new KVDocument(null, null, root));

            Assert.Multiple(() =>
            {
                Assert.That(text, Does.Contain("\"transform\" \"matrix\" \n\t\"\n\t\t1 0 0 0\n"));
                Assert.That(text, Does.Contain("\"blob\" \"binary\" \n\t\"\n\t\tDEADBEEF\n\t\"\n"));
            });
        }

        [Test]
        public void SelectsVersionFourForSourceTwoTypes()
        {
            var root = new KV2Element("DmElement", "root", RootId);
            root.Add("handle", new KVObject(1UL));

            var text = Serialize(new KVDocument(null, null, root));

            Assert.That(text, Does.StartWith("<!-- dmx encoding keyvalues2 4 format dmx 1 -->"));
        }

        [Test]
        public void KeepsTheFlatEncodingNameAndWritesEveryElementAtTopLevel()
        {
            var shared = new KV2Element("DmeShared", "shared", SharedId);
            var root = new KV2Element("DmElement", "root", RootId);
            root.Add("only", shared);

            var header = new KVHeader
            {
                Encoding = new KeyValues3.KV3ID("keyvalues2_flat", Version: 1),
                Format = new KeyValues3.KV3ID("dmx", Version: 1),
            };

            var text = Serialize(new KVDocument(header, null, root));

            Assert.Multiple(() =>
            {
                Assert.That(text, Does.StartWith("<!-- dmx encoding keyvalues2_flat 1 format dmx 1 -->"));

                // Nothing is inlined, the reference is written by id and the element follows.
                Assert.That(text, Does.Contain($"\"only\" \"element\" \"{SharedId}\""));
                Assert.That(text.Split('\n'), Does.Contain("\"DmeShared\""));
            });
        }

        [Test]
        public void OmitsIdsOfInlinedElementsInNoIdsEncoding()
        {
            var child = new KV2Element("DmeChild", "child", ItemId);
            var root = new KV2Element("DmElement", "root", RootId);
            root.Add("child", child);

            var header = new KVHeader
            {
                Encoding = new KeyValues3.KV3ID("keyvalues2_noids", Version: 1),
                Format = new KeyValues3.KV3ID("dmx", Version: 1),
            };

            var text = Serialize(new KVDocument(header, null, root));

            Assert.Multiple(() =>
            {
                Assert.That(text, Does.StartWith("<!-- dmx encoding keyvalues2_noids 1 format dmx 1 -->"));
                Assert.That(text, Does.Contain($"\"id\" \"elementid\" \"{RootId}\""));
                Assert.That(text, Does.Not.Contain(ItemId.ToString()));
            });
        }

        /// <summary>
        /// An element without an id would otherwise be indistinguishable from a null reference,
        /// so the reader gives it a fresh one.
        /// </summary>
        [Test]
        public void NoIdsDocumentRoundTrips()
        {
            var child = new KV2Element("DmeChild", "child", ItemId);
            var root = new KV2Element("DmElement", "root", RootId);
            root.Add("child", child);

            var header = new KVHeader
            {
                Encoding = new KeyValues3.KV3ID("keyvalues2_noids", Version: 1),
                Format = new KeyValues3.KV3ID("dmx", Version: 1),
            };

            var text = Serialize(new KVDocument(header, null, root));

            using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(text));
            var child2 = (KV2Element)KV2Text.Deserialize(stream).Root["child"];

            Assert.Multiple(() =>
            {
                Assert.That(child2.ClassName, Is.EqualTo("DmeChild"));
                Assert.That(child2.Name, Is.EqualTo("child"));
                Assert.That(child2.ElementId, Is.Not.EqualTo(Guid.Empty));
            });
        }

        [Test]
        public void DoesNotCopyTheSourceEncodingNameAcrossFormats()
        {
            using var stream = TestDataHelper.OpenResource("TextKV2.taunt05.dmx");
            var doc = KV2Text.Deserialize(stream);

            using var binaryStream = new MemoryStream();
            KV2Binary.Serialize(binaryStream, doc);
            binaryStream.Position = 0;

            var binaryHeader = System.Text.Encoding.UTF8.GetString(binaryStream.ToArray(), 0, 60);
            var binaryDoc = KV2Binary.Deserialize(binaryStream);
            var text = Serialize(binaryDoc);

            Assert.Multiple(() =>
            {
                Assert.That(binaryHeader, Does.StartWith("<!-- dmx encoding binary 5 format model 1 -->"));
                Assert.That(text, Does.StartWith("<!-- dmx encoding keyvalues2 1 format model 1 -->"));
            });
        }
    }
}
