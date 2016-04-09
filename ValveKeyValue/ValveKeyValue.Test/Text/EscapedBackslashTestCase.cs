using NUnit.Framework;

namespace ValveKeyValue.Test
{
    class EscapedBackslashTestCase
    {
        [Test]
        public void ConvertsDoubleBackslashToSingleBackslash()
        {
            Assert.That((string)data["key"], Is.EqualTo(@"back\slash"));
        }

        KVObject data;

        [OneTimeSetUp]
        public void SetUp()
        {
            var options = new KVSerializerOptions { HasEscapeSequences = true };
            using (var stream = TestDataHelper.OpenResource("Text.escaped_backslash.vdf"))
            {
                data = KVSerializer.Deserialize(stream, options);
            }
        }
    }
}
