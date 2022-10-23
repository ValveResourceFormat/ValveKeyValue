using System.IO;
using System.Linq;
using NUnit.Framework;

namespace ValveKeyValue.Test
{
    class IncludeTestCase
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
        [TestCase("bar", "baz")]
        [TestCase("baz", "nada")]
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

            using var stream = TestDataHelper.OpenResource("Text.kv_with_include.vdf");
            data = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(stream, options);
        }

        sealed class StubIncludedFileLoader : IIncludedFileLoader
        {
            Stream IIncludedFileLoader.OpenFile(string filePath)
            {
                if (filePath == "file.vdf")
                {
                    return TestDataHelper.OpenResource("Text.kv_included.vdf");
                }
                else if (filePath == "this_file_does_not_exist.vdf")
                {
                    return null;
                }
                else
                {
                    throw new InvalidDataException($"Received an unexpected base or include: {filePath}");
                } 
            }
        }
    }
}
