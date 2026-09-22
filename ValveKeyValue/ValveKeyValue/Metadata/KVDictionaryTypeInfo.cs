using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;

namespace ValveKeyValue.Metadata
{
    // Maps a dictionary to a KeyValues collection whose keys are the dictionary keys. Dictionary<TKey, TValue> and the
    // interfaces IDictionary<TKey, TValue> and IReadOnlyDictionary<TKey, TValue> can be deserialized; any other
    // dictionary type can only be serialized.
    sealed class KVDictionaryTypeInfo<TDictionary, TKey, TValue> : KVTypeInfo<TDictionary>
        where TDictionary : IEnumerable<KeyValuePair<TKey, TValue>>
        where TKey : notnull
    {
        public KVDictionaryTypeInfo(KVTypeInfo<TKey> key, KVTypeInfo<TValue> value)
        {
            this.key = key;
            this.value = value;
        }

        static readonly bool IsConstructible =
            typeof(TDictionary) == typeof(Dictionary<TKey, TValue>)
            || typeof(TDictionary) == typeof(IDictionary<TKey, TValue>)
            || typeof(TDictionary) == typeof(IReadOnlyDictionary<TKey, TValue>);

        readonly KVTypeInfo<TKey> key;
        readonly KVTypeInfo<TValue> value;

        internal override TDictionary ReadCore(KVObject value)
        {
            ThrowIfNotCollection(value);

            if (!IsConstructible)
            {
                throw EnumerableNotSupported();
            }

            var dictionary = new Dictionary<TKey, TValue>(value.Count);

            // The first occurrence of a key wins, and the values of later occurrences are not read.
            foreach (var (childKey, child) in value.EnumerateChildren())
            {
                var typedKey = typeof(TKey) == typeof(string) ? (TKey)(object)childKey : key.Read(new KVObject(childKey));
                ref var entry = ref CollectionsMarshal.GetValueRefOrAddDefault(dictionary, typedKey, out var exists);

                if (!exists)
                {
                    entry = this.value.Read(child);
                }
            }

            return (TDictionary)(object)dictionary;
        }

        internal override KVObject Write(TDictionary value, KVWriteContext context)
        {
            context.Enter(value);

            var children = value.TryGetNonEnumeratedCount(out var count)
                ? new List<KeyValuePair<string, KVObject>>(count)
                : [];

            foreach (var (entryKey, entryValue) in value)
            {
                var name = Convert.ToString(entryKey, CultureInfo.InvariantCulture)!;
                children.Add(new KeyValuePair<string, KVObject>(name, this.value.Write(entryValue, context)));
            }

            return new KVObject(KVValueType.Collection, children);
        }
    }
}
