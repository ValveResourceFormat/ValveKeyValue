using System;
using System.IO;
using System.Text;
using NUnit.Framework;

namespace ValveKeyValue.Test
{
    class NewLinesTestCase
    {
        [TestCase("very\ngreat\nlines")]
        [TestCase(@"very\ngreat\nlines")]
        [TestCase("\nwould\ryou\r\nlook\r\nat these\nlines\r")]
        [TestCase(@"\nwould\ryou\r\nlook\r\nat these\nlines\r")]
        [TestCase("\n")]
        [TestCase(@"\n")]
        [TestCase("\r\n")]
        [TestCase(@"\r\n")]
        public void PreservesNewLines(string value)
        {
            var text = PerformNewLineTest(value, false);
            PerformNewLineTest(value, true);

            Assert.That(text, Does.Contain(value));
        }

        string PerformNewLineTest(string value, bool hasEscapeSequences)
        {
            KVObject convertedKv;
            var kv = new KVObject("newLineTestCase", value);
            var options = new KVSerializerOptions { HasEscapeSequences = hasEscapeSequences };

            string text;
            using (var ms = new MemoryStream())
            {
                KVSerializer.Serialize(ms, kv, options);

                ms.Seek(0, SeekOrigin.Begin);

                text = Encoding.ASCII.GetString(ms.GetBuffer(), 0, (int)ms.Length);

                convertedKv = KVSerializer.Deserialize(ms, options);
            }

            Assert.That((string)convertedKv.Value, Is.EqualTo(value));

            return text;
        }
    }
}
