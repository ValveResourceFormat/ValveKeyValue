namespace ValveKeyValue.Test
{
    class KVBasicObjectIndexerTestCase
    {
        [Test]
        public void IndexerOnValueNodeThrows()
        {
            Assert.That(() => data["baz"], Throws.TypeOf<KeyNotFoundException>());
        }

        KVObject data;

        [OneTimeSetUp]
        public void SetUp()
        {
            data = new KVObject("bar");
        }
    }
}
