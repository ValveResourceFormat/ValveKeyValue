using System.Text;

namespace ValveKeyValue.Test
{
    class ValueTupleTextTestCase
    {
        [Test]
        public void DeserializeValueTuple2Elements()
        {
            var vt = ("hello", "world");

            using var stream = TestDataHelper.OpenResource("Text.valuetuple2.vdf");
            var data = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize<ValueTuple<string, string>>(stream);

            Assert.That(data, Is.EqualTo(vt));
        }

        [Test]
        public void SerializeValueTuple2Elements()
        {
            var vt = ("hello", "world");

            using var ms = new MemoryStream();
            KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Serialize(ms, vt, "root");

            var text = Encoding.UTF8.GetString(ms.ToArray());
            Assert.That(text, Is.EqualTo(TestDataHelper.ReadTextResource("Text.valuetuple2.vdf")));
        }

        [Test]
        public void DeserializeValueTuple10Elements()
        {
            var vt = (1, 2, 3, 4, 5, 6, 7, 8, 9, 10);

            using var stream = TestDataHelper.OpenResource("Text.valuetuple10.vdf");
            var data = KVSerializer.Create(KVSerializationFormat.KeyValues1Text)
                .Deserialize<ValueTuple<int, int, int, int, int, int, int, ValueTuple<int, int, int>>>(stream);

            Assert.That(data, Is.EqualTo(vt));
        }

        [Test]
        public void SerializeValueTuple10Elements()
        {
            var vt = (1, 2, 3, 4, 5, 6, 7, 8, 9, 10);

            using var ms = new MemoryStream();
            KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Serialize(ms, vt, "root");

            var text = Encoding.UTF8.GetString(ms.ToArray());
            Assert.That(text, Is.EqualTo(TestDataHelper.ReadTextResource("Text.valuetuple10.vdf")));
        }
    }
}
