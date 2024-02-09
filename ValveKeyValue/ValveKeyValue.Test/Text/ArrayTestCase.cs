using System.Linq;

namespace ValveKeyValue.Test
{
    [TestFixtureSource(typeof(TestFixtureSources), nameof(TestFixtureSources.SupportedEnumerableTypesForDeserialization))]
    class ArrayTestCase<TEnumerable>
        where TEnumerable : IEnumerable<string>
    {
        [Test]
        public void IsNotNull()
        {
            Assert.That(data, Is.Not.Null);
        }

        [Test]
        public void NumbersIsNotNullOrEmpty()
        {
            Assert.That(data.Numbers, Is.Not.Null);
            Assert.That(data.Numbers.Any(), Is.True);
        }

        [TestCase(0, "zero")]
        [TestCase(1, "one")]
        [TestCase(2, "two")]
        [TestCase(3, "three")]
        [TestCase(4, "four")]
        [TestCase(5, "five")]
        [TestCase(6, "six")]
        [TestCase(7, "seven")]
        [TestCase(8, "eight")]
        [TestCase(9, "nine")]
        [TestCase(10, "ten")]
        [TestCase(11, "eleven")]
        [TestCase(12, "twelve")]
        [TestCase(13, "thirteen")]
        public void NumbersListHasValue(int index, string expectedValue)
        {
            var actualValue = data.Numbers.ToArray()[index];
            Assert.That(actualValue, Is.EqualTo(expectedValue));
        }

        SerializedType data;

        [OneTimeSetUp]
        public void SetUp()
        {
            using var stream = TestDataHelper.OpenResource("Text.list_of_values.vdf");
            data = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize<SerializedType>(stream);
        }

        class SerializedType
        {
            public TEnumerable Numbers { get; set; }
        }
    }
}
