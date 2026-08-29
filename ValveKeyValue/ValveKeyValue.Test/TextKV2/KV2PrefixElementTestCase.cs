namespace ValveKeyValue.Test.TextKV2
{
    /// <summary>
    /// The prefix attribute container that precedes the root element in binary v9 and
    /// keyvalues2 v4 documents.
    /// </summary>
    class KV2PrefixElementTestCase
    {
        static KVSerializer KV2Text => KVSerializer.Create(KVSerializationFormat.KeyValues2Text);
        static KVSerializer KV2Binary => KVSerializer.Create(KVSerializationFormat.KeyValues2Binary);

        static readonly string[] ExpectedAssetReferences =
        [
            "models/de_warden/rocks/large_rock_01.vmdl",
            "materials/de_warden/rocks/rock_01/rock_01_dirt_02_blend.vmat",
        ];

        static readonly byte[] ExpectedThumbnail = [0xFF, 0xD8, 0xFF, 0xE0, 0x00, 0x10];

        static readonly KVSerializationFormat[] Kv2Formats =
        [
            KVSerializationFormat.KeyValues2Text,
            KVSerializationFormat.KeyValues2Binary,
        ];

        static KV2Document ReadResource(KVSerializer serializer, string name)
        {
            using var stream = TestDataHelper.OpenResource(name);
            return (KV2Document)serializer.Deserialize(stream);
        }

        static KV2Document RoundTrip(KVSerializer serializer, KVDocument doc)
        {
            using var stream = new MemoryStream();
            serializer.Serialize(stream, doc);
            stream.Position = 0;

            return (KV2Document)serializer.Deserialize(stream);
        }

        [Test]
        public void TextRootIsNotThePrefixElement()
        {
            var doc = ReadResource(KV2Text, "TextKV2.cs2_map.dmx");
            var root = (KV2Element)doc.Root;

            Assert.That(doc.PrefixElement, Is.Not.Null);

            Assert.Multiple(() =>
            {
                Assert.That(root.ClassName, Is.EqualTo("CMapRootElement"));
                Assert.That(doc.PrefixElement!.ClassName, Is.EqualTo(KV2Element.PrefixElementClassName));
                Assert.That(doc.PrefixElement["map_asset_references"].GetArray<string>(), Is.EqualTo(ExpectedAssetReferences));
                Assert.That(root.ContainsKey("map_asset_references"), Is.False);
            });
        }

        [Test]
        public void TextPrefixElementSurvivesRoundTrip()
        {
            var doc = RoundTrip(KV2Text, ReadResource(KV2Text, "TextKV2.cs2_map.dmx"));

            Assert.That(doc.PrefixElement, Is.Not.Null);

            Assert.Multiple(() =>
            {
                Assert.That(((KV2Element)doc.Root).ClassName, Is.EqualTo("CMapRootElement"));
                Assert.That(doc.PrefixElement!["map_asset_references"].GetArray<string>(), Is.EqualTo(ExpectedAssetReferences));
            });
        }

        [Test]
        public void TextPrefixElementIsWrittenFirst()
        {
            var doc = ReadResource(KV2Text, "TextKV2.cs2_map.dmx");

            using var stream = new MemoryStream();
            KV2Text.Serialize(stream, doc);

            var text = System.Text.Encoding.UTF8.GetString(stream.ToArray());
            var lines = text.Split('\n');

            Assert.Multiple(() =>
            {
                Assert.That(lines[0], Does.StartWith("<!-- dmx encoding keyvalues2 4 format vmap 35"));
                Assert.That(lines[1], Is.EqualTo("\"$prefix_element$\""));
            });
        }

        [Test]
        public void BinaryPrefixElementIsSeparateFromRoot()
        {
            var doc = ReadResource(KV2Binary, "Binary.overboss_run.dmx");

            Assert.That(doc.PrefixElement, Is.Not.Null);

            Assert.Multiple(() =>
            {
                Assert.That(doc.PrefixElement!.ClassName, Is.EqualTo(KV2Element.PrefixElementClassName));
                Assert.That(doc.PrefixElement.ContainsKey("anim_frameRate"), Is.True);
                Assert.That(doc.PrefixElement.ContainsKey("anim_duration"), Is.True);
                Assert.That(doc.Root.ContainsKey("anim_frameRate"), Is.False);
                Assert.That(doc.Root.ContainsKey("anim_duration"), Is.False);
            });
        }

        [Test]
        public void BinaryPrefixElementSurvivesRoundTrip()
        {
            var original = ReadResource(KV2Binary, "Binary.overboss_run.dmx");
            var doc = RoundTrip(KV2Binary, original);

            Assert.That(doc.PrefixElement, Is.Not.Null);

            Assert.Multiple(() =>
            {
                Assert.That(doc.Header!.Encoding.Version, Is.EqualTo(9));
                Assert.That(doc.PrefixElement!.Count, Is.EqualTo(original.PrefixElement!.Count));
                Assert.That((float)doc.PrefixElement["anim_frameRate"], Is.EqualTo((float)original.PrefixElement["anim_frameRate"]));
            });
        }

        /// <summary>
        /// The container is written before the string table, so everything in it is inline. A
        /// scalar string and a blob are the two values that would otherwise go through the table.
        /// </summary>
        [Test]
        public void BinaryPrefixCarriesInlineStringsAndBlobs([ValueSource(nameof(Kv2Formats))] KVSerializationFormat format)
        {
            var prefix = new KV2Element(KV2Element.PrefixElementClassName, string.Empty, Guid.NewGuid());
            prefix.Add("map_asset_references", KVObject.TypedArray(new List<string>(ExpectedAssetReferences)));
            prefix.Add("asset_preview_thumbnail_format", new KVObject("jpg"));
            prefix.Add("asset_preview_thumbnail", KVObject.Blob(ExpectedThumbnail));

            var root = new KV2Element("CMapRootElement", "root", Guid.NewGuid());
            root.Add("hello", new KVObject("world"));

            var doc = RoundTrip(KVSerializer.Create(format), new KV2Document(null, null, root, prefix));

            Assert.That(doc.PrefixElement, Is.Not.Null);

            Assert.Multiple(() =>
            {
                Assert.That(doc.PrefixElement!["map_asset_references"].GetArray<string>(), Is.EqualTo(ExpectedAssetReferences));
                Assert.That((string)doc.PrefixElement["asset_preview_thumbnail_format"], Is.EqualTo("jpg"));
                Assert.That(doc.PrefixElement["asset_preview_thumbnail"].AsBlob(), Is.EqualTo(ExpectedThumbnail));
                Assert.That((string)doc.Root["hello"], Is.EqualTo("world"));
            });
        }

        /// <summary>
        /// A prefix element can only be encoded by binary version 9 and keyvalues2 version 4, so
        /// writing one bumps the version even when the source document used an older one.
        /// </summary>
        [Test]
        public void WritingAPrefixElementSelectsAVersionThatSupportsIt()
        {
            var prefix = new KV2Element(KV2Element.PrefixElementClassName, string.Empty, Guid.Empty);
            prefix.Add("frameRate", new KVObject(30f));

            var root = new KV2Element("DmElement", "root", Guid.NewGuid());
            var header = new KVHeader
            {
                Encoding = new KeyValues3.KV3ID("binary", Version: 5),
                Format = new KeyValues3.KV3ID("model", Version: 22),
            };

            var doc = RoundTrip(KV2Binary, new KV2Document(header, null, root, prefix));

            Assert.That(doc.PrefixElement, Is.Not.Null);

            Assert.Multiple(() =>
            {
                Assert.That(doc.Header!.Encoding.Version, Is.EqualTo(9));
                Assert.That((float)doc.PrefixElement!["frameRate"], Is.EqualTo(30f));
            });
        }

        [Test]
        public void PrefixElementCrossesBetweenEncodings()
        {
            var text = ReadResource(KV2Text, "TextKV2.cs2_map.dmx");
            var binary = RoundTrip(KV2Binary, text);
            var backToText = RoundTrip(KV2Text, binary);

            Assert.That(backToText.PrefixElement, Is.Not.Null);

            Assert.Multiple(() =>
            {
                Assert.That(binary.Header!.Encoding.Name, Is.EqualTo("binary"));
                Assert.That(binary.Header.Encoding.Version, Is.EqualTo(9));
                Assert.That(backToText.Header!.Encoding.Name, Is.EqualTo("keyvalues2"));
                Assert.That(backToText.Header.Format.Name, Is.EqualTo("vmap"));
                Assert.That(backToText.PrefixElement!["map_asset_references"].GetArray<string>(), Is.EqualTo(ExpectedAssetReferences));
            });
        }
    }
}
