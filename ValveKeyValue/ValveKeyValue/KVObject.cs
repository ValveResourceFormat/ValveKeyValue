using System.Diagnostics;
using System.Linq;

namespace ValveKeyValue
{
    /// <summary>
    /// Represents a dynamic KeyValue object.
    /// </summary>
    [DebuggerDisplay("{DebuggerDescription}")]
    public partial class KVObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="KVObject"/> class with an empty collection value.
        /// </summary>
        /// <param name="name">Name of this object.</param>
        public KVObject(string name)
            : this(name, Array.Empty<KVObject>())
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KVObject"/> class.
        /// </summary>
        /// <param name="name">Name of this object.</param>
        /// <param name="value">Value of this object.</param>
        public KVObject(string name, KVValue value)
        {
            //ArgumentNullException.ThrowIfNull(name);  // Objects in an array will not have a name
            ArgumentNullException.ThrowIfNull(value);

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
            //ArgumentNullException.ThrowIfNull(name); // Objects in an array will not have a name
            ArgumentNullException.ThrowIfNull(items);

            Name = name;
            var value = new KVCollectionValue();
            value.AddRange(items);

            Value = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KVObject"/> class.
        /// </summary>
        /// <param name="name">Name of this object.</param>
        /// <param name="items">Child items of this object.</param>
        public KVObject(string name, IEnumerable<KVValue> items)
        {
            //ArgumentNullException.ThrowIfNull(name); // Objects in an array will not have a name
            ArgumentNullException.ThrowIfNull(items);

            Name = name;
            var value = new KVArrayValue();
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
        /// Gets the number of children in this object's collection or array value.
        /// Returns 0 if the value is neither a collection nor an array.
        /// </summary>
        public int Count => Value switch
        {
            KVCollectionValue c => c.Count,
            KVArrayValue a => a.Count,
            _ => 0,
        };

        /// <summary>
        /// Gets a value indicating whether this object's value is an array.
        /// </summary>
        public bool IsArray => Value is KVArrayValue;

        /// <summary>
        /// Indexer to find a child item by name.
        /// </summary>
        /// <param name="key">Key of the child object to find.</param>
        /// <returns>A <see cref="KVValue"/> if the child item exists, otherwise <c>null</c>.</returns>
        public KVValue this[string key]
        {
            get
            {
                if (Value is not KVCollectionValue collection)
                {
                    return null;
                }

                return collection[key];
            }

            set
            {
                var children = GetCollectionValue();
                children.Set(key, value);
            }
        }

        /// <summary>
        /// Indexer to access an array element by index.
        /// </summary>
        /// <param name="index">The array index.</param>
        /// <returns>The <see cref="KVValue"/> at the specified index.</returns>
        public KVValue this[int index]
        {
            get
            {
                if (Value is KVArrayValue array)
                {
                    return array[index];
                }

                throw new NotSupportedException($"Integer indexer on a {nameof(KVObject)} can only be used when the value is an array.");
            }
        }

        /// <summary>
        /// Adds a <see cref="KVObject" /> as a child of the current object.
        /// </summary>
        /// <param name="value">The child to add.</param>
        public void Add(KVObject value)
        {
            GetCollectionValue().Add(value);
        }

        /// <summary>
        /// Adds a value to this object's array.
        /// </summary>
        /// <param name="value">The value to add.</param>
        public void Add(KVValue value)
        {
            if (Value is not KVArrayValue array)
            {
                throw new InvalidOperationException($"This operation on a {nameof(KVObject)} can only be used when the value is an array.");
            }

            array.Add(value);
        }

        /// <summary>
        /// Adds a named value as a child of the current object.
        /// </summary>
        /// <param name="name">Name of the child.</param>
        /// <param name="value">Value of the child.</param>
        public void AddProperty(string name, KVValue value)
        {
            Add(new KVObject(name, value));
        }

        /// <summary>
        /// Gets a child <see cref="KVObject"/> by name.
        /// </summary>
        /// <param name="name">Name of the child to find.</param>
        /// <returns>The child <see cref="KVObject"/>, or <c>null</c> if not found.</returns>
        public KVObject GetChild(string name)
        {
            if (Value is KVCollectionValue collection)
            {
                return collection.Get(name);
            }

            return null;
        }

        /// <summary>
        /// Determines whether this object contains a child with the given name.
        /// </summary>
        /// <param name="name">The name to check for.</param>
        /// <returns><c>true</c> if a child with the given name exists; otherwise, <c>false</c>.</returns>
        public bool ContainsKey(string name)
        {
            if (Value is KVCollectionValue collection)
            {
                return collection.Get(name) != null;
            }

            return false;
        }

        /// <summary>
        /// Gets the children of this <see cref="KVObject"/>.
        /// </summary>
        public IEnumerable<KVObject> Children => (Value as KVCollectionValue) ?? Enumerable.Empty<KVObject>();

        /// <summary>
        /// Gets the children of this <see cref="KVObject"/>.
        /// </summary>
        public IEnumerable<KVValue> ChildrenValues => (Value as KVArrayValue) ?? Enumerable.Empty<KVValue>();

        KVCollectionValue GetCollectionValue()
        {
            if (Value is not KVCollectionValue collection)
            {
                throw new InvalidOperationException($"This operation on a {nameof(KVObject)} can only be used when the value has children.");
            }

            return collection;
        }

        string DebuggerDescription
        {
            get
            {
                if (Value.ValueType == KVValueType.String)
                {
                    return $"{Name}: {Value}";
                }

                return $"{Name}: {Value} ({Value.ValueType})";
            }
        }
    }
}
