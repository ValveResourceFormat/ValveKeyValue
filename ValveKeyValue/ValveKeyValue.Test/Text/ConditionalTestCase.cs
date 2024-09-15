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

            Assert.That((string)data["ui type"], Is.Null);
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
        public void ReadsValueWhenConditionalNotEqual(string condition)
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
            Assert.That(data.Value.ValueType, Is.EqualTo(KVValueType.Collection));

            var children = data.Children.ToArray();
            Assert.That(children, Has.Length.EqualTo(1));
            Assert.That(children[0].Name, Is.EqualTo("operating system [$WIN32]"));
            Assert.That((string)children[0].Value, Is.EqualTo("windows 32-bit"));
        }

        [Test]
        public void ConditionalBeforeObject()
        {
            var data = ParseResource("Text.conditional_before_object_value.vdf");
            Assert.That(data, Is.Not.Null);
            Assert.That(data.Value.ValueType, Is.EqualTo(KVValueType.Collection));

            var children = data.Children.ToArray();
            Assert.That(children, Has.Length.EqualTo(0));

            data = ParseResource("Text.conditional_before_object_value.vdf", ["WIN32"]);
            Assert.That(data, Is.Not.Null);
            Assert.That(data.Value.ValueType, Is.EqualTo(KVValueType.Collection));

            children = data.Children.ToArray();
            Assert.That(children, Has.Length.EqualTo(1));
            Assert.That(children[0].Name, Is.EqualTo("operating system"));
            Assert.That((string)children[0].Value, Is.EqualTo("windows 32-bit"));
        }

        [Test]
        public void ConditionalBetweenKeyAndValue()
        {
            var data = ParseResource("Text.conditional_between_key_and_value.vdf");
            Assert.That(data, Is.Not.Null);
            Assert.That(data.Value.ValueType, Is.EqualTo(KVValueType.Collection));

            var children = data.Children.ToArray();
            Assert.That(children, Has.Length.EqualTo(0));

            data = ParseResource("Text.conditional_between_key_and_value.vdf", ["WIN32"]);
            Assert.That(data, Is.Not.Null);
            Assert.That(data.Value.ValueType, Is.EqualTo(KVValueType.Collection));

            children = data.Children.ToArray();
            Assert.That(children, Has.Length.EqualTo(1));
            Assert.That(children[0].Name, Is.EqualTo("operating system"));
            Assert.That((string)children[0].Value, Is.EqualTo("windows 32-bit"));
        }

        [Test]
        public void ConditionalBeforeKey()
        {
            Assert.Throws<InvalidDataException>(() => { ParseResource("Text.conditional_before_key.vdf"); });
        }

        static KVObject ParseResource(string name)
            => ParseResource(name, []);

        static KVObject ParseResource(string name, string[] conditions)
        {
            KVObject data;
            using (var stream = TestDataHelper.OpenResource(name))
            {
                var options = new KVSerializerOptions();
                options.Conditions.Clear();

                foreach (var c in conditions)
                {
                    options.Conditions.Add(c);
                }

                data = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(stream, options);
            }

            return data;
        }
    }
}
