namespace ValveKeyValue.Test.BinaryKV2
{
    class KV2BinaryReaderTestCase
    {
        static KVSerializer KV2Binary => KVSerializer.Create(KVSerializationFormat.KeyValues2Binary);
        static KVSerializer KV2Text => KVSerializer.Create(KVSerializationFormat.KeyValues2Text);
        static readonly int[] ExpectedIntArray = [1, 2, 3];
        static readonly string[] ExpectedStringArray = ["alpha", "beta", "gamma"];

        #region v4 binary tests

        [Test]
        public void DeserializesBinary4Header()
        {
            using var stream = TestDataHelper.OpenResource("Binary.binary4.dmx");
            var doc = KV2Binary.Deserialize(stream);

            Assert.Multiple(() =>
            {
                Assert.That(doc.Header, Is.Not.Null);
                Assert.That(doc.Header.Encoding.Name, Is.EqualTo("binary"));
                Assert.That(doc.Header.Encoding.Version, Is.EqualTo(4));
                Assert.That(doc.Header.Format.Name, Is.EqualTo("model"));
                Assert.That(doc.Header.Format.Version, Is.EqualTo(15));
            });
        }

        [Test]
        public void DeserializesBinary4RootElement()
        {
            using var stream = TestDataHelper.OpenResource("Binary.binary4.dmx");
            var doc = KV2Binary.Deserialize(stream);

            Assert.That(doc.Root, Is.InstanceOf<KV2Element>());

            var root = (KV2Element)doc.Root;

            Assert.Multiple(() =>
            {
                Assert.That(root.ClassName, Is.EqualTo("DmElement"));
                Assert.That(root.Name, Is.EqualTo("root"));
                Assert.That(root.ElementId, Is.Not.EqualTo(System.Guid.Empty));
            });
        }

        [Test]
        public void DeserializesBinary4HasChildren()
        {
            using var stream = TestDataHelper.OpenResource("Binary.binary4.dmx");
            var doc = KV2Binary.Deserialize(stream);
            var root = doc.Root;

            // Root element should have child attributes (4 element references)
            Assert.That(root.Count, Is.GreaterThan(0));
        }

        [Test]
        public void DeserializesBinary4ElementReferences()
        {
            using var stream = TestDataHelper.OpenResource("Binary.binary4.dmx");
            var doc = KV2Binary.Deserialize(stream);
            var root = doc.Root;

            // Root has animationList, skeleton, makefile, vsDmxIO_exportTags
            var animationList = root["animationList"];
            Assert.That(animationList, Is.InstanceOf<KV2Element>());
            Assert.That(((KV2Element)animationList).ClassName, Is.EqualTo("DmeAnimationList"));

            var skeleton = root["skeleton"];
            Assert.That(skeleton, Is.InstanceOf<KV2Element>());
            Assert.That(((KV2Element)skeleton).ClassName, Is.EqualTo("DmeModel"));
        }

        #endregion

        #region v5 binary tests

        [Test]
        public void DeserializesBinary5Header()
        {
            using var stream = TestDataHelper.OpenResource("Binary.taunt05_b5.dmx");
            var doc = KV2Binary.Deserialize(stream);

            Assert.Multiple(() =>
            {
                Assert.That(doc.Header, Is.Not.Null);
                Assert.That(doc.Header.Encoding.Name, Is.EqualTo("binary"));
                Assert.That(doc.Header.Encoding.Version, Is.EqualTo(5));
                Assert.That(doc.Header.Format.Name, Is.EqualTo("model"));
                Assert.That(doc.Header.Format.Version, Is.EqualTo(18));
            });
        }

        [Test]
        public void DeserializesBinary5RootElement()
        {
            using var stream = TestDataHelper.OpenResource("Binary.taunt05_b5.dmx");
            var doc = KV2Binary.Deserialize(stream);

            Assert.That(doc.Root, Is.InstanceOf<KV2Element>());

            var root = (KV2Element)doc.Root;

            Assert.Multiple(() =>
            {
                Assert.That(root.ClassName, Is.EqualTo("DmElement"));
                Assert.That(root.Name, Is.EqualTo("root"));
                Assert.That(root.ElementId, Is.Not.EqualTo(System.Guid.Empty));
            });
        }

        [Test]
        public void DeserializesBinary5Skeleton()
        {
            using var stream = TestDataHelper.OpenResource("Binary.taunt05_b5.dmx");
            var doc = KV2Binary.Deserialize(stream);
            var root = doc.Root;

            var skeleton = root["skeleton"];
            Assert.That(skeleton, Is.InstanceOf<KV2Element>());
            Assert.That(((KV2Element)skeleton).ClassName, Is.EqualTo("DmeModel"));
        }

        [Test]
        public void DeserializesBinary5ElementArrays()
        {
            using var stream = TestDataHelper.OpenResource("Binary.taunt05_b5.dmx");
            var doc = KV2Binary.Deserialize(stream);
            var root = doc.Root;

            var animationList = root["animationList"];
            Assert.That(animationList, Is.InstanceOf<KV2Element>());

            var animListElem = (KV2Element)animationList;
            Assert.That(animListElem.ClassName, Is.EqualTo("DmeAnimationList"));

            // animations should be an element array
            var animations = animListElem["animations"];
            Assert.That(animations, Is.Not.Null);
            Assert.That(animations.ValueType, Is.EqualTo(KVValueType.ElementArray));
        }

        #endregion

        #region v9 binary tests

        [Test]
        public void DeserializesBinary9Header()
        {
            using var stream = TestDataHelper.OpenResource("Binary.overboss_run.dmx");
            var doc = KV2Binary.Deserialize(stream);

            Assert.Multiple(() =>
            {
                Assert.That(doc.Header, Is.Not.Null);
                Assert.That(doc.Header.Encoding.Name, Is.EqualTo("binary"));
                Assert.That(doc.Header.Encoding.Version, Is.EqualTo(9));
                Assert.That(doc.Header.Format.Name, Is.EqualTo("model"));
                Assert.That(doc.Header.Format.Version, Is.EqualTo(22));
            });
        }

        [Test]
        public void DeserializesBinary9RootElement()
        {
            using var stream = TestDataHelper.OpenResource("Binary.overboss_run.dmx");
            var doc = KV2Binary.Deserialize(stream);

            Assert.That(doc.Root, Is.InstanceOf<KV2Element>());

            var root = (KV2Element)doc.Root;

            Assert.Multiple(() =>
            {
                Assert.That(root.ClassName, Is.EqualTo("DmElement"));
                Assert.That(root.Name, Is.EqualTo("root"));
                Assert.That(root.ElementId, Is.Not.EqualTo(System.Guid.Empty));
            });
        }

        [Test]
        public void DeserializesBinary9HasChildren()
        {
            using var stream = TestDataHelper.OpenResource("Binary.overboss_run.dmx");
            var doc = KV2Binary.Deserialize(stream);
            var root = doc.Root;

            Assert.That(root.Count, Is.GreaterThan(0));
        }

        #endregion

        #region Round-trip tests

        [Test]
        public void RoundTripsV4Binary()
        {
            using var originalStream = TestDataHelper.OpenResource("Binary.binary4.dmx");
            var doc = KV2Binary.Deserialize(originalStream);

            using var outputStream = new MemoryStream();
            KV2Binary.Serialize(outputStream, doc);
            outputStream.Position = 0;

            var doc2 = KV2Binary.Deserialize(outputStream);
            var root1 = (KV2Element)doc.Root;
            var root2 = (KV2Element)doc2.Root;

            Assert.Multiple(() =>
            {
                Assert.That(root2.ClassName, Is.EqualTo(root1.ClassName));
                Assert.That(root2.Name, Is.EqualTo(root1.Name));
                Assert.That(root2.ElementId, Is.EqualTo(root1.ElementId));
                Assert.That(root2.Count, Is.EqualTo(root1.Count));
            });
        }

        [Test]
        public void RoundTripsV5Binary()
        {
            using var originalStream = TestDataHelper.OpenResource("Binary.taunt05_b5.dmx");
            var doc = KV2Binary.Deserialize(originalStream);

            using var outputStream = new MemoryStream();
            KV2Binary.Serialize(outputStream, doc);
            outputStream.Position = 0;

            var doc2 = KV2Binary.Deserialize(outputStream);
            var root1 = (KV2Element)doc.Root;
            var root2 = (KV2Element)doc2.Root;

            Assert.Multiple(() =>
            {
                Assert.That(root2.ClassName, Is.EqualTo(root1.ClassName));
                Assert.That(root2.Name, Is.EqualTo(root1.Name));
                Assert.That(root2.ElementId, Is.EqualTo(root1.ElementId));
                Assert.That(doc2.Header.Encoding.Version, Is.EqualTo(doc.Header.Encoding.Version));
                Assert.That(doc2.Header.Format.Name, Is.EqualTo(doc.Header.Format.Name));
                Assert.That(doc2.Header.Format.Version, Is.EqualTo(doc.Header.Format.Version));
                Assert.That(root2.Count, Is.EqualTo(root1.Count));
            });
        }

        [Test]
        public void RoundTripsV9Binary()
        {
            using var originalStream = TestDataHelper.OpenResource("Binary.overboss_run.dmx");
            var doc = KV2Binary.Deserialize(originalStream);

            using var outputStream = new MemoryStream();
            KV2Binary.Serialize(outputStream, doc);
            outputStream.Position = 0;

            var doc2 = KV2Binary.Deserialize(outputStream);
            var root1 = (KV2Element)doc.Root;
            var root2 = (KV2Element)doc2.Root;

            Assert.Multiple(() =>
            {
                Assert.That(root2.ClassName, Is.EqualTo(root1.ClassName));
                Assert.That(root2.Name, Is.EqualTo(root1.Name));
                Assert.That(root2.ElementId, Is.EqualTo(root1.ElementId));
            });
        }

        #endregion

        #region Cross-format tests

        [Test]
        public void BinaryToTextRoundTrip()
        {
            // Read text, write as binary, read back, write as text, read back
            using var textStream = TestDataHelper.OpenResource("TextKV2.basic.dmx");
            var doc = KV2Text.Deserialize(textStream);

            // Write as binary
            using var binaryStream = new MemoryStream();
            KV2Binary.Serialize(binaryStream, doc);
            binaryStream.Position = 0;

            // Read binary
            var doc2 = KV2Binary.Deserialize(binaryStream);

            // Write as text
            using var textStream2 = new MemoryStream();
            KV2Text.Serialize(textStream2, doc2);
            textStream2.Position = 0;

            // Read text again
            var doc3 = KV2Text.Deserialize(textStream2);
            var root1 = (KV2Element)doc.Root;
            var root3 = (KV2Element)doc3.Root;

            Assert.Multiple(() =>
            {
                Assert.That(root3.ClassName, Is.EqualTo(root1.ClassName));
                Assert.That(root3.Name, Is.EqualTo(root1.Name));
                Assert.That(root3.ElementId, Is.EqualTo(root1.ElementId));
            });
        }

        [Test]
        public void TextToBinaryRoundTrip()
        {
            // Read text
            using var textStream = TestDataHelper.OpenResource("TextKV2.basic.dmx");
            var doc = KV2Text.Deserialize(textStream);

            // Write as binary
            using var binaryStream = new MemoryStream();
            KV2Binary.Serialize(binaryStream, doc);
            binaryStream.Position = 0;

            // Read back as binary
            var doc2 = KV2Binary.Deserialize(binaryStream);
            var root1 = (KV2Element)doc.Root;
            var root2 = (KV2Element)doc2.Root;

            Assert.Multiple(() =>
            {
                Assert.That(root2.ClassName, Is.EqualTo(root1.ClassName));
                Assert.That(root2.Name, Is.EqualTo(root1.Name));
                Assert.That(root2.ElementId, Is.EqualTo(root1.ElementId));
            });
        }

        #endregion

        #region Attribute value verification

        [Test]
        public void TextToBinaryPreservesScalarTypes()
        {
            // Read text DMX with known values
            using var textStream = TestDataHelper.OpenResource("TextKV2.basic.dmx");
            var doc = KV2Text.Deserialize(textStream);

            // Write as binary and read back
            using var binaryStream = new MemoryStream();
            KV2Binary.Serialize(binaryStream, doc);
            binaryStream.Position = 0;

            var doc2 = KV2Binary.Deserialize(binaryStream);
            var root = doc2.Root;

            Assert.Multiple(() =>
            {
                Assert.That((int)root["intVal"], Is.EqualTo(42));
                Assert.That((float)root["floatVal"], Is.EqualTo(3.14f).Within(0.001f));
                Assert.That((bool)root["boolVal"], Is.True);
                Assert.That((string)root["stringVal"], Is.EqualTo("hello world"));
            });
        }

        [Test]
        public void TextToBinaryPreservesArrays()
        {
            using var textStream = TestDataHelper.OpenResource("TextKV2.basic.dmx");
            var doc = KV2Text.Deserialize(textStream);

            using var binaryStream = new MemoryStream();
            KV2Binary.Serialize(binaryStream, doc);
            binaryStream.Position = 0;

            var doc2 = KV2Binary.Deserialize(binaryStream);
            var root = doc2.Root;

            Assert.Multiple(() =>
            {
                Assert.That(root["intArray"].ValueType, Is.EqualTo(KVValueType.Int32Array));
                Assert.That(root["intArray"].GetArray<int>(), Is.EqualTo(ExpectedIntArray));

                Assert.That(root["stringArray"].ValueType, Is.EqualTo(KVValueType.StringArray));
                Assert.That(root["stringArray"].GetArray<string>(), Is.EqualTo(ExpectedStringArray));
            });
        }

        [Test]
        public void TextToBinaryPreservesElementReferences()
        {
            using var textStream = TestDataHelper.OpenResource("TextKV2.basic.dmx");
            var doc = KV2Text.Deserialize(textStream);

            using var binaryStream = new MemoryStream();
            KV2Binary.Serialize(binaryStream, doc);
            binaryStream.Position = 0;

            var doc2 = KV2Binary.Deserialize(binaryStream);
            var root = doc2.Root;

            var child = root["child"];
            Assert.That(child, Is.InstanceOf<KV2Element>());
            Assert.That(((KV2Element)child).ClassName, Is.EqualTo("DmeChild"));
            Assert.That(((KV2Element)child).Name, Is.EqualTo("child1"));

            var nullRef = root["nullRef"];
            Assert.That(nullRef, Is.SameAs(KV2Element.Null));
        }

        [Test]
        public void TextToBinaryPreservesElementArrays()
        {
            using var textStream = TestDataHelper.OpenResource("TextKV2.basic.dmx");
            var doc = KV2Text.Deserialize(textStream);

            using var binaryStream = new MemoryStream();
            KV2Binary.Serialize(binaryStream, doc);
            binaryStream.Position = 0;

            var doc2 = KV2Binary.Deserialize(binaryStream);
            var root = doc2.Root;

            var elements = root["elements"];
            Assert.That(elements.ValueType, Is.EqualTo(KVValueType.ElementArray));

            var array = elements.GetArray<KV2Element>();
            Assert.Multiple(() =>
            {
                Assert.That(array, Has.Count.EqualTo(2));
                Assert.That(array[0].ClassName, Is.EqualTo("DmeItem"));
                Assert.That(array[0].Name, Is.EqualTo("item1"));
                Assert.That(array[1].ClassName, Is.EqualTo("DmeItem"));
                Assert.That(array[1].Name, Is.EqualTo("item2"));
            });
        }

        #endregion
    }
}
