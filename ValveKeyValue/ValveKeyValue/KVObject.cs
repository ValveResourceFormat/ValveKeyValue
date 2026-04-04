using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;

namespace ValveKeyValue
{
    /// <summary>
    /// Represents a KeyValue value node. This is the single type for all KV data.
    /// Scalar data (int, float, string, etc.) is stored inline.
    /// Collection/array data holds references to child KVObjects.
    /// Keys (names) are stored in the parent container, not on the child.
    /// </summary>
    [DebuggerDisplay("{DebuggerDescription}")]
    public partial class KVObject : IReadOnlyDictionary<string, KVObject>
    {
        #region Properties

        /// <summary>
        /// Gets the value type of this object.
        /// </summary>
        public KVValueType ValueType { get; }

        /// <summary>
        /// Gets or sets the flags of this object.
        /// </summary>
        public KVFlag Flag { get; set; }

        // Inline storage for scalar types (no boxing).
        // Interpretation depends on ValueType.
        internal readonly long _scalar;

        // Reference storage for heap types: string, byte[], List<KVObject>, Dictionary<string, KVObject>, etc.
        internal readonly object? _ref;

        /// <summary>
        /// Gets a value indicating whether this value is null.
        /// </summary>
        public bool IsNull => ValueType == KVValueType.Null;

        /// <summary>
        /// Gets a value indicating whether this value is an array.
        /// </summary>
        public bool IsArray => ValueType == KVValueType.Array;

        /// <summary>
        /// Gets a value indicating whether this value is a collection.
        /// </summary>
        public bool IsCollection => ValueType == KVValueType.Collection;

        /// <summary>
        /// Gets the number of children in this object's collection or array value.
        /// Returns 0 if the value is neither a collection nor an array.
        /// </summary>
        public int Count => ValueType switch
        {
            KVValueType.Collection => GetCollectionCount(),
            KVValueType.Array => ((List<KVObject>)_ref!).Count,
            _ => 0,
        };

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="KVObject"/> class as an empty dictionary-backed collection.
        /// </summary>
        public KVObject()
        {
            ValueType = KVValueType.Collection;
            _ref = new Dictionary<string, KVObject>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KVObject"/> class with a string value.
        /// </summary>
        public KVObject(string value)
        {
            ArgumentNullException.ThrowIfNull(value);
            ValueType = KVValueType.String;
            _ref = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KVObject"/> class with a boolean value.
        /// </summary>
        public KVObject(bool value)
        {
            ValueType = KVValueType.Boolean;
            _scalar = value ? 1L : 0L;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KVObject"/> class with a 16-bit integer value.
        /// </summary>
        public KVObject(short value)
        {
            ValueType = KVValueType.Int16;
            _scalar = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KVObject"/> class with an unsigned 16-bit integer value.
        /// </summary>
        public KVObject(ushort value)
        {
            ValueType = KVValueType.UInt16;
            _scalar = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KVObject"/> class with an integer value.
        /// </summary>
        public KVObject(int value)
        {
            ValueType = KVValueType.Int32;
            _scalar = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KVObject"/> class with an unsigned integer value.
        /// </summary>
        public KVObject(uint value)
        {
            ValueType = KVValueType.UInt32;
            _scalar = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KVObject"/> class with a 64-bit integer value.
        /// </summary>
        public KVObject(long value)
        {
            ValueType = KVValueType.Int64;
            _scalar = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KVObject"/> class with an unsigned 64-bit integer value.
        /// </summary>
        public KVObject(ulong value)
        {
            ValueType = KVValueType.UInt64;
            _scalar = unchecked((long)value);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KVObject"/> class with a float value.
        /// </summary>
        public KVObject(float value)
        {
            ValueType = KVValueType.FloatingPoint;
            _scalar = BitConverter.SingleToInt32Bits(value);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KVObject"/> class with a double value.
        /// </summary>
        public KVObject(double value)
        {
            ValueType = KVValueType.FloatingPoint64;
            _scalar = BitConverter.DoubleToInt64Bits(value);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KVObject"/> class with a pointer value.
        /// </summary>
        public KVObject(IntPtr value)
        {
            ValueType = KVValueType.Pointer;
            _scalar = value.ToInt32();
        }

        internal KVObject(KVValueType type, long scalar, object? refValue = null, KVFlag flag = KVFlag.None)
        {
            ValueType = type;
            Flag = flag;
            _scalar = scalar;
            _ref = refValue;
        }

        internal KVObject(KVValueType type, object refValue, KVFlag flag = KVFlag.None)
        {
            ValueType = type;
            Flag = flag;
            _ref = refValue;
        }

        #endregion

        #region Indexers

        /// <summary>
        /// Gets or sets a child by key.
        /// Setting a value creates or replaces the child with the given key.
        /// </summary>
        /// <exception cref="KeyNotFoundException">The key was not found (getter only).</exception>
        public KVObject this[string key]
        {
            get
            {
                if (!TryGetValue(key, out var child))
                {
                    throw new KeyNotFoundException($"The given key '{key}' was not present.");
                }

                return child;
            }
            set
            {
                ArgumentNullException.ThrowIfNull(key);
                TryInsert(key, value ?? Null(), InsertionBehavior.OverwriteExisting);
            }
        }

        /// <summary>
        /// Gets a child by index (for arrays and list-backed collections).
        /// </summary>
        public KVObject this[int index]
        {
            get
            {
                if (ValueType == KVValueType.Array)
                {
                    var listArray = (List<KVObject>)_ref!;
                    return listArray[index];
                }

                if (ValueType == KVValueType.Collection && _ref is List<KeyValuePair<string, KVObject>> list)
                {
                    return list[index].Value;
                }

                throw new NotSupportedException($"Integer indexer on a {nameof(KVObject)} can only be used when the value is an array or list-backed collection.");
            }
        }

        #endregion

        #region Navigation

        /// <summary>
        /// Tries to get a child <see cref="KVObject"/> by key.
        /// </summary>
        public bool TryGetValue(string name, [MaybeNullWhen(false)] out KVObject child)
        {
            ArgumentNullException.ThrowIfNull(name);

            switch (_ref)
            {
                case Dictionary<string, KVObject> dict:
                    return dict.TryGetValue(name, out child);
                case List<KeyValuePair<string, KVObject>> list when ValueType == KVValueType.Collection:
                    return TryFindInList(list, name, out child);
                default:
                    child = null!;
                    return false;
            }
        }

        /// <summary>
        /// Determines whether this object contains a child with the given key.
        /// </summary>
        public bool ContainsKey(string name)
        {
            ArgumentNullException.ThrowIfNull(name);

            return _ref switch
            {
                Dictionary<string, KVObject> dict => dict.ContainsKey(name),
                List<KeyValuePair<string, KVObject>> list when ValueType == KVValueType.Collection => TryFindInList(list, name, out _),
                _ => false,
            };
        }

        /// <summary>
        /// Gets the children of this <see cref="KVObject"/> as a sequence of key-value pairs.
        /// For arrays, keys are <c>null</c>.
        /// Empty if this is not a collection or array.
        /// </summary>
        public IEnumerable<KeyValuePair<string, KVObject>> Children => ValueType switch
        {
            KVValueType.Collection => EnumerateCollection(),
            KVValueType.Array => EnumerateArray(),
            _ => [],
        };

        /// <summary>
        /// Gets the keys of this collection.
        /// Empty if this is not a collection.
        /// </summary>
        public IEnumerable<string> Keys => _ref switch
        {
            Dictionary<string, KVObject> dict when ValueType == KVValueType.Collection => dict.Keys,
            List<KeyValuePair<string, KVObject>> list when ValueType == KVValueType.Collection => list.Select(kvp => kvp.Key),
            _ => [],
        };

        /// <summary>
        /// Gets the values of this collection or array.
        /// Empty if this is not a collection or array.
        /// </summary>
        public IEnumerable<KVObject> Values => _ref switch
        {
            Dictionary<string, KVObject> dict when ValueType == KVValueType.Collection => dict.Values,
            List<KeyValuePair<string, KVObject>> list when ValueType == KVValueType.Collection => list.Select(kvp => kvp.Value),
            List<KVObject> list when ValueType == KVValueType.Array => list,
            _ => [],
        };

        #endregion

        #region Mutation

        /// <summary>
        /// Adds a named child to this collection.
        /// For dictionary-backed collections, throws if the key already exists.
        /// For list-backed collections, always appends (duplicate keys are allowed).
        /// </summary>
        /// <exception cref="ArgumentException">The key already exists in a dictionary-backed collection.</exception>
        public void Add(string key, KVObject value)
        {
            ArgumentNullException.ThrowIfNull(key);
            TryInsert(key, value ?? Null(), InsertionBehavior.ThrowOnExisting);
        }

        /// <summary>
        /// Tries to add a named child to this collection.
        /// Returns <c>false</c> if the key already exists in a dictionary-backed collection.
        /// For list-backed collections, always succeeds (duplicate keys are allowed).
        /// </summary>
        public bool TryAdd(string key, KVObject value)
        {
            ArgumentNullException.ThrowIfNull(key);
            return TryInsert(key, value ?? Null(), InsertionBehavior.None);
        }

        /// <summary>
        /// Adds a value to this object's array.
        /// </summary>
        public void Add(KVObject value)
        {
            if (ValueType != KVValueType.Array)
            {
                throw new InvalidOperationException($"Cannot add an array element to a {ValueType} value.");
            }

            var list = (List<KVObject>)_ref!;
            list.Add(value ?? Null());
        }

        /// <summary>
        /// Removes a child by key.
        /// </summary>
        public bool Remove(string key)
        {
            ArgumentNullException.ThrowIfNull(key);

            return _ref switch
            {
                Dictionary<string, KVObject> dict => dict.Remove(key),
                // RemoveAll: removes all entries with this key, not just the first (list-backed collections allow duplicate keys)
                List<KeyValuePair<string, KVObject>> list when ValueType == KVValueType.Collection => list.RemoveAll(c => c.Key == key) > 0,
                _ => throw new InvalidOperationException($"Cannot remove a named child from a {ValueType} value."),
            };
        }

        /// <summary>
        /// Removes an element from an array by index.
        /// </summary>
        public void RemoveAt(int index)
        {
            if (ValueType != KVValueType.Array)
            {
                throw new InvalidOperationException($"Cannot remove by index from a {ValueType} value.");
            }

            var list = (List<KVObject>)_ref!;
            list.RemoveAt(index);
        }

        /// <summary>
        /// Removes all children or array elements.
        /// </summary>
        public void Clear()
        {
            switch (_ref)
            {
                case Dictionary<string, KVObject> dict:
                    dict.Clear();
                    break;
                case List<KeyValuePair<string, KVObject>> list when ValueType == KVValueType.Collection:
                    list.Clear();
                    break;
                case List<KVObject> list when ValueType == KVValueType.Array:
                    list.Clear();
                    break;
                default:
                    throw new InvalidOperationException($"Cannot clear a {ValueType} value.");
            }
        }

        #endregion

        #region Static factory methods

        /// <summary>
        /// Creates an empty dictionary-backed collection.
        /// </summary>
        public static KVObject Collection()
            => new();

        /// <summary>
        /// Creates an empty dictionary-backed collection with the specified capacity.
        /// </summary>
        public static KVObject Collection(int capacity)
            => new(KVValueType.Collection, new Dictionary<string, KVObject>(capacity));

        /// <summary>
        /// Creates a dictionary-backed collection from the given children.
        /// </summary>
        public static KVObject Collection(IEnumerable<KeyValuePair<string, KVObject>> children)
        {
            ArgumentNullException.ThrowIfNull(children);

            var capacity = children is ICollection<KeyValuePair<string, KVObject>> col ? col.Count : 0;
            var dict = new Dictionary<string, KVObject>(capacity);
            foreach (var (key, value) in children)
            {
                dict[key] = value;
            }

            return new KVObject(KVValueType.Collection, dict);
        }

        /// <summary>
        /// Creates an empty list-backed collection.
        /// Preserves insertion order and allows duplicate keys (used for KV1 format).
        /// </summary>
        public static KVObject ListCollection()
            => new(KVValueType.Collection, new List<KeyValuePair<string, KVObject>>());

        /// <summary>
        /// Creates an empty list-backed collection with the specified capacity.
        /// Preserves insertion order and allows duplicate keys (used for KV1 format).
        /// </summary>
        public static KVObject ListCollection(int capacity)
            => new(KVValueType.Collection, new List<KeyValuePair<string, KVObject>>(capacity));

        /// <summary>
        /// Creates a list-backed collection from the given children.
        /// Preserves insertion order and allows duplicate keys (used for KV1 format).
        /// </summary>
        public static KVObject ListCollection(IEnumerable<KeyValuePair<string, KVObject>> children)
        {
            ArgumentNullException.ThrowIfNull(children);
            return new KVObject(KVValueType.Collection, new List<KeyValuePair<string, KVObject>>(children));
        }

        /// <summary>
        /// Creates an empty array-valued <see cref="KVObject"/>.
        /// </summary>
        public static KVObject Array()
            => new(KVValueType.Array, new List<KVObject>());

        /// <summary>
        /// Creates an empty array-valued <see cref="KVObject"/> with the specified capacity.
        /// </summary>
        public static KVObject Array(int capacity)
            => new(KVValueType.Array, new List<KVObject>(capacity));

        /// <summary>
        /// Creates an array-valued <see cref="KVObject"/> from the given elements.
        /// </summary>
        public static KVObject Array(IEnumerable<KVObject> elements)
        {
            ArgumentNullException.ThrowIfNull(elements);
            return new KVObject(KVValueType.Array, new List<KVObject>(elements));
        }

        /// <summary>
        /// Creates a binary blob <see cref="KVObject"/>.
        /// </summary>
        public static KVObject Blob(byte[] data)
        {
            ArgumentNullException.ThrowIfNull(data);
            return new KVObject(KVValueType.BinaryBlob, data);
        }

        /// <summary>
        /// Creates a null-valued <see cref="KVObject"/>.
        /// </summary>
        public static KVObject Null() => new(KVValueType.Null, 0L);

        #endregion

        #region Blob and list access

        /// <summary>Gets the binary blob data as a byte array.</summary>
        public byte[] AsBlob()
        {
            if (ValueType != KVValueType.BinaryBlob)
            {
                throw new InvalidOperationException($"Cannot get blob from a {ValueType} value.");
            }

            return (byte[])_ref!;
        }

        /// <summary>Gets the array elements as a span.</summary>
        public Span<KVObject> AsArraySpan()
        {
            if (ValueType != KVValueType.Array)
            {
                throw new InvalidOperationException($"Cannot get list from a {ValueType} value.");
            }

            return CollectionsMarshal.AsSpan((List<KVObject>)_ref!);
        }

        #endregion

        #region Private helpers

        private enum InsertionBehavior
        {
            None,
            OverwriteExisting,
            ThrowOnExisting,
        }

        private bool TryInsert(string key, KVObject value, InsertionBehavior behavior)
        {
            switch (_ref)
            {
                case Dictionary<string, KVObject> dict:
                    if (behavior == InsertionBehavior.OverwriteExisting)
                    {
                        dict[key] = value;
                        return true;
                    }

                    if (dict.TryAdd(key, value))
                    {
                        return true;
                    }

                    if (behavior == InsertionBehavior.ThrowOnExisting)
                    {
                        throw new ArgumentException($"An item with the same key has already been added. Key: {key}");
                    }

                    return false;

                case List<KeyValuePair<string, KVObject>> list when ValueType == KVValueType.Collection:
                    if (behavior == InsertionBehavior.OverwriteExisting)
                    {
                        var firstIndex = list.FindIndex(c => c.Key == key);
                        if (firstIndex >= 0)
                        {
                            list[firstIndex] = new KeyValuePair<string, KVObject>(key, value);

                            // Remove any remaining duplicates after the replaced entry
                            for (var i = list.Count - 1; i > firstIndex; i--)
                            {
                                if (list[i].Key == key)
                                {
                                    list.RemoveAt(i);
                                }
                            }

                            return true;
                        }
                    }

                    list.Add(new KeyValuePair<string, KVObject>(key, value));
                    return true;

                default:
                    throw new InvalidOperationException($"Cannot insert a named child into a {ValueType} value.");
            }
        }

        private int GetCollectionCount() => _ref switch
        {
            Dictionary<string, KVObject> dict => dict.Count,
            List<KeyValuePair<string, KVObject>> list => list.Count,
            _ => 0,
        };

        private IEnumerable<KeyValuePair<string, KVObject>> EnumerateCollection() => _ref switch
        {
            Dictionary<string, KVObject> dict => dict,
            List<KeyValuePair<string, KVObject>> list => list,
            _ => [],
        };

        private IEnumerable<KeyValuePair<string, KVObject>> EnumerateArray()
        {
            var list = (List<KVObject>)_ref!;
            foreach (var item in list)
            {
                yield return new KeyValuePair<string, KVObject>(null!, item);
            }
        }

        private static bool TryFindInList(List<KeyValuePair<string, KVObject>> list, string name, [MaybeNullWhen(false)] out KVObject value)
        {
            foreach (var kvp in list)
            {
                if (kvp.Key == name)
                {
                    value = kvp.Value;
                    return true;
                }
            }

            value = null!;
            return false;
        }

        private string DebuggerDescription => ValueType switch
        {
            KVValueType.String => $"\"{_ref}\"",
            KVValueType.Null => "null",
            KVValueType.Collection => $"Collection ({Count} items)",
            KVValueType.Array => $"Array ({Count} items)",
            _ => $"{ToString(CultureInfo.InvariantCulture)} ({ValueType})",
        };

        #endregion
    }
}
