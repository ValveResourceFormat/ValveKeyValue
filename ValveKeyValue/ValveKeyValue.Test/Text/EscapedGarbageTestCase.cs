namespace ValveKeyValue.Test
{
    class EscapedGarbageTestCase
    {
        [Test]
        public void ReadsRawValueWhenNotHasEscapeSequences()
        {
            KVObject data;
            using (var stream = TestDataHelper.OpenResource("Text.escaped_garbage.vdf"))
            {
                data = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(stream);
            }

            Assert.That((string)data["key"], Is.EqualTo(@"abcd\7efg"));
        }

        [Test]
        public void ThrowsExceptionWhenHasEscapeSequences()
        {
            var options = new KVSerializerOptions { HasEscapeSequences = true };
            using var stream = TestDataHelper.OpenResource("Text.escaped_garbage.vdf");
            Assert.That(
                () => KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(stream, options),
                Throws.Exception.TypeOf<KeyValueException>()
                .With.InnerException.TypeOf<InvalidDataException>()
                .With.Message.EqualTo(@"Unknown escape sequence '\7' at line 3, column 14."));
        }

        [Test]
        public void ReadsValueWithNullByteWhenBugCompatibilityEnabled()
        {
            var options = new KVSerializerOptions
            {
                EnableValveNullByteBugBehavior = true,
                HasEscapeSequences = true,
            };

            KVObject data;
            using (var stream = TestDataHelper.OpenResource("Text.escaped_garbage.vdf"))
            {
                data = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(stream, options);
            }

            Assert.That((string)data["key"], Is.EqualTo("abcd"));
        }
    }
}
