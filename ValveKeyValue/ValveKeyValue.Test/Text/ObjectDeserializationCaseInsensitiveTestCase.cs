namespace ValveKeyValue.Test
{
    class ObjectDeserializationCaseInsensitiveTestCase
    {
        [Test]
        public void CanDeserializeObjectListAndPropertyName()
        {
            var text = TestDataHelper.ReadTextResource("Text.serialization_expected.vdf");
            var options = new KVSerializerOptions { HasEscapeSequences = true };
            var actual = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize<DataObject[]>(text, options);

            Assert.That(actual[0].Name, Is.EqualTo("Dota 2"));
            Assert.That(actual[0].Summary, Is.EqualTo("Dota 2 is a complex game where you get sworn at\nin Russian all the time."));

            Assert.That(actual[1].Developer, Is.EqualTo("Valve Software"));
            Assert.That(actual[1].Summary, Is.EqualTo("Known as \"America's #1 war-themed hat simulator\", this game lets you wear stupid items while killing people."));
        }

        [Test]
        public void CanDeserializeFromRandomlyCasedKeys()
        {
            var text = TestDataHelper.ReadTextResource("Text.random_case_object.vdf");
            var options = new KVSerializerOptions { HasEscapeSequences = true };
            var actual = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize<DataObject>(text, options);

            Assert.That(actual.Name, Is.EqualTo("Dota 2"));
            Assert.That(actual.Summary, Is.EqualTo("Dota 2 is a complex game where you get sworn at\nin Russian all the time."));
            Assert.That(actual.Developer, Is.EqualTo("Valve Software"));
        }

        class DataObject
        {
            public string Name { get; set; }

            public string Developer { get; set; }

            [KVProperty("description")]
            public string Summary { get; set; }

            [KVIgnore]
            public string ExtraData { get; set; }
        }
    }
}
