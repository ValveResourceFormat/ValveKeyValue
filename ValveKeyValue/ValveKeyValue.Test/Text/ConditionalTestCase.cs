using System.Linq;

namespace ValveKeyValue.Test
{
    class ConditionalTestCase
    {
        [Test]
        public void ReadsValueWhenConditionalEqual()
        {
            var conditions = new[] { "WIN32" };
            var data = ParseResource("Text.conditional.vdf", conditions);

            Assert.That((string)data["operating system"], Is.EqualTo("windows 32-bit"));
        }

        [TestCase("WIN32")]
        [TestCase("WIN64")]
        public void ReadsValueWhenConditionalWithOrMatches(string condition)
        {
            var conditions = new[] { condition };
            var data = ParseResource("Text.conditional.vdf", conditions);

            Assert.That((string)data["platform"], Is.EqualTo("windows"));
        }

        [Test]
        public void ReadsValueWhenConditionalWithAndMatches()
        {
            var conditions = new[] { "X360", "X360WIDE" };
            var data = ParseResource("Text.conditional.vdf", conditions);

            Assert.That((string)data["ui type"], Is.EqualTo("Widescreen Xbox 360"));
        }

        [Test]
        public void ReadsValueWhenConditionalWithAndMatchesWithNegatedSide()
        {
            var conditions = new[] { "X360" };
            var data = ParseResource("Text.conditional.vdf", conditions);

            Assert.That((string)data["ui type"], Is.EqualTo("Xbox 360"));
        }

        [Test]
        public void ReadsValueWhenConditionalWithAndOnlyMatchesOneSide()
        {
            var conditions = new[] { "X360WIDE" };
            var data = ParseResource("Text.conditional.vdf", conditions);

            Assert.That(data.ContainsKey("ui type"), Is.False);
        }

        [TestCase("WIN32")]
        [TestCase("WIN64")]
        public void ReadsValueWhenBothSidesOfConditionalAreBracketed(string condition)
        {
            var data = ParseResource("Text.conditional.vdf", [condition]);

            Assert.That((string)data["bracketed sides"], Is.EqualTo("windows"));
        }

        [Test]
        public void EvaluatesNumericLiterals()
        {
            var data = ParseResource("Text.conditional.vdf");

            using (Assert.EnterMultipleScope())
            {
                Assert.That((string)data["literal true"], Is.EqualTo("yes"));
                Assert.That(data.ContainsKey("literal false"), Is.False);
                Assert.That((string)data["literal multi digit"], Is.EqualTo("yes"));
                Assert.That((string)data["literal brackets"], Is.EqualTo("yes"));
            }
        }

        [Test]
        public void ReadsValueWhenNegatedGroupMatches()
        {
            var data = ParseResource("Text.conditional.vdf");

            Assert.That((string)data["negated group"], Is.EqualTo("yes"));
        }

        [TestCase("GERMAN")]
        [TestCase("FRENCH")]
        public void DiscardsValueWhenNegatedGroupDoesNotMatch(string condition)
        {
            var data = ParseResource("Text.conditional.vdf", [condition]);

            Assert.That(data.ContainsKey("negated group"), Is.False);
        }

        [Test]
        public void ReadsValueFromNestedBrackets()
        {
            var data = ParseResource("Text.conditional.vdf", ["WIN32"]);

            Assert.That((string)data["nested brackets"], Is.EqualTo("yes"));
        }

        // && and || have equal precedence and are left-associative, matching Valve's CExpressionEvaluator.
        [Test]
        public void AndOrHaveEqualPrecedenceAndAreLeftAssociative()
        {
            var data = ParseResource("Text.conditional.vdf", ["POLISH"]);

            // ($GERMAN && $FRENCH) || $POLISH, not $GERMAN && ($FRENCH || $POLISH)
            Assert.That((string)data["left associative"], Is.EqualTo("yes"));
        }

        // Distinguishes equal precedence from C style precedence: ($GERMAN || $FRENCH) && $POLISH
        // is false with only GERMAN defined, $GERMAN || ($FRENCH && $POLISH) would be true.
        [Test]
        public void OrBeforeAndEvaluatesWithEqualPrecedence()
        {
            var data = ParseResource("Text.conditional.vdf", ["GERMAN"]);
            Assert.That(data.ContainsKey("or before and"), Is.False);

            data = ParseResource("Text.conditional.vdf", ["GERMAN", "POLISH"]);
            Assert.That((string)data["or before and"], Is.EqualTo("yes"));
        }

        // Valve's evaluator mangles !!$A into !$A, we evaluate standard double negation instead.
        [Test]
        public void SupportsDoubleNegation()
        {
            var data = ParseResource("Text.conditional.vdf", ["WIN32"]);
            Assert.That((string)data["double negation"], Is.EqualTo("yes"));

            data = ParseResource("Text.conditional.vdf");
            Assert.That(data.ContainsKey("double negation"), Is.False);
        }

        [Test]
        public void BareDollarEvaluatesToFalseWithoutError()
        {
            var data = ParseResource("Text.conditional.vdf");
            Assert.That(data.ContainsKey("dollar only"), Is.False);
        }

        // $0 is a variable lookup, not a numeric literal. Matches Valve, where only bare digit
        // runs are constants.
        [Test]
        public void VariableMadeOfDigitsIsNotALiteral()
        {
            var data = ParseResource("Text.conditional.vdf", ["0"]);
            Assert.That((string)data["digit variable"], Is.EqualTo("yes"));

            data = ParseResource("Text.conditional.vdf");
            Assert.That(data.ContainsKey("digit variable"), Is.False);
        }

        // Matches Valve, whose symbol resolution uses case-insensitive V_stricmp.
        [Test]
        public void VariableMatchingIsCaseInsensitive()
        {
            var data = ParseResource("Text.conditional.vdf", ["WIN32"]);

            // Lowercase variable in the file, uppercase defined condition.
            Assert.That((string)data["lowercase variable"], Is.EqualTo("yes"));

            // Uppercase variable in the file, lowercase defined condition.
            data = ParseResource("Text.conditional.vdf", ["win32"]);
            Assert.That((string)data["operating system"], Is.EqualTo("windows 32-bit"));

            data = ParseResource("Text.conditional.vdf");
            Assert.That(data.ContainsKey("lowercase variable"), Is.False);
        }

        [Test]
        public void SupportsWhitespacePaddingInConditions()
        {
            var data = ParseResource("Text.conditional.vdf", ["WIN32", "X360", "X360WIDE"]);

            using (Assert.EnterMultipleScope())
            {
                Assert.That((string)data["padded condition"], Is.EqualTo("yes"));
                Assert.That((string)data["tabbed condition"], Is.EqualTo("yes"));
            }
        }

        [Test]
        public void SupportsConditionalsWithUnderscores()
        {
            var conditions = new[] { "SOMETHING_WITH_UNDERSCORE" };
            var data = ParseResource("Text.conditional.vdf", conditions);

            Assert.That((string)data["underscore_condition"], Is.EqualTo("yes"));
        }

        [TestCase(null)]
        [TestCase("OSX")]
        [TestCase("LINUX")]
        [TestCase("PS3")]
        public void ReadsValueWhenConditionalNotEqual(string? condition)
        {
            string[] conditions;
            if (condition == null)
            {
                conditions = [];
            }
            else
            {
                conditions = [condition];
            }

            var data = ParseResource("Text.conditional.vdf", conditions);
            Assert.That((string)data["operating system"], Is.EqualTo("something else"));
        }

        [TestCase([new string[] { "X360" }], ExpectedResult = "small", TestName = "ReadsValueFromComplexBracketedConditional([\"X360\"]) => \"small\"")]
        [TestCase([new[] { "X360", "GERMAN" }], ExpectedResult = "medium", TestName = "ReadsValueFromComplexBracketedConditional([\"X360\", \"GERMAN\"]) => \"medium\"")]
        [TestCase([new[] { "X360", "FRENCH" }], ExpectedResult = "medium", TestName = "ReadsValueFromComplexBracketedConditional([\"X360\", \"FRENCH\"]) => \"medium\"")]
        [TestCase([new[] { "X360", "POLISH" }], ExpectedResult = "large", TestName = "ReadsValueFromComplexBracketedConditional([\"X360\", \"POLISH\"]) => \"large\"")]
        public string ReadsValueFromComplexBracketedConditional(string[] conditions)
        {
            var data = ParseResource("Text.conditional.vdf", conditions);
            return (string)data["ui size"];
        }

        [Test]
        public void ConditionalInKey()
        {
            var data = ParseResource("Text.conditional_in_key.vdf");
            Assert.That(data, Is.Not.Null);
            Assert.That(data.ValueType, Is.EqualTo(KVValueType.Collection));

            var children = data.Children.ToArray();
            Assert.That(children, Has.Length.EqualTo(1));
            using (Assert.EnterMultipleScope())
            {
                Assert.That(children[0].Key, Is.EqualTo("operating system [$WIN32]"));
                Assert.That((string)children[0].Value, Is.EqualTo("windows 32-bit"));
            }
        }

        [Test]
        public void ConditionalBeforeObject()
        {
            var data = ParseResource("Text.conditional_before_object_value.vdf");
            Assert.That(data, Is.Not.Null);
            Assert.That(data.ValueType, Is.EqualTo(KVValueType.Collection));

            var children = data.Children.ToArray();
            Assert.That(children, Has.Length.EqualTo(0));

            data = ParseResource("Text.conditional_before_object_value.vdf", ["WIN32"]);
            Assert.That(data, Is.Not.Null);
            Assert.That(data.ValueType, Is.EqualTo(KVValueType.Collection));

            children = data.Children.ToArray();
            Assert.That(children, Has.Length.EqualTo(1));
            using (Assert.EnterMultipleScope())
            {
                Assert.That(children[0].Key, Is.EqualTo("operating system"));
                Assert.That((string)children[0].Value, Is.EqualTo("windows 32-bit"));
            }
        }

        [Test]
        public void ConditionalBetweenKeyAndValue()
        {
            var data = ParseResource("Text.conditional_between_key_and_value.vdf");
            Assert.That(data, Is.Not.Null);
            Assert.That(data.ValueType, Is.EqualTo(KVValueType.Collection));

            var children = data.Children.ToArray();
            Assert.That(children, Has.Length.EqualTo(0));

            data = ParseResource("Text.conditional_between_key_and_value.vdf", ["WIN32"]);
            Assert.That(data, Is.Not.Null);
            Assert.That(data.ValueType, Is.EqualTo(KVValueType.Collection));

            children = data.Children.ToArray();
            Assert.That(children, Has.Length.EqualTo(1));
            using (Assert.EnterMultipleScope())
            {
                Assert.That(children[0].Key, Is.EqualTo("operating system"));
                Assert.That((string)children[0].Value, Is.EqualTo("windows 32-bit"));
            }
        }

        [Test]
        public void ConditionalBeforeKey()
        {
            Assert.Throws<KeyValueException>(() => { ParseResource("Text.conditional_before_key.vdf"); });
        }

        static KVObject ParseResource(string name)
            => ParseResource(name, []);

        static KVObject ParseResource(string name, string[] conditions)
        {
            using var stream = TestDataHelper.OpenResource(name);
            var options = new KVSerializerOptions();
            options.Conditions.Clear();

            foreach (var c in conditions)
            {
                options.Conditions.Add(c);
            }

            return KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(stream, options).Root;
        }
    }
}
