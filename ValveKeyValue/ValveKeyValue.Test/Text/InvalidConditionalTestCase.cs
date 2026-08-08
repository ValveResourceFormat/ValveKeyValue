using System.Text;

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
        [TestCase("(")]
        [TestCase(")")]
        [TestCase("$ABC && ($DEF || $GHI")]
        [TestCase("$ABC && $DEF)")]
        [TestCase("1 2")]
        [TestCase("1$ABC")]
        [TestCase("$ABC 1")]
        [TestCase("1 !")]
        [TestCase("")]
        [TestCase(" ")]
        [TestCase("$ABC &&")]
        [TestCase("$ABC ||")]
        [TestCase("$ABC &")]
        [TestCase("$ABC |")]
        [TestCase("$ABC($DEF)")]
        [TestCase("$ABC &&&& $DEF")]
        public void ThrowsKeyValueException(string conditional)
        {
            var text = TestDataHelper.ReadTextResource("Text.invalid_conditional.vdf");
            text = text.Replace("{CONDITION}", conditional, StringComparison.Ordinal);

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));
            Assert.That(
                () => KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(stream),
                Throws.Exception.TypeOf<KeyValueException>()
                .With.InnerException.InstanceOf<InvalidOperationException>()
                .With.Message.EqualTo($"Invalid conditional syntax \"{conditional}\" at line 3, column 14."));
        }

        // Pathologically nested input must fail with a parse error rather than a StackOverflowException.
        [Test]
        public void ThrowsForDeeplyNestedConditional()
        {
            ThrowsKeyValueException(new string('(', 10000) + "$ABC" + new string(')', 10000));
            ThrowsKeyValueException(new string('!', 10000) + "$ABC");
        }
    }
}
