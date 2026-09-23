using System.Text;

namespace ValveKeyValue.Test
{
    class MalformedSyntaxTestCase
    {
        const string Kv3Header = "<!-- kv3 encoding:text:version{e21c7f3c-8a33-41c5-9977-a76d3a32aa0d} format:generic:version{7412167c-06e9-4698-aff2-e63eb59037e7} -->\n";

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
        public void MalformedKV3ThrowsKeyValueException(string body)
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(Kv3Header + body));

            Assert.That(
                () => KVSerializer.Create(KVSerializationFormat.KeyValues3Text).Deserialize(stream),
                Throws.TypeOf<KeyValueException>());
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
