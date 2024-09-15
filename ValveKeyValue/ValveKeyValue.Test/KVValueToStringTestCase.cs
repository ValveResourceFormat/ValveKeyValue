using System.Collections;
using System.Linq;

namespace ValveKeyValue.Test
{
    class KVValueToStringTestCase
    {
        [TestCaseSource(nameof(ToStringTestCases))]
        public string KVValueToStringIsSane(KVValue value) => value.ToString();

        public static IEnumerable ToStringTestCases
        {
            get
            {
                yield return new TestCaseData(new KVObject("a", "blah").Value).Returns("blah");
                yield return new TestCaseData(new KVObject("a", "yay").Value).Returns("yay");
                yield return new TestCaseData(new KVObject("a", []).Value).Returns("[Collection]").SetName("{m} - Empty Collection");
                yield return new TestCaseData(new KVObject("a", [new KVObject("boo", "aah")]).Value).Returns("[Collection]").SetName("{m} - Collection With Value");
            }
        }
    }
}
