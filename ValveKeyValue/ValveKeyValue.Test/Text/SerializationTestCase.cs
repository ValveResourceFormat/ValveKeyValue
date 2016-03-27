using System.IO;
using NUnit.Framework;

namespace ValveKeyValue.Test.Text
{
    class SerializationTestCase
    {
        [Test]
        public void CreatesTextDocument()
        {
            var kv = new KVObject(
                "test data",
                new[]
                {
                    new KVObject(
                        "0",
                        new[]
                        {
                            new KVObject("developer", "Valve Software"),
                            new KVObject("name", "Dota 2"),
                            new KVObject("description", "Dota 2 is a complex game where you get sworn at\nin Russian all the time.")
                        }),
                    new KVObject(
                        "1",
                        new[]
                        {
                            new KVObject("developer", "Valve Software"),
                            new KVObject("name", "Team Fortress 2"),
                            new KVObject("description", "Known as \"America's #1 war-themed hat simulator\", this game lets you wear stupid items while killing people.")
                        })
                });

            string text;
            using (var ms = new MemoryStream())
            {
                KVSerializer.Serialize(ms, kv);

                ms.Seek(0, SeekOrigin.Begin);
                using (var reader = new StreamReader(ms))
                {
                    text = reader.ReadToEnd();
                }
            }

            var expected = TestDataHelper.ReadTextResource("Text.serialization_expected.vdf");
            Assert.That(text, Is.EqualTo(expected));
        }
    }
}