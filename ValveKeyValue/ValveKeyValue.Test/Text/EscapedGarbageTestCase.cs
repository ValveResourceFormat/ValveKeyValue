using System.IO;
using NUnit.Framework;

namespace ValveKeyValue.Test
{
    class EscapedGarbageTestCase
    {
        [Test]
        public void ThrowsException()
        {
            using (var stream = TestDataHelper.OpenResource("Text.escaped_garbage.vdf"))
            {
                Assert.That(
                    () => KVSerializer.Deserialize(stream),
                    Throws.Exception.TypeOf<KeyValueException>()
                    .With.InnerException.TypeOf<InvalidDataException>()
                    .With.Message.EqualTo(@"Unknown escaped character '\7'."));
            }
        }
    }
}
