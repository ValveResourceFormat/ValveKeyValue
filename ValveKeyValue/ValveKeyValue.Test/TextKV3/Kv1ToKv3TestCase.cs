namespace ValveKeyValue.Test.TextKV3
{
    class Kv1ToKv3TestCase
    {
        [Test]
        public void SerializesBasicObjects()
        {
            using var stream = TestDataHelper.OpenResource("TextKV3.flagged_value_kv1.vdf");
            var expected = TestDataHelper.ReadTextResource("TextKV3.flagged_value_from_kv1.kv3");

            var kv1 = KVSerializer.Create(KVSerializationFormat.KeyValues1Text);
            var kv3 = KVSerializer.Create(KVSerializationFormat.KeyValues3Text);
            var data = kv1.Deserialize(stream);

            data.Add(new KVObject("test", "success"));

            string text;
            using (var ms = new MemoryStream())
            {
                kv3.Serialize(ms, data);

                ms.Seek(0, SeekOrigin.Begin);
                using var reader = new StreamReader(ms);
                text = reader.ReadToEnd();
            }

            Assert.That(text, Is.EqualTo(expected));
        }

        [Test]
        public void SerializesAndKeepsLinearObjects() // TODO: Perhaps in the future KV1 arrays can use the KVArray type so it can be emitted as an array
        {
            using var stream = TestDataHelper.OpenResource("TextKV3.array_kv1.vdf");
            var expected = TestDataHelper.ReadTextResource("TextKV3.array_from_kv1.kv3");

            var kv1 = KVSerializer.Create(KVSerializationFormat.KeyValues1Text);
            var kv3 = KVSerializer.Create(KVSerializationFormat.KeyValues3Text);
            var data = kv1.Deserialize(stream);

            data.Add(new KVObject("test", "success"));

            string text;
            using (var ms = new MemoryStream())
            {
                kv3.Serialize(ms, data);

                ms.Seek(0, SeekOrigin.Begin);
                using var reader = new StreamReader(ms);
                text = reader.ReadToEnd();
            }

            Assert.That(text, Is.EqualTo(expected));
        }
    }
}
