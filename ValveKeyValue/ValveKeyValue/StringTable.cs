namespace ValveKeyValue
{
    public sealed class StringTable
    {
        public StringTable(Memory<string> values)
        {
            this.lookup = values;
        }

        readonly Memory<string> lookup;
        Dictionary<string, int> reverse;

        public string this[int index]
        {
            get
            {
                if (index < 0)
                {
                    throw new ArgumentOutOfRangeException(nameof(index), "Index must be non-negative.");
                }

                if (index >= lookup.Length)
                {
                    throw new ArgumentOutOfRangeException(nameof(index), index, "Index must be less than the number of strings in the table.");
                }

                return lookup.Span[index];
            }
        }

        public int IndexOf(string value)
        {
            if (reverse is null)
            {
                throw new InvalidOperationException("String table has not been prepared for serialization.");
            }

            return reverse[value];
        }

        public void PrepareForSerialization()
        {
            if (reverse is not null)
            {
                return;
            }

            reverse = new Dictionary<string, int>(capacity: lookup.Length, StringComparer.Ordinal);
            var span = lookup.Span;

            for (var i = 0; i < span.Length; i++)
            {
                var value = span[i];
                reverse[value] = i;
            }
        }
    }
}
