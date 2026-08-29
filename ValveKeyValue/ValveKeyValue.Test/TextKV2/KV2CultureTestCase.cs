using System.Numerics;

namespace ValveKeyValue.Test.TextKV2
{
    /// <summary>
    /// DMX text is culture independent. de-DE and fa-IR are chosen because together they differ
    /// from the invariant culture in every way that matters here: comma and Arabic decimal
    /// separators, a period group separator, and a non-ASCII negative sign. The composite types
    /// are the ones at risk, since each writes several numbers into one token.
    /// </summary>
    class KV2CultureTestCase
    {
        static KVSerializer KV2Text => KVSerializer.Create(KVSerializationFormat.KeyValues2Text);
        static KVSerializer KV2Binary => KVSerializer.Create(KVSerializationFormat.KeyValues2Binary);

        static readonly Guid RootId = Guid.Parse("00000000-0000-0000-0000-0000000000c1");

        static readonly Vector2 Vector2Value = new(1.5f, -2.25f);
        static readonly Vector3 Vector3Value = new(1.5f, -2.25f, 0.125f);
        static readonly Vector4 Vector4Value = new(1.5f, -2.25f, 0.125f, -4f);
        static readonly Quaternion QuaternionValue = new(0.5f, -0.5f, 0.5f, -0.5f);
        static readonly QAngle QAngleValue = new(-1.5f, 22.25f, -333.125f);
        static readonly DmxColor ColorValue = new(1, 255, 2, 244);

        static readonly Matrix4x4 MatrixValue = new(
            1.5f, -2.25f, 0, 0,
            0, 1, 0, 0,
            0, 0, 1, 0,
            -5.75f, 6.5f, 7.125f, 1);

        static readonly float[] ExpectedFloatArray = [1.5f, -2.25f];

        static KVDocument MakeDocument()
        {
            var root = new KV2Element("DmElement", "root", RootId);

            root.Add("float", new KVObject(-2.25f));
            root.Add("int", new KVObject(-1234567));
            root.Add("byte", KVObject.Byte(255));
            root.Add("uint64", new KVObject(0xdf41645d6af564aUL));
            root.Add("time", new KVObject(new DmxTime(15000)));
            root.Add("color", new KVObject(ColorValue));
            root.Add("vector2", new KVObject(Vector2Value));
            root.Add("vector3", new KVObject(Vector3Value));
            root.Add("vector4", new KVObject(Vector4Value));
            root.Add("quaternion", new KVObject(QuaternionValue));
            root.Add("qangle", new KVObject(QAngleValue));
            root.Add("matrix", new KVObject(MatrixValue));
            root.Add("floatArray", KVObject.TypedArray(new List<float>(ExpectedFloatArray)));

            return new KVDocument(null, null, root);
        }

        static string Serialize(KVSerializer serializer, KVDocument doc)
        {
            using var stream = new MemoryStream();
            serializer.Serialize(stream, doc);

            return System.Text.Encoding.UTF8.GetString(stream.ToArray());
        }

        static KV2Element RoundTrip(KVSerializer serializer)
        {
            using var stream = new MemoryStream();
            serializer.Serialize(stream, MakeDocument());
            stream.Position = 0;

            return (KV2Element)serializer.Deserialize(stream).Root;
        }

        [Test]
        [SetCulture("de-DE")]
        public void TextIsInvariant_DE() => AssertTextIsInvariant();

        [Test]
        [SetCulture("fa-IR")]
        public void TextIsInvariant_FA() => AssertTextIsInvariant();

        static void AssertTextIsInvariant()
        {
            var text = Serialize(KV2Text, MakeDocument());

            Assert.Multiple(() =>
            {
                Assert.That(text, Does.Contain("\"float\" \"float\" \"-2.25\""));
                Assert.That(text, Does.Contain("\"int\" \"int\" \"-1234567\""));
                Assert.That(text, Does.Contain("\"byte\" \"uint8\" \"255\""));
                Assert.That(text, Does.Contain("\"uint64\" \"uint64\" \"0xdf41645d6af564a\""));
                Assert.That(text, Does.Contain("\"time\" \"time\" \"1.5000\""));
                Assert.That(text, Does.Contain("\"color\" \"color\" \"1 255 2 244\""));
                Assert.That(text, Does.Contain("\"vector2\" \"vector2\" \"1.5 -2.25\""));
                Assert.That(text, Does.Contain("\"vector3\" \"vector3\" \"1.5 -2.25 0.125\""));
                Assert.That(text, Does.Contain("\"vector4\" \"vector4\" \"1.5 -2.25 0.125 -4\""));
                Assert.That(text, Does.Contain("\"quaternion\" \"quaternion\" \"0.5 -0.5 0.5 -0.5\""));
                Assert.That(text, Does.Contain("\"qangle\" \"qangle\" \"-1.5 22.25 -333.125\""));

                // A matrix is written one row per line.
                Assert.That(text, Does.Contain("1.5 -2.25 0 0"));
                Assert.That(text, Does.Contain("-5.75 6.5 7.125 1"));

                Assert.That(text, Does.Contain("\"1.5\","));
                Assert.That(text, Does.Contain("\"-2.25\""));
            });
        }

        [Test]
        [SetCulture("de-DE")]
        public void TextRoundTripIsInvariant_DE() => AssertRoundTrip(KV2Text);

        [Test]
        [SetCulture("fa-IR")]
        public void TextRoundTripIsInvariant_FA() => AssertRoundTrip(KV2Text);

        [Test]
        [SetCulture("de-DE")]
        public void BinaryRoundTripIsInvariant_DE() => AssertRoundTrip(KV2Binary);

        [Test]
        [SetCulture("fa-IR")]
        public void BinaryRoundTripIsInvariant_FA() => AssertRoundTrip(KV2Binary);

        static void AssertRoundTrip(KVSerializer serializer)
        {
            var root = RoundTrip(serializer);

            Assert.Multiple(() =>
            {
                Assert.That((float)root["float"], Is.EqualTo(-2.25f));
                Assert.That((int)root["int"], Is.EqualTo(-1234567));
                Assert.That(root["byte"].GetValue<byte>(), Is.EqualTo(255));
                Assert.That((ulong)root["uint64"], Is.EqualTo(0xdf41645d6af564aUL));
                Assert.That(root["time"].GetValue<DmxTime>().Ticks, Is.EqualTo(15000));
                Assert.That(root["color"].GetValue<DmxColor>(), Is.EqualTo(ColorValue));
                Assert.That(root["vector2"].GetValue<Vector2>(), Is.EqualTo(Vector2Value));
                Assert.That(root["vector3"].GetValue<Vector3>(), Is.EqualTo(Vector3Value));
                Assert.That(root["vector4"].GetValue<Vector4>(), Is.EqualTo(Vector4Value));
                Assert.That(root["quaternion"].GetValue<Quaternion>(), Is.EqualTo(QuaternionValue));
                Assert.That(root["qangle"].GetValue<QAngle>(), Is.EqualTo(QAngleValue));
                Assert.That(root["matrix"].GetValue<Matrix4x4>(), Is.EqualTo(MatrixValue));
                Assert.That(root["floatArray"].GetArray<float>(), Is.EqualTo(ExpectedFloatArray));
            });
        }

        /// <summary>
        /// The reader has to parse invariant text no matter what culture it runs under, which a
        /// round trip alone would not catch if both sides moved together.
        /// </summary>
        [Test]
        [SetCulture("de-DE")]
        public void ReadsInvariantTextUnderAForeignCulture_DE() => AssertReadsInvariantText();

        [Test]
        [SetCulture("fa-IR")]
        public void ReadsInvariantTextUnderAForeignCulture_FA() => AssertReadsInvariantText();

        static void AssertReadsInvariantText()
        {
            const string document = """
                <!-- dmx encoding keyvalues2 4 format dmx 1 -->
                "DmElement"
                {
                	"id" "elementid" "00000000-0000-0000-0000-0000000000c1"
                	"name" "string" "root"
                	"float" "float" "-2.25"
                	"int" "int" "-1234567"
                	"time" "time" "1.5"
                	"vector3" "vector3" "1.5 -2.25 0.125"
                	"qangle" "qangle" "-1.5 22.25 -333.125"
                }
                """;

            using var stream = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(document), writable: false);
            var root = (KV2Element)KV2Text.Deserialize(stream).Root;

            Assert.Multiple(() =>
            {
                Assert.That((float)root["float"], Is.EqualTo(-2.25f));
                Assert.That((int)root["int"], Is.EqualTo(-1234567));
                Assert.That(root["time"].GetValue<DmxTime>().Ticks, Is.EqualTo(15000));
                Assert.That(root["vector3"].GetValue<Vector3>(), Is.EqualTo(Vector3Value));
                Assert.That(root["qangle"].GetValue<QAngle>(), Is.EqualTo(QAngleValue));
            });
        }
    }
}
