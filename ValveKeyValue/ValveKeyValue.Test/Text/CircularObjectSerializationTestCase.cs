using System.Text;

namespace ValveKeyValue.Test
{
    class CircularObjectSerializationTestCase
    {
        [Test]
        public void ThrowsException()
        {
            var dataObject1 = new DataObject
            {
                Name = "First"
            };

            var dataObject2 = new DataObject
            {
                Name = "Second",
                Other = dataObject1
            };

            dataObject1.Other = dataObject2;

            Assert.That(dataObject1.Other, Is.SameAs(dataObject2), "Sanity check");
            Assert.That(dataObject2.Other, Is.SameAs(dataObject1), "Sanity check");

            using var ms = new MemoryStream();
            Assert.That(
                () => KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Serialize(ms, dataObject1, "test data"),
                Throws.Exception.InstanceOf<KeyValueException>()
                .With.Message.EqualTo("Serialization failed - circular object reference detected."));
        }

        [Test]
        public void DuplicatePrimitiveValuesAreNotCircularObjectReference()
        {
            var dataObject = new DataObjectWithList
            {
                Strings = { "test", "test" },
                Ints = { 1, 2, 1 }
            };

            string text;
            using (var ms = new MemoryStream())
            using (var reader = new StreamReader(ms, Encoding.UTF8))
            {
                KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Serialize(ms, dataObject, "test");

                ms.Seek(0, SeekOrigin.Begin);
                text = reader.ReadToEnd();
            }

            var expected = TestDataHelper.ReadTextResource("Text.non_circular_list.vdf");
            Assert.That(text, Is.EqualTo(expected));
        }

        class DataObject
        {
            public string Name { get; set; }

            public DataObject Other { get; set; }
        }

        public class DataObjectWithList
        {
            public List<string> Strings { get; set; } = new List<string>();
            public List<int> Ints { get; set; } = new List<int>();
        }
    }
}