using System.Text;

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
            var text = PerformNewLineTest(value, hasEscapeSequences: false);
            PerformNewLineTest(value, hasEscapeSequences: true);

            Assert.That(text, Does.Contain(value));
        }

        static string PerformNewLineTest(string value, bool hasEscapeSequences)
        {
            KVObject convertedKv;
            var kv = new KVObject("newLineTestCase", value);
            var options = new KVSerializerOptions { HasEscapeSequences = hasEscapeSequences };

            string text;
            using (var ms = new MemoryStream())
            {
                var serializer = KVSerializer.Create(KVSerializationFormat.KeyValues1Text);

                serializer.Serialize(ms, kv, options);

                ms.Seek(0, SeekOrigin.Begin);

                text = Encoding.ASCII.GetString(ms.ToArray(), 0, (int)ms.Length);

                convertedKv = serializer.Deserialize(ms, options);
            }

            Assert.That((string)convertedKv.Value, Is.EqualTo(value));

            return text;
        }
    }
}
