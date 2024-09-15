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
        public void IndexerOnValueNodeThrowsException()
        {
            Assert.That(
                () => data["foo"]["bar"],
                Throws.Exception.InstanceOf<NotSupportedException>()
                .With.Message.EqualTo("The indexer on a KVValue can only be used on a KVValue that has children."));
        }

        KVObject data;

        [OneTimeSetUp]
        public void SetUp()
        {
            data = new KVObject(
                "test data",
                [
                    new KVObject("foo", "bar"),
                    new KVObject("bar", "baz"),
                    new KVObject("baz", "-"),
                ]);
        }
    }
}
