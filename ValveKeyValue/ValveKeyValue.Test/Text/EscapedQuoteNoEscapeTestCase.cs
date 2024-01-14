using NUnit.Framework;

namespace ValveKeyValue.Test
{
    class EscapedQuoteNoEscapeTestCase
    {
        [Test]
        public void ParsesStringWithBackwardsSlashWithQuoteAtTheEnd()
        {
            Assert.That((string)data["key"], Is.EqualTo("some value\\\""));
            Assert.That((string)data["foo"], Is.EqualTo("bar"));
        }

        KVObject data;

        [OneTimeSetUp]
        public void SetUp()
        {
            var options = new KVSerializerOptions { HasEscapeSequences = false };
            using var stream = TestDataHelper.OpenResource("Text.escaped_ending_quote.vdf");
            data = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(stream, options);
        }
    }
}
