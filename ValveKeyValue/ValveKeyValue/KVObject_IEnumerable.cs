using System.Collections;

namespace ValveKeyValue
{
    public partial class KVObject
    {
        /// <inheritdoc/>
        public IEnumerator<KeyValuePair<string, KVObject>> GetEnumerator()
            => Children.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => Children.GetEnumerator();

        // Enumerates the named children of a collection (empty for any other value)
        // with the backing store's struct enumerator, so nothing is boxed.
        internal CollectionEnumerator EnumerateNamedChildren() => new(ValueType == KVValueType.Collection ? _ref : null);

        internal struct CollectionEnumerator
        {
            public CollectionEnumerator(object? storage)
            {
                switch (storage)
                {
                    case Dictionary<string, KVObject> dictionary:
                        dictionaryEnumerator = dictionary.GetEnumerator();
                        isDictionary = true;
                        break;

                    case List<KeyValuePair<string, KVObject>> list:
                        listEnumerator = list.GetEnumerator();
                        isList = true;
                        break;
                }
            }

            Dictionary<string, KVObject>.Enumerator dictionaryEnumerator;
            List<KeyValuePair<string, KVObject>>.Enumerator listEnumerator;
            readonly bool isDictionary;
            readonly bool isList;

            public readonly KeyValuePair<string, KVObject> Current
            {
                get
                {
                    if (isDictionary)
                    {
                        return dictionaryEnumerator.Current;
                    }

                    return listEnumerator.Current;
                }
            }

            public bool MoveNext()
            {
                if (isDictionary)
                {
                    return dictionaryEnumerator.MoveNext();
                }

                if (isList)
                {
                    return listEnumerator.MoveNext();
                }

                return false;
            }

            public readonly CollectionEnumerator GetEnumerator() => this;
        }

        // The values of a packed array. Reads go through the owner, so the view stays correct
        // after the owner is unpacked.
        internal sealed class PackedArrayValues : IReadOnlyList<KVObject>
        {
            public PackedArrayValues(KVObject owner)
            {
                this.owner = owner;
            }

            readonly KVObject owner;

            public int Count => owner.Count;

            public KVObject this[int index] => owner[index];

            public IEnumerator<KVObject> GetEnumerator()
            {
                for (var i = 0; i < owner.Count; i++)
                {
                    yield return owner[i];
                }
            }

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        }
    }
}
