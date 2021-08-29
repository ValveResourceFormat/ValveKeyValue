using NUnit.Framework;

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
            
            Assert.AreEqual("Dota 2", actual[0].Name);
            Assert.AreEqual("Dota 2 is a complex game where you get sworn at\nin Russian all the time.", actual[0].Summary);

            Assert.AreEqual("Valve Software", actual[1].Developer);
            Assert.AreEqual("Known as \"America's #1 war-themed hat simulator\", this game lets you wear stupid items while killing people.", actual[1].Summary);
        }

        [Test]
        public void CanDeserializeFromRandomlyCasedKeys()
        {
            var text = TestDataHelper.ReadTextResource("Text.random_case_object.vdf");
            var options = new KVSerializerOptions { HasEscapeSequences = true };
            var actual = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize<DataObject>(text, options);
            
            Assert.AreEqual("Dota 2", actual.Name);
            Assert.AreEqual("Dota 2 is a complex game where you get sworn at\nin Russian all the time.", actual.Summary);
            Assert.AreEqual("Valve Software", actual.Developer);
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
