using NUnit.Framework;

namespace ValveKeyValue.Test
{
    class ConditionalTestCase
    {
        [Test]
        public void ReadsValueWhenConditionalEqual()
        {
            var conditions = new[] { "WIN32" };
            KVObject data;
            using (var stream = TestDataHelper.OpenResource("Text.conditional.vdf"))
            {
                data = KVSerialiser.Deserialize(stream, conditions);
            }

            Assert.That((string)data["operating system"], Is.EqualTo("windows"));
        }

        [TestCase(null)]
        [TestCase("OSX")]
        [TestCase("LINUX")]
        [TestCase("PS3")]
        public void ReadsValueWhenConditionalNotEqual(string condition)
        {
            string[] conditions;
            if (condition == null)
            {
                conditions = new string[0];
            }
            else
            {
                conditions = new[] { condition };
            }

            KVObject data;
            using (var stream = TestDataHelper.OpenResource("Text.conditional.vdf"))
            {
                data = KVSerialiser.Deserialize(stream, conditions);
            }

            Assert.That((string)data["operating system"], Is.EqualTo("something else"));
        }
    }
}
