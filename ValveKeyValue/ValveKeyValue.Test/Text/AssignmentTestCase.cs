using System.Linq;

namespace ValveKeyValue.Test
{
    class AssignmentTestCase
    {
        [TestCase("\"root\" { key=value }")]
        [TestCase("\"root\" { key = value }")]
        [TestCase("\"root\" { \"key\" = \"value\" }")]
        [TestCase("\"root\" { \"key\"=\"value\" }")]
        [TestCase("\"root\" { key\t=\nvalue }")]
        [TestCase("\"root\" { key = // comment\nvalue }")]
        [TestCase("\"root\" { key value}")]
        [TestCase("\"root\"{key\"value\"}")]
        public void ReadsKeyValuePair(string text)
        {
            var data = Parse(text);

            Assert.That(data.Children.Select(c => (c.Key, (string)c.Value)), Is.EqualTo(new[] { ("key", "value") }));
        }

        [Test]
        public void ReadsUnquotedAssignmentFollowedByQuotedPair()
        {
            var data = Parse("\"root\" { key=value \"b\" \"2\" }");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(data.Count, Is.EqualTo(2));
                Assert.That((string)data["key"], Is.EqualTo("value"));
                Assert.That((int)data["b"], Is.EqualTo(2));
            }
        }

        [Test]
        public void ReadsAssignmentsInsideObject()
        {
            var data = Parse("\"root\" { \"key\" { a=1 b=2 } }");

            var key = data["key"];
            using (Assert.EnterMultipleScope())
            {
                Assert.That(data.Count, Is.EqualTo(1));
                Assert.That(key.Count, Is.EqualTo(2));
                Assert.That(key["a"].ValueType, Is.EqualTo(KVValueType.Int32));
                Assert.That((int)key["a"], Is.EqualTo(1));
                Assert.That((int)key["b"], Is.EqualTo(2));
            }
        }

        [TestCase("\"root\" { key = { a 1 } }")]
        [TestCase("\"root\" { key={a=1} }")]
        [TestCase("\"root\" { key{ a 1 } }")]
        public void ReadsAssignmentBeforeObject(string text)
        {
            var data = Parse(text);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(data.Count, Is.EqualTo(1));
                Assert.That(data["key"].Count, Is.EqualTo(1));
                Assert.That((int)data["key"]["a"], Is.EqualTo(1));
            }
        }

        [Test]
        public void QuotedSpecialCharactersAreLiteral()
        {
            var data = Parse("\"root\" { \"a\" \"x=y{}\" \"=\" \"{\" \"b\" \"=\" }");

            using (Assert.EnterMultipleScope())
            {
                Assert.That((string)data["a"], Is.EqualTo("x=y{}"));
                Assert.That((string)data["="], Is.EqualTo("{"));
                Assert.That((string)data["b"], Is.EqualTo("="));
            }
        }

        // Only the first '=' after a key is an assignment. Anywhere else it is a one-character string.
        [Test]
        public void AssignmentOutsideKeyAndValueIsString()
        {
            var data = Parse("\"root\" { a=1=2 key value=x }");

            Assert.That(
                data.Children.Select(c => (c.Key, (string)c.Value)),
                Is.EqualTo(new[] { ("a", "1"), ("=", "2"), ("key", "value"), ("=", "x") }));
        }

        [TestCase("\"root\" = { a 1 }", "Found '=' after the root key at line 1, column 8, the root key must be followed by '{'.")]
        [TestCase("\"root\" { a = }", "Attempted to finalize object while in state InObjectAfterAssignment at line 1, column 14.")]
        [TestCase("\"root\" { a = = b }", "Attempted to finalize object while in state InObjectBetweenKeyAndValue at line 1, column 18.")]
        public void InvalidAssignmentThrows(string text, string message)
        {
            Assert.That(
                () => Parse(text),
                Throws.InstanceOf<KeyValueException>()
                .With.Message.EqualTo(message));
        }

        [TestCase("\"root\" { key = [$WIN32] value }", true)]
        [TestCase("\"root\" { key = [$X360] value }", false)]
        [TestCase("\"root\" { key [$WIN32] = value }", true)]
        [TestCase("\"root\" { key [$X360] = value }", false)]
        [TestCase("\"root\" { key [$X360] = [$WIN32] value }", true)]
        [TestCase("\"root\" { key [$WIN32] = [$X360] value }", false)]
        [TestCase("\"root\" { key = value [$X360] }", false)]
        [TestCase("\"root\" { key [$X360] value [$WIN32] }", true)]
        [TestCase("\"root\" { key [$WIN32] value [$X360] }", false)]
        [TestCase("\"root\" { key [$WIN32]=value }", true)]
        public void LastConditionalDecides(string text, bool isKept)
        {
            var data = Parse(text, "WIN32");

            Assert.That(data.ContainsKey("key"), Is.EqualTo(isKept));
        }

        [TestCase("\"root\" { key [$WIN32] [$WIN32] value }", 23)]
        [TestCase("\"root\" { key = [$WIN32] [$WIN32] value }", 25)]
        [TestCase("\"root\" { key value [$WIN32] [$WIN32] }", 29)]
        [TestCase("\"root\" [$WIN32] [$WIN32] { }", 17)]
        public void ConsecutiveConditionalsThrow(string text, int column)
        {
            Assert.That(
                () => Parse(text, "WIN32"),
                Throws.InstanceOf<KeyValueException>()
                .With.Message.EqualTo($"Found a second consecutive conditional at line 1, column {column}."));
        }

        // A matching conditional after '=' replaces the first earlier pair with the same key.
        [TestCase("\"root\" { xpos = 1 xpos = [$WIN32] 2 }", "xpos=2")]
        [TestCase("\"root\" { xpos = 1 xpos = [$X360] 2 }", "xpos=1")]
        [TestCase("\"root\" { xpos 1 xpos [$WIN32] 2 }", "xpos=1 xpos=2")]
        [TestCase("\"root\" { xpos 1 xpos = 2 }", "xpos=1 xpos=2")]
        [TestCase("\"root\" { a = 1 A = [$WIN32] 2 }", "A=2")]
        [TestCase("\"root\" { a 1 b 2 a = [$WIN32] 3 }", "b=2 a=3")]
        [TestCase("\"root\" { a 1 a 2 a = [$WIN32] 3 }", "a=2 a=3")]
        [TestCase("\"root\" { a = [$WIN32] 3 }", "a=3")]
        [TestCase("\"root\" { a 1 a = [$WIN32] 2 [$X360] }", "")]
        [TestCase("\"root\" { a [$X360] = [$WIN32] 2 }", "a=2")]
        public void MatchingConditionalAfterAssignmentReplacesEarlierKey(string text, string expected)
        {
            var data = Parse(text, "WIN32");

            Assert.That(string.Join(" ", data.Children.Select(c => $"{c.Key}={c.Value}")), Is.EqualTo(expected));
        }

        [Test]
        public void MatchingConditionalAfterAssignmentReplacesEarlierObject()
        {
            var data = Parse("\"root\" { a = [$WIN32] { x 1 } a = [$WIN32] { y 2 } }", "WIN32");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(data.Count, Is.EqualTo(1));
                Assert.That((int)data["a"]["y"], Is.EqualTo(2));
                Assert.That(data["a"].ContainsKey("x"), Is.False);
            }
        }

        static KVObject Parse(string text, params string[] conditions)
        {
            var options = new KVSerializerOptions();
            options.Conditions.Clear();

            foreach (var condition in conditions)
            {
                options.Conditions.Add(condition);
            }

            return KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(text, options).Root;
        }
    }
}
