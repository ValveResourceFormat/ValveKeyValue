using System.IO;
using NUnit.Framework;

namespace ValveKeyValue.Test
{
    class EscapedGarbageTestCase
    {
        [Test]
        public void ReadsRawValueWhenNotHasEscapeSequences()
        {
            KVObject data;
            using (var stream = TestDataHelper.OpenResource("Text.escaped_garbage.vdf"))
            {
                data = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(stream);
            }

            Assert.That((string)data["key"], Is.EqualTo(@"\7"));
        }

        [Test]
        public void ThrowsExceptionWhenHasEscapeSequences()
        {
            var options = new KVSerializerOptions { HasEscapeSequences = true };
            using (var stream = TestDataHelper.OpenResource("Text.escaped_garbage.vdf"))
            {
                Assert.That(
                    () => KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(stream, options),
                    Throws.Exception.TypeOf<KeyValueException>()
                    .With.InnerException.TypeOf<InvalidDataException>()
                    .With.Message.EqualTo(@"Unknown escaped character '\7'."));
            }
        }
    }
}
