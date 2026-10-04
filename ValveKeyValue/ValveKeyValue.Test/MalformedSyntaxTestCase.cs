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
        [TestCase("{ a = \"\"\" \nx\n\"\"\"\n}", TestName = "Kv3MultilineWithTextAfterOpenThrows")]
        [TestCase("{ a = \"\"\"\nx\\\n\"\"\"\n}", TestName = "Kv3MultilineCloseAfterBackslashThrows")]
        [TestCase("{ a = \"\"\"\nx\n\t\"\"\"\n}", TestName = "Kv3MultilineIndentedCloseThrows")]
        [TestCase("{ a 1 }", TestName = "Kv3MissingAssignmentThrows")]
        [TestCase("{ a == 1 }", TestName = "Kv3DoubleAssignmentThrows")]
        [TestCase("{ a = [1 2] }", TestName = "Kv3MissingArrayCommaThrows")]
        [TestCase("{ a = [1,,2] }", TestName = "Kv3DoubleArrayCommaThrows")]
        [TestCase("{ a = [ , ] }", TestName = "Kv3LeadingArrayCommaThrows")]
        [TestCase("{ a = [1,\"s\"#[01]] }", TestName = "Kv3GluedArrayElementsThrows")]
        [TestCase("{ a = [ [1][2] ] }", TestName = "Kv3GluedNestedArraysThrows")]
        [TestCase("{ a = [ {}{} ] }", TestName = "Kv3GluedNestedObjectsThrows")]
        [TestCase("{ a = #[ 0102 ] }", TestName = "Kv3JoinedBlobBytesThrows")]
        [TestCase("{ a = #[ 01,02 ] }", TestName = "Kv3CommaInBlobThrows")]
        [TestCase("{ a = #[ 01/* c */ ] }", TestName = "Kv3CommentGluedToBlobByteThrows")]
        [TestCase("{ a = #/* c */[ 01 ] }", TestName = "Kv3CommentGluedToBlobMarkerThrows")]
        [TestCase("{ a = foo }", TestName = "Kv3UnquotedStringThrows")]
        [TestCase("{ a = -x }", TestName = "Kv3UnquotedMinusStringThrows")]
        [TestCase("{ a = .x }", TestName = "Kv3UnquotedDotStringThrows")]
        [TestCase("{ a = 5abc }", TestName = "Kv3NumberWithSuffixThrows")]
        [TestCase("{ a = 1.2.3 }", TestName = "Kv3DoubleDecimalPointThrows")]
        [TestCase("{ a = 1.5e }", TestName = "Kv3MissingExponentThrows")]
        [TestCase("{ a = - }", TestName = "Kv3LoneMinusThrows")]
        [TestCase("{ a = e5 }", TestName = "Kv3ExponentOnlyThrows")]
        [TestCase("{ a = . }", TestName = "Kv3LoneDecimalPointThrows")]
        [TestCase("{ a = --5 }", TestName = "Kv3DoubleMinusThrows")]
        [TestCase("{ a = 0x10 }", TestName = "Kv3HexNumberThrows")]
        [TestCase("{ a = 1_000 }", TestName = "Kv3NumberWithUnderscoreThrows")]
        [TestCase("{ a = 0.1f }", TestName = "Kv3FloatSuffixThrows")]
        [TestCase("{ a = -nan }", TestName = "Kv3NegativeNanThrows")]
        [TestCase("{ a = infinity }", TestName = "Kv3InfinityWordThrows")]
        [TestCase("{ 1abc = 1 }", TestName = "Kv3KeyStartingWithDigitThrows")]
        [TestCase("{ a-b = 1 }", TestName = "Kv3UnquotedKeyWithDashThrows")]
        [TestCase("{ aé = 1 }", TestName = "Kv3NonAsciiKeyThrows")]
        [TestCase("{ a = é }", TestName = "Kv3NonAsciiValueThrows")]
        [TestCase("{ a = 1\u007f }", TestName = "Kv3DeleteCharacterThrows")]
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
