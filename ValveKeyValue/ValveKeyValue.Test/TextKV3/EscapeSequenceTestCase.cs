namespace ValveKeyValue.Test.TextKV3
{
    class EscapeSequenceTestCase
    {
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
    }
}
