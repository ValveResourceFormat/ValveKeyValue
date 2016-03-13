using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace ValveKeyValue.Test
{
    static class TestFixtureSources
    {
        public static IEnumerable SupportedEnumerableTypesForDeserialization
        {
            get
            {
                yield return typeof(List<string>);
                yield return typeof(string[]);
                yield return typeof(Collection<string>);
                yield return typeof(IList<string>);
                yield return typeof(ICollection<string>);
                yield return typeof(ObservableCollection<string>);
            }
        }
    }
}
