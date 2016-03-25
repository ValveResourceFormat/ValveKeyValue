using System;
using System.Collections.Generic;

namespace ValveKeyValue
{
    static class StringExtensions
    {
        public static IEnumerable<string> Split(this string haystack, string delimeter, StringSplitOptions options)
        {
            Require.NotNull(haystack, nameof(haystack));
            Require.NotNull(delimeter, nameof(delimeter));

            var strings = new List<string>();
            var maxSubstring = haystack.Length - delimeter.Length;
            var startIndex = 0;

            for (int i = 0; i < maxSubstring; i++)
            {
                var possibleDelimeterText = haystack.Substring(i, delimeter.Length);
                if (!string.Equals(possibleDelimeterText, delimeter, StringComparison.Ordinal))
                {
                    continue;
                }

                var foundText = haystack.Substring(startIndex, i - startIndex);
                strings.Add(foundText);
                startIndex = i + delimeter.Length;
                i += delimeter.Length;
            }

            strings.Add(haystack.Substring(startIndex, haystack.Length - startIndex));

            return strings;
        }
    }
}
