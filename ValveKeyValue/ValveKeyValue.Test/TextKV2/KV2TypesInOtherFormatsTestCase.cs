using System.Numerics;

namespace ValveKeyValue.Test.TextKV2
{
    /// <summary>
    /// The KeyValues1 and KeyValues3 serializers have no representation for the DMX only types,
    /// so they report them rather than writing something meaningless.
    /// </summary>
    class KV2TypesInOtherFormatsTestCase
    {
        public static IEnumerable<TestCaseData> DmxValues()
        {
            yield return new TestCaseData(new KVObject(new DmxColor(1, 2, 3, 4))).SetName("{m}(color)");
            yield return new TestCaseData(new KVObject(new DmxTime(1))).SetName("{m}(time)");
            yield return new TestCaseData(new KVObject(new Vector3(1, 2, 3))).SetName("{m}(vector3)");
            yield return new TestCaseData(new KVObject(new QAngle(1, 2, 3))).SetName("{m}(qangle)");
            yield return new TestCaseData(new KVObject(Matrix4x4.Identity)).SetName("{m}(matrix)");
            yield return new TestCaseData(KVObject.TypedArray(new List<int> { 1 })).SetName("{m}(int_array)");
            yield return new TestCaseData(KVObject.TypedArray(new List<string> { "a" })).SetName("{m}(string_array)");
        }

        [TestCaseSource(nameof(DmxValues))]
        public void KeyValues1TextRejectsDmxTypes(KVObject value)
            => AssertRejects(KVSerializationFormat.KeyValues1Text, value);

        [TestCaseSource(nameof(DmxValues))]
        public void KeyValues3TextRejectsDmxTypes(KVObject value)
            => AssertRejects(KVSerializationFormat.KeyValues3Text, value);

        static void AssertRejects(KVSerializationFormat format, KVObject value)
        {
            var root = KVObject.Collection();
            root.Add("value", value);

            using var stream = new MemoryStream();

            Assert.That(
                () => KVSerializer.Create(format).Serialize(stream, root, "root"),
                Throws.Exception);
        }
    }
}
