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
            using (var stream = TestDataHelper.OpenResource("Text.escaped_backslash.vdf"))
            {
                data = KVSerializer.Deserialize(stream);
            }
        }
    }
}
