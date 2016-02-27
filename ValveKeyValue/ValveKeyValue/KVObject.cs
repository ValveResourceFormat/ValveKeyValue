using System.Collections.Generic;
using System.Linq;

namespace ValveKeyValue
{
    /// <summary>
    /// Represents a dynamic KeyValue object.
    /// </summary>
    public partial class KVObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="KVObject"/> class.
        /// </summary>
        /// <param name="name">Name of this object.</param>
        /// <param name="value">Value of this object.</param>
        public KVObject(string name, KVValue value)
        {
            Require.NotNull(name, nameof(name));
            Require.NotNull(value, nameof(value));

            this.name = name;
            this.value = value;
            items = new List<KVObject>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KVObject"/> class.
        /// </summary>
        /// <param name="name">Name of this object.</param>
        /// <param name="items">Child items of this object.</param>
        public KVObject(string name, IEnumerable<KVObject> items)
        {
            Require.NotNull(name, nameof(name));
            Require.NotNull(items, nameof(items));

            this.name = name;
            this.items = items.ToList();
        }

        readonly string name;
        readonly KVValue value;
        readonly IList<KVObject> items;

        /// <summary>
        /// Gets the name of this object.
        /// </summary>
        public string Name => name;

        /// <summary>
        /// Gets the value of this object.
        /// </summary>
        public KVValue Value => value;

        /// <summary>
        /// Gets children of this object, if any.
        /// </summary>
        public IEnumerable<KVObject> Items => items;

        /// <summary>
        /// Indexer to find a child item by name.
        /// </summary>
        /// <param name="key">Key of the child object to find</param>
        /// <returns>A <see cref="KVObject"/> if the child item exists, otherwise <c>null</c>.</returns>
        public KVObject this[string key]
        {
            get
            {
                return Items.SingleOrDefault(kv => kv.Name == key);
            }
        }
    }
}
