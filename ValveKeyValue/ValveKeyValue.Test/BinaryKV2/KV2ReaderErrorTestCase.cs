namespace ValveKeyValue.Test.BinaryKV2
{
    /// <summary>
    /// Malformed input has to surface as <see cref="KeyValueException"/> rather than whatever the
    /// parsing primitives happen to throw.
    /// </summary>
    class KV2ReaderErrorTestCase
    {
        static KVSerializer KV2Text => KVSerializer.Create(KVSerializationFormat.KeyValues2Text);
        static KVSerializer KV2Binary => KVSerializer.Create(KVSerializationFormat.KeyValues2Binary);

        static MemoryStream TextStream(string text) => new(System.Text.Encoding.UTF8.GetBytes(text));

        /// <summary>
        /// An odd number of hex characters makes the blob parser throw
        /// <see cref="InvalidDataException"/>, which the reader has to wrap.
        /// </summary>
        [TestCase("\"blob\" \"binary\" \"DEADBEE\"")]
        [TestCase("\"blobs\" \"binary_array\"\n\t[\n\t\t\"ABC\"\n\t]")]
        public void WrapsOddLengthHexBlob(string attribute)
        {
            var text = $$"""
                <!-- dmx encoding keyvalues2 1 format dmx 1 -->
                "DmElement"
                {
                	"id" "elementid" "11111111-1111-1111-1111-111111111111"
                	{{attribute}}
                }
                """;

            using var stream = TextStream(text);

            Assert.That(() => KV2Text.Deserialize(stream), Throws.InstanceOf<KeyValueException>());
        }

        [Test]
        public void WrapsUnparseableNumber()
        {
            const string Text = """
                <!-- dmx encoding keyvalues2 1 format dmx 1 -->
                "DmElement"
                {
                	"id" "elementid" "11111111-1111-1111-1111-111111111111"
                	"count" "int" "not a number"
                }
                """;

            using var stream = TextStream(Text);

            Assert.That(() => KV2Text.Deserialize(stream), Throws.InstanceOf<KeyValueException>());
        }

        [Test]
        public void WrapsOutOfRangeNumber()
        {
            const string Text = """
                <!-- dmx encoding keyvalues2 1 format dmx 1 -->
                "DmElement"
                {
                	"id" "elementid" "11111111-1111-1111-1111-111111111111"
                	"count" "int" "99999999999999999999"
                }
                """;

            using var stream = TextStream(Text);

            Assert.That(() => KV2Text.Deserialize(stream), Throws.InstanceOf<KeyValueException>());
        }

        /// <summary>
        /// Valve requires the encoding name to match the codec reading the file. Without the check
        /// a text document handed to the binary reader misparses instead of reporting a mismatch.
        /// </summary>
        [Test]
        public void BinaryReaderRejectsTextEncodingName()
        {
            using var stream = new DmxBinaryBuilder()
                .RawHeader("<!-- dmx encoding keyvalues2 1 format dmx 1 -->")
                .Int(1).Str("DmElement")
                .Int(1)
                .Int(0).Int(0).Id(Guid.NewGuid())
                .Int(0)
                .ToStream();

            Assert.That(
                () => KV2Binary.Deserialize(stream),
                Throws.InstanceOf<KeyValueException>().With.Message.Contains("keyvalues2"));
        }

        [Test]
        public void TextReaderRejectsBinaryEncodingName()
        {
            using var stream = TextStream("<!-- dmx encoding binary 5 format dmx 1 -->\n");

            Assert.That(
                () => KV2Text.Deserialize(stream),
                Throws.InstanceOf<KeyValueException>().With.Message.Contains("binary"));
        }

        [Test]
        public void TextReaderAcceptsEveryTextEncodingVariant([Values("keyvalues2", "keyvalues2_flat", "keyvalues2_noids")] string encoding)
        {
            var text = $$"""
                <!-- dmx encoding {{encoding}} 1 format dmx 1 -->
                "DmElement"
                {
                	"id" "elementid" "11111111-1111-1111-1111-111111111111"
                }
                """;

            using var stream = TextStream(text);

            Assert.That(((KV2Element)KV2Text.Deserialize(stream).Root).ClassName, Is.EqualTo("DmElement"));
        }

        [Test]
        public void BinaryReaderAcceptsSequentialIdsEncodingName()
        {
            using var stream = new DmxBinaryBuilder()
                .Header("binary_seqids", 9)
                .Int(0) // no prefix containers
                .Int(1).Str("DmElement")
                .Int(1)
                .Int(0).Int(0).Id(Guid.NewGuid())
                .Int(0)
                .ToStream();

            Assert.That(((KV2Element)KV2Binary.Deserialize(stream).Root).ClassName, Is.EqualTo("DmElement"));
        }
    }
}
