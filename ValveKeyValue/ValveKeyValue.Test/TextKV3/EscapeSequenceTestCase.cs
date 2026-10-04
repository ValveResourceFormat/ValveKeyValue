namespace ValveKeyValue.Test.TextKV3
{
    class EscapeSequenceTestCase
    {
        private static readonly string[] ExpectedQuotedKeys = ["a\tb", "c\nd", "e\\f", "g\"h", "i", "u00e9"];

        [Test]
        public void RareEscapeSequences_CarriageReturn()
        {
            using var stream = TestDataHelper.OpenResource("TextKV3.rare_escape_sequences.kv3");
            var data = KVSerializer.Create(KVSerializationFormat.KeyValues3Text).Deserialize(stream);

            Assert.That((string)data["carriage_return"], Is.EqualTo("hellorworld"));
        }

        [Test]
        public void RareEscapeSequences_VerticalTab()
        {
            using var stream = TestDataHelper.OpenResource("TextKV3.rare_escape_sequences.kv3");
            var data = KVSerializer.Create(KVSerializationFormat.KeyValues3Text).Deserialize(stream);

            Assert.That((string)data["vertical_tab"], Is.EqualTo("hellovworld"));
        }

        [Test]
        public void RareEscapeSequences_Backspace()
        {
            using var stream = TestDataHelper.OpenResource("TextKV3.rare_escape_sequences.kv3");
            var data = KVSerializer.Create(KVSerializationFormat.KeyValues3Text).Deserialize(stream);

            Assert.That((string)data["backspace"], Is.EqualTo("hellobworld"));
        }

        [Test]
        public void RareEscapeSequences_FormFeed()
        {
            using var stream = TestDataHelper.OpenResource("TextKV3.rare_escape_sequences.kv3");
            var data = KVSerializer.Create(KVSerializationFormat.KeyValues3Text).Deserialize(stream);

            Assert.That((string)data["form_feed"], Is.EqualTo("hellofworld"));
        }

        [Test]
        public void RareEscapeSequences_Alert()
        {
            using var stream = TestDataHelper.OpenResource("TextKV3.rare_escape_sequences.kv3");
            var data = KVSerializer.Create(KVSerializationFormat.KeyValues3Text).Deserialize(stream);

            Assert.That((string)data["alert"], Is.EqualTo("helloaworld"));
        }

        [Test]
        public void RareEscapeSequences_QuestionMark()
        {
            using var stream = TestDataHelper.OpenResource("TextKV3.rare_escape_sequences.kv3");
            var data = KVSerializer.Create(KVSerializationFormat.KeyValues3Text).Deserialize(stream);

            Assert.That((string)data["question_mark"], Is.EqualTo("hello?world"));
        }

        [Test]
        public void RareEscapeSequences_SingleQuote()
        {
            using var stream = TestDataHelper.OpenResource("TextKV3.rare_escape_sequences.kv3");
            var data = KVSerializer.Create(KVSerializationFormat.KeyValues3Text).Deserialize(stream);

            Assert.That((string)data["single_quote"], Is.EqualTo("hello'world"));
        }

        [Test]
        public void ChainedBackslashPatterns_BackslashThenQuote()
        {
            using var stream = TestDataHelper.OpenResource("TextKV3.chained_backslash_patterns.kv3");
            var data = KVSerializer.Create(KVSerializationFormat.KeyValues3Text).Deserialize(stream);

            Assert.That((string)data["backslash_quote"], Is.EqualTo("\\\""));
        }

        [Test]
        public void ChainedBackslashPatterns_FourBackslashes()
        {
            using var stream = TestDataHelper.OpenResource("TextKV3.chained_backslash_patterns.kv3");
            var data = KVSerializer.Create(KVSerializationFormat.KeyValues3Text).Deserialize(stream);

            Assert.That((string)data["four_backslashes"], Is.EqualTo("\\\\\\\\"));
        }

        [Test]
        public void ChainedBackslashPatterns_Alternating()
        {
            using var stream = TestDataHelper.OpenResource("TextKV3.chained_backslash_patterns.kv3");
            var data = KVSerializer.Create(KVSerializationFormat.KeyValues3Text).Deserialize(stream);

            Assert.That((string)data["alternating"], Is.EqualTo("\\\"\\\""));
        }

        [Test]
        public void ChainedBackslashPatterns_ComplexPath()
        {
            using var stream = TestDataHelper.OpenResource("TextKV3.chained_backslash_patterns.kv3");
            var data = KVSerializer.Create(KVSerializationFormat.KeyValues3Text).Deserialize(stream);

            Assert.That((string)data["complex_path"], Is.EqualTo("C:\\Program Files\\\"Game\"\\data\\"));
        }

        [Test]
        public void ChainedBackslashPatterns_UnknownEscape()
        {
            using var stream = TestDataHelper.OpenResource("TextKV3.chained_backslash_patterns.kv3");
            var data = KVSerializer.Create(KVSerializationFormat.KeyValues3Text).Deserialize(stream);

            Assert.That((string)data["unknown_escape"], Is.EqualTo("x"));
        }

        [Test]
        public void ChainedBackslashPatterns_UnknownEscapeMid()
        {
            using var stream = TestDataHelper.OpenResource("TextKV3.chained_backslash_patterns.kv3");
            var data = KVSerializer.Create(KVSerializationFormat.KeyValues3Text).Deserialize(stream);

            Assert.That((string)data["unknown_escape_mid"], Is.EqualTo("hexllo"));
        }

        [Test]
        public void EscapedQuoteAtStart_SingleEscapedQuote()
        {
            using var stream = TestDataHelper.OpenResource("TextKV3.escaped_quote_at_start.kv3");
            var data = KVSerializer.Create(KVSerializationFormat.KeyValues3Text).Deserialize(stream);

            Assert.That((string)data["escaped_quote"], Is.EqualTo("\""));
        }

        [Test]
        public void EscapedQuoteAtStart_EscapedQuoteThenText()
        {
            using var stream = TestDataHelper.OpenResource("TextKV3.escaped_quote_at_start.kv3");
            var data = KVSerializer.Create(KVSerializationFormat.KeyValues3Text).Deserialize(stream);

            Assert.That((string)data["escaped_quote_then_text"], Is.EqualTo("\"hello"));
        }

        [Test]
        public void EscapedQuoteAtStart_TextThenEscapedQuote()
        {
            using var stream = TestDataHelper.OpenResource("TextKV3.escaped_quote_at_start.kv3");
            var data = KVSerializer.Create(KVSerializationFormat.KeyValues3Text).Deserialize(stream);

            Assert.That((string)data["text_then_escaped_quote"], Is.EqualTo("hello\""));
        }

        [TestCase("\"\\x\\r\\0\\u0041\\a\"", "xr0u0041a")]
        [TestCase("\"line1\nline2\"", "line1\nline2")]
        [TestCase("\"line1\\\nline2\"", "line1\nline2")]
        [TestCase("'it\\'s \"q\"'", "it's \"q\"")]
        [TestCase("'a\\tb'", "a\tb")]
        [TestCase("\"\"", "")]
        [TestCase("''", "")]
        [TestCase("\"#[01] //x /*y*/ {}[]=,:|;\"", "#[01] //x /*y*/ {}[]=,:|;")]
        public void StringValueEscapes(string text, string expected)
        {
            var data = TestDataHelper.ParseKV3Text($"{{ a = {text} }}");

            Assert.That((string)data["a"], Is.EqualTo(expected));
        }

        [Test]
        public void QuotedKeyEscapes()
        {
            var data = TestDataHelper.ParseKV3Text("{ \"a\\tb\" = 1 \"c\\nd\" = 2 \"e\\\\f\" = 3 \"g\\\"h\" = 4 'i' = 5 \"\\u00e9\" = 6 }");

            Assert.That(data.Root.Keys, Is.EqualTo(ExpectedQuotedKeys));
        }
    }
}
