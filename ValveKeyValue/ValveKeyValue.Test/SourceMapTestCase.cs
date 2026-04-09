using System.Linq;

namespace ValveKeyValue.Test
{
    /// <summary>
    /// Verifies the per-token source maps produced by the text serializers and parsers.
    /// Each recorded span must precisely cover its token in the associated text, so that
    /// <c>text.Substring(span.Start, span.End - span.Start)</c> equals what a highlighter
    /// is expected to colour.
    /// </summary>
    class SourceMapTestCase
    {
        [Test]
        public void Kv3SerializerSpansLineUpWithEmittedText()
        {
            var root = KVObject.Collection();
            root.Add("foo", new KVObject("bar"));
            root.Add("count", new KVObject(42));
            root.Add("enabled", new KVObject(true));
            var flagged = new KVObject("models/foo.vmdl") { Flag = KVFlag.Resource };
            root.Add("flagged", flagged);

            var (text, spans) = KVSerializer.Create(KVSerializationFormat.KeyValues3Text)
                .SerializeWithSourceMap(root);

            AssertSpansAreWellFormed(text, spans);

            // The header sits at offset 0 and is the first recorded span.
            Assert.That(spans[0].TokenType, Is.EqualTo(KVTokenType.Header));
            Assert.That(spans[0].Start, Is.EqualTo(0));
            Assert.That(text.AsSpan(spans[0].Start, spans[0].End - spans[0].Start).StartsWith("<!--"), Is.True);

            AssertSpanExists(text, spans, KVTokenType.Key, "foo");
            AssertSpanExists(text, spans, KVTokenType.Key, "count");
            AssertSpanExists(text, spans, KVTokenType.Key, "enabled");
            AssertSpanExists(text, spans, KVTokenType.Key, "flagged");

            // Quoted string values are tagged String, bare literals are Identifier.
            AssertSpanExists(text, spans, KVTokenType.String, "\"bar\"");
            AssertSpanExists(text, spans, KVTokenType.Identifier, "42");
            AssertSpanExists(text, spans, KVTokenType.Identifier, "true");
            AssertSpanExists(text, spans, KVTokenType.Flag, "resource:");

            Assert.That(spans.Any(s => s.TokenType == KVTokenType.ObjectStart && text[s.Start] == '{'), Is.True);
            Assert.That(spans.Any(s => s.TokenType == KVTokenType.ObjectEnd && text[s.Start] == '}'), Is.True);
        }

        [Test]
        public void Kv3ParserSpansLineUpWithInputText()
        {
            const string text = "<!-- kv3 encoding:text:version{e21c7f3c-8a33-41c5-9977-a76d3a32aa0d} format:generic:version{7412167c-06e9-4698-aff2-e63eb59037e7} -->\n{\n\tfoo = \"bar\"\n\tcount = 42\n\tenabled = true\n}\n";

            var (doc, spans) = KVSerializer.Create(KVSerializationFormat.KeyValues3Text)
                .DeserializeWithSourceMap(text);

            Assert.That((string)doc["foo"], Is.EqualTo("bar"));
            Assert.That((int)doc["count"], Is.EqualTo(42));
            Assert.That((bool)doc["enabled"], Is.True);

            AssertSpansAreWellFormed(text, spans);

            var header = spans.First(s => s.TokenType == KVTokenType.Header);
            Assert.That(text.AsSpan(header.Start, header.End - header.Start).StartsWith("<!--"), Is.True);
            Assert.That(text.AsSpan(header.Start, header.End - header.Start).EndsWith("-->"), Is.True);

            // Identifiers in key position are resolved to KVTokenType.Key by the parser.
            AssertSpanExists(text, spans, KVTokenType.Key, "foo");
            AssertSpanExists(text, spans, KVTokenType.Key, "count");
            AssertSpanExists(text, spans, KVTokenType.Key, "enabled");

            AssertSpanExists(text, spans, KVTokenType.String, "\"bar\"");
            AssertSpanExists(text, spans, KVTokenType.Identifier, "42");
            AssertSpanExists(text, spans, KVTokenType.Identifier, "true");

            Assert.That(spans.Any(s => s.TokenType == KVTokenType.ObjectStart && text[s.Start] == '{'), Is.True);
            Assert.That(spans.Any(s => s.TokenType == KVTokenType.ObjectEnd && text[s.Start] == '}'), Is.True);
        }

        [Test]
        public void Kv1SerializerSpansLineUpWithEmittedText()
        {
            var root = KVObject.ListCollection();
            root.Add("name", new KVObject("hello"));
            root.Add("count", new KVObject(7));

            var (text, spans) = KVSerializer.Create(KVSerializationFormat.KeyValues1Text)
                .SerializeWithSourceMap(root, name: "root");

            AssertSpansAreWellFormed(text, spans);

            // KV1 quotes everything; recorded spans include the surrounding quotes.
            AssertSpanExists(text, spans, KVTokenType.Key, "\"root\"");
            AssertSpanExists(text, spans, KVTokenType.Key, "\"name\"");
            AssertSpanExists(text, spans, KVTokenType.Key, "\"count\"");
            AssertSpanExists(text, spans, KVTokenType.String, "\"hello\"");
            AssertSpanExists(text, spans, KVTokenType.String, "\"7\"");

            Assert.That(spans.Any(s => s.TokenType == KVTokenType.ObjectStart && text[s.Start] == '{'), Is.True);
            Assert.That(spans.Any(s => s.TokenType == KVTokenType.ObjectEnd && text[s.Start] == '}'), Is.True);
        }

        [Test]
        public void Kv1ParserSpansLineUpWithInputText()
        {
            const string text = "\"root\"\n{\n\t\"name\"\t\"hello\"\n\t\"count\"\t\"7\"\n}\n";

            var (doc, spans) = KVSerializer.Create(KVSerializationFormat.KeyValues1Text)
                .DeserializeWithSourceMap(text);

            Assert.That((string)doc["name"], Is.EqualTo("hello"));
            Assert.That((int)doc["count"], Is.EqualTo(7));

            AssertSpansAreWellFormed(text, spans);

            AssertSpanExists(text, spans, KVTokenType.Key, "\"root\"");
            AssertSpanExists(text, spans, KVTokenType.Key, "\"name\"");
            AssertSpanExists(text, spans, KVTokenType.Key, "\"count\"");
            AssertSpanExists(text, spans, KVTokenType.String, "\"hello\"");
            AssertSpanExists(text, spans, KVTokenType.String, "\"7\"");

            Assert.That(spans.Any(s => s.TokenType == KVTokenType.ObjectStart && text[s.Start] == '{'), Is.True);
            Assert.That(spans.Any(s => s.TokenType == KVTokenType.ObjectEnd && text[s.Start] == '}'), Is.True);
        }

        // Asserts the universal source-map invariants: spans lie within the text, are non-empty,
        // and are sorted by Start ascending. Highlighters depend on the ordering for single-pass walks.
        static void AssertSpansAreWellFormed(string text, IReadOnlyList<KvSourceSpan> spans)
        {
            Assert.That(spans, Is.Not.Empty, "no spans were recorded");

            foreach (var span in spans)
            {
                Assert.That(span.Start, Is.GreaterThanOrEqualTo(0), $"span starts before text: {span}");
                Assert.That(span.End, Is.LessThanOrEqualTo(text.Length), $"span ends past text: {span}");
                Assert.That(span.Start, Is.LessThan(span.End), $"span is empty or reversed: {span}");
            }

            for (var i = 1; i < spans.Count; i++)
            {
                Assert.That(spans[i].Start, Is.GreaterThanOrEqualTo(spans[i - 1].Start),
                    $"spans not sorted by Start at index {i}: {spans[i - 1]} then {spans[i]}");
            }
        }

        static void AssertSpanExists(string text, IReadOnlyList<KvSourceSpan> spans, KVTokenType tokenType, string expectedText)
        {
            var match = spans.FirstOrDefault(s =>
                s.TokenType == tokenType && text.Substring(s.Start, s.End - s.Start) == expectedText);
            Assert.That(match, Is.Not.EqualTo(default(KvSourceSpan)),
                $"expected a {tokenType} span covering '{expectedText}'");
        }
    }
}
