using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;

namespace ValveKeyValue
{
    /// <summary>
    /// Represents a KeyValue value as a lightweight discriminated union.
    /// This is a value type -- scalar data is stored inline without heap allocation.
    /// </summary>
    [DebuggerDisplay("{DebuggerDescription}")]
    [StructLayout(LayoutKind.Auto)]
    public readonly partial record struct KVValue : IConvertible
    {
        /// <summary>
        /// Gets the value type of this <see cref="KVValue"/>.
        /// </summary>
        public KVValueType ValueType { get; init; }

        /// <summary>
        /// Gets the current flags of this <see cref="KVValue"/>.
        /// </summary>
        public KVFlag Flag { get; init; }

        // Inline storage for scalar types (no boxing).
        // Interpretation depends on ValueType.
        private readonly long _scalar;

        // Reference storage for heap types: string, byte[], List<KVObject>, etc.
        private readonly object _ref;

        #region Properties

        /// <summary>
        /// Gets a value indicating whether this value is null.
        /// </summary>
        public bool IsNull => ValueType == KVValueType.Null;

        /// <summary>
        /// Gets a value indicating whether this value is an array.
        /// </summary>
        public bool IsArray => ValueType == KVValueType.Array;

        #endregion

        #region Internal constructors

        internal KVValue(KVValueType type, long scalar, object refValue = null, KVFlag flag = KVFlag.None)
        {
            ValueType = type;
            Flag = flag;
            _scalar = scalar;
            _ref = refValue;
        }

        internal KVValue(KVValueType type, object refValue, KVFlag flag = KVFlag.None)
        {
            ValueType = type;
            Flag = flag;
            _scalar = 0;
            _ref = refValue;
        }

        #endregion

        #region Factory methods

        /// <summary>
        /// Creates a binary blob value.
        /// </summary>
        public static KVValue Blob(byte[] data)
        {
            ArgumentNullException.ThrowIfNull(data);
            return new KVValue(KVValueType.BinaryBlob, data);
        }

        /// <summary>
        /// Creates a binary blob value.
        /// </summary>
        public static KVValue Blob(Memory<byte> data)
            => Blob(data.ToArray());

        /// <summary>
        /// Creates a collection value backed by a dictionary for O(1) key lookup.
        /// Used for KV3 format where duplicate keys are not allowed.
        /// </summary>
        internal static KVValue CreateDictCollection(List<KVObject> items)
        {
            var dict = new Dictionary<string, KVObject>(items.Count);
            foreach (var item in items)
            {
                dict[item.Name] = item;
            }
            return new KVValue(KVValueType.Collection, dict);
        }

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

        /// <summary>Returns the string representation of this value, or null if the value is null.</summary>
        public string AsString() => IsNull ? null : ToString(null);

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

        internal object RefValue => _ref;
        internal long ScalarValue => _scalar;

        internal List<KVObject> GetCollectionList()
            => (List<KVObject>)_ref;

        internal Dictionary<string, KVObject> GetCollectionDict()
            => (Dictionary<string, KVObject>)_ref;

        internal List<KVObject> GetArrayList()
            => (List<KVObject>)_ref;

        #endregion

        /// <inheritdoc/>
        public override string ToString() => ToString(CultureInfo.InvariantCulture);

        private string DebuggerDescription => ValueType switch
        {
            KVValueType.String => $"\"{_ref}\"",
            KVValueType.Null => "null",
            _ => $"{ToString(CultureInfo.InvariantCulture)} ({ValueType})",
        };
    }
}
