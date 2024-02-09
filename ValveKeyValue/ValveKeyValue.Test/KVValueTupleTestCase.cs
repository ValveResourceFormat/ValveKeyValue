namespace ValveKeyValue.Test
{
    class KVValueTupleTestCase
    {
        [Test]
        public void SymmetricSerialization2Element()
        {
            var expected = ("hello", "world");

            var serializer = KVSerializer.Create(KVSerializationFormat.KeyValues1Text);

            using var ms = new MemoryStream();
            serializer.Serialize(ms, expected, "root");
            ms.Seek(0, SeekOrigin.Begin);
            var actual = serializer.Deserialize<ValueTuple<string, string>>(ms);

            Assert.That(actual, Is.EqualTo(expected));
        }

        [Test]
        public void SymmetricSerialization10Element()
        {
            var expected = ("hello", "world", "this", "is", "quite", "a", "long", "tuple", "isn't", "it?");

            var serializer = KVSerializer.Create(KVSerializationFormat.KeyValues1Text);

            using var ms = new MemoryStream();
            serializer.Serialize(ms, expected, "root");
            ms.Seek(0, SeekOrigin.Begin);
            var actual = serializer.Deserialize<ValueTuple<string, string, string, string, string, string, string, ValueTuple<string, string, string>>>(ms);

            Assert.That(actual, Is.EqualTo(expected));
        }
    }
}
