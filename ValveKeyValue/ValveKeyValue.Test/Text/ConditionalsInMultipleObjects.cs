namespace ValveKeyValue.Test
{
    class ConditionalsInMultipleObjects
    {
        [Test]
        public void ShouldCorrectlyDiscardMultipleConditionsAndEndObject()
        {
            using var stream = TestDataHelper.OpenResource("Text.conditional_discard.vdf");
            var kv = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(stream);

            Assert.That(kv["One"]["Key1"], Is.Null);
            Assert.That(kv["Two"]["Key2"], Is.Null);
            Assert.That(kv["Three"]["Key3"], Is.Null);
        }
    }
}
