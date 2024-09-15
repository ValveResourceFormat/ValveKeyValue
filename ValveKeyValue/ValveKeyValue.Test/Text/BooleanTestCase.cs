using System.Text;

namespace ValveKeyValue.Test
{
    class BooleanTestCase
    {
        [Test]
        public void DynamicDeserialization()
        {
            using var stream = TestDataHelper.OpenResource("Text.boolean.vdf");
            var data = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(stream);

            Assert.Multiple(() =>
            {
                Assert.That((bool)data["test1_false"], Is.False, "test1_false");
                Assert.That((bool)data["test2_true"], Is.True, "test2_true");
                Assert.That((bool)data["test3_oob"], Is.True, "test3_oob");
            });
        }

        [Test]
        public void StronglyTypedDeserialization()
        {
            using var stream = TestDataHelper.OpenResource("Text.boolean.vdf");
            var data = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize<SerializedType>(stream);

            Assert.Multiple(() =>
            {
                Assert.That(data.test1_false, Is.False, nameof(data.test1_false));
                Assert.That(data.test2_true, Is.True, nameof(data.test2_true));
                Assert.That(data.test3_oob, Is.True, nameof(data.test3_oob));
            });
        }
        
        [Test]
        public void DynamicSerialization()
        {
            var data = new KVObject("object",
            [
                new KVObject("test1_false", false),
                new KVObject("test2_true", true),
            ]);

            var expected = TestDataHelper.ReadTextResource("Text.boolean_serialization.vdf");

            using var ms = new MemoryStream();
            KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Serialize(ms, data);
            var text = Encoding.UTF8.GetString(ms.ToArray());

            Assert.That(text, Is.EqualTo(expected));
        }
        
        [Test]
        public void StronglyTypedSerialization()
        {
            var data = new
            {
                test1_false = false,
                test2_true = true,
            };

            var expected = TestDataHelper.ReadTextResource("Text.boolean_serialization.vdf");

            using var ms = new MemoryStream();
            KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Serialize(ms, data, "object");
            var text = Encoding.UTF8.GetString(ms.ToArray());

            Assert.That(text, Is.EqualTo(expected));
        }

        class SerializedType
        {
#pragma warning disable IDE1006 // Naming Styles
            public bool test1_false { get; set; }
            public bool test2_true { get; set; }
            public bool test3_oob { get; set; }
#pragma warning restore IDE1006
        }
    }
}
