using System.Collections;
using System.Collections.ObjectModel;
using NUnit.Framework.Internal;

namespace ValveKeyValue.Test
{
    static class TestFixtureSources
    {
        public static IEnumerable SupportedEnumerableTypesForDeserialization
        {
            get
            {
                yield return CreateTestFixtureDataForGenericTest(typeof(List<string>));
                yield return CreateTestFixtureDataForGenericTest(typeof(string[]));
                yield return CreateTestFixtureDataForGenericTest(typeof(Collection<string>));
                yield return CreateTestFixtureDataForGenericTest(typeof(IList<string>));
                yield return CreateTestFixtureDataForGenericTest(typeof(ICollection<string>));
                yield return CreateTestFixtureDataForGenericTest(typeof(ObservableCollection<string>));
            }
        }

        static TestFixtureParameters CreateTestFixtureDataForGenericTest(Type genericType)
        {
            var data = new TestFixtureAttribute
            {
                TypeArgs = [genericType]
            };
            var parameters = new TestFixtureParameters(data);
            return parameters;
        }
    }
}
