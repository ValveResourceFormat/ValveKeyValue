namespace ValveKeyValue.Test
{
    class KVObjectIndexerTestCase
    {
        [TestCase("foo", ExpectedResult = "bar")]
        [TestCase("bar", ExpectedResult = "baz")]
        [TestCase("baz", ExpectedResult = "-")]
        [TestCase("foobar", ExpectedResult = null)]
        public string IndexerReturnsChildValue(string key) => (string)data[key];

        [Test]
        public void IndexerOnValueNodeReturnsNull()
        {
            Assert.That(data["foo"]["bar"], Is.Null);
        }

        KVObject data;

        [OneTimeSetUp]
        public void SetUp()
        {
            data = KVObject.ListCollection();
            data.Add("foo", "bar");
            data.Add("bar", "baz");
            data.Add("baz", "-");
        }
    }
}
