using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;

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
        // Interpretation depends on ValueType. For a packed array, holds the element flag.
        private readonly long _scalar;

        // Reference storage for heap types: string, byte[], List<KVObject>, Dictionary<string, KVObject>, etc.
        // An array holds either List<KVObject> or a packed primitive array, which is replaced
        // by an equivalent List<KVObject> the first time an element object is needed.
        private object? _ref;

        /// <summary>
        /// Gets a value indicating whether this value is null.
        /// </summary>
        public bool IsNull => ValueType == KVValueType.Null;

        /// <summary>
        /// Gets a value indicating whether this value is an array.
        /// </summary>
        public bool IsArray => ValueType == KVValueType.Array;

        /// <summary>
        /// Gets a value indicating whether this is an array that still stores its values packed,
        /// so every element is a scalar. Accessing the elements as <see cref="KVObject"/> instances,
        /// for example through <see cref="Children"/> or <see cref="Values"/>, unpacks it.
        /// </summary>
        public bool IsPackedArray => TryGetPackedArray(out _);

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
            KVValueType.Array => GetArrayCount(_ref),
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
            : this(new ScalarValue(value))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KVObject"/> class with a 16-bit integer value.
        /// </summary>
        public KVObject(short value)
            : this(new ScalarValue(value))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KVObject"/> class with an unsigned 16-bit integer value.
        /// </summary>
        public KVObject(ushort value)
            : this(new ScalarValue(value))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KVObject"/> class with an integer value.
        /// </summary>
        public KVObject(int value)
            : this(new ScalarValue(value))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KVObject"/> class with an unsigned integer value.
        /// </summary>
        public KVObject(uint value)
            : this(new ScalarValue(value))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KVObject"/> class with a 64-bit integer value.
        /// </summary>
        public KVObject(long value)
            : this(new ScalarValue(value))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KVObject"/> class with an unsigned 64-bit integer value.
        /// </summary>
        public KVObject(ulong value)
            : this(new ScalarValue(value))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KVObject"/> class with a float value.
        /// </summary>
        public KVObject(float value)
            : this(new ScalarValue(value))
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KVObject"/> class with a double value.
        /// </summary>
        public KVObject(double value)
            : this(new ScalarValue(value))
        {
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

        private KVObject(ScalarValue value, KVFlag flag = KVFlag.None)
            : this(value.Type, value.Bits, value.Reference, flag)
        {
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
                    return GetArrayList()[index];
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
            List<KeyValuePair<string, KVObject>> list when ValueType == KVValueType.Collection => EnumerateListKeys(list),
            _ => [],
        };

        /// <summary>
        /// Gets the values of this collection or array.
        /// Empty if this is not a collection or array.
        /// </summary>
        public IEnumerable<KVObject> Values => _ref switch
        {
            Dictionary<string, KVObject> dict when ValueType == KVValueType.Collection => dict.Values,
            List<KeyValuePair<string, KVObject>> list when ValueType == KVValueType.Collection => EnumerateListValues(list),
            _ when ValueType == KVValueType.Array => GetArrayList(),
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

            GetArrayList().Add(value ?? Null());
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
                case System.Array when ValueType == KVValueType.Array:
                    // Dropping the packed values leaves an empty list, as unpacking and clearing would.
                    _ref = new List<KVObject>();
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

        // Enumerates the list directly rather than through IEnumerable, which would box its enumerator.
        internal static KVObject Collection(List<KeyValuePair<string, KVObject>> children)
        {
            var dict = new Dictionary<string, KVObject>(children.Count);
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
        /// Creates a packed array-valued <see cref="KVObject"/> that stores <paramref name="values"/> directly,
        /// without an element <see cref="KVObject"/> per value. Takes ownership of <paramref name="values"/>,
        /// which must not be modified afterwards.
        /// </summary>
        /// <remarks>
        /// <see cref="TryGetArraySpan{T}(out ReadOnlySpan{T})"/>, <see cref="ToArray{T}"/> and <see cref="GetElement{T}(int)"/>
        /// read the values without unpacking.
        /// Element access through the integer indexer, <see cref="AsArraySpan"/>, <see cref="Values"/>,
        /// <see cref="Children"/> or mutation unpacks it into elements equal to <c>new KVObject(value)</c>
        /// with <paramref name="elementFlag"/> applied.
        /// </remarks>
        /// <param name="values">The element values.</param>
        /// <param name="elementFlag">The flag of every element.</param>
        public static KVObject PackedArray(bool[] values, KVFlag elementFlag = KVFlag.None)
            => CreatePackedArray(values, elementFlag);

        /// <inheritdoc cref="PackedArray(bool[], KVFlag)"/>
        public static KVObject PackedArray(short[] values, KVFlag elementFlag = KVFlag.None)
            => CreatePackedArray(values, elementFlag);

        /// <inheritdoc cref="PackedArray(bool[], KVFlag)"/>
        public static KVObject PackedArray(ushort[] values, KVFlag elementFlag = KVFlag.None)
            => CreatePackedArray(values, elementFlag);

        /// <inheritdoc cref="PackedArray(bool[], KVFlag)"/>
        public static KVObject PackedArray(int[] values, KVFlag elementFlag = KVFlag.None)
            => CreatePackedArray(values, elementFlag);

        /// <inheritdoc cref="PackedArray(bool[], KVFlag)"/>
        public static KVObject PackedArray(uint[] values, KVFlag elementFlag = KVFlag.None)
            => CreatePackedArray(values, elementFlag);

        /// <inheritdoc cref="PackedArray(bool[], KVFlag)"/>
        public static KVObject PackedArray(long[] values, KVFlag elementFlag = KVFlag.None)
            => CreatePackedArray(values, elementFlag);

        /// <inheritdoc cref="PackedArray(bool[], KVFlag)"/>
        public static KVObject PackedArray(ulong[] values, KVFlag elementFlag = KVFlag.None)
            => CreatePackedArray(values, elementFlag);

        /// <inheritdoc cref="PackedArray(bool[], KVFlag)"/>
        public static KVObject PackedArray(float[] values, KVFlag elementFlag = KVFlag.None)
            => CreatePackedArray(values, elementFlag);

        /// <inheritdoc cref="PackedArray(bool[], KVFlag)"/>
        public static KVObject PackedArray(double[] values, KVFlag elementFlag = KVFlag.None)
            => CreatePackedArray(values, elementFlag);

        private static KVObject CreatePackedArray(System.Array values, KVFlag elementFlag)
        {
            ArgumentNullException.ThrowIfNull(values);
            return new KVObject(KVValueType.Array, (long)elementFlag, values);
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

        /// <summary>Gets the array elements as a span. Unpacks a packed array.</summary>
        public Span<KVObject> AsArraySpan()
        {
            if (ValueType != KVValueType.Array)
            {
                throw new InvalidOperationException($"Cannot get list from a {ValueType} value.");
            }

            return CollectionsMarshal.AsSpan(GetArrayList());
        }

        /// <summary>
        /// Gets the values of a packed array without unpacking it.
        /// </summary>
        /// <typeparam name="T">The element type, which must be exactly the type the array was created with.</typeparam>
        /// <param name="values">The packed values.</param>
        /// <returns>
        /// <c>true</c> if this is a packed array of <typeparamref name="T"/>; <c>false</c> if it is not an array,
        /// was created with another element type or from <see cref="KVObject"/> elements, or has been unpacked.
        /// </returns>
        public bool TryGetArraySpan<T>(out ReadOnlySpan<T> values)
            where T : unmanaged
        {
            var storage = _ref;

            if (ValueType == KVValueType.Array && IsPackedArrayOf<T>(storage!))
            {
                values = Unsafe.As<T[]>(storage);
                return true;
            }

            values = default;
            return false;
        }

        /// <summary>
        /// Converts every array element to <typeparamref name="T"/> as the explicit cast operator to
        /// <typeparamref name="T"/> does, without unpacking a packed array.
        /// </summary>
        /// <typeparam name="T">
        /// A type with an explicit cast operator from <see cref="KVObject"/>: <see cref="bool"/>, <see cref="byte"/>,
        /// <see cref="sbyte"/>, <see cref="char"/>, <see cref="short"/>, <see cref="ushort"/>, <see cref="int"/>,
        /// <see cref="uint"/>, <see cref="long"/>, <see cref="ulong"/>, <see cref="float"/>, <see cref="double"/>,
        /// <see cref="decimal"/> or <see cref="string"/>.
        /// </typeparam>
        /// <returns>A new array; a packed array of exactly <typeparamref name="T"/> is copied.</returns>
        /// <exception cref="InvalidOperationException">This is not an array.</exception>
        /// <exception cref="NotSupportedException"><typeparamref name="T"/> is not a supported type, or an element cannot be converted.</exception>
        /// <exception cref="OverflowException">An element does not fit in <typeparamref name="T"/>.</exception>
        /// <exception cref="FormatException">A string element cannot be parsed as <typeparamref name="T"/>.</exception>
        public T[] ToArray<T>()
        {
            ThrowIfCannotConvertElementsTo<T>();

            var storage = _ref!;

            if (IsPackedArrayOf<T>(storage))
            {
                return (T[])Unsafe.As<T[]>(storage).Clone();
            }

            var converted = new T[GetArrayCount(storage)];

            for (var i = 0; i < converted.Length; i++)
            {
                converted[i] = ConvertTo<T>(GetElementScalar(storage, i));
            }

            return converted;
        }

        /// <summary>
        /// Converts one array element to <typeparamref name="T"/> as the explicit cast operator to
        /// <typeparamref name="T"/> does, without unpacking a packed array.
        /// </summary>
        /// <typeparam name="T">A type supported by <see cref="ToArray{T}"/>.</typeparam>
        /// <param name="index">The element index.</param>
        /// <exception cref="InvalidOperationException">This is not an array.</exception>
        /// <exception cref="NotSupportedException"><typeparamref name="T"/> is not a supported type, or the element cannot be converted.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="index"/> is outside the array.</exception>
        /// <exception cref="OverflowException">The element does not fit in <typeparamref name="T"/>.</exception>
        /// <exception cref="FormatException">A string element cannot be parsed as <typeparamref name="T"/>.</exception>
        public T GetElement<T>(int index)
        {
            var storage = _ref;

            if (ValueType == KVValueType.Array && IsPackedArrayOf<T>(storage!))
            {
                var values = Unsafe.As<T[]>(storage)!;
                ThrowIfIndexOutOfRange(values, index);
                return values[index];
            }

            ThrowIfCannotConvertElementsTo<T>();

            return ConvertTo<T>(GetElementScalar(storage!, index));
        }

        // Gets the packed values of an array that has not been unpacked yet.
        internal bool TryGetPackedArray([NotNullWhen(true)] out System.Array? packed)
        {
            if (ValueType == KVValueType.Array && _ref is System.Array storage)
            {
                packed = storage;
                return true;
            }

            packed = null;
            return false;
        }

        private void ThrowIfCannotConvertElementsTo<T>()
        {
            if (ValueType != KVValueType.Array)
            {
                throw new InvalidOperationException($"Cannot convert elements of a {ValueType} value.");
            }

            if (!CanConvertTo(typeof(T)))
            {
                throw new NotSupportedException($"Cannot convert array elements to {typeof(T)}.");
            }
        }

        // Creates the element at index of this object's packed values, equal to what the public
        // constructor for its type creates.
        internal KVObject CreatePackedElement(System.Array packed, int index)
            => new(GetPackedScalar(packed, index), (KVFlag)_scalar);

        // Compares exact types, because the runtime also casts int[] to uint[] and other same-size pairs.
        private static bool IsPackedArrayOf<T>(object storage) => storage.GetType() == typeof(T[]);

        // Gets the element at index of an array's storage without unpacking it.
        private static ScalarValue GetElementScalar(object storage, int index)
        {
            if (storage is List<KVObject> list)
            {
                return list[index].Scalar;
            }

            var packed = (System.Array)storage;
            ThrowIfIndexOutOfRange(packed, index);
            return GetPackedScalar(packed, index);
        }

        private static void ThrowIfIndexOutOfRange(System.Array packed, int index)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(index);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(index, packed.Length);
        }

        // Compares exact array types, which the JIT turns into method table comparisons.
        private static ScalarValue GetPackedScalar(System.Array packed, int index)
        {
            var arrayType = packed.GetType();

            if (arrayType == typeof(float[]))
            {
                return new ScalarValue(Unsafe.As<float[]>(packed)[index]);
            }

            if (arrayType == typeof(int[]))
            {
                return new ScalarValue(Unsafe.As<int[]>(packed)[index]);
            }

            if (arrayType == typeof(uint[]))
            {
                return new ScalarValue(Unsafe.As<uint[]>(packed)[index]);
            }

            if (arrayType == typeof(double[]))
            {
                return new ScalarValue(Unsafe.As<double[]>(packed)[index]);
            }

            if (arrayType == typeof(long[]))
            {
                return new ScalarValue(Unsafe.As<long[]>(packed)[index]);
            }

            if (arrayType == typeof(ulong[]))
            {
                return new ScalarValue(Unsafe.As<ulong[]>(packed)[index]);
            }

            if (arrayType == typeof(short[]))
            {
                return new ScalarValue(Unsafe.As<short[]>(packed)[index]);
            }

            if (arrayType == typeof(ushort[]))
            {
                return new ScalarValue(Unsafe.As<ushort[]>(packed)[index]);
            }

            if (arrayType == typeof(bool[]))
            {
                return new ScalarValue(Unsafe.As<bool[]>(packed)[index]);
            }

            throw new InvalidOperationException($"Unsupported packed array type: {arrayType}.");
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
                        var firstIndex = IndexOfInList(list, key);
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

        private static int GetArrayCount(object? storage) => storage switch
        {
            List<KVObject> list => list.Count,
            System.Array packed => packed.Length,
            _ => 0,
        };

        // Gets the elements of an array, unpacking it on first use. Concurrent first calls may each
        // build a list, but only the first one published is stored and returned to all of them.
        private List<KVObject> GetArrayList()
        {
            var storage = _ref;

            if (storage is List<KVObject> list)
            {
                return list;
            }

            var packed = (System.Array)storage!;
            var unpacked = new List<KVObject>(packed.Length);

            for (var i = 0; i < packed.Length; i++)
            {
                unpacked.Add(CreatePackedElement(packed, i));
            }

            var published = Interlocked.CompareExchange(ref _ref, unpacked, packed);

            if (published == packed)
            {
                return unpacked;
            }

            return (List<KVObject>)published!;
        }

        private IEnumerable<KeyValuePair<string, KVObject>> EnumerateArray()
        {
            var list = GetArrayList();
            foreach (var item in list)
            {
                yield return new KeyValuePair<string, KVObject>(null!, item);
            }
        }

        private static IEnumerable<string> EnumerateListKeys(List<KeyValuePair<string, KVObject>> list)
        {
            foreach (var kvp in list)
            {
                yield return kvp.Key;
            }
        }

        private static IEnumerable<KVObject> EnumerateListValues(List<KeyValuePair<string, KVObject>> list)
        {
            foreach (var kvp in list)
            {
                yield return kvp.Value;
            }
        }

        private static bool TryFindInList(List<KeyValuePair<string, KVObject>> list, string name, [MaybeNullWhen(false)] out KVObject value)
        {
            var index = IndexOfInList(list, name);
            if (index < 0)
            {
                value = null!;
                return false;
            }

            value = list[index].Value;
            return true;
        }

        internal static int IndexOfInList(List<KeyValuePair<string, KVObject>> list, string name)
        {
            for (var i = 0; i < list.Count; i++)
            {
                if (list[i].Key == name)
                {
                    return i;
                }
            }

            return -1;
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
