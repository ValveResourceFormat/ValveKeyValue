using System.Diagnostics;
using System.Globalization;
using System.Text;

namespace ValveKeyValue
{
    public partial class KVObject : IConvertible
    {
        #region IConvertible (explicit interface)

        TypeCode IConvertible.GetTypeCode() => ValueType switch
        {
            KVValueType.Boolean => TypeCode.Boolean,
            KVValueType.String => TypeCode.String,
            KVValueType.Int16 => TypeCode.Int16,
            KVValueType.Int32 or KVValueType.Pointer => TypeCode.Int32,
            KVValueType.Int64 => TypeCode.Int64,
            KVValueType.UInt16 => TypeCode.UInt16,
            KVValueType.UInt32 => TypeCode.UInt32,
            KVValueType.UInt64 => TypeCode.UInt64,
            KVValueType.FloatingPoint => TypeCode.Single,
            KVValueType.FloatingPoint64 => TypeCode.Double,
            KVValueType.Null => TypeCode.Empty,
            _ => TypeCode.Object,
        };

        bool IConvertible.ToBoolean(IFormatProvider? provider) => ToBoolean(provider);
        byte IConvertible.ToByte(IFormatProvider? provider) => ToByte(provider);
        char IConvertible.ToChar(IFormatProvider? provider) => ToChar(provider);
        DateTime IConvertible.ToDateTime(IFormatProvider? provider) => throw new NotSupportedException("Cannot convert KeyValue to DateTime.");
        decimal IConvertible.ToDecimal(IFormatProvider? provider) => ToDecimal(provider);
        double IConvertible.ToDouble(IFormatProvider? provider) => ToDouble(provider);
        short IConvertible.ToInt16(IFormatProvider? provider) => ToInt16(provider);
        int IConvertible.ToInt32(IFormatProvider? provider) => ToInt32(provider);
        long IConvertible.ToInt64(IFormatProvider? provider) => ToInt64(provider);
        sbyte IConvertible.ToSByte(IFormatProvider? provider) => ToSByte(provider);
        float IConvertible.ToSingle(IFormatProvider? provider) => ToSingle(provider);
        string IConvertible.ToString(IFormatProvider? provider) => ToString(provider);
        object IConvertible.ToType(Type conversionType, IFormatProvider? provider) => ToType(conversionType, provider);
        ushort IConvertible.ToUInt16(IFormatProvider? provider) => ToUInt16(provider);
        uint IConvertible.ToUInt32(IFormatProvider? provider) => ToUInt32(provider);
        ulong IConvertible.ToUInt64(IFormatProvider? provider) => ToUInt64(provider);

        #endregion

        #region Convenience overloads

        /// <summary>Converts this value to a <see cref="bool"/>.</summary>
        public bool ToBoolean() => ToBoolean(null);

        /// <summary>Converts this value to a <see cref="byte"/>.</summary>
        public byte ToByte() => ToByte(null);

        /// <summary>Converts this value to an <see cref="sbyte"/>.</summary>
        public sbyte ToSByte() => ToSByte(null);

        /// <summary>Converts this value to a <see cref="char"/>.</summary>
        public char ToChar() => ToChar(null);

        /// <summary>Converts this value to a <see cref="short"/>.</summary>
        public short ToInt16() => ToInt16(null);

        /// <summary>Converts this value to a <see cref="ushort"/>.</summary>
        public ushort ToUInt16() => ToUInt16(null);

        /// <summary>Converts this value to an <see cref="int"/>.</summary>
        public int ToInt32() => ToInt32(null);

        /// <summary>Converts this value to a <see cref="uint"/>.</summary>
        public uint ToUInt32() => ToUInt32(null);

        /// <summary>Converts this value to a <see cref="long"/>.</summary>
        public long ToInt64() => ToInt64(null);

        /// <summary>Converts this value to a <see cref="ulong"/>.</summary>
        public ulong ToUInt64() => ToUInt64(null);

        /// <summary>Converts this value to a <see cref="float"/>.</summary>
        public float ToSingle() => ToSingle(null);

        /// <summary>Converts this value to a <see cref="double"/>.</summary>
        public double ToDouble() => ToDouble(null);

        /// <summary>Converts this value to a <see cref="decimal"/>.</summary>
        public decimal ToDecimal() => ToDecimal(null);

        /// <inheritdoc/>
        public override string ToString() => ToString(CultureInfo.InvariantCulture);

        #endregion

        #region Conversion methods

        /// <inheritdoc cref="IConvertible.ToBoolean"/>
        public bool ToBoolean(IFormatProvider? provider) => Scalar.ToBoolean(provider);

        /// <inheritdoc cref="IConvertible.ToByte"/>
        public byte ToByte(IFormatProvider? provider) => Scalar.ToByte(provider);

        /// <inheritdoc cref="IConvertible.ToChar"/>
        public char ToChar(IFormatProvider? provider) => Scalar.ToChar(provider);

        /// <inheritdoc cref="IConvertible.ToDecimal"/>
        public decimal ToDecimal(IFormatProvider? provider) => Scalar.ToDecimal(provider);

        /// <inheritdoc cref="IConvertible.ToDouble"/>
        public double ToDouble(IFormatProvider? provider) => Scalar.ToDouble(provider);

        /// <inheritdoc cref="IConvertible.ToInt16"/>
        public short ToInt16(IFormatProvider? provider) => Scalar.ToInt16(provider);

        /// <inheritdoc cref="IConvertible.ToInt32"/>
        public int ToInt32(IFormatProvider? provider) => Scalar.ToInt32(provider);

        /// <inheritdoc cref="IConvertible.ToInt64"/>
        public long ToInt64(IFormatProvider? provider) => Scalar.ToInt64(provider);

        /// <inheritdoc cref="IConvertible.ToSByte"/>
        public sbyte ToSByte(IFormatProvider? provider) => Scalar.ToSByte(provider);

        /// <inheritdoc cref="IConvertible.ToSingle"/>
        public float ToSingle(IFormatProvider? provider) => Scalar.ToSingle(provider);

        /// <inheritdoc cref="IConvertible.ToString"/>
        public string ToString(IFormatProvider? provider) => Scalar.ToString(provider);

        // Formats numeric values exactly as ToString(provider) does, without allocating a string.
        // Returns false for non-numeric values or when destination is too small.
        internal bool TryFormatNumber(Span<char> destination, out int charsWritten, IFormatProvider? provider)
        {
            provider ??= CultureInfo.InvariantCulture;

            switch (ValueType)
            {
                case KVValueType.FloatingPoint:
                    return BitConverter.Int32BitsToSingle((int)_scalar).TryFormat(destination, out charsWritten, default, provider);
                case KVValueType.FloatingPoint64:
                    return BitConverter.Int64BitsToDouble(_scalar).TryFormat(destination, out charsWritten, default, provider);
                case KVValueType.Int32:
                case KVValueType.Pointer:
                    return ((int)_scalar).TryFormat(destination, out charsWritten, default, provider);
                case KVValueType.Int64:
                    return _scalar.TryFormat(destination, out charsWritten, default, provider);
                case KVValueType.UInt64:
                    return ((ulong)_scalar).TryFormat(destination, out charsWritten, default, provider);
                case KVValueType.UInt32:
                    return ((uint)_scalar).TryFormat(destination, out charsWritten, default, provider);
                case KVValueType.Int16:
                    return ((short)_scalar).TryFormat(destination, out charsWritten, default, provider);
                case KVValueType.UInt16:
                    return ((ushort)_scalar).TryFormat(destination, out charsWritten, default, provider);
                default:
                    charsWritten = 0;
                    return false;
            }
        }

        /// <inheritdoc cref="IConvertible.ToUInt16"/>
        public ushort ToUInt16(IFormatProvider? provider) => Scalar.ToUInt16(provider);

        /// <inheritdoc cref="IConvertible.ToUInt32"/>
        public uint ToUInt32(IFormatProvider? provider) => Scalar.ToUInt32(provider);

        /// <inheritdoc cref="IConvertible.ToUInt64"/>
        public ulong ToUInt64(IFormatProvider? provider) => Scalar.ToUInt64(provider);

        /// <inheritdoc cref="IConvertible.ToType"/>
        public object ToType(Type conversionType, IFormatProvider? provider)
            => Convert.ChangeType(GetBoxedValue(), conversionType, provider ?? CultureInfo.InvariantCulture)!;

        #endregion

        #region Conversion helpers

        // Types that the explicit cast operators convert to.
        internal static bool CanConvertTo(Type type)
        {
            return
                type == typeof(bool) ||
                type == typeof(byte) ||
                type == typeof(char) ||
                type == typeof(decimal) ||
                type == typeof(double) ||
                type == typeof(float) ||
                type == typeof(int) ||
                type == typeof(long) ||
                type == typeof(uint) ||
                type == typeof(ulong) ||
                type == typeof(ushort) ||
                type == typeof(sbyte) ||
                type == typeof(short) ||
                type == typeof(string);
        }

        // Converts with the same method that the explicit cast operator to T calls.
        private static T ConvertTo<T>(in ScalarValue value)
        {
            if (typeof(T) == typeof(bool))
            {
                return (T)(object)value.ToBoolean(null);
            }

            if (typeof(T) == typeof(byte))
            {
                return (T)(object)value.ToByte(null);
            }

            if (typeof(T) == typeof(sbyte))
            {
                return (T)(object)value.ToSByte(null);
            }

            if (typeof(T) == typeof(char))
            {
                return (T)(object)value.ToChar(null);
            }

            if (typeof(T) == typeof(short))
            {
                return (T)(object)value.ToInt16(null);
            }

            if (typeof(T) == typeof(ushort))
            {
                return (T)(object)value.ToUInt16(null);
            }

            if (typeof(T) == typeof(int))
            {
                return (T)(object)value.ToInt32(null);
            }

            if (typeof(T) == typeof(uint))
            {
                return (T)(object)value.ToUInt32(null);
            }

            if (typeof(T) == typeof(long))
            {
                return (T)(object)value.ToInt64(null);
            }

            if (typeof(T) == typeof(ulong))
            {
                return (T)(object)value.ToUInt64(null);
            }

            if (typeof(T) == typeof(float))
            {
                return (T)(object)value.ToSingle(null);
            }

            if (typeof(T) == typeof(double))
            {
                return (T)(object)value.ToDouble(null);
            }

            if (typeof(T) == typeof(decimal))
            {
                return (T)(object)value.ToDecimal(null);
            }

            if (typeof(T) == typeof(string))
            {
                return (T)(object)value.ToString(null);
            }

            // Callers check CanConvertTo first.
            throw new UnreachableException($"No conversion to {typeof(T)}.");
        }

        private ScalarValue Scalar => new(ValueType, _scalar, _ref);

        private object? GetBoxedValue() => ValueType switch
        {
            KVValueType.Boolean => _scalar != 0,
            KVValueType.String => (string)_ref!,
            KVValueType.Int16 => (short)_scalar,
            KVValueType.Int32 or KVValueType.Pointer => (int)_scalar,
            KVValueType.Int64 => _scalar,
            KVValueType.UInt16 => (ushort)_scalar,
            KVValueType.UInt32 => (uint)_scalar,
            KVValueType.UInt64 => unchecked((ulong)_scalar),
            KVValueType.FloatingPoint => BitConverter.Int32BitsToSingle((int)_scalar),
            KVValueType.FloatingPoint64 => BitConverter.Int64BitsToDouble(_scalar),
            KVValueType.BinaryBlob => (byte[])_ref!,
            KVValueType.Array => GetArrayList(),
            KVValueType.Null => null,
            _ => _ref,
        };

        #endregion

        // The parts of a value that its scalar conversions read. Primitive constructors encode their
        // value through it, so packed array elements are encoded and converted without an element object.
        private readonly struct ScalarValue
        {
            public ScalarValue(KVValueType type, long bits, object? reference)
            {
                Type = type;
                Bits = bits;
                Reference = reference;
            }

            public ScalarValue(bool value)
                : this(KVValueType.Boolean, value ? 1L : 0L, null)
            {
            }

            public ScalarValue(short value)
                : this(KVValueType.Int16, value, null)
            {
            }

            public ScalarValue(ushort value)
                : this(KVValueType.UInt16, value, null)
            {
            }

            public ScalarValue(int value)
                : this(KVValueType.Int32, value, null)
            {
            }

            public ScalarValue(uint value)
                : this(KVValueType.UInt32, value, null)
            {
            }

            public ScalarValue(long value)
                : this(KVValueType.Int64, value, null)
            {
            }

            public ScalarValue(ulong value)
                : this(KVValueType.UInt64, unchecked((long)value), null)
            {
            }

            public ScalarValue(float value)
                : this(KVValueType.FloatingPoint, BitConverter.SingleToInt32Bits(value), null)
            {
            }

            public ScalarValue(double value)
                : this(KVValueType.FloatingPoint64, BitConverter.DoubleToInt64Bits(value), null)
            {
            }

            public KVValueType Type { get; }

            // Inline storage, interpreted by Type.
            public long Bits { get; }

            public object? Reference { get; }

            public bool ToBoolean(IFormatProvider? provider) => Type switch
            {
                KVValueType.Boolean or KVValueType.Int16 or KVValueType.UInt16
                    or KVValueType.Int32 or KVValueType.UInt32 or KVValueType.Pointer
                    or KVValueType.Int64 or KVValueType.UInt64
                    => Bits != 0,
                KVValueType.FloatingPoint => BitConverter.Int32BitsToSingle((int)Bits) != 0,
                KVValueType.FloatingPoint64 => BitConverter.Int64BitsToDouble(Bits) != 0,
                KVValueType.String => ToInt32(provider) != 0,
                KVValueType.Null => throw new NotSupportedException("Cannot convert null to Boolean."),
                _ => throw new NotSupportedException($"Cannot convert {Type} to Boolean."),
            };

            public byte ToByte(IFormatProvider? provider) => checked((byte)ToInt64(provider));

            public char ToChar(IFormatProvider? provider) => Type switch
            {
                KVValueType.String => ((string)Reference!).Length == 1
                    ? ((string)Reference!)[0]
                    : throw new FormatException($"Cannot convert string \"{Reference}\" to Char."),
                _ => checked((char)ToInt64(provider)),
            };

            public decimal ToDecimal(IFormatProvider? provider) => Type switch
            {
                KVValueType.String => ConvertFromString<decimal>(provider),
                KVValueType.FloatingPoint => (decimal)BitConverter.Int32BitsToSingle((int)Bits),
                KVValueType.FloatingPoint64 => (decimal)BitConverter.Int64BitsToDouble(Bits),
                KVValueType.UInt64 => (decimal)unchecked((ulong)Bits),
                _ => (decimal)ToInt64(provider),
            };

            public double ToDouble(IFormatProvider? provider) => Type switch
            {
                KVValueType.FloatingPoint => BitConverter.Int32BitsToSingle((int)Bits),
                KVValueType.FloatingPoint64 => BitConverter.Int64BitsToDouble(Bits),
                KVValueType.String => ConvertFromString<double>(provider),
                KVValueType.UInt64 => (double)unchecked((ulong)Bits),
                KVValueType.Null => throw new NotSupportedException("Cannot convert null to Double."),
                KVValueType.Collection or KVValueType.Array or KVValueType.BinaryBlob
                    => throw new NotSupportedException($"Cannot convert {Type} to Double."),
                _ => (double)Bits,
            };

            public short ToInt16(IFormatProvider? provider) => checked((short)ToInt64(provider));

            public int ToInt32(IFormatProvider? provider) => Type switch
            {
                KVValueType.Int32 or KVValueType.Pointer or KVValueType.Boolean
                    or KVValueType.Int16 or KVValueType.UInt16
                    => (int)Bits,
                KVValueType.UInt32 or KVValueType.Int64 => checked((int)Bits),
                KVValueType.UInt64 => checked((int)unchecked((ulong)Bits)),
                KVValueType.FloatingPoint => (int)BitConverter.Int32BitsToSingle((int)Bits),
                KVValueType.FloatingPoint64 => (int)BitConverter.Int64BitsToDouble(Bits),
                KVValueType.String => ConvertFromString<int>(provider),
                KVValueType.Null => throw new NotSupportedException("Cannot convert null to Int32."),
                _ => throw new NotSupportedException($"Cannot convert {Type} to Int32."),
            };

            public long ToInt64(IFormatProvider? provider) => Type switch
            {
                KVValueType.Int32 or KVValueType.Pointer or KVValueType.Boolean
                    or KVValueType.Int16 or KVValueType.UInt16 or KVValueType.UInt32
                    or KVValueType.Int64 => Bits,
                KVValueType.UInt64 => checked((long)unchecked((ulong)Bits)),
                KVValueType.FloatingPoint => (long)BitConverter.Int32BitsToSingle((int)Bits),
                KVValueType.FloatingPoint64 => (long)BitConverter.Int64BitsToDouble(Bits),
                KVValueType.String => ConvertFromString<long>(provider),
                KVValueType.Null => throw new NotSupportedException("Cannot convert null to Int64."),
                _ => throw new NotSupportedException($"Cannot convert {Type} to Int64."),
            };

            public sbyte ToSByte(IFormatProvider? provider) => checked((sbyte)ToInt64(provider));

            public float ToSingle(IFormatProvider? provider) => Type switch
            {
                KVValueType.FloatingPoint => BitConverter.Int32BitsToSingle((int)Bits),
                KVValueType.FloatingPoint64 => (float)BitConverter.Int64BitsToDouble(Bits),
                KVValueType.String => ConvertFromString<float>(provider),
                KVValueType.UInt64 => (float)unchecked((ulong)Bits),
                KVValueType.Null => throw new NotSupportedException("Cannot convert null to Single."),
                KVValueType.Collection or KVValueType.Array or KVValueType.BinaryBlob
                    => throw new NotSupportedException($"Cannot convert {Type} to Single."),
                _ => (float)Bits,
            };

            public string ToString(IFormatProvider? provider)
            {
                // Default to invariant culture so callers passing null (the (string) cast operator and
                // the text serializers) never emit locale-specific separators or digits. Mirrors the
                // parse-side fallback in ConvertFromString.
                provider ??= CultureInfo.InvariantCulture;

                return Type switch
                {
                    KVValueType.String => (string)Reference!,
                    KVValueType.Boolean => Bits != 0 ? "1" : "0",
                    KVValueType.Null => "null",
                    KVValueType.FloatingPoint => BitConverter.Int32BitsToSingle((int)Bits).ToString(provider),
                    KVValueType.FloatingPoint64 => BitConverter.Int64BitsToDouble(Bits).ToString(provider),
                    KVValueType.Int32 or KVValueType.Pointer => ((int)Bits).ToString(provider),
                    KVValueType.Int64 => Bits.ToString(provider),
                    KVValueType.UInt64 => ((ulong)Bits).ToString(provider),
                    KVValueType.UInt32 => ((uint)Bits).ToString(provider),
                    KVValueType.Int16 => ((short)Bits).ToString(provider),
                    KVValueType.UInt16 => ((ushort)Bits).ToString(provider),
                    KVValueType.BinaryBlob => FormatBlob(),
                    KVValueType.Collection => "[Collection]",
                    KVValueType.Array => "[Array]",
                    _ => string.Empty,
                };
            }

            public ushort ToUInt16(IFormatProvider? provider) => checked((ushort)ToUInt64(provider));

            public uint ToUInt32(IFormatProvider? provider) => Type switch
            {
                KVValueType.UInt32 or KVValueType.UInt16 or KVValueType.Boolean
                    => (uint)Bits,
                KVValueType.Int32 or KVValueType.Pointer or KVValueType.Int16
                    => checked((uint)Bits),
                KVValueType.String => ConvertFromString<uint>(provider),
                _ => checked((uint)ToUInt64(provider)),
            };

            public ulong ToUInt64(IFormatProvider? provider) => Type switch
            {
                KVValueType.UInt64 => unchecked((ulong)Bits),
                KVValueType.Int64 => checked((ulong)Bits),
                KVValueType.UInt32 or KVValueType.UInt16 or KVValueType.Boolean
                    => (ulong)Bits,
                KVValueType.Int32 or KVValueType.Pointer or KVValueType.Int16
                    => checked((ulong)Bits),
                KVValueType.FloatingPoint => (ulong)BitConverter.Int32BitsToSingle((int)Bits),
                KVValueType.FloatingPoint64 => (ulong)BitConverter.Int64BitsToDouble(Bits),
                KVValueType.String => ConvertFromString<ulong>(provider),
                KVValueType.Null => throw new NotSupportedException("Cannot convert null to UInt64."),
                _ => throw new NotSupportedException($"Cannot convert {Type} to UInt64."),
            };

            private T ConvertFromString<T>(IFormatProvider? provider) where T : IParsable<T>
            {
                var str = (string)Reference!;
                return T.Parse(str, provider ?? CultureInfo.InvariantCulture);
            }

            private string FormatBlob()
            {
                var bytes = ((byte[])Reference!).AsSpan();

                if (bytes.Length == 0)
                {
                    return string.Empty;
                }

                var builder = new StringBuilder(bytes.Length * 3 - 1);

                for (var i = 0; i < bytes.Length; i++)
                {
                    if (i > 0)
                    {
                        builder.Append(' ');
                    }

                    var b = bytes[i];
                    builder.Append(HexStringHelper.HexToCharUpper(b >> 4));
                    builder.Append(HexStringHelper.HexToCharUpper(b));
                }

                return builder.ToString();
            }
        }
    }
}
