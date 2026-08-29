using System.Numerics;

namespace ValveKeyValue.Test.TextKV2
{
    /// <summary>
    /// Every DMX array type through both codecs. DMX has one array type per scalar type, and each
    /// has its own wire encoding, so each needs exercising in both directions.
    /// </summary>
    class KV2EveryArrayTypeTestCase
    {
        static KVSerializer KV2Text => KVSerializer.Create(KVSerializationFormat.KeyValues2Text);
        static KVSerializer KV2Binary => KVSerializer.Create(KVSerializationFormat.KeyValues2Binary);

        static readonly Vector2[] ExpectedVec2 = [new(1, 2), new(3, 4)];
        static readonly Vector4[] ExpectedVec4 = [new(1, 2, 3, 4)];
        static readonly QAngle[] ExpectedQAngles = [new(10, 20, 30), new(40, 50, 60)];
        static readonly Quaternion[] ExpectedQuats = [new(0, 0, 0, 1)];
        static readonly Matrix4x4[] ExpectedMatrices = [Matrix4x4.Identity];
        static readonly byte[] ExpectedBytes = [1, 255];
        static readonly ulong[] ExpectedUInt64s = [1, ulong.MaxValue];
        static readonly byte[][] ExpectedBlobs = [[0xDE, 0xAD], [0xBE, 0xEF]];

        static KVObject ReadAllTypes()
        {
            using var stream = TestDataHelper.OpenResource("TextKV2.all_types.dmx");
            return KV2Text.Deserialize(stream).Root;
        }

        static KVObject RoundTrip(KVSerializer serializer)
        {
            using var stream = TestDataHelper.OpenResource("TextKV2.all_types.dmx");
            var doc = KV2Text.Deserialize(stream);

            using var output = new MemoryStream();
            serializer.Serialize(output, doc);
            output.Position = 0;

            return serializer.Deserialize(output).Root;
        }

        static void AssertEveryArray(KVObject root)
        {
            Assert.Multiple(() =>
            {
                Assert.That(root["vec2Array"].GetArray<Vector2>(), Is.EqualTo(ExpectedVec2));
                Assert.That(root["vec4Array"].GetArray<Vector4>(), Is.EqualTo(ExpectedVec4));
                Assert.That(root["qangleArray"].GetArray<QAngle>(), Is.EqualTo(ExpectedQAngles));
                Assert.That(root["quatArray"].GetArray<Quaternion>(), Is.EqualTo(ExpectedQuats));
                Assert.That(root["matrixArray"].GetArray<Matrix4x4>(), Is.EqualTo(ExpectedMatrices));
                Assert.That(root["uint8Array"].GetArray<byte>(), Is.EqualTo(ExpectedBytes));
                Assert.That(root["uint64Array"].GetArray<ulong>(), Is.EqualTo(ExpectedUInt64s));
                Assert.That(root["blobArray"].GetArray<byte[]>(), Is.EqualTo(ExpectedBlobs));
            });
        }

        [Test]
        public void TextReaderReadsEveryArrayType() => AssertEveryArray(ReadAllTypes());

        [Test]
        public void TextRoundTripsEveryArrayType() => AssertEveryArray(RoundTrip(KV2Text));

        [Test]
        public void BinaryRoundTripsEveryArrayType() => AssertEveryArray(RoundTrip(KV2Binary));

        /// <summary>
        /// uint8 and uint64 arrays only exist from binary version 9, so their presence has to
        /// drive the version selection.
        /// </summary>
        [Test]
        public void SourceTwoArrayTypesSelectVersionNine()
        {
            using var stream = TestDataHelper.OpenResource("TextKV2.all_types.dmx");
            var doc = KV2Text.Deserialize(stream);

            using var output = new MemoryStream();
            KV2Binary.Serialize(output, doc);
            output.Position = 0;

            Assert.That(KV2Binary.Deserialize(output).Header!.Encoding.Version, Is.EqualTo(9));
        }

        [Test]
        public void EveryArrayTypeHasADistinctValueType()
        {
            var root = ReadAllTypes();

            Assert.Multiple(() =>
            {
                Assert.That(root["vec2Array"].ValueType, Is.EqualTo(KVValueType.Vector2Array));
                Assert.That(root["vec4Array"].ValueType, Is.EqualTo(KVValueType.Vector4Array));
                Assert.That(root["qangleArray"].ValueType, Is.EqualTo(KVValueType.QAngleArray));
                Assert.That(root["quatArray"].ValueType, Is.EqualTo(KVValueType.QuaternionArray));
                Assert.That(root["matrixArray"].ValueType, Is.EqualTo(KVValueType.Matrix4x4Array));
                Assert.That(root["uint8Array"].ValueType, Is.EqualTo(KVValueType.ByteArray));
                Assert.That(root["uint64Array"].ValueType, Is.EqualTo(KVValueType.UInt64Array));
                Assert.That(root["blobArray"].ValueType, Is.EqualTo(KVValueType.BinaryBlobArray));
            });
        }
    }
}
