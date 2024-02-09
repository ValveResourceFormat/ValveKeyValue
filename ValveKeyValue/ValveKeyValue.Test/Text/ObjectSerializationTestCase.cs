namespace ValveKeyValue.Test
{
    class ObjectSerializationTestCase
    {
        [Test]
        public void CreatesTextDocument()
        {
            var dataObject = new[]
            {
                new DataObject
                {
                    Developer = "Valve Software",
                    Name = "Dota 2",
                    Summary = "Dota 2 is a complex game where you get sworn at\nin Russian all the time.",
                    ExtraData = "Hidden Stuff Here"
                },
                new DataObject
                {
                    Developer = "Valve Software",
                    Name = "Team Fortress 2",
                    Summary = "Known as \"America's #1 war-themed hat simulator\", this game lets you wear stupid items while killing people.",
                    ExtraData = "More Secrets"
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
