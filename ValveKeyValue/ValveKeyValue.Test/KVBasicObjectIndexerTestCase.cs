namespace ValveKeyValue.Test
{
    class KVBasicObjectIndexerTestCase
    {
        [Test]
        public void IndexerOnValueNodeThrowsException()
        {
            Assert.That(
                () => data["baz"],
                Throws.Exception.InstanceOf<InvalidOperationException>()
                .With.Message.EqualTo("This operation on a KVObject can only be used when the value has children."));
        }

        KVObject data;

        [OneTimeSetUp]
        public void SetUp()
        {
            data = new KVObject("foo", "bar");
        }
    }
}
