using System.IO;
using NUnit.Framework;

namespace ValveKeyValue.Test.TextKV3
{
    class SerializationTestCase
    {
        [Test]
        public void CreatesTextDocument()
        {
            using var stream = TestDataHelper.OpenResource("TextKV3.types.kv3");
            var expected = TestDataHelper.ReadTextResource("TextKV3.types_serialized.kv3");

            var kv = KVSerializer.Create(KVSerializationFormat.KeyValues3Text);
            var data = kv.Deserialize(stream);

            data.Add(new KVObject("multiLineString", "hello\nworld"));

            string text;
            using (var ms = new MemoryStream())
            {
                kv.Serialize(ms, data);

                ms.Seek(0, SeekOrigin.Begin);
                using var reader = new StreamReader(ms);
                text = reader.ReadToEnd();
            }

            Assert.That(text, Is.EqualTo(expected));
        }

        [Test]
        public void SerializesArray()
        {
            using var stream = TestDataHelper.OpenResource("TextKV3.array.kv3");
            var expected = TestDataHelper.ReadTextResource("TextKV3.array_serialized.kv3");

            var kv = KVSerializer.Create(KVSerializationFormat.KeyValues3Text);
            var data = kv.Deserialize(stream);

            data.Add(new KVObject("test", "success"));

            string text;
            using (var ms = new MemoryStream())
            {
                kv.Serialize(ms, data);

                ms.Seek(0, SeekOrigin.Begin);
                using var reader = new StreamReader(ms);
                text = reader.ReadToEnd();
            }

            Assert.That(text, Is.EqualTo(expected));
        }

        [Test]
        public void SerializesNestedArray()
        {
            var expected = TestDataHelper.ReadTextResource("TextKV3.array_nested.kv3");

            var kv = KVSerializer.Create(KVSerializationFormat.KeyValues3Text);
            var data = kv.Deserialize(expected);

            string text;
            using (var ms = new MemoryStream())
            {
                kv.Serialize(ms, data);

                ms.Seek(0, SeekOrigin.Begin);
                using var reader = new StreamReader(ms);
                text = reader.ReadToEnd();
            }

            Assert.That(text, Is.EqualTo(expected));
        }

        [Test]
        public void SerializesFlags()
        {
            using var stream = TestDataHelper.OpenResource("TextKV3.flagged_value.kv3");
            var expected = TestDataHelper.ReadTextResource("TextKV3.flagged_value_serialized.kv3");

            var kv = KVSerializer.Create(KVSerializationFormat.KeyValues3Text);
            var data = kv.Deserialize(stream);

            data.Add(new KVObject("test", "success"));

            string text;
            using (var ms = new MemoryStream())
            {
                kv.Serialize(ms, data);

                ms.Seek(0, SeekOrigin.Begin);
                using var reader = new StreamReader(ms);
                text = reader.ReadToEnd();
            }

            Assert.That(text, Is.EqualTo(expected));
        }
    }
}
