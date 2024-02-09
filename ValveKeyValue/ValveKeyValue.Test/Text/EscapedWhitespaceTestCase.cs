namespace ValveKeyValue.Test
{
    class EscapedWhitespaceTestCase
    {
        [Test]
        public void ConvertsBackslashCharToActualRepresentation()
        {
            Assert.That((string)data["key"], Is.EqualTo("line1\nline2\tline2pt2"));
        }

        KVObject data;

        [OneTimeSetUp]
        public void SetUp()
        {
            var options = new KVSerializerOptions { HasEscapeSequences = true };
            using var stream = TestDataHelper.OpenResource("Text.escaped_whitespace.vdf");
            data = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(stream, options);
        }
    }
}
