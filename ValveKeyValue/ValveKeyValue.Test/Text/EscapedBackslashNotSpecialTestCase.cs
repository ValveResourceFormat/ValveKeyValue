using NUnit.Framework;

namespace ValveKeyValue.Test
{
    class EscapedBackslashNotSpecialTestCase
    {
        [Test]
        public void ReadsDoubleBackslashQuoteCorrectly()
        {
            Assert.That((string)data["edge case"], Is.EqualTo(@"this is \"" quite fun"));
        }

        KVObject data;

        [OneTimeSetUp]
        public void SetUp()
        {
            using (var stream = TestDataHelper.OpenResource("Text.escaped_backslash_not_special.vdf"))
            {
                data = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(stream);
            }
        }
    }
}
