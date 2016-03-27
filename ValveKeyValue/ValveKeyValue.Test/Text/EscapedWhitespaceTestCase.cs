using NUnit.Framework;

namespace ValveKeyValue.Test
{
    class EscapedWhitespaceTestCase
    {
        [Test]
        public void ConvertsBackslashCharToActualRepresentation()
        {
            Assert.That((string)data["key"], Is.EqualTo("line1\nline2\tline2pt2"));
        }

        KVObject data;

        [OneTimeSetUp]
        public void SetUp()
        {
            using (var stream = TestDataHelper.OpenResource("Text.escaped_whitespace.vdf"))
            {
                data = KVSerializer.Deserialize(stream);
            }
        }
    }
}
