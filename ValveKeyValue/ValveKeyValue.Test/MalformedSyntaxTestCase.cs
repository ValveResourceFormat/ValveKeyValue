using System.Text;

namespace ValveKeyValue.Test
{
    class MalformedSyntaxTestCase
    {
        [TestCase("", TestName = "Kv3EmptyDocumentThrows")]
        [TestCase("}", TestName = "Kv3LeadingObjectEndThrows")]
        [TestCase("]", TestName = "Kv3LeadingArrayEndThrows")]
        [TestCase("{ a = }", TestName = "Kv3MissingValueThrows")]
        [TestCase("{ = 1 }", TestName = "Kv3MissingKeyThrows")]
        [TestCase("{ a = 1, b = 2 }", TestName = "Kv3CommaInObjectThrows")]
        [TestCase("{ #[00] = 1 }", TestName = "Kv3BlobAsKeyThrows")]
        [TestCase("{ resource:a = 1 }", TestName = "Kv3FlaggedKeyThrows")]
        [TestCase("{ a = [1, 2 }", TestName = "Kv3MismatchedBracketThrows")]
        [TestCase("{ a = 1", TestName = "Kv3UnterminatedObjectThrows")]
        [TestCase("{ a = [1, 2", TestName = "Kv3UnterminatedArrayThrows")]
        [TestCase("{ a = 1; b = 2 }", TestName = "Kv3StraySemicolonThrows")]
        [TestCase("{ a = | }", TestName = "Kv3StrayPipeThrows")]
        [TestCase("{ a = 1 : }", TestName = "Kv3StrayColonThrows")]
        [TestCase("{ a = [ 1, : ] }", TestName = "Kv3StrayColonInArrayThrows")]
        [TestCase("{ a = \u0001 }", TestName = "Kv3ControlCharacterThrows")]
        [TestCase("{ a = /foo }", TestName = "Kv3SlashValueThrows")]
        [TestCase("{ a = int32:5 }", TestName = "Kv3UnknownFlagThrows")]
        [TestCase("   ", TestName = "Kv3WhitespaceOnlyDocumentThrows")]
        [TestCase("{ a = abc\\\"def }", TestName = "Kv3UnquotedTokenWithQuoteThrows")]
        [TestCase("{ a = R\"(raw)\" }", TestName = "Kv3RawStringLiteralThrows")]
        public void MalformedKV3ThrowsKeyValueException(string body)
        {
            Assert.That(() => TestDataHelper.ParseKV3Text(body), Throws.TypeOf<KeyValueException>());
        }

        [TestCase("\"a\" { \"b\" }", TestName = "Kv1MissingValueThrows")]
        [TestCase("\"a\" { \"b\" { }", TestName = "Kv1UnterminatedObjectThrows")]
        [TestCase("\"a\" { {", TestName = "Kv1ObjectWithoutKeyThrows")]
        [TestCase("\"a\"", TestName = "Kv1RootWithoutValueThrows")]
        public void MalformedKV1ThrowsKeyValueException(string text)
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));

            Assert.That(
                () => KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(stream),
                Throws.TypeOf<KeyValueException>());
        }
    }
}
