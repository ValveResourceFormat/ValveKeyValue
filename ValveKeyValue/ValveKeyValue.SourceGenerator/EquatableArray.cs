using System.Collections;
using System.Linq;

namespace ValveKeyValue.SourceGenerator
{
    // An immutable array with value equality, so that models stay comparable between generator runs.
    readonly struct EquatableArray<T> : IEquatable<EquatableArray<T>>, IReadOnlyList<T>
        where T : IEquatable<T>
    {
        public EquatableArray(IEnumerable<T> items)
        {
            array = items.ToArray();
        }

        readonly T[]? array;

        public static EquatableArray<T> Empty => default;

        public int Count => array?.Length ?? 0;

        public T this[int index] => array![index];

        public static bool operator ==(EquatableArray<T> left, EquatableArray<T> right) => left.Equals(right);

        public static bool operator !=(EquatableArray<T> left, EquatableArray<T> right) => !left.Equals(right);

        public bool Equals(EquatableArray<T> other)
        {
            if (Count != other.Count)
            {
                return false;
            }

            for (var i = 0; i < Count; i++)
            {
                if (!EqualityComparer<T>.Default.Equals(array![i], other.array![i]))
                {
                    return false;
                }
            }

            return true;
        }

        public override bool Equals(object? obj) => obj is EquatableArray<T> other && Equals(other);

        public override int GetHashCode()
        {
            var hash = 17;

            unchecked
            {
                for (var i = 0; i < Count; i++)
                {
                    hash = (hash * 31) + (array![i]?.GetHashCode() ?? 0);
                }
            }

            return hash;
        }

        public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)(array ?? [])).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
