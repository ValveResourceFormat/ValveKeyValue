using System.Globalization;
using System.Linq;

namespace ValveKeyValue.Test.BinaryKV2
{
    /// <summary>
    /// Compares two element graphs attribute by attribute, so a round trip is checked all the way
    /// down rather than at the root only.
    /// </summary>
    static class KV2GraphComparer
    {
        /// <summary>
        /// Asserts that two element graphs hold the same elements, attributes, values and order,
        /// and that shared references remain shared in the same places.
        /// </summary>
        public static void AssertSameGraph(KVObject expected, KVObject actual)
        {
            var differences = new List<string>();
            Compare((KV2Element)expected, (KV2Element)actual, "root", new Dictionary<KV2Element, KV2Element>(ReferenceEqualityComparer.Instance), differences);

            Assert.That(differences, Is.Empty, string.Join("\n", differences));
        }

        static void Compare(KV2Element expected, KV2Element actual, string path, Dictionary<KV2Element, KV2Element> seen, List<string> differences)
        {
            if (seen.TryGetValue(expected, out var alreadyPairedWith))
            {
                // The same element appeared earlier, so it must be the same instance here too,
                // otherwise a shared reference was duplicated by the round trip.
                if (!ReferenceEquals(alreadyPairedWith, actual))
                {
                    differences.Add($"{path}: shared element was not shared after the round trip");
                }

                return;
            }

            seen.Add(expected, actual);

            if (expected.ClassName != actual.ClassName || expected.Name != actual.Name || expected.ElementId != actual.ElementId)
            {
                differences.Add($"{path}: element identity differs ({expected.ClassName}/{expected.Name}/{expected.ElementId} vs {actual.ClassName}/{actual.Name}/{actual.ElementId})");
                return;
            }

            var expectedKeys = Keys(expected);
            var actualKeys = Keys(actual);

            if (!expectedKeys.SequenceEqual(actualKeys))
            {
                differences.Add($"{path}: attributes differ or are reordered ({string.Join(",", expectedKeys)} vs {string.Join(",", actualKeys)})");
                return;
            }

            foreach (var key in expectedKeys)
            {
                CompareValue(expected[key], actual[key], $"{path}.{key}", seen, differences);
            }
        }

        static void CompareValue(KVObject expected, KVObject actual, string path, Dictionary<KV2Element, KV2Element> seen, List<string> differences)
        {
            if (expected.ValueType != actual.ValueType)
            {
                differences.Add($"{path}: type differs ({expected.ValueType} vs {actual.ValueType})");
                return;
            }

            if (expected is KV2Element expectedElement)
            {
                if (actual is not KV2Element actualElement)
                {
                    differences.Add($"{path}: expected an element");
                    return;
                }

                if (ReferenceEquals(expectedElement, KV2Element.Null) || expectedElement.IsStub)
                {
                    if (expectedElement.ElementId != actualElement.ElementId)
                    {
                        differences.Add($"{path}: null or stub reference differs");
                    }

                    return;
                }

                Compare(expectedElement, actualElement, path, seen, differences);
                return;
            }

            if (expected.ValueType == KVValueType.ElementArray)
            {
                var expectedItems = expected.GetArray<KV2Element>();
                var actualItems = actual.GetArray<KV2Element>();

                if (expectedItems.Count != actualItems.Count)
                {
                    differences.Add($"{path}: element array length differs ({expectedItems.Count} vs {actualItems.Count})");
                    return;
                }

                for (var i = 0; i < expectedItems.Count; i++)
                {
                    CompareValue(expectedItems[i], actualItems[i], $"{path}[{i}]", seen, differences);
                }

                return;
            }

            if (expected.ValueType == KVValueType.BinaryBlob)
            {
                if (!expected.AsBlob().SequenceEqual(actual.AsBlob()))
                {
                    differences.Add($"{path}: blob differs");
                }

                return;
            }

            if (expected.IsTypedArray)
            {
                if (expected.Count != actual.Count)
                {
                    differences.Add($"{path}: array length differs ({expected.Count} vs {actual.Count})");
                }

                return;
            }

            var expectedText = expected.ToString(CultureInfo.InvariantCulture);
            var actualText = actual.ToString(CultureInfo.InvariantCulture);

            if (expectedText != actualText)
            {
                differences.Add($"{path}: value differs ({expectedText} vs {actualText})");
            }
        }

        static List<string> Keys(KVObject element)
        {
            var keys = new List<string>();

            foreach (var key in element.Keys)
            {
                keys.Add(key);
            }

            return keys;
        }
    }
}
