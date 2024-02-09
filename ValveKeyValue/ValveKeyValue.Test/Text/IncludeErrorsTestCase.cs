namespace ValveKeyValue.Test
{
    class IncludeErrorsTestCase
    {
        [Test]
        public void IncludeNotAtStart()
        {
            var text = @"""root""
{
#include ""foo.txt""
}
            ";

            var serializer = KVSerializer.Create(KVSerializationFormat.KeyValues1Text);
            Assert.That(
                () => serializer.Deserialize(text),
                Throws.InstanceOf<KeyValueException>()
                .With.Message.EqualTo("Inclusions are only valid at the beginning of a file, but found one at line 3, column 1."));
        }

        [Test]
        public void BaseNotAtStart()
        {
            var text = @"""root""
{
    #base ""foo.txt""
}
            ";

            var serializer = KVSerializer.Create(KVSerializationFormat.KeyValues1Text);
            Assert.That(
                () => serializer.Deserialize(text),
                Throws.InstanceOf<KeyValueException>()
                .With.Message.EqualTo("Inclusions are only valid at the beginning of a file, but found one at line 3, column 5."));
        }
    }
}
