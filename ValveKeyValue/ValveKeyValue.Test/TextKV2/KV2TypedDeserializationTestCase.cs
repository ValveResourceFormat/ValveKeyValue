using System.Numerics;

namespace ValveKeyValue.Test.TextKV2
{
    /// <summary>
    /// Deserializing a DMX document into a typed object. Attribute names are matched case
    /// insensitively, so the camelCase names DMX uses line up with C# properties directly.
    /// </summary>
    class KV2TypedDeserializationTestCase
    {
        static KVSerializer KV2Text => KVSerializer.Create(KVSerializationFormat.KeyValues2Text);
        static KVSerializer KV2Binary => KVSerializer.Create(KVSerializationFormat.KeyValues2Binary);

        static readonly int[] ExpectedIntArray = [10, 20, 30];
        static readonly string[] ExpectedStringArray = ["alpha", "beta"];
        static readonly byte[] ExpectedBlob = [0xDE, 0xAD, 0xBE, 0xEF];

        class AllTypes
        {
            public int IntVal { get; set; }
            public float FloatVal { get; set; }
            public bool BoolTrue { get; set; }
            public string StringVal { get; set; } = string.Empty;
            public byte[] BinaryVal { get; set; } = [];
            public DmxTime TimeVal { get; set; }
            public DmxColor ColorVal { get; set; }
            public Vector2 Vec2Val { get; set; }
            public Vector3 Vec3Val { get; set; }
            public Vector4 Vec4Val { get; set; }
            public QAngle QAngleVal { get; set; }
            public Quaternion QuatVal { get; set; }
            public Matrix4x4 MatrixVal { get; set; }
            public byte Uint8Val { get; set; }
            public ulong Uint64Val { get; set; }

            public int[] IntArray { get; set; } = [];
            public List<string> StringArray { get; set; } = [];
            public List<Vector3> Vec3Array { get; set; } = [];
            public DmxColor[] ColorArray { get; set; } = [];
            public IReadOnlyList<DmxTime> TimeArray { get; set; } = [];
        }

        static AllTypes Read(KVSerializer serializer, Stream stream) => serializer.Deserialize<AllTypes>(stream);

        [Test]
        public void DeserializesScalarsIntoTypedObject()
        {
            using var stream = TestDataHelper.OpenResource("TextKV2.all_types.dmx");
            var obj = Read(KV2Text, stream);

            Assert.Multiple(() =>
            {
                Assert.That(obj.IntVal, Is.EqualTo(42));
                Assert.That(obj.FloatVal, Is.EqualTo(3.14f).Within(0.001f));
                Assert.That(obj.BoolTrue, Is.True);
                Assert.That(obj.StringVal, Is.EqualTo("hello"));
                Assert.That(obj.BinaryVal, Is.EqualTo(ExpectedBlob));
                Assert.That(obj.Uint8Val, Is.EqualTo(255));
                Assert.That(obj.Uint64Val, Is.EqualTo(ulong.MaxValue));
            });
        }

        [Test]
        public void DeserializesDmxStructsIntoTypedObject()
        {
            using var stream = TestDataHelper.OpenResource("TextKV2.all_types.dmx");
            var obj = Read(KV2Text, stream);

            Assert.Multiple(() =>
            {
                Assert.That(obj.TimeVal, Is.EqualTo(new DmxTime(12345)));
                Assert.That(obj.ColorVal, Is.EqualTo(new DmxColor(255, 128, 0, 200)));
                Assert.That(obj.Vec2Val, Is.EqualTo(new Vector2(1.5f, 2.5f)));
                Assert.That(obj.Vec3Val, Is.EqualTo(new Vector3(1, 2, 3)));
                Assert.That(obj.Vec4Val, Is.EqualTo(new Vector4(1, 2, 3, 4)));
                Assert.That(obj.QAngleVal, Is.EqualTo(new QAngle(10, 20, 30)));
                Assert.That(obj.QuatVal, Is.EqualTo(new Quaternion(0.1f, 0.2f, 0.3f, 0.9f)));
                Assert.That(obj.MatrixVal, Is.EqualTo(Matrix4x4.Identity));
            });
        }

        [Test]
        public void DeserializesTypedArraysIntoTypedObject()
        {
            using var stream = TestDataHelper.OpenResource("TextKV2.all_types.dmx");
            var obj = Read(KV2Text, stream);

            Assert.Multiple(() =>
            {
                Assert.That(obj.IntArray, Is.EqualTo(ExpectedIntArray));
                Assert.That(obj.StringArray, Is.EqualTo(ExpectedStringArray));
                Assert.That(obj.Vec3Array, Is.EqualTo(new[] { new Vector3(1, 2, 3), new Vector3(4, 5, 6) }));
                Assert.That(obj.ColorArray, Is.EqualTo(new[] { new DmxColor(255, 0, 0, 255), new DmxColor(0, 255, 0, 128) }));
                Assert.That(obj.TimeArray, Is.EqualTo(new[] { new DmxTime(100), new DmxTime(200) }));
            });
        }

        /// <summary>
        /// The same document read through the binary codec has to produce the same object.
        /// </summary>
        [Test]
        public void DeserializesTheSameFromBinary()
        {
            using var textStream = TestDataHelper.OpenResource("TextKV2.all_types.dmx");
            var doc = KV2Text.Deserialize(textStream);

            using var binaryStream = new MemoryStream();
            KV2Binary.Serialize(binaryStream, doc);
            binaryStream.Position = 0;

            var obj = Read(KV2Binary, binaryStream);

            Assert.Multiple(() =>
            {
                Assert.That(obj.IntVal, Is.EqualTo(42));
                Assert.That(obj.Vec3Val, Is.EqualTo(new Vector3(1, 2, 3)));
                Assert.That(obj.IntArray, Is.EqualTo(ExpectedIntArray));
                Assert.That(obj.StringArray, Is.EqualTo(ExpectedStringArray));
                Assert.That(obj.TimeVal, Is.EqualTo(new DmxTime(12345)));
            });
        }

        class WithNestedElement
        {
            public int IntVal { get; set; }
            public InlineChild InlineChild { get; set; } = new();
        }

        class InlineChild
        {
            public int Value { get; set; }
            public Nested Nested { get; set; } = new();
        }

        class Nested
        {
            public int Deep { get; set; }
        }

        [Test]
        public void DeserializesNestedElementsIntoTypedObjects()
        {
            using var stream = TestDataHelper.OpenResource("TextKV2.all_types.dmx");
            var obj = KV2Text.Deserialize<WithNestedElement>(stream);

            Assert.Multiple(() =>
            {
                Assert.That(obj.IntVal, Is.EqualTo(42));
                Assert.That(obj.InlineChild.Value, Is.EqualTo(99));
                Assert.That(obj.InlineChild.Nested.Deep, Is.EqualTo(7));
            });
        }
    }
}
