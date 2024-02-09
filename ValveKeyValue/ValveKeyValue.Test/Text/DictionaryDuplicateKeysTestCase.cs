namespace ValveKeyValue.Test
{
    class DictionaryDuplicateKeysTestCase
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

        [Test]
        public void FirstValueWins()
        {
            Assert.That(data["foo"], Is.EqualTo("foo"));
        }

        Dictionary<string, string> data;

        [OneTimeSetUp]
        public void SetUp()
        {
            using var stream = TestDataHelper.OpenResource("Text.duplicate_keys.vdf");
            data = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize<Dictionary<string, string>>(stream);
        }
    }
}
