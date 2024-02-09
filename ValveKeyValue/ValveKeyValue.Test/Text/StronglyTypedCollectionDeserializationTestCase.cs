using System.Collections;
using System.Linq;

using SimpleObject = ValveKeyValue.Test.StronglyTypedCollectionDeserializationTestCase.SimpleObject;

namespace ValveKeyValue.Test
{
    class StronglyTypedCollectionDeserializationTestCase
    {
        public class RootObject<TCollection>
        {
            public TCollection Numbers { get; set; }
        }

        public class SimpleObject
        {
            public string Name { get; set; }
            public string Value { get; set; }
        }
    }

    [TestFixture(typeof(Dictionary<string, SimpleObject>))]
    class StronglyTypedDictionaryDeserializationTestCase<TDictionary>
        where TDictionary : IDictionary<string, SimpleObject>
    {
        [Test]
        public void CanDeserializeToObject()
        {
            StronglyTypedCollectionDeserializationTestCase.RootObject<TDictionary> rootObject;

            using (var resourceStream = TestDataHelper.OpenResource("Text.list_of_objects.vdf"))
            {
                rootObject = KVSerializer.Create(KVSerializationFormat.KeyValues1Text)
                    .Deserialize<StronglyTypedCollectionDeserializationTestCase.RootObject<TDictionary>>(resourceStream);
            }

            Assert.That(rootObject, Is.Not.Null);
            Assert.That(rootObject.Numbers, Is.Not.Null);
            Assert.That(rootObject.Numbers, Is.InstanceOf<TDictionary>());
            Assert.That(rootObject.Numbers, Has.Count.EqualTo(3));
            Assert.That(rootObject.Numbers["0"], Is.Not.Null);
            Assert.That(rootObject.Numbers["0"].Name, Is.EqualTo("zero"));
            Assert.That(rootObject.Numbers["0"].Value, Is.EqualTo("nothing"));

            Assert.That(rootObject.Numbers["1"], Is.Not.Null);
            Assert.That(rootObject.Numbers["1"].Name, Is.EqualTo("one"));
            Assert.That(rootObject.Numbers["1"].Value, Is.EqualTo("a bit"));

            Assert.That(rootObject.Numbers["2"], Is.Not.Null);
            Assert.That(rootObject.Numbers["2"].Name, Is.EqualTo("two"));
            Assert.That(rootObject.Numbers["2"].Value, Is.EqualTo("a bit more"));
        }
    }

    [TestFixture(typeof(SimpleObject[]))]
    [TestFixture(typeof(List<SimpleObject>))]
    class StronglyTypedCollectionDeserializationTestCase<TCollection>
        where TCollection : IEnumerable
    {
        [Test]
        public void CanDeserializeToObject()
        {
            StronglyTypedCollectionDeserializationTestCase.RootObject<TCollection> rootObject;

            using (var resourceStream = TestDataHelper.OpenResource("Text.list_of_objects.vdf"))
            {
                rootObject = KVSerializer.Create(KVSerializationFormat.KeyValues1Text)
                    .Deserialize<StronglyTypedCollectionDeserializationTestCase.RootObject<TCollection>>(resourceStream);
            }

            Assert.That(rootObject, Is.Not.Null);
            Assert.That(rootObject.Numbers, Is.Not.Null);
            Assert.That(rootObject.Numbers, Is.InstanceOf<TCollection>());

            var numbers = rootObject.Numbers.Cast<SimpleObject>().ToArray();
            Assert.That(numbers, Has.Length.EqualTo(3));

            Assert.That(numbers[0], Is.Not.Null);
            Assert.That(numbers[0].Name, Is.EqualTo("zero"));
            Assert.That(numbers[0].Value, Is.EqualTo("nothing"));

            Assert.That(numbers[1], Is.Not.Null);
            Assert.That(numbers[1].Name, Is.EqualTo("one"));
            Assert.That(numbers[1].Value, Is.EqualTo("a bit"));

            Assert.That(numbers[2], Is.Not.Null);
            Assert.That(numbers[2].Name, Is.EqualTo("two"));
            Assert.That(numbers[2].Value, Is.EqualTo("a bit more"));
        }
    }
}
