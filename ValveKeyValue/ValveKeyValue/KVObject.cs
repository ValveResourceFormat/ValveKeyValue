using System;
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

            Name = name;
            Value = value;
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

            Name = name;
            var value = new KVChildrenValue();
            value.AddRange(items);

            Value = value;
        }

        /// <summary>
        /// Gets the name of this object.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the value of this object.
        /// </summary>
        public KVValue Value { get; }

        /// <summary>
        /// Indexer to find a child item by name.
        /// </summary>
        /// <param name="key">Key of the child object to find</param>
        /// <returns>A <see cref="KVObject"/> if the child item exists, otherwise <c>null</c>.</returns>
        public KVValue this[string key]
        {
            get
            {
                Require.NotNull(key, nameof(key));

                var children = GetChildrenValue();
                return children[key];
            }

            set
            {
                Require.NotNull(key, nameof(key));

                var children = GetChildrenValue();
                children.Set(key, value);
            }
        }

        /// <summary>
        /// Adds a <see cref="KVObject" /> as a child of the current object.
        /// </summary>
        /// <param name="value">The child to add.</param>
        public void Add(KVObject value)
        {
            Require.NotNull(value, nameof(value));
            GetChildrenValue().Add(value);
        }

        /// <summary>
        /// Gets the children of this <see cref="KVObject"/>.
        /// </summary>
        public IEnumerable<KVObject> Children => (Value as KVChildrenValue) ?? Enumerable.Empty<KVObject>();

        KVChildrenValue GetChildrenValue()
        {
            var children = Value as KVChildrenValue;
            if (children == null)
            {
                throw new InvalidOperationException($"This operation on a {nameof(KVObject)} can only be used when the value has children.");
            }

            return children;
        }
    }
}
