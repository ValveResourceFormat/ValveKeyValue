using System.Collections;
using System.Linq;
using NUnit.Framework;

namespace ValveKeyValue.Test
{
    class KVValueToStringTestCase
    {
        [Test]
        public void Foo()
        {
        }

        [TestCaseSource(nameof(ToStringTestCases))]
        public string KVValueToStringIsSane(KVValue value) => value.ToString();

        public static IEnumerable ToStringTestCases
        {
            get
            {
                yield return new TestCaseData(new KVObject("a", "blah").Value).Returns("blah");
                yield return new TestCaseData(new KVObject("a", "yay").Value).Returns("yay");
                yield return new TestCaseData(new KVObject("a", Enumerable.Empty<KVObject>()).Value).Returns("[Collection]");
                yield return new TestCaseData(new KVObject("a", new[] { new KVObject("boo", "aah") }).Value).Returns("[Collection]");
            }
        }
    }
}
