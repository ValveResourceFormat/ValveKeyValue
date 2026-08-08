namespace ValveKeyValue.Test
{
    class EscapedBackslashNoEscapeTestCase
    {
        [Test]
        public void ParsesStringWithBackwardsSlashAtTheEnd()
        {
            Assert.That((string)data["BuildOutput"], Is.EqualTo(@"..\output\"));
        }

        [Test]
        public void KeepsBackslashesLiteralIncludingBeforeClosingQuote()
        {
            var options = new KVSerializerOptions { HasEscapeSequences = false };

            KVObject doubleBackslashData;
            using (var stream = TestDataHelper.OpenResource("Text.escaped_backslash.vdf"))
            {
                doubleBackslashData = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(stream, options);
            }

            Assert.That((string)doubleBackslashData["key"], Is.EqualTo(@"back\\slash"));
            Assert.That((string)doubleBackslashData["edge case"], Is.EqualTo(@"this is fun\\"));
        }

        KVObject data;

        [OneTimeSetUp]
        public void SetUp()
        {
            var options = new KVSerializerOptions { HasEscapeSequences = false };
            using var stream = TestDataHelper.OpenResource("Text.escaped_backslash_single_slash.vdf");
            data = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(stream, options).Root;
        }
    }
}
