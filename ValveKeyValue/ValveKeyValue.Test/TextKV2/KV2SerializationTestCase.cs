using System.Globalization;
using System.Numerics;

namespace ValveKeyValue.Test.TextKV2
{
    class KV2SerializationTestCase
    {
        static KVSerializer KV2Text => KVSerializer.Create(KVSerializationFormat.KeyValues2Text);
        static KVSerializer KV2Binary => KVSerializer.Create(KVSerializationFormat.KeyValues2Binary);

        static readonly int[] ExpectedIntArray = [1, 2, 3];
        static readonly string[] ExpectedStringArray = ["alpha", "beta", "gamma"];

        #region Text round-trip

        [Test]
        public void TextRoundTripsBasicDmx()
        {
            using var stream = TestDataHelper.OpenResource("TextKV2.basic.dmx");
            var doc = KV2Text.Deserialize(stream);

            using var outputStream = new MemoryStream();
            KV2Text.Serialize(outputStream, doc);
            outputStream.Position = 0;

            var doc2 = KV2Text.Deserialize(outputStream);
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
        public void TextRoundTripsScalarValues()
        {
            using var stream = TestDataHelper.OpenResource("TextKV2.basic.dmx");
            var doc = KV2Text.Deserialize(stream);

            using var outputStream = new MemoryStream();
            KV2Text.Serialize(outputStream, doc);
            outputStream.Position = 0;

            var doc2 = KV2Text.Deserialize(outputStream);
            var root = doc2.Root;

            Assert.Multiple(() =>
            {
                Assert.That((int)root["intVal"], Is.EqualTo(42));
                Assert.That((float)root["floatVal"], Is.EqualTo(3.14f).Within(0.001f));
                Assert.That((bool)root["boolVal"], Is.True);
                Assert.That((string)root["stringVal"], Is.EqualTo("hello world"));
                Assert.That(root["color"].ToString(CultureInfo.InvariantCulture), Is.EqualTo("255 128 0 255"));
                Assert.That(root["position"].ToString(CultureInfo.InvariantCulture), Is.EqualTo("1.5 2.5 3.5"));
                Assert.That(root["angles"].ToString(CultureInfo.InvariantCulture), Is.EqualTo("10 20 30"));
                Assert.That(root["rotation"].ToString(CultureInfo.InvariantCulture), Is.EqualTo("0 0 0 1"));
                Assert.That(root["uv"].ToString(CultureInfo.InvariantCulture), Is.EqualTo("0.5 0.75"));
                Assert.That(root["vec4"].ToString(CultureInfo.InvariantCulture), Is.EqualTo("1 2 3 4"));
            });
        }

        [Test]
        public void TextRoundTripsArrays()
        {
            using var stream = TestDataHelper.OpenResource("TextKV2.basic.dmx");
            var doc = KV2Text.Deserialize(stream);

            using var outputStream = new MemoryStream();
            KV2Text.Serialize(outputStream, doc);
            outputStream.Position = 0;

            var doc2 = KV2Text.Deserialize(outputStream);
            var root = doc2.Root;

            Assert.Multiple(() =>
            {
                Assert.That(root["intArray"].GetArray<int>(), Is.EqualTo(ExpectedIntArray));
                Assert.That(root["stringArray"].GetArray<string>(), Is.EqualTo(ExpectedStringArray));
            });
        }

        [Test]
        public void TextRoundTripsElementArrays()
        {
            using var stream = TestDataHelper.OpenResource("TextKV2.basic.dmx");
            var doc = KV2Text.Deserialize(stream);

            using var outputStream = new MemoryStream();
            KV2Text.Serialize(outputStream, doc);
            outputStream.Position = 0;

            var doc2 = KV2Text.Deserialize(outputStream);
            var root = doc2.Root;
            var elements = root["elements"].GetArray<KV2Element>();

            Assert.Multiple(() =>
            {
                Assert.That(elements, Has.Count.EqualTo(2));
                Assert.That(elements[0].ClassName, Is.EqualTo("DmeItem"));
                Assert.That(elements[0].Name, Is.EqualTo("item1"));
                Assert.That(elements[1].Name, Is.EqualTo("item2"));
            });
        }

        [Test]
        public void TextRoundTripsChildElement()
        {
            using var stream = TestDataHelper.OpenResource("TextKV2.basic.dmx");
            var doc = KV2Text.Deserialize(stream);

            using var outputStream = new MemoryStream();
            KV2Text.Serialize(outputStream, doc);
            outputStream.Position = 0;

            var doc2 = KV2Text.Deserialize(outputStream);
            var child = (KV2Element)doc2.Root["child"];

            Assert.Multiple(() =>
            {
                Assert.That(child.ClassName, Is.EqualTo("DmeChild"));
                Assert.That(child.Name, Is.EqualTo("child1"));
                Assert.That((int)child["value"], Is.EqualTo(99));
            });
        }

        [Test]
        public void TextRoundTripsNullElement()
        {
            using var stream = TestDataHelper.OpenResource("TextKV2.basic.dmx");
            var doc = KV2Text.Deserialize(stream);

            using var outputStream = new MemoryStream();
            KV2Text.Serialize(outputStream, doc);
            outputStream.Position = 0;

            var doc2 = KV2Text.Deserialize(outputStream);
            Assert.That(doc2.Root["nullRef"], Is.SameAs(KV2Element.Null));
        }

        [Test]
        public void TextRoundTripsSharedReferences()
        {
            using var stream = TestDataHelper.OpenResource("TextKV2.shared_refs.dmx");
            var doc = KV2Text.Deserialize(stream);

            using var outputStream = new MemoryStream();
            KV2Text.Serialize(outputStream, doc);
            outputStream.Position = 0;

            var doc2 = KV2Text.Deserialize(outputStream);
            var root = doc2.Root;

            var shared = root["sharedElement"];
            var refToShared = root["refToShared"];
            var arrayRef = root["arrayWithRefs"].GetArray<KV2Element>()[0];

            Assert.Multiple(() =>
            {
                Assert.That(shared, Is.InstanceOf<KV2Element>());
                Assert.That(refToShared, Is.SameAs(shared));
                Assert.That(arrayRef, Is.SameAs(shared));
            });
        }

        [Test]
        public void TextRoundTripsHeaderPreservation()
        {
            using var stream = TestDataHelper.OpenResource("TextKV2.basic.dmx");
            var doc = KV2Text.Deserialize(stream);

            using var outputStream = new MemoryStream();
            KV2Text.Serialize(outputStream, doc);
            outputStream.Position = 0;

            var doc2 = KV2Text.Deserialize(outputStream);

            Assert.Multiple(() =>
            {
                Assert.That(doc2.Header.Encoding.Name, Is.EqualTo("keyvalues2"));
                Assert.That(doc2.Header.Encoding.Version, Is.EqualTo(1));
                Assert.That(doc2.Header.Format.Name, Is.EqualTo("dmx"));
                Assert.That(doc2.Header.Format.Version, Is.EqualTo(1));
            });
        }

        [Test]
        public void TextRoundTripsTaunt05()
        {
            using var stream = TestDataHelper.OpenResource("TextKV2.taunt05.dmx");
            var doc = KV2Text.Deserialize(stream);

            using var outputStream = new MemoryStream();
            KV2Text.Serialize(outputStream, doc);
            outputStream.Position = 0;

            var doc2 = KV2Text.Deserialize(outputStream);
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

        #region Programmatic construction and serialization

        [Test]
        public void SerializesProgrammaticElement()
        {
            var id = Guid.NewGuid();
            var root = new KV2Element("TestClass", "testElement", id);
            root.Add("health", new KVObject(100));
            root.Add("label", new KVObject("hero"));
            root.Add("active", new KVObject(true));

            var doc = new KVDocument(null, null, root);

            using var stream = new MemoryStream();
            KV2Text.Serialize(stream, doc);
            stream.Position = 0;

            var doc2 = KV2Text.Deserialize(stream);
            var root2 = (KV2Element)doc2.Root;

            Assert.Multiple(() =>
            {
                Assert.That(root2.ClassName, Is.EqualTo("TestClass"));
                Assert.That(root2.Name, Is.EqualTo("testElement"));
                Assert.That(root2.ElementId, Is.EqualTo(id));
                Assert.That((int)root2["health"], Is.EqualTo(100));
                Assert.That((string)root2["label"], Is.EqualTo("hero"));
                Assert.That((bool)root2["active"], Is.True);
            });
        }

        [Test]
        public void SerializesProgrammaticElementWithDmxTypes()
        {
            var root = new KV2Element("TestClass", "test", Guid.NewGuid());
            root.Add("color", new KVObject(new DmxColor(255, 0, 128, 200)));
            root.Add("pos", new KVObject(new Vector3(1.5f, 2.5f, 3.5f)));
            root.Add("angle", new KVObject(new QAngle(10, 20, 30)));
            root.Add("rot", new KVObject(new Quaternion(0, 0, 0, 1)));
            root.Add("uv", new KVObject(new Vector2(0.5f, 0.75f)));
            root.Add("vec4", new KVObject(new Vector4(1, 2, 3, 4)));
            root.Add("time", new KVObject(new DmxTime(500)));

            var doc = new KVDocument(null, null, root);

            using var stream = new MemoryStream();
            KV2Text.Serialize(stream, doc);
            stream.Position = 0;

            var doc2 = KV2Text.Deserialize(stream);
            var root2 = doc2.Root;

            Assert.Multiple(() =>
            {
                Assert.That(root2["color"].ToString(CultureInfo.InvariantCulture), Is.EqualTo("255 0 128 200"));
                Assert.That(root2["pos"].ToString(CultureInfo.InvariantCulture), Is.EqualTo("1.5 2.5 3.5"));
                Assert.That(root2["angle"].ToString(CultureInfo.InvariantCulture), Is.EqualTo("10 20 30"));
                Assert.That(root2["rot"].ToString(CultureInfo.InvariantCulture), Is.EqualTo("0 0 0 1"));
                Assert.That(root2["uv"].ToString(CultureInfo.InvariantCulture), Is.EqualTo("0.5 0.75"));
                Assert.That(root2["vec4"].ToString(CultureInfo.InvariantCulture), Is.EqualTo("1 2 3 4"));
                Assert.That(root2["time"].ValueType, Is.EqualTo(KVValueType.TimeSpan));
            });
        }

        [Test]
        public void SerializesProgrammaticNullElementRef()
        {
            var root = new KV2Element("TestClass", "test", Guid.NewGuid());
            root.Add("nothing", KV2Element.Null);

            var doc = new KVDocument(null, null, root);

            using var stream = new MemoryStream();
            KV2Text.Serialize(stream, doc);
            stream.Position = 0;

            var doc2 = KV2Text.Deserialize(stream);
            Assert.That(doc2.Root["nothing"], Is.SameAs(KV2Element.Null));
        }

        [Test]
        public void SerializesProgrammaticSharedReferences()
        {
            var shared = new KV2Element("Shared", "sharedObj", Guid.NewGuid());
            shared.Add("data", new KVObject("shared data"));

            var root = new KV2Element("Root", "root", Guid.NewGuid());
            root.Add("ref1", shared);
            root.Add("ref2", shared); // Same object referenced twice

            var doc = new KVDocument(null, null, root);

            using var stream = new MemoryStream();
            KV2Text.Serialize(stream, doc);
            stream.Position = 0;

            var doc2 = KV2Text.Deserialize(stream);
            var ref1 = doc2.Root["ref1"];
            var ref2 = doc2.Root["ref2"];

            Assert.Multiple(() =>
            {
                Assert.That(ref1, Is.InstanceOf<KV2Element>());
                Assert.That(ref2, Is.SameAs(ref1), "Both references should resolve to the same object");
                Assert.That((string)ref1["data"], Is.EqualTo("shared data"));
            });
        }

        [Test]
        public void SerializesWithEscapeSequences()
        {
            var root = new KV2Element("TestClass", "test", Guid.NewGuid());
            root.Add("escaped", new KVObject("line1\nline2\ttab\"quote\\backslash"));

            var doc = new KVDocument(null, null, root);

            using var stream = new MemoryStream();
            KV2Text.Serialize(stream, doc);
            stream.Position = 0;

            var doc2 = KV2Text.Deserialize(stream);
            Assert.That((string)doc2.Root["escaped"], Is.EqualTo("line1\nline2\ttab\"quote\\backslash"));
        }

        [Test]
        public void SerializesBinaryFromProgrammaticElement()
        {
            var root = new KV2Element("TestClass", "test", Guid.NewGuid());
            root.Add("health", new KVObject(100));
            root.Add("name", new KVObject("hero"));
            root.Add("pos", new KVObject(new Vector3(1, 2, 3)));

            var doc = new KVDocument(null, null, root);

            using var stream = new MemoryStream();
            KV2Binary.Serialize(stream, doc);
            stream.Position = 0;

            var doc2 = KV2Binary.Deserialize(stream);
            var root2 = (KV2Element)doc2.Root;

            Assert.Multiple(() =>
            {
                Assert.That(root2.ClassName, Is.EqualTo("TestClass"));
                Assert.That(root2.Name, Is.EqualTo("test"));
                Assert.That((int)root2["health"], Is.EqualTo(100));
                Assert.That((string)root2["name"], Is.EqualTo("hero"));
                Assert.That(root2["pos"].ToString(CultureInfo.InvariantCulture), Is.EqualTo("1 2 3"));
            });
        }

        [Test]
        public void TextToBinaryThenBackToText()
        {
            // Text → Binary → Text → verify
            using var stream = TestDataHelper.OpenResource("TextKV2.basic.dmx");
            var doc = KV2Text.Deserialize(stream);

            using var binaryStream = new MemoryStream();
            KV2Binary.Serialize(binaryStream, doc);
            binaryStream.Position = 0;

            var doc2 = KV2Binary.Deserialize(binaryStream);

            using var textStream = new MemoryStream();
            KV2Text.Serialize(textStream, doc2);
            textStream.Position = 0;

            var doc3 = KV2Text.Deserialize(textStream);
            var root = doc3.Root;

            Assert.Multiple(() =>
            {
                Assert.That((int)root["intVal"], Is.EqualTo(42));
                Assert.That((string)root["stringVal"], Is.EqualTo("hello world"));
                Assert.That(root["intArray"].GetArray<int>(), Is.EqualTo(ExpectedIntArray));
                Assert.That(root["child"], Is.InstanceOf<KV2Element>());
                Assert.That(root["nullRef"], Is.SameAs(KV2Element.Null));
            });
        }

        #endregion

        #region Typed deserialization

        [Test]
        public void DeserializesIntoTypedObject()
        {
            using var stream = TestDataHelper.OpenResource("TextKV2.basic.dmx");
            var obj = KV2Text.Deserialize<BasicDmxObject>(stream);

            Assert.Multiple(() =>
            {
                Assert.That(obj.IntVal, Is.EqualTo(42));
                Assert.That(obj.FloatVal, Is.EqualTo(3.14f).Within(0.001f));
                Assert.That(obj.BoolVal, Is.True);
                Assert.That(obj.StringVal, Is.EqualTo("hello world"));
            });
        }

        [Test]
        public void DeserializesBinaryIntoTypedObject()
        {
            // Read text, write as binary, then deserialize binary into typed object
            using var textStream = TestDataHelper.OpenResource("TextKV2.basic.dmx");
            var doc = KV2Text.Deserialize(textStream);

            using var binaryStream = new MemoryStream();
            KV2Binary.Serialize(binaryStream, doc);
            binaryStream.Position = 0;

            var obj = KV2Binary.Deserialize<BasicDmxObject>(binaryStream);

            Assert.Multiple(() =>
            {
                Assert.That(obj.IntVal, Is.EqualTo(42));
                Assert.That(obj.FloatVal, Is.EqualTo(3.14f).Within(0.001f));
                Assert.That(obj.BoolVal, Is.True);
                Assert.That(obj.StringVal, Is.EqualTo("hello world"));
            });
        }

        class BasicDmxObject
        {
            public int IntVal { get; set; }
            public float FloatVal { get; set; }
            public bool BoolVal { get; set; }
            public string StringVal { get; set; }
        }

        #endregion
    }
}
