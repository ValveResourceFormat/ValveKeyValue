using System.Collections.Generic;

namespace ValveKeyValue
{
    static class StringExtensions
    {
        public static IEnumerable<string> Split(this string haystack, string delimeter)
        {
            Require.NotNull(haystack, nameof(haystack));
            Require.NotNull(delimeter, nameof(delimeter));

            var strings = new List<string>();
            var maxSubstring = haystack.Length - delimeter.Length;
            var lastStartIndex = 0;
            var startIndex = 0;

            while ((startIndex = haystack.IndexOf(delimeter, startIndex)) >= 0)
            {
                var foundText = haystack.Substring(lastStartIndex, startIndex - lastStartIndex);
                strings.Add(foundText);
                startIndex += delimeter.Length;
                lastStartIndex = startIndex;
            }

            strings.Add(haystack.Substring(lastStartIndex, haystack.Length - lastStartIndex));

            return strings;
        }
    }
}
