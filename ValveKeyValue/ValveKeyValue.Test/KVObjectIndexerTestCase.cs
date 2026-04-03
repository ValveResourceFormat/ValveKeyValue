namespace ValveKeyValue.Test
{
    class KVObjectIndexerTestCase
    {
        [TestCase("foo", ExpectedResult = "bar")]
        [TestCase("bar", ExpectedResult = "baz")]
        [TestCase("baz", ExpectedResult = "-")]
        public string IndexerReturnsChildValue(string key) => (string)data[key];

        [Test]
        public void IndexerThrowsForMissingKey()
        {
            Assert.That(() => data["foobar"], Throws.TypeOf<KeyNotFoundException>());
        }

        [Test]
        public void IndexerOnValueNodeThrows()
        {
            Assert.That(() => data["foo"]["bar"], Throws.TypeOf<KeyNotFoundException>());
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
