using System.Diagnostics;
using System.Globalization;
using System.Linq;

namespace ValveKeyValue
{
    /// <summary>
    /// Represents a KeyValue value node. This is the single type for all KV data.
    /// Scalar data (int, float, string, etc.) is stored inline.
    /// Collection/array data holds references to child KVObjects.
    /// Keys (names) are stored in the parent container, not on the child.
    /// </summary>
    [DebuggerDisplay("{DebuggerDescription}")]
    public partial class KVObject : IEnumerable<KeyValuePair<string, KVObject>>
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
        internal readonly object _ref;

        /// <summary>
        /// Gets a value indicating whether this value is null.
        /// </summary>
        public bool IsNull => ValueType == KVValueType.Null;

        /// <summary>
        /// Gets a value indicating whether this value is an array.
        /// </summary>
        public bool IsArray => ValueType == KVValueType.Array;

        /// <summary>
        /// Gets the number of children in this object's collection or array value.
        /// Returns 0 if the value is neither a collection nor an array.
        /// </summary>
        public int Count => ValueType switch
        {
            KVValueType.Collection => GetCollectionCount(),
            KVValueType.Array => GetArrayList().Count,
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
            if (value is null)
            {
                ValueType = KVValueType.Null;
                return;
            }

            ValueType = KVValueType.String;
            _ref = value;
        }

        /// <inheritdoc cref="KVObject(string)"/>
        public KVObject(bool value)
        {
            ValueType = KVValueType.Boolean;
            _scalar = value ? 1L : 0L;
        }

        /// <inheritdoc cref="KVObject(string)"/>
        public KVObject(int value)
        {
            ValueType = KVValueType.Int32;
            _scalar = value;
        }

        /// <inheritdoc cref="KVObject(string)"/>
        public KVObject(uint value)
        {
            ValueType = KVValueType.UInt32;
            _scalar = value;
        }

        /// <inheritdoc cref="KVObject(string)"/>
        public KVObject(long value)
        {
            ValueType = KVValueType.Int64;
            _scalar = value;
        }

        /// <inheritdoc cref="KVObject(string)"/>
        public KVObject(ulong value)
        {
            ValueType = KVValueType.UInt64;
            _scalar = unchecked((long)value);
        }

        /// <inheritdoc cref="KVObject(string)"/>
        public KVObject(float value)
        {
            ValueType = KVValueType.FloatingPoint;
            _scalar = BitConverter.SingleToInt32Bits(value);
        }

        /// <inheritdoc cref="KVObject(string)"/>
        public KVObject(double value)
        {
            ValueType = KVValueType.FloatingPoint64;
            _scalar = BitConverter.DoubleToInt64Bits(value);
        }

        /// <inheritdoc cref="KVObject(string)"/>
        public KVObject(IntPtr value)
        {
            ValueType = KVValueType.Pointer;
            _scalar = value.ToInt32();
        }

        internal KVObject(KVValueType type, long scalar, object refValue = null, KVFlag flag = KVFlag.None)
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
        /// Gets or sets a child by key. Returns <c>null</c> if the key is not found.
        /// Setting a value creates or replaces the child with the given key.
        /// </summary>
        public KVObject this[string key]
        {
            get => GetChild(key);
            set
            {
                ArgumentNullException.ThrowIfNull(key);
                SetChild(key, value);
            }
        }

        /// <summary>
        /// Gets a child by index (for arrays and collections by insertion order).
        /// </summary>
        public KVObject this[int index]
        {
            get
            {
                if (ValueType == KVValueType.Array)
                {
                    return GetArrayList()[index];
                }

                if (ValueType == KVValueType.Collection)
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
        /// Gets a child <see cref="KVObject"/> by key.
        /// </summary>
        public KVObject GetChild(string name)
        {
            ArgumentNullException.ThrowIfNull(name);

            return _ref switch
            {
                Dictionary<string, KVObject> dict => dict.GetValueOrDefault(name),
                List<KeyValuePair<string, KVObject>> list when ValueType == KVValueType.Collection => FindInList(list, name),
                _ => null,
            };
        }

        /// <summary>
        /// Tries to get a child <see cref="KVObject"/> by key.
        /// </summary>
        public bool TryGetValue(string name, out KVObject child)
        {
            child = GetChild(name);
            return child != null;
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
                List<KeyValuePair<string, KVObject>> list when ValueType == KVValueType.Collection => FindInList(list, name) != null,
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
            KVValueType.Collection => GetCollectionChildren(),
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
        /// </summary>
        public void Add(string key, KVObject value)
        {
            ArgumentNullException.ThrowIfNull(key);

            switch (_ref)
            {
                case Dictionary<string, KVObject> dict:
                    dict[key] = value;
                    break;
                case List<KeyValuePair<string, KVObject>> list when ValueType == KVValueType.Collection:
                    list.Add(new KeyValuePair<string, KVObject>(key, value));
                    break;
                default:
                    throw new InvalidOperationException($"Cannot add a named child to a {ValueType} value.");
            }
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

            GetArrayList().Add(value);
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
                _ => false,
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

            GetArrayList().RemoveAt(index);
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
            }
        }

        #endregion

        // IEnumerable<KeyValuePair<string, KVObject>> is in KVObject_IEnumerable.cs

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

        #region Scalar access

        /// <summary>Converts this value to a <see cref="bool"/>.</summary>
        public bool ToBoolean() => ToBoolean(null);

        /// <summary>Converts this value to an <see cref="int"/>.</summary>
        public int ToInt32() => ToInt32(null);

        /// <summary>Converts this value to a <see cref="long"/>.</summary>
        public long ToInt64() => ToInt64(null);

        /// <summary>Converts this value to a <see cref="float"/>.</summary>
        public float ToSingle() => ToSingle(null);

        /// <summary>Converts this value to a <see cref="double"/>.</summary>
        public double ToDouble() => ToDouble(null);

        /// <summary>Gets the binary blob data as a byte array.</summary>
        public byte[] AsBlob()
        {
            if (ValueType != KVValueType.BinaryBlob)
            {
                throw new InvalidOperationException($"Cannot get blob from a {ValueType} value.");
            }

            return (byte[])_ref;
        }

        /// <summary>Gets the binary blob data as a span.</summary>
        public ReadOnlySpan<byte> AsSpan()
        {
            if (ValueType != KVValueType.BinaryBlob)
            {
                throw new InvalidOperationException($"Cannot get blob span from a {ValueType} value.");
            }

            return ((byte[])_ref).AsSpan();
        }

        #endregion

        #region Internal accessors

        internal List<KVObject> GetArrayList()
            => (List<KVObject>)_ref;

        internal Dictionary<string, KVObject> GetCollectionDict()
            => (Dictionary<string, KVObject>)_ref;

        internal List<KeyValuePair<string, KVObject>> GetCollectionList()
            => (List<KeyValuePair<string, KVObject>>)_ref;

        #endregion

        /// <inheritdoc/>
        public override string ToString() => ToString(CultureInfo.InvariantCulture);

        #region Private helpers

        private void SetChild(string key, KVObject value)
        {
            switch (_ref)
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
                case List<KeyValuePair<string, KVObject>> list when ValueType == KVValueType.Collection:
                    var firstIndex = list.FindIndex(c => c.Key == key);
                    if (firstIndex >= 0)
                    {
                        if (value != null)
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
                        }
                        else
                        {
                            list.RemoveAll(c => c.Key == key);
                        }
                    }
                    else if (value != null)
                    {
                        list.Add(new KeyValuePair<string, KVObject>(key, value));
                    }

                    break;
                default:
                    throw new InvalidOperationException($"Cannot set a child on a {ValueType} value.");
            }
        }

        private int GetCollectionCount() => _ref switch
        {
            Dictionary<string, KVObject> dict => dict.Count,
            List<KeyValuePair<string, KVObject>> list => list.Count,
            _ => 0,
        };

        private IEnumerable<KeyValuePair<string, KVObject>> GetCollectionChildren() => _ref switch
        {
            Dictionary<string, KVObject> dict => dict,
            List<KeyValuePair<string, KVObject>> list => list,
            _ => [],
        };

        private KVObject GetCollectionByIndex(int index) => _ref switch
        {
            Dictionary<string, KVObject> dict => dict.Values.ElementAt(index),
            List<KeyValuePair<string, KVObject>> list => list[index].Value,
            _ => throw new InvalidOperationException("Not a collection."),
        };

        private IEnumerable<KeyValuePair<string, KVObject>> EnumerateArray()
        {
            var list = GetArrayList();
            for (var i = 0; i < list.Count; i++)
            {
                yield return new KeyValuePair<string, KVObject>(null, list[i]);
            }
        }

        private static KVObject FindInList(List<KeyValuePair<string, KVObject>> list, string name)
        {
            foreach (var kvp in list)
            {
                if (kvp.Key == name)
                {
                    return kvp.Value;
                }
            }

            return null;
        }

        private string DebuggerDescription => ValueType switch
        {
            KVValueType.String => $"\"{_ref}\"",
            KVValueType.Null => "null",
            KVValueType.Collection => $"Collection ({GetCollectionCount()} items)",
            KVValueType.Array => $"Array ({GetArrayList().Count} items)",
            _ => $"{ToString(CultureInfo.InvariantCulture)} ({ValueType})",
        };

        #endregion
    }
}
