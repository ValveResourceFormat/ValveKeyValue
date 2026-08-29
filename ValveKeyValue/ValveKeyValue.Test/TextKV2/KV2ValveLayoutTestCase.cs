using System.Globalization;
using System.Numerics;

namespace ValveKeyValue.Test.TextKV2
{
    /// <summary>
    /// Reads a fixture written the way Valve writes keyvalues2: multi line matrix and binary
    /// values, comments, whitespace runs in the header, the full escape set, and the spellings
    /// the engine side writer uses.
    /// </summary>
    class KV2ValveLayoutTestCase
    {
        static KVSerializer KV2Text => KVSerializer.Create(KVSerializationFormat.KeyValues2Text);
        static readonly Guid ExternalId = Guid.Parse("ffffffff-ffff-ffff-ffff-ffffffffffff");
        static readonly byte[] ExpectedBlob = [0xDE, 0xAD, 0xBE, 0xEF, 0x01, 0x02, 0x03, 0x04, 0x05];
        static readonly byte[] ExpectedFirstBlobItem = [0xAA, 0xBB];
        static readonly byte[] ExpectedSecondBlobItem = [0xCC, 0xDD];

        KVDocument doc = null!;
        KV2Element root = null!;

        [OneTimeSetUp]
        public void SetUp()
        {
            using var stream = TestDataHelper.OpenResource("TextKV2.valve_layout.dmx");
            doc = KV2Text.Deserialize(stream);
            root = (KV2Element)doc.Root;
        }

        [Test]
        public void ParsesHeaderWithWhitespaceRuns()
        {
            Assert.Multiple(() =>
            {
                Assert.That(doc.Header!.Encoding.Name, Is.EqualTo("keyvalues2"));
                Assert.That(doc.Header.Encoding.Version, Is.EqualTo(1));
                Assert.That(doc.Header.Format.Name, Is.EqualTo("dmx"));
                Assert.That(doc.Header.Format.Version, Is.EqualTo(1));
            });
        }

        [Test]
        public void ReadsIdThatIsNotTheFirstAttribute()
        {
            Assert.Multiple(() =>
            {
                Assert.That(root.ElementId, Is.EqualTo(Guid.Parse("aaaaaaaa-0000-0000-0000-000000000001")));
                Assert.That(root.Name, Is.EqualTo("valve layout"));
            });
        }

        [Test]
        public void ReadsFullEscapeSet()
        {
            Assert.That((string)root["apostrophe"], Is.EqualTo("it's a ? \"quote\""));
        }

        [Test]
        public void ReadsMultiLineMatrix()
        {
            var matrix = root["transform"];

            Assert.That(matrix.ValueType, Is.EqualTo(KVValueType.Matrix4x4));
            Assert.That(matrix.GetValue<Matrix4x4>(), Is.EqualTo(new Matrix4x4(
                1, 0, 0, 0,
                0, 1, 0, 0,
                0, 0, 1, 0,
                5, 6, 7, 1)));
        }

        [Test]
        public void ReadsMultiLineBinaryBlob()
        {
            Assert.That(root["blob"].AsBlob(), Is.EqualTo(ExpectedBlob));
        }

        [Test]
        public void ReadsBinaryArrayWithWrappedItems()
        {
            var blobs = root["blobs"].GetArray<byte[]>();

            Assert.That(blobs, Has.Count.EqualTo(2));

            Assert.Multiple(() =>
            {
                Assert.That(blobs[0], Is.EqualTo(ExpectedFirstBlobItem));
                Assert.That(blobs[1], Is.EqualTo(ExpectedSecondBlobItem));
            });
        }

        [Test]
        public void ReadsUnknownTypeAsNullElementReference()
        {
            Assert.That(root["nothing"], Is.SameAs(KV2Element.Null));
        }

        [Test]
        public void ReadsUnresolvedReferenceAsStub()
        {
            var external = (KV2Element)root["external"];

            Assert.Multiple(() =>
            {
                Assert.That(external.IsStub, Is.True);
                Assert.That(external.ElementId, Is.EqualTo(ExternalId));
            });
        }

        [Test]
        public void ReadsAnyNonZeroIntegerAsTrue()
        {
            Assert.That((bool)root["truthy"], Is.True);
        }

        [Test]
        public void ReadsTypeNamesCaseInsensitively()
        {
            Assert.That(root["mixedCase"].ValueType, Is.EqualTo(KVValueType.Vector3));
            Assert.That(root["mixedCase"].ToString(CultureInfo.InvariantCulture), Is.EqualTo("1 2 3"));
        }

        [Test]
        public void ReadsTimeAsSeconds()
        {
            Assert.That(root["seconds"].GetValue<DmxTime>().Ticks, Is.EqualTo(15000));
        }

        [Test]
        public void ReadsHexUInt64()
        {
            Assert.That((ulong)root["handle"], Is.EqualTo(0xdf41645d6af564aUL));
        }

        [Test]
        public void IgnoresElementIdAttributesOtherThanId()
        {
            Assert.That(root.ContainsKey("stray"), Is.False);
        }

        [Test]
        public void RoundTripsThroughText()
        {
            using var output = new MemoryStream();
            KV2Text.Serialize(output, doc);
            output.Position = 0;

            var root2 = (KV2Element)KV2Text.Deserialize(output).Root;

            Assert.Multiple(() =>
            {
                Assert.That((string)root2["apostrophe"], Is.EqualTo("it's a ? \"quote\""));
                Assert.That(root2["transform"].GetValue<Matrix4x4>(), Is.EqualTo(root["transform"].GetValue<Matrix4x4>()));
                Assert.That(root2["blob"].AsBlob(), Is.EqualTo(ExpectedBlob));
                Assert.That(root2["blobs"].GetArray<byte[]>()[0], Is.EqualTo(ExpectedFirstBlobItem));
                Assert.That(root2["seconds"].GetValue<DmxTime>().Ticks, Is.EqualTo(15000));
                Assert.That((ulong)root2["handle"], Is.EqualTo(0xdf41645d6af564aUL));
                Assert.That(((KV2Element)root2["external"]).IsStub, Is.True);
                Assert.That(root2["nothing"], Is.SameAs(KV2Element.Null));
            });
        }

        [Test]
        public void RoundTripsThroughBinary()
        {
            var binary = KVSerializer.Create(KVSerializationFormat.KeyValues2Binary);

            using var output = new MemoryStream();
            binary.Serialize(output, doc);
            output.Position = 0;

            var root2 = (KV2Element)binary.Deserialize(output).Root;

            Assert.Multiple(() =>
            {
                Assert.That((string)root2["apostrophe"], Is.EqualTo("it's a ? \"quote\""));
                Assert.That(root2["blob"].AsBlob(), Is.EqualTo(ExpectedBlob));
                Assert.That(root2["seconds"].GetValue<DmxTime>().Ticks, Is.EqualTo(15000));
                Assert.That((ulong)root2["handle"], Is.EqualTo(0xdf41645d6af564aUL));
                Assert.That(((KV2Element)root2["external"]).ElementId, Is.EqualTo(ExternalId));
            });
        }
    }
}
