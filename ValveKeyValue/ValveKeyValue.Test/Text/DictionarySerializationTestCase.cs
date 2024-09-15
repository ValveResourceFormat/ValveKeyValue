namespace ValveKeyValue.Test
{
    class DictionarySerializationTestCase
    {
        [Test]
        public void CreatesTextDocument()
        {
            var dataObject = new[]
            {
                new Dictionary<string, string>
                {
                    ["description"] = "Dota 2 is a complex game where you get sworn at\nin Russian all the time.",
                    ["Developer"] = "Valve Software",
                    ["Name"] = "Dota 2"
                },

                new Dictionary<string, string>
                {
                    ["description"] = "Known as \"America's #1 war-themed hat simulator\", this game lets you wear stupid items while killing people.",
                    ["Developer"] = "Valve Software",
                    ["Name"] = "Team Fortress 2"
                },
            };

            string text;
            using (var ms = new MemoryStream())
            {
                KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Serialize(ms, dataObject, "test data");

                ms.Seek(0, SeekOrigin.Begin);
                using var reader = new StreamReader(ms);
                text = reader.ReadToEnd();
            }

            var expected = TestDataHelper.ReadTextResource("Text.serialization_expected.vdf");
            Assert.That(text, Is.EqualTo(expected));
        }

        [Test]
        public void SerializesValuesCorrectly()
        {
            var dataObject = new DataObject
            {
                Test = new Dictionary<string, float[]>
                {
                    ["test"] = [1.1234f, 2.2345f, 3.54677f],
                    ["test2"] = [1.1234f, 2.2345f, 3.54677f]
                },
            };

            string text;
            using (var ms = new MemoryStream())
            {
                KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Serialize(ms, dataObject, "test");

                ms.Seek(0, SeekOrigin.Begin);
                using var reader = new StreamReader(ms);
                text = reader.ReadToEnd();
            }

            var expected = TestDataHelper.ReadTextResource("Text.dictionary_with_array_values.vdf");
            Assert.That(text, Is.EqualTo(expected));
        }

        [Test]
        [SetCulture("pl-PL")]
        public void SerializesValuesCorrectlyWithCulture()
        {
            var dataObject = new DataObject
            {
                Test = new Dictionary<string, float[]>
                {
                    ["test"] = [1.1234f, 2.2345f, 3.54677f],
                    ["test2"] = [1.1234f, 2.2345f, 3.54677f]
                },
            };

            string text;
            using (var ms = new MemoryStream())
            {
                KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Serialize(ms, dataObject, "test");

                ms.Seek(0, SeekOrigin.Begin);
                using var reader = new StreamReader(ms);
                text = reader.ReadToEnd();
            }

            var expected = TestDataHelper.ReadTextResource("Text.dictionary_with_array_values.vdf");
            Assert.That(text, Is.EqualTo(expected));
        }

        [Test]
        public void DeserializesValuesCorrectly()
        {
            DataObject dataObject;

            using (var rs = TestDataHelper.OpenResource("Text.dictionary_with_array_values.vdf"))
            {
                dataObject = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize<DataObject>(rs);
            }

            Assert.That(dataObject, Is.Not.Null);
            Assert.That(dataObject.Test, Is.Not.Null);
            Assert.That(dataObject.Test, Has.Count.EqualTo(2));
            Assert.That(dataObject.Test["test"], Is.EqualTo(new[] { 1.1234f, 2.2345f, 3.54677f }));
            Assert.That(dataObject.Test["test2"], Is.EqualTo(new[] { 1.1234f, 2.2345f, 3.54677f }));
        }

        class DataObject
        {
            [KVProperty("test")]
            public Dictionary<string, float[]> Test { get; set; }
        }
    }
}
