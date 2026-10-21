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
    }
}
