namespace ValveKeyValue.Test.BinaryKV2
{
    /// <summary>
    /// Writing a document, reading it back and writing it again must produce the same bytes.
    /// Anything the writer decides per run rather than per document — element order, string table
    /// order, a regenerated identifier — shows up as a difference on the second pass.
    /// </summary>
    class KV2ReserializationTestCase
    {
        static readonly string[] Fixtures =
        [
            "TextKV2.all_types.dmx",
            "TextKV2.basic.dmx",
            "TextKV2.cs2_map.dmx",
            "TextKV2.reflectiontest.dmx",
            "TextKV2.shared_refs.dmx",
            "TextKV2.taunt05.dmx",
            "TextKV2.valve_layout.dmx",
            "Binary.binary4.dmx",
            "Binary.overboss_run.dmx",
            "Binary.taunt05_b5.dmx",
        ];

        static KVSerializationFormat FormatOf(string resource)
            => resource.StartsWith("Binary.", StringComparison.Ordinal)
                ? KVSerializationFormat.KeyValues2Binary
                : KVSerializationFormat.KeyValues2Text;

        static byte[] Serialize(KVSerializer serializer, KVDocument doc)
        {
            using var stream = new MemoryStream();
            serializer.Serialize(stream, doc);

            return stream.ToArray();
        }

        static KVDocument Deserialize(KVSerializer serializer, byte[] document)
        {
            using var stream = new MemoryStream(document, writable: false);

            return serializer.Deserialize(stream);
        }

        static void AssertStable(string resource, KVSerializationFormat format)
        {
            var serializer = KVSerializer.Create(format);

            using var source = TestDataHelper.OpenResource(resource);
            var first = Serialize(serializer, KVSerializer.Create(FormatOf(resource)).Deserialize(source));
            var second = Serialize(serializer, Deserialize(serializer, first));

            Assert.That(second, Is.EqualTo(first));
        }

        [TestCaseSource(nameof(Fixtures))]
        public void TextOutputIsStable(string resource) => AssertStable(resource, KVSerializationFormat.KeyValues2Text);

        [TestCaseSource(nameof(Fixtures))]
        public void BinaryOutputIsStable(string resource) => AssertStable(resource, KVSerializationFormat.KeyValues2Binary);
    }
}
