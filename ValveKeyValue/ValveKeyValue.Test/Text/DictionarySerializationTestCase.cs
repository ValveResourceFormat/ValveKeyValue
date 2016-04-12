using System.Collections.Generic;
using System.IO;
using NUnit.Framework;

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
                    ["developer"] = "Valve Software",
                    ["name"] = "Dota 2"
                },

                new Dictionary<string, string>
                {
                    ["description"] = "Known as \"America's #1 war-themed hat simulator\", this game lets you wear stupid items while killing people.",
                    ["developer"] = "Valve Software",
                    ["name"] = "Team Fortress 2"
                },
            };

            string text;
            using (var ms = new MemoryStream())
            {
                KVSerializer.Serialize(ms, dataObject, "test data");

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
