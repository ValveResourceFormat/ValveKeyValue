using System.IO;
using NUnit.Framework;

namespace ValveKeyValue.Test.TextKV3
{
    class Kv3ToKv1TestCase
    {
        [Test]
        public void SerializesAndDropsFlags()
        {
            using var stream = TestDataHelper.OpenResource("TextKV3.flagged_value.kv3");
            var expected = TestDataHelper.ReadTextResource("TextKV3.flagged_value_kv1.vdf");

            var kv1 = KVSerializer.Create(KVSerializationFormat.KeyValues1Text);
            var kv3 = KVSerializer.Create(KVSerializationFormat.KeyValues3Text);
            var data = kv3.Deserialize(stream);

            data.Add(new KVObject("test", "success"));

            string text;
            using (var ms = new MemoryStream())
            {
                kv1.Serialize(ms, data);

                ms.Seek(0, SeekOrigin.Begin);
                using var reader = new StreamReader(ms);
                text = reader.ReadToEnd();
            }

            Assert.That(text, Is.EqualTo(expected));
        }

        [Test]
        public void SerializesArraysToObjects()
        {
            using var stream = TestDataHelper.OpenResource("TextKV3.array.kv3");
            var expected = TestDataHelper.ReadTextResource("TextKV3.array_kv1.vdf");

            var kv1 = KVSerializer.Create(KVSerializationFormat.KeyValues1Text);
            var kv3 = KVSerializer.Create(KVSerializationFormat.KeyValues3Text);
            var data = kv3.Deserialize(stream);

            data.Add(new KVObject("test", "success"));

            string text;
            using (var ms = new MemoryStream())
            {
                kv1.Serialize(ms, data);

                ms.Seek(0, SeekOrigin.Begin);
                using var reader = new StreamReader(ms);
                text = reader.ReadToEnd();
            }

            Assert.That(text, Is.EqualTo(expected));
        }
    }
}
