using System.Collections.Generic;
using NUnit.Framework;

namespace ValveKeyValue.Test
{
    class DictionaryTestCase
    {
        [Test]
        public void IsNotNull()
        {
            Assert.That(data, Is.Not.Null);
        }

        [Test]
        public void IsNotEmpty()
        {
            Assert.That(data, Is.Not.Empty);
        }

        [TestCase("FirstName", ExpectedResult = "Bob")]
        [TestCase("LastName", ExpectedResult = "Builder")]
        [TestCase("CanFixIt", ExpectedResult = "1")]
        public string HasValueForKey(string key) => data[key];

        Dictionary<string, string> data;

        [OneTimeSetUp]
        public void SetUp()
        {
            using (var stream = TestDataHelper.OpenResource("Text.object_person.vdf"))
            {
                data = KVSerializer.Deserialize<Dictionary<string, string>>(stream);
            }
        }
    }
}
