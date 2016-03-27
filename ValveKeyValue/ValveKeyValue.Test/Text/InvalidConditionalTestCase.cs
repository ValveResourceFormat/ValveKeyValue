using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Text;
using NUnit.Framework;

namespace ValveKeyValue.Test
{
    class InvalidConditionalTestCase
    {
        [TestCase("$ABC | $DEF")]
        [TestCase("$ABC & $DEF")]
        [TestCase("$ABC &| $DEF")]
        [TestCase("$ABC |& $DEF")]
        [TestCase("$ABC ! $DEF")]
        [TestCase("!")]
        [TestCase("&&")]
        [TestCase("||")]
        [TestCase("()")]
        [TestCase("$ABC & ()")]
        [TestCase("$ABC && (!)")]
        [TestCase("$ABC && ($DEF!)")]
        [TestCase("$DEF!")]
        [SuppressMessage("Microsoft.Usage", "CA2202:Do not dispose objects multiple times", Justification = "It's fine to dispose something multiple times.")]
        public void ThrowsInvalidDataException(string conditional)
        {
            string text;
            using (var stream = TestDataHelper.OpenResource("Text.invalid_conditional.vdf"))
            using (var reader = new StreamReader(stream))
            {
                text = reader.ReadToEnd();
            }

            text = text.Replace("{CONDITION}", conditional);
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(text)))
            {
                Assert.That(
                    () => KVSerializer.Deserialize(stream),
                    Throws.Exception.InstanceOf<InvalidDataException>()
                    .With.Message.EqualTo($"Invalid conditional syntax \"{conditional}\""));
            }
        }
    }
}
