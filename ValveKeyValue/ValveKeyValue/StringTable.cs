using System.Linq;

namespace ValveKeyValue
{
    public sealed class StringTable
    {
        public StringTable()
            : this(new List<string>(), writable: true)
        {
        }

        public StringTable(int capacity)
            : this(new List<string>(capacity), writable: true)
        {
        }

        public StringTable(IList<string> values)
            : this(values, writable: !values.IsReadOnly)
        {
        }

        StringTable(IList<string> values, bool writable)
        {
            this.lookup = values;
            this.writable = writable;

            reverse = new Dictionary<string, int>(capacity: lookup.Count, StringComparer.Ordinal);

            for (var i = 0; i < lookup.Count; i++)
            {
                var value = lookup[i];
                reverse[value] = i;
            }
        }


        readonly IList<string> lookup;
        readonly bool writable;
        readonly Dictionary<string, int> reverse;

        public string this[int index]
        {
            get
            {
                if (index < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(index), "Index must be non-negative.");
                }

                if (index >= lookup.Count)
                {
                    throw new ArgumentOutOfRangeException(nameof(index), index, "Index must be less than the number of strings in the table.");
                }

                return lookup[index];
            }
        }
        
        public void Add(string value)
        {
            if (!writable)
            {
                throw new InvalidOperationException("Unable to add to read-only string table.");
            }

            lookup.Add(value);
            reverse.TryAdd(value, lookup.Count - 1);
        }

        public int GetOrAdd(string value)
        {
            if (!reverse.TryGetValue(value, out var index))
            {
                Add(value);
                index = lookup.Count - 1;
            }

            return index;
        }

        public string[] ToArray() => lookup.ToArray();
    }
}
