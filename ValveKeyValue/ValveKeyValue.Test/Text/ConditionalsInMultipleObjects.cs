using NUnit.Framework;

namespace ValveKeyValue.Test
{
    class ConditionalsInMultipleObjects
    {
        [Test]
        public void ShouldCorrectlyDiscardMultipleConditionsAndEndObject()
        {
            using (var stream = TestDataHelper.OpenResource("Text.conditional_discard.vdf"))
            {
                var kv = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(stream);
                
                Assert.IsNull(kv["One"]["Key1"]);
                Assert.IsNull(kv["Two"]["Key2"]);
                Assert.IsNull(kv["Three"]["Key3"]);
            }
        }
    }
}
