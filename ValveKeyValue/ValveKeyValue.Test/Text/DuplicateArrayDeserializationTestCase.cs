using System.Linq;

namespace ValveKeyValue.Test
{
    class DuplicateArrayDeserializationTestCase
    {
        [Test]
        public void IsNotNull()
        {
            Assert.That(data, Is.Not.Null);
        }

        [Test]
        public void HasTwoChildren()
        {
            Assert.That(data.Children?.Count(), Is.EqualTo(2));
        }

        [Test]
        public void IndexerUsesFirstChild()
        {
            var child = data["Values"];
            Assert.That(child?.ValueType, Is.EqualTo(KVValueType.Collection));

            var valueObjects = child!.ToArray();
            Assert.That(valueObjects, Has.Length.EqualTo(2));

            Assert.That((string)valueObjects[0].Value!["name"]!, Is.EqualTo("first"));
            Assert.That((string)valueObjects[1].Value!["name"]!, Is.EqualTo("second"));
        }

        [Test]
        public void BothChildrenArePresent()
        {
            var children = data.ToArray();
            Assert.That(children, Has.Length.EqualTo(2));

            var firstNode = children[0].Value;
            Assert.That(firstNode, Is.Not.Null);

            var firstArray = firstNode!.ToArray();
            Assert.That(firstArray, Has.Length.EqualTo(2));
            Assert.That((string)firstArray[0].Value!["name"]!, Is.EqualTo("first"));
            Assert.That((string)firstArray[1].Value!["name"]!, Is.EqualTo("second"));

            var secondNode = children[1].Value;
            Assert.That(secondNode, Is.Not.Null);

            var secondArray = secondNode!.ToArray();
            Assert.That(secondArray, Has.Length.EqualTo(2));
            Assert.That((string)secondArray[0].Value!["name"]!, Is.EqualTo("third"));
            Assert.That((string)secondArray[1].Value!["name"]!, Is.EqualTo("fourth"));
        }

        KVObject data;

        [OneTimeSetUp]
        public void SetUp()
        {
            using var stream = TestDataHelper.OpenResource("Text.duplicate_lists.vdf");
            data = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(stream);
        }
    }
}
