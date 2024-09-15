namespace ValveKeyValue.Test
{
    class SerializationTestCase
    {
        [Test]
        public void CreatesTextDocument()
        {
            var kv = new KVObject(
                "test data",
                [
                    new KVObject(
                        "0",
                        [
                            new KVObject("description", "Dota 2 is a complex game where you get sworn at\nin Russian all the time."),
                            new KVObject("Developer", "Valve Software"),
                            new KVObject("Name", "Dota 2")
                        ]),
                    new KVObject(
                        "1",
                        [
                            new KVObject("description", "Known as \"America's #1 war-themed hat simulator\", this game lets you wear stupid items while killing people."),
                            new KVObject("Developer", "Valve Software"),
                            new KVObject("Name", "Team Fortress 2")
                        ])
                ]);

            string text;
            using (var ms = new MemoryStream())
            {
                KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Serialize(ms, kv);

                Assert.That(ms.CanRead, Is.True);
                ms.Seek(0, SeekOrigin.Begin);
                using var reader = new StreamReader(ms);
                text = reader.ReadToEnd();
            }

            var expected = TestDataHelper.ReadTextResource("Text.serialization_expected.vdf");
            Assert.That(text, Is.EqualTo(expected));
        }
    }
}
