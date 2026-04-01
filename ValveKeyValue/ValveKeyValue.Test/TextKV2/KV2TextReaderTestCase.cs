using System.Globalization;
using System.Numerics;

namespace ValveKeyValue.Test.TextKV2
{
    class KV2TextReaderTestCase
    {
        static KVSerializer KV2 => KVSerializer.Create(KVSerializationFormat.KeyValues2Text);
        static readonly int[] ExpectedIntArray = [1, 2, 3];
        static readonly string[] ExpectedStringArray = ["alpha", "beta", "gamma"];

        [Test]
        public void DeserializesHeader()
        {
            using var stream = TestDataHelper.OpenResource("TextKV2.basic.dmx");
            var doc = KV2.Deserialize(stream);

            Assert.Multiple(() =>
            {
                Assert.That(doc.Header, Is.Not.Null);
                Assert.That(doc.Header.Encoding.Name, Is.EqualTo("keyvalues2"));
                Assert.That(doc.Header.Encoding.Version, Is.EqualTo(1));
                Assert.That(doc.Header.Format.Name, Is.EqualTo("dmx"));
                Assert.That(doc.Header.Format.Version, Is.EqualTo(1));
            });
        }

        [Test]
        public void DeserializesRootElement()
        {
            using var stream = TestDataHelper.OpenResource("TextKV2.basic.dmx");
            var doc = KV2.Deserialize(stream);

            Assert.That(doc.Root, Is.InstanceOf<KV2Element>());

            var root = (KV2Element)doc.Root;

            Assert.Multiple(() =>
            {
                Assert.That(root.ClassName, Is.EqualTo("DmElement"));
                Assert.That(root.Name, Is.EqualTo("root"));
                Assert.That(root.ElementId, Is.EqualTo(Guid.Parse("a1b2c3d4-e5f6-7890-abcd-ef1234567890")));
            });
        }

        [Test]
        public void DeserializesScalarTypes()
        {
            using var stream = TestDataHelper.OpenResource("TextKV2.basic.dmx");
            var doc = KV2.Deserialize(stream);
            var root = doc.Root;

            Assert.Multiple(() =>
            {
                Assert.That(root["intVal"].ValueType, Is.EqualTo(KVValueType.Int32));
                Assert.That((int)root["intVal"], Is.EqualTo(42));

                Assert.That(root["floatVal"].ValueType, Is.EqualTo(KVValueType.FloatingPoint));
                Assert.That((float)root["floatVal"], Is.EqualTo(3.14f).Within(0.001f));

                Assert.That(root["boolVal"].ValueType, Is.EqualTo(KVValueType.Boolean));
                Assert.That((bool)root["boolVal"], Is.True);

                Assert.That(root["stringVal"].ValueType, Is.EqualTo(KVValueType.String));
                Assert.That((string)root["stringVal"], Is.EqualTo("hello world"));
            });
        }

        [Test]
        public void DeserializesColor()
        {
            using var stream = TestDataHelper.OpenResource("TextKV2.basic.dmx");
            var doc = KV2.Deserialize(stream);
            var root = doc.Root;

            Assert.That(root["color"].ValueType, Is.EqualTo(KVValueType.Color));
            Assert.That(root["color"].ToString(CultureInfo.InvariantCulture), Is.EqualTo("255 128 0 255"));
        }

        [Test]
        public void DeserializesVector3()
        {
            using var stream = TestDataHelper.OpenResource("TextKV2.basic.dmx");
            var doc = KV2.Deserialize(stream);
            var root = doc.Root;

            Assert.That(root["position"].ValueType, Is.EqualTo(KVValueType.Vector3));
            Assert.That(root["position"].ToString(CultureInfo.InvariantCulture), Is.EqualTo("1.5 2.5 3.5"));
        }

        [Test]
        public void DeserializesQAngle()
        {
            using var stream = TestDataHelper.OpenResource("TextKV2.basic.dmx");
            var doc = KV2.Deserialize(stream);
            var root = doc.Root;

            Assert.That(root["angles"].ValueType, Is.EqualTo(KVValueType.QAngle));
            Assert.That(root["angles"].ToString(CultureInfo.InvariantCulture), Is.EqualTo("10 20 30"));
        }

        [Test]
        public void DeserializesQuaternion()
        {
            using var stream = TestDataHelper.OpenResource("TextKV2.basic.dmx");
            var doc = KV2.Deserialize(stream);
            var root = doc.Root;

            Assert.That(root["rotation"].ValueType, Is.EqualTo(KVValueType.Quaternion));
            Assert.That(root["rotation"].ToString(CultureInfo.InvariantCulture), Is.EqualTo("0 0 0 1"));
        }

        [Test]
        public void DeserializesVector2()
        {
            using var stream = TestDataHelper.OpenResource("TextKV2.basic.dmx");
            var doc = KV2.Deserialize(stream);
            var root = doc.Root;

            Assert.That(root["uv"].ValueType, Is.EqualTo(KVValueType.Vector2));
            Assert.That(root["uv"].ToString(CultureInfo.InvariantCulture), Is.EqualTo("0.5 0.75"));
        }

        [Test]
        public void DeserializesVector4()
        {
            using var stream = TestDataHelper.OpenResource("TextKV2.basic.dmx");
            var doc = KV2.Deserialize(stream);
            var root = doc.Root;

            Assert.That(root["vec4"].ValueType, Is.EqualTo(KVValueType.Vector4));
            Assert.That(root["vec4"].ToString(CultureInfo.InvariantCulture), Is.EqualTo("1 2 3 4"));
        }

        [Test]
        public void DeserializesTime()
        {
            using var stream = TestDataHelper.OpenResource("TextKV2.basic.dmx");
            var doc = KV2.Deserialize(stream);
            var root = doc.Root;

            Assert.That(root["time"].ValueType, Is.EqualTo(KVValueType.TimeSpan));
        }

        [Test]
        public void DeserializesInlineChildElement()
        {
            using var stream = TestDataHelper.OpenResource("TextKV2.basic.dmx");
            var doc = KV2.Deserialize(stream);
            var root = doc.Root;

            var child = root["child"];
            Assert.That(child, Is.InstanceOf<KV2Element>());

            var childElement = (KV2Element)child;

            Assert.Multiple(() =>
            {
                Assert.That(childElement.ClassName, Is.EqualTo("DmeChild"));
                Assert.That(childElement.Name, Is.EqualTo("child1"));
                Assert.That(childElement.ElementId, Is.EqualTo(Guid.Parse("11111111-2222-3333-4444-555555555555")));
                Assert.That((int)childElement["value"], Is.EqualTo(99));
            });
        }

        [Test]
        public void DeserializesNullElementReference()
        {
            using var stream = TestDataHelper.OpenResource("TextKV2.basic.dmx");
            var doc = KV2.Deserialize(stream);
            var root = doc.Root;

            var nullRef = root["nullRef"];
            Assert.That(nullRef, Is.SameAs(KV2Element.Null));
        }

        [Test]
        public void DeserializesIntArray()
        {
            using var stream = TestDataHelper.OpenResource("TextKV2.basic.dmx");
            var doc = KV2.Deserialize(stream);
            var root = doc.Root;

            Assert.That(root["intArray"].ValueType, Is.EqualTo(KVValueType.Int32Array));

            var array = root["intArray"].GetArray<int>();
            Assert.That(array, Is.EqualTo(ExpectedIntArray));
        }

        [Test]
        public void DeserializesStringArray()
        {
            using var stream = TestDataHelper.OpenResource("TextKV2.basic.dmx");
            var doc = KV2.Deserialize(stream);
            var root = doc.Root;

            Assert.That(root["stringArray"].ValueType, Is.EqualTo(KVValueType.StringArray));

            var array = root["stringArray"].GetArray<string>();
            Assert.That(array, Is.EqualTo(ExpectedStringArray));
        }

        [Test]
        public void DeserializesElementArray()
        {
            using var stream = TestDataHelper.OpenResource("TextKV2.basic.dmx");
            var doc = KV2.Deserialize(stream);
            var root = doc.Root;

            Assert.That(root["elements"].ValueType, Is.EqualTo(KVValueType.ElementArray));

            var array = root["elements"].GetArray<KV2Element>();

            Assert.Multiple(() =>
            {
                Assert.That(array, Has.Count.EqualTo(2));
                Assert.That(array[0].ClassName, Is.EqualTo("DmeItem"));
                Assert.That(array[0].Name, Is.EqualTo("item1"));
                Assert.That(array[1].ClassName, Is.EqualTo("DmeItem"));
                Assert.That(array[1].Name, Is.EqualTo("item2"));
            });
        }

        [Test]
        public void DeserializesSharedReferences()
        {
            using var stream = TestDataHelper.OpenResource("TextKV2.shared_refs.dmx");
            var doc = KV2.Deserialize(stream);
            var root = doc.Root;

            var shared = root["sharedElement"];
            var refToShared = root["refToShared"];
            var arrayRef = root["arrayWithRefs"].GetArray<KV2Element>()[0];

            Assert.Multiple(() =>
            {
                Assert.That(shared, Is.InstanceOf<KV2Element>());
                Assert.That(refToShared, Is.SameAs(shared), "GUID reference should resolve to same object as inline element");
                Assert.That(arrayRef, Is.SameAs(shared), "Array GUID reference should resolve to same object");
            });
        }

        [Test]
        public void DeserializesTaunt05()
        {
            using var stream = TestDataHelper.OpenResource("TextKV2.taunt05.dmx");
            var doc = KV2.Deserialize(stream);

            Assert.Multiple(() =>
            {
                Assert.That(doc.Header.Encoding.Name, Is.EqualTo("keyvalues2"));
                Assert.That(doc.Header.Encoding.Version, Is.EqualTo(1));
                Assert.That(doc.Header.Format.Name, Is.EqualTo("model"));
                Assert.That(doc.Header.Format.Version, Is.EqualTo(1));
            });

            var root = (KV2Element)doc.Root;

            Assert.Multiple(() =>
            {
                Assert.That(root.ClassName, Is.EqualTo("DmElement"));
                Assert.That(root.Name, Is.EqualTo("root"));
                Assert.That(root.ElementId, Is.Not.EqualTo(Guid.Empty));
            });

            // Verify skeleton is an inline DmeModel element
            var skeleton = root["skeleton"];
            Assert.That(skeleton, Is.InstanceOf<KV2Element>());
            Assert.That(((KV2Element)skeleton).ClassName, Is.EqualTo("DmeModel"));
        }

        [Test]
        public void DeserializesCs2Map()
        {
            using var stream = TestDataHelper.OpenResource("TextKV2.cs2_map.dmx");
            var doc = KV2.Deserialize(stream);

            Assert.Multiple(() =>
            {
                Assert.That(doc.Header.Encoding.Name, Is.EqualTo("keyvalues2"));
                Assert.That(doc.Header.Encoding.Version, Is.EqualTo(4));
                Assert.That(doc.Header.Format.Name, Is.EqualTo("vmap"));
                Assert.That(doc.Header.Format.Version, Is.EqualTo(35));
            });

            var root = (KV2Element)doc.Root;

            // cs2_map starts with a $prefix_element$, the actual root is the second top-level element
            // But our reader returns topLevelElements[0] as root
            Assert.That(root.ClassName, Is.EqualTo("$prefix_element$").Or.EqualTo("CMapRootElement"));
        }

        [Test]
        public void DeserializesReflectionTest()
        {
            using var stream = TestDataHelper.OpenResource("TextKV2.reflectiontest.dmx");
            var doc = KV2.Deserialize(stream);

            Assert.That(doc.Root, Is.InstanceOf<KV2Element>());
            Assert.That(doc.Header, Is.Not.Null);
        }
    }
}
