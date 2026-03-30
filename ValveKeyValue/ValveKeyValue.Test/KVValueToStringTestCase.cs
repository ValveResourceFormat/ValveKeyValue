using System.Collections;
using System.Globalization;

namespace ValveKeyValue.Test
{
    class KVValueToStringTestCase
    {
        [TestCaseSource(nameof(ToStringTestCases))]
        public string KVObjectToStringIsSane(KVObject value) => value.ToString(CultureInfo.InvariantCulture);

        public static IEnumerable ToStringTestCases
        {
            get
            {
                yield return new TestCaseData(new KVObject("blah")).Returns("blah");
                yield return new TestCaseData(new KVObject("yay")).Returns("yay");
                yield return new TestCaseData(KVObject.Collection()).Returns("[Collection]").SetName("{m} - Empty Collection");
                var collection = KVObject.Collection();
                collection.Add("boo", "aah");
                yield return new TestCaseData(collection).Returns("[Collection]").SetName("{m} - Collection With Value");
            }
        }
    }
}
