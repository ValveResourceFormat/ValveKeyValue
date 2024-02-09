using System.Linq;

namespace ValveKeyValue.Test
{
    class BaseIncludeNestedTestCase
    {
        [Test]
        public void IsNotNull()
        {
            Assert.That(data, Is.Not.Null);
        }

        [Test]
        public void HasFourItems()
        {
            Assert.That(data.Count(), Is.EqualTo(4));
        }

        [TestCase("foo", "bar")]
        [TestCase("bar", "baz")]
        [TestCase("aaa", "bbb")]
        [TestCase("ccc", "ddd")]
        public void HasKeyWithValue(string key, string expectedValue)
        {
            var actualValue = (string)data[key];
            Assert.That(actualValue, Is.EqualTo(expectedValue), key);
        }

        KVObject data;

        [OneTimeSetUp]
        public void SetUp()
        {
            var options = new KVSerializerOptions { FileLoader = new StubIncludedFileLoader() };

            using var stream = TestDataHelper.OpenResource("Text.kv_with_base_nesting.vdf");
            data = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(stream, options);
        }

        sealed class StubIncludedFileLoader : IIncludedFileLoader
        {
            Stream IIncludedFileLoader.OpenFile(string filePath)
            {
                if (filePath == "kv_base_nested_once.vdf")
                {
                    return TestDataHelper.OpenResource("Text.kv_base_nested_once.vdf");
                }
                else if (filePath == "kv_base_nested_twice.vdf")
                {
                    return TestDataHelper.OpenResource("Text.kv_base_nested_twice.vdf");
                }
                else
                {
                    throw new InvalidDataException($"Received an unexpected base or include: {filePath}");
                }                
            }
        }
    }
}
