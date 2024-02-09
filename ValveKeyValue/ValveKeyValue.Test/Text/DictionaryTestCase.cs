namespace ValveKeyValue.Test
{
    [TestFixture(typeof(StreamKVTextReader))]
    [TestFixture(typeof(StringKVTextReader))]
    class DictionaryTestCase<TReader>
        where TReader : IKVTextReader, new()
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
            data = new TReader().Read<Dictionary<string, string>>("Text.object_person.vdf");
        }
    }
}
