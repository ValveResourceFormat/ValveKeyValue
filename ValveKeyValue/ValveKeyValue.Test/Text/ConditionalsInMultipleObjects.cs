namespace ValveKeyValue.Test
{
    class ConditionalsInMultipleObjects
    {
        [Test]
        public void ShouldCorrectlyDiscardMultipleConditionsAndEndObject()
        {
            using var stream = TestDataHelper.OpenResource("Text.conditional_discard.vdf");
            var kv = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(stream);

            Assert.That(kv["One"].ContainsKey("Key1"), Is.False);
            Assert.That(kv["Two"].ContainsKey("Key2"), Is.False);
            Assert.That(kv["Three"].ContainsKey("Key3"), Is.False);
        }
    }
}
