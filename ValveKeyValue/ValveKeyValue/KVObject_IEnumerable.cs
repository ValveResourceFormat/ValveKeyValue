using System.Collections;
using System.Runtime.InteropServices;

namespace ValveKeyValue
{
    public partial class KVObject
    {
        /// <inheritdoc/>
        public IEnumerator<KeyValuePair<string, KVObject>> GetEnumerator()
            => Children.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => Children.GetEnumerator();

        // Enumerates the children of a collection without allocating. Any other value has no children.
        internal ChildEnumerator EnumerateChildren() => new(ValueType == KVValueType.Collection ? _ref : null);

        internal ref struct ChildEnumerator
        {
            public ChildEnumerator(object? children)
            {
                switch (children)
                {
                    case Dictionary<string, KVObject> dictionary:
                        isDictionary = true;
                        dictionaryEnumerator = dictionary.GetEnumerator();
                        break;
                    case List<KeyValuePair<string, KVObject>> list:
                        this.list = CollectionsMarshal.AsSpan(list);
                        break;
                }

                index = -1;
            }

            readonly bool isDictionary;
            readonly ReadOnlySpan<KeyValuePair<string, KVObject>> list;
            Dictionary<string, KVObject>.Enumerator dictionaryEnumerator;
            int index;

            public KeyValuePair<string, KVObject> Current { get; private set; }

            public readonly ChildEnumerator GetEnumerator() => this;

            public bool MoveNext()
            {
                if (isDictionary)
                {
                    if (!dictionaryEnumerator.MoveNext())
                    {
                        return false;
                    }

                    Current = dictionaryEnumerator.Current;
                    return true;
                }

                if (++index >= list.Length)
                {
                    return false;
                }

                Current = list[index];
                return true;
            }
        }
    }
}
