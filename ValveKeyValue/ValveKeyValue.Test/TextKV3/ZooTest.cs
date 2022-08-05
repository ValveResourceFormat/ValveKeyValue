using NUnit.Framework;

namespace ValveKeyValue.Test.TextKV3
{
    class ZooTest
    {
        [Test]
        public void Test()
        {
            using var stream = TestDataHelper.OpenResource("TextKV3.zoo.kv3");
            var data = KVSerializer.Create(KVSerializationFormat.KeyValues3Text).Deserialize(stream);

            Assert.Fail();
        }
    }
}
