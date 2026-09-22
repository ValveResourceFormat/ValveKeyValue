using System.Linq;

namespace ValveKeyValue.Metadata
{
    // Maps a lookup to a KeyValues collection that may contain duplicate keys. Each grouping is written as a
    // collection of its values keyed by their indices, so the grouping keys are not preserved.
    sealed class KVLookupTypeInfo<TValue> : KVTypeInfo<ILookup<string, TValue>>
    {
        public KVLookupTypeInfo(KVTypeInfo<TValue> value)
        {
            this.value = value;
            groupings = new(new KVCollectionTypeInfo<IGrouping<string, TValue>, TValue>(value));
        }

        readonly KVTypeInfo<TValue> value;
        readonly KVCollectionTypeInfo<ILookup<string, TValue>, IGrouping<string, TValue>> groupings;

        internal override ILookup<string, TValue> ReadCore(KVObject value)
        {
            ThrowIfNotCollection(value);

            var entries = new KeyValuePair<string, TValue>[value.Count];
            var index = 0;

            foreach (var (key, child) in value.EnumerateChildren())
            {
                entries[index++] = new KeyValuePair<string, TValue>(key, this.value.Read(child));
            }

            return entries.ToLookup(static entry => entry.Key, static entry => entry.Value);
        }

        internal override KVObject Write(ILookup<string, TValue> value, KVWriteContext context) => groupings.Write(value, context);
    }
}
