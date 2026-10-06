namespace ValveKeyValue.Test
{
    class IncludeErrorsTestCase
    {
        [Test]
        public void IncludeInsideObjectIsKeyValuePair()
        {
            var text = @"""root""
{
#include ""foo.txt""
}
            ";

            var data = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(text);

            Assert.That((string)data["#include"], Is.EqualTo("foo.txt"));
        }

        [Test]
        public void BaseInsideObjectIsKeyValuePair()
        {
            var text = @"""root""
{
    #base ""foo.txt""
}
            ";

            var data = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(text);

            Assert.That((string)data["#base"], Is.EqualTo("foo.txt"));
        }

        [Test]
        public void UnquotedValueStartingWithHashIsString()
        {
            var data = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize("\"root\" { labelText #DOTA_Label }");

            Assert.That((string)data["labelText"], Is.EqualTo("#DOTA_Label"));
        }

        [TestCase("#include \"foo.txt\" \"root\" { }")]
        [TestCase("#INCLUDE \"foo.txt\" \"root\" { }")]
        [TestCase("\"#include\" \"foo.txt\" \"root\" { }")]
        [TestCase("#include\"foo.txt\" \"root\" { }")]
        [TestCase("\"root\" { } #include \"foo.txt\"")]
        public void IncludeOutsideRootObjectIsDirective(string text)
        {
            var options = new KVSerializerOptions { FileLoader = new StubIncludedFileLoader() };

            var data = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(text, options);

            Assert.That((string)data["included"], Is.EqualTo("yes"));
        }

        [TestCase("#base \"foo.txt\" \"root\" { }")]
        [TestCase("#Base \"foo.txt\" \"root\" { }")]
        [TestCase("\"root\" { } #base \"foo.txt\"")]
        public void BaseOutsideRootObjectIsDirective(string text)
        {
            var options = new KVSerializerOptions { FileLoader = new StubIncludedFileLoader() };

            var data = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(text, options);

            Assert.That((string)data["included"], Is.EqualTo("yes"));
        }

        [TestCase("#include", "Found end of file when another token type was expected at line 1, column 9.")]
        [TestCase("#include { }", "Attempted to begin new object while in state InDocumentBeforeIncludePath at line 1, column 10.")]
        [TestCase("#base \"\" \"root\" { }", "Found an inclusion directive with an empty file path at line 1, column 7.")]
        public void InvalidDirectiveThrows(string text, string message)
        {
            Assert.That(
                () => KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(text),
                Throws.InstanceOf<KeyValueException>()
                .With.Message.EqualTo(message));
        }

        sealed class StubIncludedFileLoader : IIncludedFileLoader
        {
            Stream IIncludedFileLoader.OpenFile(string filePath)
            {
                Assert.That(filePath, Is.EqualTo("foo.txt"));
                return new MemoryStream("\"foo\" { \"included\" \"yes\" }"u8.ToArray());
            }
        }
    }
}
