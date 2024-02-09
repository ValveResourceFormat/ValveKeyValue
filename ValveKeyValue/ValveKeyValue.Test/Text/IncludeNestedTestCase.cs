using System.Linq;

namespace ValveKeyValue.Test
{
    class IncludeNestedTestCase
    {
        [Test]
        public void IsNotNull()
        {
            Assert.That(data, Is.Not.Null);
        }

        [Test]
        public void HasThreeItems()
        {
            Assert.That(data.Count(), Is.EqualTo(3));
        }

        [TestCase("foo", "bar")]
        [TestCase("aaa", "bbb")]
        [TestCase("ccc", "ddd")]
        public void HasKeyWithValue(string key, string expectedValue)
        {
            var actualValue = (string)data[key];
            Assert.That(actualValue, Is.EqualTo(expectedValue));
        }

        KVObject data;

        [OneTimeSetUp]
        public void SetUp()
        {
            var options = new KVSerializerOptions { FileLoader = new StubIncludedFileLoader() };

            using var stream = TestDataHelper.OpenResource("Text.kv_with_include_nesting.vdf");
            data = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(stream, options);
        }

        sealed class StubIncludedFileLoader : IIncludedFileLoader
        {
            Stream IIncludedFileLoader.OpenFile(string filePath)
            {
                if (filePath == "kv_include_nested_once.vdf")
                {
                    return TestDataHelper.OpenResource("Text.kv_include_nested_once.vdf");
                }
                else if (filePath == "kv_include_nested_twice.vdf")
                {
                    return TestDataHelper.OpenResource("Text.kv_include_nested_twice.vdf");
                }
                else
                {
                    throw new InvalidDataException($"Received an unexpected base or include: {filePath}");
                }  
            }
        }
    }
}
