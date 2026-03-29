using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;

namespace ValveKeyValue
{
    /// <summary>
    /// Represents a named KeyValue node in a tree structure.
    /// </summary>
    [DebuggerDisplay("{DebuggerDescription}")]
    public partial class KVObject : IEnumerable<KVObject>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="KVObject"/> class with an empty dictionary-backed collection.
        /// </summary>
        /// <param name="name">Name of this object.</param>
        public KVObject(string name)
        {
            Name = name;
            Value = new KVValue(KVValueType.Collection, new Dictionary<string, KVObject>());
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KVObject"/> class with a scalar or blob value.
        /// </summary>
        /// <param name="name">Name of this object.</param>
        /// <param name="value">Value of this object.</param>
        public KVObject(string name, KVValue value)
        {
            Name = name;
            Value = value;
        }

        /// <inheritdoc cref="KVObject(string, KVValue)"/>
        public KVObject(string name, string value) : this(name, (KVValue)value) { }

        /// <inheritdoc cref="KVObject(string, KVValue)"/>
        public KVObject(string name, bool value) : this(name, (KVValue)value) { }

        /// <inheritdoc cref="KVObject(string, KVValue)"/>
        public KVObject(string name, int value) : this(name, (KVValue)value) { }

        /// <inheritdoc cref="KVObject(string, KVValue)"/>
        public KVObject(string name, uint value) : this(name, (KVValue)value) { }

        /// <inheritdoc cref="KVObject(string, KVValue)"/>
        public KVObject(string name, long value) : this(name, (KVValue)value) { }

        /// <inheritdoc cref="KVObject(string, KVValue)"/>
        public KVObject(string name, ulong value) : this(name, (KVValue)value) { }

        /// <inheritdoc cref="KVObject(string, KVValue)"/>
        public KVObject(string name, float value) : this(name, (KVValue)value) { }

        /// <inheritdoc cref="KVObject(string, KVValue)"/>
        public KVObject(string name, double value) : this(name, (KVValue)value) { }

        /// <inheritdoc cref="KVObject(string, KVValue)"/>
        public KVObject(string name, IntPtr value) : this(name, (KVValue)value) { }

        /// <summary>
        /// Initializes a new instance of the <see cref="KVObject"/> class as a list-backed collection of named children.
        /// Preserves insertion order and allows duplicate keys (used for KV1 format).
        /// </summary>
        /// <remarks>
        /// Uses a list backing (O(n) key lookup) to support duplicate keys.
        /// For O(1) lookup with unique keys, use <see cref="Collection(string)"/> or the parameterless constructor instead.
        /// </remarks>
        /// <param name="name">Name of this object.</param>
        /// <param name="items">Child items of this object.</param>
        public KVObject(string name, IEnumerable<KVObject> items)
        {
            ArgumentNullException.ThrowIfNull(items);

            Name = name;
            var list = new List<KVObject>(items);
            Value = new KVValue(KVValueType.Collection, list);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KVObject"/> class as an array of values.
        /// Each value is wrapped in an unnamed <see cref="KVObject"/>.
        /// </summary>
        /// <param name="name">Name of this object.</param>
        /// <param name="values">The array values.</param>
        public KVObject(string name, IEnumerable<KVValue> values)
        {
            ArgumentNullException.ThrowIfNull(values);

            Name = name;
            var list = new List<KVObject>();
            foreach (var v in values)
            {
                list.Add(new KVObject(null, v));
            }
            Value = new KVValue(KVValueType.Array, list);
        }

        /// <summary>
        /// Gets or sets the name of this object.
        /// </summary>
        /// <remarks>
        /// Changing the name of a child that is already in a dictionary-backed parent
        /// will not update the parent's key. Use the parent's string indexer to re-key instead.
        /// </remarks>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the value of this object.
        /// </summary>
        public KVValue Value { get; set; }

        /// <summary>
        /// Gets the number of children in this object's collection or array value.
        /// Returns 0 if the value is neither a collection nor an array.
        /// </summary>
        public int Count => Value.ValueType switch
        {
            KVValueType.Collection => GetCollectionCount(),
            KVValueType.Array => Value.GetArrayList().Count,
            _ => 0,
        };

        /// <summary>
        /// Gets the value type of this object's value.
        /// </summary>
        public KVValueType ValueType => Value.ValueType;

        /// <summary>
        /// Gets a value indicating whether this object's value is an array.
        /// </summary>
        public bool IsArray => Value.IsArray;

        /// <summary>
        /// Gets a value indicating whether this object's value is null.
        /// </summary>
        public bool IsNull => Value.IsNull;

        #region Indexers

        /// <summary>
        /// Gets or sets a child by name. Returns <c>null</c> if the key is not found.
        /// Setting a value creates or replaces the child with the given key.
        /// </summary>
        /// <param name="key">Key of the child object to find.</param>
        /// <returns>The child <see cref="KVObject"/>, or <c>null</c> if not found.</returns>
        public KVObject this[string key]
        {
            get => GetChild(key);
            set
            {
                ArgumentNullException.ThrowIfNull(key);

                if (value != null)
                {
                    value.Name = key;
                }

                SetChild(key, value);
            }
        }

        /// <summary>
        /// Gets a child by index (for arrays and collections by insertion order).
        /// </summary>
        /// <param name="index">The index.</param>
        /// <returns>The child <see cref="KVObject"/> at the specified index.</returns>
        public KVObject this[int index]
        {
            get
            {
                if (Value.ValueType == KVValueType.Array)
                {
                    return Value.GetArrayList()[index];
                }

                if (Value.ValueType == KVValueType.Collection)
                {
                    return GetCollectionByIndex(index);
                }

                throw new NotSupportedException($"Integer indexer on a {nameof(KVObject)} can only be used when the value is an array or collection.");
            }
        }

        #endregion

        // Operators are in KVObject_operators.cs

        #region Navigation

        /// <summary>
        /// Gets a child <see cref="KVObject"/> by name.
        /// </summary>
        /// <param name="name">Name of the child to find.</param>
        /// <returns>The child <see cref="KVObject"/>, or <c>null</c> if not found.</returns>
        public KVObject GetChild(string name)
        {
            ArgumentNullException.ThrowIfNull(name);

            return Value.RefValue switch
            {
                Dictionary<string, KVObject> dict => dict.GetValueOrDefault(name),
                List<KVObject> list when Value.ValueType == KVValueType.Collection => FindInList(list, name),
                _ => null,
            };
        }

        /// <summary>
        /// Tries to get a child <see cref="KVObject"/> by name.
        /// </summary>
        /// <param name="name">Name of the child to find.</param>
        /// <param name="child">The child if found; otherwise <c>null</c>.</param>
        /// <returns><c>true</c> if the child was found; otherwise <c>false</c>.</returns>
        public bool TryGetChild(string name, out KVObject child)
        {
            child = GetChild(name);
            return child != null;
        }

        /// <summary>
        /// Determines whether this object contains a child with the given name.
        /// </summary>
        /// <param name="name">The name to check for.</param>
        /// <returns><c>true</c> if a child with the given name exists; otherwise, <c>false</c>.</returns>
        public bool ContainsKey(string name)
        {
            ArgumentNullException.ThrowIfNull(name);

            return Value.RefValue switch
            {
                Dictionary<string, KVObject> dict => dict.ContainsKey(name),
                List<KVObject> list when Value.ValueType == KVValueType.Collection => FindInList(list, name) != null,
                _ => false,
            };
        }

        /// <summary>
        /// Gets the children of this <see cref="KVObject"/> as a sequence.
        /// Empty if this is not a collection or array.
        /// </summary>
        public IEnumerable<KVObject> Children => Value.ValueType switch
        {
            KVValueType.Collection => GetCollectionChildren(),
            KVValueType.Array => Value.GetArrayList(),
            _ => [],
        };

        #endregion

        #region Mutation

        /// <summary>
        /// Adds a <see cref="KVObject" /> as a child of the current collection.
        /// </summary>
        /// <param name="child">The child to add.</param>
        public void Add(KVObject child)
        {
            ArgumentNullException.ThrowIfNull(child);

            switch (Value.RefValue)
            {
                case Dictionary<string, KVObject> dict:
                    dict[child.Name] = child;
                    break;
                case List<KVObject> list:
                    list.Add(child);
                    break;
                default:
                    throw new InvalidOperationException($"Cannot add a child to a {Value.ValueType} value.");
            }
        }

        /// <summary>
        /// Adds a named value as a child of the current collection.
        /// </summary>
        /// <param name="name">Name of the child.</param>
        /// <param name="value">Value of the child.</param>
        public void Add(string name, KVValue value)
        {
            Add(new KVObject(name, value));
        }

        /// <summary>
        /// Adds a value to this object's array, wrapped in an unnamed <see cref="KVObject"/>.
        /// </summary>
        /// <param name="value">The value to add.</param>
        public void Add(KVValue value)
        {
            if (Value.ValueType != KVValueType.Array)
            {
                throw new InvalidOperationException($"Cannot add an array element to a {Value.ValueType} value.");
            }

            Value.GetArrayList().Add(new KVObject(null, value));
        }

        /// <summary>
        /// Removes a child by key name.
        /// </summary>
        /// <param name="key">The key of the child to remove.</param>
        /// <returns><c>true</c> if the child was found and removed; otherwise <c>false</c>.</returns>
        public bool Remove(string key)
        {
            ArgumentNullException.ThrowIfNull(key);

            return Value.RefValue switch
            {
                Dictionary<string, KVObject> dict => dict.Remove(key),
                List<KVObject> list when Value.ValueType == KVValueType.Collection => list.RemoveAll(c => c.Name == key) > 0,
                _ => false,
            };
        }

        /// <summary>
        /// Removes an element from an array by index.
        /// </summary>
        /// <param name="index">The index of the element to remove.</param>
        public void RemoveAt(int index)
        {
            if (Value.ValueType != KVValueType.Array)
            {
                throw new InvalidOperationException($"Cannot remove by index from a {Value.ValueType} value.");
            }

            Value.GetArrayList().RemoveAt(index);
        }

        /// <summary>
        /// Removes all children or array elements.
        /// </summary>
        public void Clear()
        {
            switch (Value.RefValue)
            {
                case Dictionary<string, KVObject> dict:
                    dict.Clear();
                    break;
                case List<KVObject> list:
                    list.Clear();
                    break;
            }
        }

        #endregion

        // IEnumerable<KVObject> is in KVObject_IEnumerable.cs

        #region Static factory methods

        /// <summary>
        /// Creates an empty dictionary-backed collection <see cref="KVObject"/>.
        /// Provides O(1) key lookup. Does not allow duplicate keys.
        /// </summary>
        /// <param name="name">Name of the object.</param>
        /// <returns>A new <see cref="KVObject"/> with an empty dictionary-backed collection value.</returns>
        public static KVObject Collection(string name)
            => new(name);

        /// <summary>
        /// Creates a dictionary-backed collection <see cref="KVObject"/> from the given children.
        /// Provides O(1) key lookup. Does not allow duplicate keys.
        /// </summary>
        /// <param name="name">Name of the object.</param>
        /// <param name="children">The child objects.</param>
        /// <returns>A new <see cref="KVObject"/> with a dictionary-backed collection value.</returns>
        public static KVObject Collection(string name, IEnumerable<KVObject> children)
        {
            ArgumentNullException.ThrowIfNull(children);

            var dict = new Dictionary<string, KVObject>();
            foreach (var child in children)
            {
                dict[child.Name] = child;
            }

            return new KVObject(name, new KVValue(KVValueType.Collection, dict));
        }

        /// <summary>
        /// Creates an empty list-backed collection <see cref="KVObject"/>.
        /// Preserves insertion order and allows duplicate keys (used for KV1 format).
        /// </summary>
        /// <param name="name">Name of the object.</param>
        /// <returns>A new <see cref="KVObject"/> with an empty list-backed collection value.</returns>
        public static KVObject ListCollection(string name)
            => new(name, System.Array.Empty<KVObject>());

        /// <summary>
        /// Creates a list-backed collection <see cref="KVObject"/> from the given children.
        /// Preserves insertion order and allows duplicate keys (used for KV1 format).
        /// </summary>
        /// <param name="name">Name of the object.</param>
        /// <param name="children">The child objects.</param>
        /// <returns>A new <see cref="KVObject"/> with a list-backed collection value.</returns>
        public static KVObject ListCollection(string name, IEnumerable<KVObject> children)
            => new(name, children);

        /// <summary>
        /// Creates an empty array-valued <see cref="KVObject"/>.
        /// </summary>
        /// <param name="name">Name of the object.</param>
        /// <returns>A new <see cref="KVObject"/> with an empty array value.</returns>
        public static KVObject Array(string name)
            => new(name, new KVValue(KVValueType.Array, new List<KVObject>()));

        /// <summary>
        /// Creates an array-valued <see cref="KVObject"/> from the given elements.
        /// </summary>
        /// <param name="name">Name of the object.</param>
        /// <param name="elements">The array elements.</param>
        /// <returns>A new <see cref="KVObject"/> with an array value.</returns>
        public static KVObject Array(string name, IEnumerable<KVObject> elements)
        {
            ArgumentNullException.ThrowIfNull(elements);

            var list = new List<KVObject>(elements);
            return new KVObject(name, new KVValue(KVValueType.Array, list));
        }

        /// <summary>
        /// Creates an array-valued <see cref="KVObject"/> from the given values.
        /// </summary>
        /// <param name="name">Name of the object.</param>
        /// <param name="values">The array values (wrapped in unnamed KVObjects).</param>
        /// <returns>A new <see cref="KVObject"/> with an array value.</returns>
        public static KVObject Array(string name, IEnumerable<KVValue> values)
        {
            ArgumentNullException.ThrowIfNull(values);

            var list = new List<KVObject>();
            foreach (var v in values)
            {
                list.Add(new KVObject(null, v));
            }

            return new KVObject(name, new KVValue(KVValueType.Array, list));
        }

        /// <summary>
        /// Creates a binary blob <see cref="KVObject"/>.
        /// </summary>
        public static KVObject Blob(string name, byte[] data)
            => new(name, KVValue.Blob(data));

        #endregion

        #region Private helpers

        private void SetChild(string key, KVObject value)
        {
            switch (Value.RefValue)
            {
                case Dictionary<string, KVObject> dict:
                    if (value == null)
                    {
                        dict.Remove(key);
                    }
                    else
                    {
                        dict[key] = value;
                    }

                    break;
                case List<KVObject> list when Value.ValueType == KVValueType.Collection:
                    var firstIndex = list.FindIndex(c => c.Name == key);
                    if (firstIndex >= 0)
                    {
                        if (value != null)
                        {
                            list[firstIndex] = value;

                            // Remove any remaining duplicates after the replaced entry
                            for (var i = list.Count - 1; i > firstIndex; i--)
                            {
                                if (list[i].Name == key)
                                {
                                    list.RemoveAt(i);
                                }
                            }
                        }
                        else
                        {
                            list.RemoveAll(c => c.Name == key);
                        }
                    }
                    else if (value != null)
                    {
                        list.Add(value);
                    }

                    break;
                default:
                    throw new InvalidOperationException($"Cannot set a child on a {Value.ValueType} value.");
            }
        }

        private int GetCollectionCount() => Value.RefValue switch
        {
            Dictionary<string, KVObject> dict => dict.Count,
            List<KVObject> list => list.Count,
            _ => 0,
        };

        private IEnumerable<KVObject> GetCollectionChildren() => Value.RefValue switch
        {
            Dictionary<string, KVObject> dict => dict.Values,
            List<KVObject> list => list,
            _ => [],
        };

        private KVObject GetCollectionByIndex(int index) => Value.RefValue switch
        {
            Dictionary<string, KVObject> dict => dict.Values.ElementAt(index),
            List<KVObject> list => list[index],
            _ => throw new InvalidOperationException("Not a collection."),
        };

        private static KVObject FindInList(List<KVObject> list, string name)
        {
            var span = CollectionsMarshal.AsSpan(list);
            foreach (ref readonly var item in span)
            {
                if (item.Name == name)
                {
                    return item;
                }
            }

            return null;
        }

        /// <inheritdoc/>
        public override string ToString() => Name != null ? $"{Name}: {Value}" : Value.ToString(CultureInfo.InvariantCulture);

        private string DebuggerDescription
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

        #endregion
    }
}
