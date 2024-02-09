namespace ValveKeyValue.Test
{
    class EscapedBackslashTestCase
    {
        [Test]
        public void ConvertsDoubleBackslashToSingleBackslash()
        {
            Assert.That((string)data["key"], Is.EqualTo(@"back\slash"));
        }

        [Test]
        public void DoubleBackslashQuoteEscapesJustTheBackslashNotTheQuote()
        {
            Assert.That((string)data["edge case"], Is.EqualTo(@"this is fun\"));
        }

        KVObject data;

        [OneTimeSetUp]
        public void SetUp()
        {
            var options = new KVSerializerOptions { HasEscapeSequences = true };
            using var stream = TestDataHelper.OpenResource("Text.escaped_backslash.vdf");
            data = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(stream, options);
        }
    }
}
