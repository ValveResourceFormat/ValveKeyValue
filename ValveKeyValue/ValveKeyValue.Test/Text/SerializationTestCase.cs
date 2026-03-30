namespace ValveKeyValue.Test
{
    class SerializationTestCase
    {
        [Test]
        public void CreatesTextDocument()
        {
            var item0 = KVObject.ListCollection();
            item0.Add("description", "Dota 2 is a complex game where you get sworn at\nin Russian all the time.");
            item0.Add("Developer", "Valve Software");
            item0.Add("Name", "Dota 2");

            var item1 = KVObject.ListCollection();
            item1.Add("description", "Known as \"America's #1 war-themed hat simulator\", this game lets you wear stupid items while killing people.");
            item1.Add("Developer", "Valve Software");
            item1.Add("Name", "Team Fortress 2");

            var kv = KVObject.ListCollection();
            kv.Add("0", item0);
            kv.Add("1", item1);
            var doc = new KVDocument(null, "test data", kv);

            string text;
            using (var ms = new MemoryStream())
            {
                KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Serialize(ms, doc);

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
