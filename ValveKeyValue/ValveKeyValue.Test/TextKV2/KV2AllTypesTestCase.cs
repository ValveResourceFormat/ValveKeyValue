using System.Globalization;
using System.Numerics;

namespace ValveKeyValue.Test.TextKV2
{
    class KV2AllTypesTestCase
    {
        static KVSerializer KV2Text => KVSerializer.Create(KVSerializationFormat.KeyValues2Text);
        static KVSerializer KV2Binary => KVSerializer.Create(KVSerializationFormat.KeyValues2Binary);

        static readonly int[] ExpectedIntArray = [10, 20, 30];
        static readonly float[] ExpectedFloatArray = [1.5f, 2.5f];
        static readonly bool[] ExpectedBoolArray = [true, false, true];
        static readonly string[] ExpectedStringArray = ["alpha", "beta"];
        static readonly byte[] ExpectedBlobData = [0xDE, 0xAD, 0xBE, 0xEF];
        static readonly int[] ExpectedSingleArray = [42];
        static readonly int[] ExpectedBasicIntArray = [1, 2, 3];
        static readonly string[] ExpectedBasicStringArray = ["alpha", "beta", "gamma"];

        KVDocument doc;
        KVObject root;

        [OneTimeSetUp]
        public void SetUp()
        {
            using var stream = TestDataHelper.OpenResource("TextKV2.all_types.dmx");
            doc = KV2Text.Deserialize(stream);
            root = doc.Root;
        }

        #region All scalar types

        [Test]
        public void DeserializesAllScalarTypes()
        {
            Assert.Multiple(() =>
            {
                Assert.That((int)root["intVal"], Is.EqualTo(42));
                Assert.That((float)root["floatVal"], Is.EqualTo(3.14f).Within(0.001f));
                Assert.That((bool)root["boolTrue"], Is.True);
                Assert.That((bool)root["boolFalse"], Is.False);
                Assert.That((string)root["stringVal"], Is.EqualTo("hello"));
                Assert.That((string)root["emptyString"], Is.EqualTo(string.Empty));
                Assert.That(root["binaryVal"].ValueType, Is.EqualTo(KVValueType.BinaryBlob));
                Assert.That(root["binaryVal"].AsBlob(), Is.EqualTo(ExpectedBlobData));
                Assert.That(root["timeVal"].ValueType, Is.EqualTo(KVValueType.TimeSpan));
                Assert.That(root["uint8Val"].ValueType, Is.EqualTo(KVValueType.Byte));
                Assert.That(root["uint64Val"].ValueType, Is.EqualTo(KVValueType.UInt64));
            });
        }

        [Test]
        public void DeserializesVectorTypes()
        {
            Assert.Multiple(() =>
            {
                Assert.That(root["colorVal"].ToString(CultureInfo.InvariantCulture), Is.EqualTo("255 128 0 200"));
                Assert.That(root["vec2Val"].ToString(CultureInfo.InvariantCulture), Is.EqualTo("1.5 2.5"));
                Assert.That(root["vec3Val"].ToString(CultureInfo.InvariantCulture), Is.EqualTo("1 2 3"));
                Assert.That(root["vec4Val"].ToString(CultureInfo.InvariantCulture), Is.EqualTo("1 2 3 4"));
                Assert.That(root["qangleVal"].ToString(CultureInfo.InvariantCulture), Is.EqualTo("10 20 30"));
                Assert.That(root["quatVal"].ToString(CultureInfo.InvariantCulture), Is.EqualTo("0.1 0.2 0.3 0.9"));
            });
        }

        [Test]
        public void DeserializesMatrix4x4()
        {
            Assert.That(root["matrixVal"].ValueType, Is.EqualTo(KVValueType.Matrix4x4));
            Assert.That(root["matrixVal"].ToString(CultureInfo.InvariantCulture),
                Is.EqualTo("1 0 0 0 0 1 0 0 0 0 1 0 0 0 0 1"));
        }

        #endregion

        #region Array types

        [Test]
        public void DeserializesTypedArrays()
        {
            Assert.Multiple(() =>
            {
                Assert.That(root["intArray"].GetArray<int>(), Is.EqualTo(ExpectedIntArray));
                Assert.That(root["floatArray"].GetArray<float>(), Is.EqualTo(ExpectedFloatArray));
                Assert.That(root["boolArray"].GetArray<bool>(), Is.EqualTo(ExpectedBoolArray));
                Assert.That(root["stringArray"].GetArray<string>(), Is.EqualTo(ExpectedStringArray));
            });
        }

        [Test]
        public void DeserializesEmptyArrays()
        {
            Assert.Multiple(() =>
            {
                Assert.That(root["emptyIntArray"].GetArray<int>(), Is.Empty);
                Assert.That(root["emptyStringArray"].GetArray<string>(), Is.Empty);
            });
        }

        [Test]
        public void DeserializesSingleElementArray()
        {
            Assert.That(root["singleElementArray"].GetArray<int>(), Is.EqualTo(ExpectedSingleArray));
        }

        [Test]
        public void DeserializesVector3Array()
        {
            var array = root["vec3Array"].GetArray<Vector3>();
            Assert.Multiple(() =>
            {
                Assert.That(array, Has.Count.EqualTo(2));
                Assert.That(array[0], Is.EqualTo(new Vector3(1, 2, 3)));
                Assert.That(array[1], Is.EqualTo(new Vector3(4, 5, 6)));
            });
        }

        [Test]
        public void DeserializesColorArray()
        {
            var array = root["colorArray"].GetArray<DmxColor>();
            Assert.Multiple(() =>
            {
                Assert.That(array, Has.Count.EqualTo(2));
                Assert.That(array[0], Is.EqualTo(new DmxColor(255, 0, 0, 255)));
                Assert.That(array[1], Is.EqualTo(new DmxColor(0, 255, 0, 128)));
            });
        }

        [Test]
        public void DeserializesTimeArray()
        {
            var array = root["timeArray"].GetArray<DmxTime>();
            Assert.Multiple(() =>
            {
                Assert.That(array, Has.Count.EqualTo(2));
                Assert.That(array[0].Ticks, Is.EqualTo(100));
                Assert.That(array[1].Ticks, Is.EqualTo(200));
            });
        }

        [Test]
        public void DeserializesEmptyElementArray()
        {
            var array = root["emptyElementArray"].GetArray<KV2Element>();
            Assert.That(array, Is.Empty);
        }

        [Test]
        public void DeserializesMixedElementArray()
        {
            var array = root["mixedElementArray"].GetArray<KV2Element>();

            Assert.Multiple(() =>
            {
                Assert.That(array, Has.Count.EqualTo(3));
                Assert.That(array[0], Is.SameAs(KV2Element.Null));
                Assert.That(array[1], Is.InstanceOf<KV2Element>());
                Assert.That(array[1].Name, Is.EqualTo("array_item"));
                Assert.That((int)array[1]["score"], Is.EqualTo(100));
                Assert.That(array[2], Is.SameAs(KV2Element.Null));
            });
        }

        #endregion

        #region Nested elements

        [Test]
        public void DeserializesNestedElements()
        {
            var child = (KV2Element)root["inlineChild"];
            var nested = (KV2Element)child["nested"];

            Assert.Multiple(() =>
            {
                Assert.That(child.Name, Is.EqualTo("child"));
                Assert.That((int)child["value"], Is.EqualTo(99));
                Assert.That(nested.Name, Is.EqualTo("nested_child"));
                Assert.That((int)nested["deep"], Is.EqualTo(7));
            });
        }

        #endregion

        #region Text round-trip preserves all types

        [Test]
        public void TextRoundTripsAllScalarTypes()
        {
            using var output = new MemoryStream();
            KV2Text.Serialize(output, doc);
            output.Position = 0;

            var doc2 = KV2Text.Deserialize(output);
            var r = doc2.Root;

            Assert.Multiple(() =>
            {
                Assert.That((int)r["intVal"], Is.EqualTo(42));
                Assert.That((float)r["floatVal"], Is.EqualTo(3.14f).Within(0.001f));
                Assert.That((bool)r["boolTrue"], Is.True);
                Assert.That((bool)r["boolFalse"], Is.False);
                Assert.That((string)r["stringVal"], Is.EqualTo("hello"));
                Assert.That((string)r["emptyString"], Is.EqualTo(string.Empty));
                Assert.That(r["binaryVal"].AsBlob(), Is.EqualTo(ExpectedBlobData));
                Assert.That(r["matrixVal"].ToString(CultureInfo.InvariantCulture),
                    Is.EqualTo("1 0 0 0 0 1 0 0 0 0 1 0 0 0 0 1"));
            });
        }

        [Test]
        public void TextRoundTripsAllArrayTypes()
        {
            using var output = new MemoryStream();
            KV2Text.Serialize(output, doc);
            output.Position = 0;

            var doc2 = KV2Text.Deserialize(output);
            var r = doc2.Root;

            Assert.Multiple(() =>
            {
                Assert.That(r["intArray"].GetArray<int>(), Is.EqualTo(ExpectedIntArray));
                Assert.That(r["floatArray"].GetArray<float>(), Is.EqualTo(ExpectedFloatArray));
                Assert.That(r["boolArray"].GetArray<bool>(), Is.EqualTo(ExpectedBoolArray));
                Assert.That(r["stringArray"].GetArray<string>(), Is.EqualTo(ExpectedStringArray));
                Assert.That(r["emptyIntArray"].GetArray<int>(), Is.Empty);
                Assert.That(r["emptyStringArray"].GetArray<string>(), Is.Empty);
                Assert.That(r["singleElementArray"].GetArray<int>(), Is.EqualTo(ExpectedSingleArray));
                Assert.That(r["vec3Array"].GetArray<Vector3>(), Has.Count.EqualTo(2));
                Assert.That(r["colorArray"].GetArray<DmxColor>(), Has.Count.EqualTo(2));
                Assert.That(r["timeArray"].GetArray<DmxTime>(), Has.Count.EqualTo(2));
            });
        }

        [Test]
        public void TextRoundTripsNestedElements()
        {
            using var output = new MemoryStream();
            KV2Text.Serialize(output, doc);
            output.Position = 0;

            var doc2 = KV2Text.Deserialize(output);
            var child = (KV2Element)doc2.Root["inlineChild"];
            var nested = (KV2Element)child["nested"];

            Assert.Multiple(() =>
            {
                Assert.That(child.Name, Is.EqualTo("child"));
                Assert.That((int)child["value"], Is.EqualTo(99));
                Assert.That(nested.Name, Is.EqualTo("nested_child"));
                Assert.That((int)nested["deep"], Is.EqualTo(7));
            });
        }

        [Test]
        public void TextRoundTripsMixedElementArray()
        {
            using var output = new MemoryStream();
            KV2Text.Serialize(output, doc);
            output.Position = 0;

            var doc2 = KV2Text.Deserialize(output);
            var array = doc2.Root["mixedElementArray"].GetArray<KV2Element>();

            Assert.Multiple(() =>
            {
                Assert.That(array, Has.Count.EqualTo(3));
                Assert.That(array[0], Is.SameAs(KV2Element.Null));
                Assert.That(array[1].Name, Is.EqualTo("array_item"));
                Assert.That(array[2], Is.SameAs(KV2Element.Null));
            });
        }

        #endregion

        #region Binary round-trip preserves all types

        [Test]
        public void BinaryRoundTripsAllScalarTypes()
        {
            // Use basic.dmx for binary round-trip — it doesn't have v9-only types (uint8, uint64)
            using var basicStream = TestDataHelper.OpenResource("TextKV2.basic.dmx");
            var basicDoc = KV2Text.Deserialize(basicStream);

            using var binaryStream = new MemoryStream();
            KV2Binary.Serialize(binaryStream, basicDoc);
            binaryStream.Position = 0;

            var doc2 = KV2Binary.Deserialize(binaryStream);
            var r = doc2.Root;

            Assert.Multiple(() =>
            {
                Assert.That((int)r["intVal"], Is.EqualTo(42));
                Assert.That((float)r["floatVal"], Is.EqualTo(3.14f).Within(0.001f));
                Assert.That((bool)r["boolVal"], Is.True);
                Assert.That((string)r["stringVal"], Is.EqualTo("hello world"));
                Assert.That(r["color"].ToString(CultureInfo.InvariantCulture), Is.EqualTo("255 128 0 255"));
                Assert.That(r["position"].ToString(CultureInfo.InvariantCulture), Is.EqualTo("1.5 2.5 3.5"));
                Assert.That(r["angles"].ToString(CultureInfo.InvariantCulture), Is.EqualTo("10 20 30"));
                Assert.That(r["rotation"].ToString(CultureInfo.InvariantCulture), Is.EqualTo("0 0 0 1"));
            });
        }

        [Test]
        public void BinaryRoundTripsAllArrayTypes()
        {
            using var basicStream = TestDataHelper.OpenResource("TextKV2.basic.dmx");
            var basicDoc = KV2Text.Deserialize(basicStream);

            using var binaryStream = new MemoryStream();
            KV2Binary.Serialize(binaryStream, basicDoc);
            binaryStream.Position = 0;

            var doc2 = KV2Binary.Deserialize(binaryStream);
            var r = doc2.Root;

            Assert.Multiple(() =>
            {
                Assert.That(r["intArray"].GetArray<int>(), Is.EqualTo(ExpectedBasicIntArray));
                Assert.That(r["stringArray"].GetArray<string>(), Is.EqualTo(ExpectedBasicStringArray));
            });
        }

        [Test]
        public void BinaryRoundTripsNestedElements()
        {
            using var basicStream = TestDataHelper.OpenResource("TextKV2.basic.dmx");
            var basicDoc = KV2Text.Deserialize(basicStream);

            using var binaryStream = new MemoryStream();
            KV2Binary.Serialize(binaryStream, basicDoc);
            binaryStream.Position = 0;

            var doc2 = KV2Binary.Deserialize(binaryStream);
            var child = (KV2Element)doc2.Root["child"];

            Assert.Multiple(() =>
            {
                Assert.That(child.ClassName, Is.EqualTo("DmeChild"));
                Assert.That(child.Name, Is.EqualTo("child1"));
                Assert.That((int)child["value"], Is.EqualTo(99));
            });
        }

        #endregion

        #region Comments in text format

        [Test]
        public void TextFormatIgnoresComments()
        {
            // The all_types.dmx file has a // comment line
            Assert.That(root, Is.Not.Null);
            Assert.That(((KV2Element)root).Name, Is.EqualTo("all_types_root"));
        }

        #endregion

        #region Shared references across attributes and arrays

        const string SharedRefsDmx = """
            <!-- dmx encoding keyvalues2 1 format dmx 1 -->
            "Root"
            {
                "id" "elementid" "00000001-0000-0000-0000-000000000001"
                "name" "string" "root"
                "direct" "Shared"
                {
                    "id" "elementid" "00000002-0000-0000-0000-000000000002"
                    "name" "string" "shared"
                    "val" "int" "42"
                }
                "alsoRef" "element" "00000002-0000-0000-0000-000000000002"
                "arr" "element_array"
                [
                    "element" "00000002-0000-0000-0000-000000000002",
                    "element" "",
                    "element" "00000002-0000-0000-0000-000000000002"
                ]
            }
            """;

        [Test]
        public void SharedElementAcrossAttributeAndArray()
        {
            var testDoc = KV2Text.Deserialize(SharedRefsDmx);

            // Text round-trip
            using var stream = new MemoryStream();
            KV2Text.Serialize(stream, testDoc);
            stream.Position = 0;

            var doc2 = KV2Text.Deserialize(stream);
            var r = doc2.Root;

            var direct = r["direct"];
            var alsoRef = r["alsoRef"];
            var arr = r["arr"].GetArray<KV2Element>();

            Assert.Multiple(() =>
            {
                Assert.That(direct, Is.SameAs(alsoRef));
                Assert.That(arr[0], Is.SameAs(direct));
                Assert.That(arr[1], Is.SameAs(KV2Element.Null));
                Assert.That(arr[2], Is.SameAs(direct));
            });
        }

        [Test]
        public void SharedElementAcrossAttributeAndArrayBinary()
        {
            var testDoc = KV2Text.Deserialize(SharedRefsDmx);

            // Binary round-trip
            using var stream = new MemoryStream();
            KV2Binary.Serialize(stream, testDoc);
            stream.Position = 0;

            var doc2 = KV2Binary.Deserialize(stream);
            var r = doc2.Root;

            var direct = r["direct"];
            var alsoRef = r["alsoRef"];
            var arr = r["arr"].GetArray<KV2Element>();

            Assert.Multiple(() =>
            {
                Assert.That(direct, Is.SameAs(alsoRef));
                Assert.That(arr[0], Is.SameAs(direct));
                Assert.That(arr[1], Is.SameAs(KV2Element.Null));
                Assert.That(arr[2], Is.SameAs(direct));
            });
        }

        #endregion
    }
}
