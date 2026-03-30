namespace ValveKeyValue.Test
{
    class KVBasicObjectIndexerTestCase
    {
        [Test]
        public void IndexerOnValueNodeReturnsNull()
        {
            Assert.That(data["baz"], Is.Null);
        }

        KVObject data;

        [OneTimeSetUp]
        public void SetUp()
        {
            data = new KVObject("bar");
        }
    }
}
