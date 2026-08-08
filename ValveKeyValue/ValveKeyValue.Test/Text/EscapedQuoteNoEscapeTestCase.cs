namespace ValveKeyValue.Test
{
    class EscapedQuoteNoEscapeTestCase
    {
        [Test]
        public void SuggestsEnablingEscapeSequencesWhenParsingFails()
        {
            using var stream = TestDataHelper.OpenResource("Text.escaped_ending_quote.vdf");
            Assert.That(
                () => KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(stream),
                Throws.Exception.TypeOf<KeyValueException>()
                .With.InnerException.TypeOf<EndOfStreamException>()
                .With.Message.Contains("KVSerializerOptions.HasEscapeSequences"));
        }

        [Test]
        public void SuggestsEnablingEscapeSequencesWhenInclusionIsMisparsed()
        {
            using var stream = TestDataHelper.OpenResource("Text.escaped_quote_font_color.vdf");
            Assert.That(
                () => KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(stream),
                Throws.Exception.TypeOf<KeyValueException>()
                .With.Message.Contains("Unrecognized term after '#' symbol")
                .And.Message.Contains("KVSerializerOptions.HasEscapeSequences"));
        }

        [Test]
        public void DoesNotSuggestEscapeSequencesWhenNoEscapedQuoteWasRead()
        {
            using var stream = TestDataHelper.OpenResource("Text.partial_partialvalue.vdf");
            Assert.That(
                () => KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(stream),
                Throws.Exception.TypeOf<KeyValueException>()
                .With.InnerException.TypeOf<EndOfStreamException>()
                .With.Message.Not.Contains("HasEscapeSequences"));
        }

        [Test]
        public void FontColorParsesWhenEscapeSequencesAreEnabled()
        {
            var options = new KVSerializerOptions { HasEscapeSequences = true };

            KVObject data;
            using (var stream = TestDataHelper.OpenResource("Text.escaped_quote_font_color.vdf"))
            {
                data = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(stream, options);
            }

            Assert.That((string)data["Tokens"]["leaderboard_region_abbr_Asia"], Is.EqualTo("<font color=\"#fc8200\">AS</font>"));
        }

        [Test]
        public void ParsesWhenEscapeSequencesAreEnabled()
        {
            var options = new KVSerializerOptions { HasEscapeSequences = true };

            KVObject data;
            using (var stream = TestDataHelper.OpenResource("Text.escaped_ending_quote.vdf"))
            {
                data = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(stream, options);
            }

            Assert.That((string)data["key"], Is.EqualTo("some value\""));
            Assert.That((string)data["foo"], Is.EqualTo("bar"));
        }
    }
}
