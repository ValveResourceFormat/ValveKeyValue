using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;

namespace ValveKeyValue.Metadata
{
    // Maps a collection to a KeyValues array or to a collection whose keys are the element indices. Arrays, List<T>,
    // Collection<T>, ObservableCollection<T> and the interfaces that they implement can be deserialized; any other
    // collection type can only be serialized.
    sealed class KVCollectionTypeInfo<TCollection, TElement> : KVTypeInfo<TCollection>
        where TCollection : IEnumerable<TElement>
    {
        public KVCollectionTypeInfo(KVTypeInfo<TElement> element)
        {
            this.element = element;
        }

        const int StackAllocThreshold = 256;

        static readonly Construction Kind = GetConstruction();

        readonly KVTypeInfo<TElement> element;

        enum Construction
        {
            None,
            Array,
            List,
            Collection,
            ObservableCollection,
        }

        internal override TCollection ReadCore(KVObject value)
        {
            switch (value.ValueType)
            {
                case KVValueType.Collection:
                    return Kind != Construction.None ? Construct(value) : throw EnumerableNotSupported();

                case KVValueType.Array:
                    return Kind != Construction.None ? Construct(value) : throw ArrayNotSupported();

                default:
                    throw ConversionNotSupported(value);
            }
        }

        internal override KVObject Write(TCollection value, KVWriteContext context)
        {
            context.Enter(value);

            var children = value.TryGetNonEnumeratedCount(out var count)
                ? new List<KeyValuePair<string, KVObject>>(count)
                : [];

            foreach (var item in value)
            {
                children.Add(new KeyValuePair<string, KVObject>(GetIndexKey(children.Count), element.Write(item, context)));
            }

            return new KVObject(KVValueType.Collection, children);
        }

        TCollection Construct(KVObject value)
        {
            var count = value.Count;

            if (Kind == Construction.Array)
            {
                var array = new TElement[count];
                ReadElements(value, array);
                return (TCollection)(object)array;
            }

            var list = new List<TElement>(count);
            CollectionsMarshal.SetCount(list, count);
            ReadElements(value, CollectionsMarshal.AsSpan(list));

            return Kind switch
            {
                Construction.Collection => (TCollection)(object)new Collection<TElement>(list),
                Construction.ObservableCollection => (TCollection)(object)new ObservableCollection<TElement>(list),
                _ => (TCollection)(object)list,
            };
        }

        // A KeyValues array is read in order. A collection is read as an array when its keys are exactly the integers
        // 0 to n-1, in any order, and each element is stored at the index of its key.
        void ReadElements(KVObject value, Span<TElement> target)
        {
            if (value.ValueType == KVValueType.Array)
            {
                var source = value.AsArraySpan();

                for (var i = 0; i < source.Length; i++)
                {
                    target[i] = element.Read(source[i]);
                }

                return;
            }

            Span<bool> seen = target.Length <= StackAllocThreshold ? stackalloc bool[target.Length] : new bool[target.Length];

            foreach (var (key, child) in value.EnumerateChildren())
            {
                if (!int.TryParse(key, NumberStyles.Number, CultureInfo.InvariantCulture, out var index)
                    || (uint)index >= (uint)seen.Length
                    || seen[index])
                {
                    throw new InvalidOperationException($"Cannot deserialize a non-array value to type \"{typeof(TCollection).Namespace}.{typeof(TCollection).Name}\".");
                }

                seen[index] = true;
                target[index] = element.Read(child);
            }
        }

        static Construction GetConstruction()
        {
            var type = typeof(TCollection);

            if (type == typeof(TElement[]))
            {
                return Construction.Array;
            }

            if (type == typeof(List<TElement>)
                || type == typeof(IList<TElement>)
                || type == typeof(IReadOnlyList<TElement>)
                || type == typeof(IReadOnlyCollection<TElement>)
                || type == typeof(IEnumerable<TElement>))
            {
                return Construction.List;
            }

            if (type == typeof(Collection<TElement>) || type == typeof(ICollection<TElement>))
            {
                return Construction.Collection;
            }

            if (type == typeof(ObservableCollection<TElement>))
            {
                return Construction.ObservableCollection;
            }

            return Construction.None;
        }
    }
}
