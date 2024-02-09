namespace ValveKeyValue.Test
{
    class DictionaryPropertyTestCase
    {
        [Test]
        public void IsNotNull()
        {
            Assert.That(data, Is.Not.Null);
        }

        [Test]
        public void NumbersIsNotNullEmpty()
        {
            Assert.That(data.Numbers, Is.Not.Null.Or.Empty);
        }

        [Test]
        public void HasNumbers()
        {
            Assert.That(data.Numbers, Has.Count.EqualTo(14));
        }

        [TestCase(0, "zero")]
        [TestCase(1, "one")]
        [TestCase(2, "two")]
        [TestCase(3, "three")]
        [TestCase(4, "four")]
        [TestCase(5, "five")]
        [TestCase(6, "six")]
        [TestCase(7, "seven")]
        [TestCase(8, "eight")]
        [TestCase(9, "nine")]
        [TestCase(10, "ten")]
        [TestCase(11, "eleven")]
        [TestCase(12, "twelve")]
        [TestCase(13, "thirteen")]
        public void NumbersHasValue(int key, string value)
        {
            Assert.That(data.Numbers[key], Is.EqualTo(value));
        }

        ContainerClass data;

        [OneTimeSetUp]
        public void SetUp()
        {
            using var stream = TestDataHelper.OpenResource("Text.list_of_values.vdf");
            data = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize<ContainerClass>(stream);
        }

        class ContainerClass
        {
            public Dictionary<int, string> Numbers { get; set; }
        }
    }
}
