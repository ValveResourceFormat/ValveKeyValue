using System.Linq;

namespace ValveKeyValue.Test
{
    class LookupTestCase
    {
        [Test]
        public void IsNotNullOrEmpty()
        {
            Assert.That(data, Is.Not.Null.Or.Empty);
        }

        [Test]
        public void LookupIsNotNull()
        {
            Assert.That(data.FooLookup, Is.Not.Null);
        }

        [Test]
        public void LookupHasTwoItems()
        {
            Assert.That(data.FooLookup, Has.Count.EqualTo(2));
        }

        [TestCase("Foo", new string[] { "I am Foo." })]
        [TestCase("Bar", new string[] { "First Bar", "Second Bar", "Third Bar" })]
        public void LookupItems(string key, string[] expectedValues)
        {
            var lookupValues = data.FooLookup[key].ToArray();
            Assert.That(lookupValues, Is.EquivalentTo(expectedValues));
        }

        ContainerClass data;

        [OneTimeSetUp]
        public void SetUp()
        {
            data = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize<ContainerClass>(TestDataHelper.ReadTextResource("Text.duplicate_keys_object.vdf"));
        }

        class ContainerClass
        {
            public ILookup<string, string> FooLookup { get; set; }
        }
    }
}
