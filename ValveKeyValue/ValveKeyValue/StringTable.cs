using System.Linq;

namespace ValveKeyValue
{
    /// <summary>
    /// Represents a string table for efficient binary serialization.
    /// </summary>
    public sealed class StringTable
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StringTable"/> class.
        /// </summary>
        public StringTable()
            : this(new List<string>(), writable: true)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StringTable"/> class with the specified capacity.
        /// </summary>
        /// <param name="capacity">The initial capacity.</param>
        public StringTable(int capacity)
            : this(new List<string>(capacity), writable: true)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="StringTable"/> class with the specified values.
        /// </summary>
        /// <param name="values">The initial string values.</param>
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

        /// <summary>
        /// Gets the string at the specified index.
        /// </summary>
        /// <param name="index">The zero-based index.</param>
        /// <returns>The string at the specified index.</returns>
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

        /// <summary>
        /// Adds a string to the table.
        /// </summary>
        /// <param name="value">The string to add.</param>
        public void Add(string value)
        {
            if (!writable)
            {
                throw new InvalidOperationException("Unable to add to read-only string table.");
            }

            lookup.Add(value);
            reverse.TryAdd(value, lookup.Count - 1);
        }

        /// <summary>
        /// Gets the index of a string, or adds it if not found.
        /// </summary>
        /// <param name="value">The string to find or add.</param>
        /// <returns>The index of the string.</returns>
        public int GetOrAdd(string value)
        {
            if (!reverse.TryGetValue(value, out var index))
            {
                Add(value);
                index = lookup.Count - 1;
            }

            return index;
        }

        /// <summary>
        /// Converts the string table to an array.
        /// </summary>
        /// <returns>An array of strings.</returns>
        public string[] ToArray() => lookup.ToArray();
    }
}
