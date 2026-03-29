using System.Globalization;
using System.Text;

namespace ValveKeyValue
{
    public readonly partial record struct KVValue
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

        bool IConvertible.ToBoolean(IFormatProvider provider) => ToBoolean(provider);
        byte IConvertible.ToByte(IFormatProvider provider) => ToByte(provider);
        char IConvertible.ToChar(IFormatProvider provider) => ToChar(provider);
        DateTime IConvertible.ToDateTime(IFormatProvider provider) => throw new NotSupportedException("Cannot convert KeyValue to DateTime.");
        decimal IConvertible.ToDecimal(IFormatProvider provider) => ToDecimal(provider);
        double IConvertible.ToDouble(IFormatProvider provider) => ToDouble(provider);
        short IConvertible.ToInt16(IFormatProvider provider) => ToInt16(provider);
        int IConvertible.ToInt32(IFormatProvider provider) => ToInt32(provider);
        long IConvertible.ToInt64(IFormatProvider provider) => ToInt64(provider);
        sbyte IConvertible.ToSByte(IFormatProvider provider) => ToSByte(provider);
        float IConvertible.ToSingle(IFormatProvider provider) => ToSingle(provider);
        string IConvertible.ToString(IFormatProvider provider) => ToString(provider);
        object IConvertible.ToType(Type conversionType, IFormatProvider provider) => ToType(conversionType, provider);
        ushort IConvertible.ToUInt16(IFormatProvider provider) => ToUInt16(provider);
        uint IConvertible.ToUInt32(IFormatProvider provider) => ToUInt32(provider);
        ulong IConvertible.ToUInt64(IFormatProvider provider) => ToUInt64(provider);

        #endregion

        #region Conversion methods

        /// <inheritdoc cref="IConvertible.ToBoolean"/>
        public bool ToBoolean(IFormatProvider provider) => ValueType switch
        {
            KVValueType.Boolean => _scalar != 0,
            KVValueType.Null => throw new NotSupportedException("Cannot convert null to Boolean."),
            KVValueType.Collection or KVValueType.Array or KVValueType.BinaryBlob
                => throw new NotSupportedException($"Cannot convert {ValueType} to Boolean."),
            _ => ToInt32(provider) != 0, // Strings are converted via ToInt32
        };

        /// <inheritdoc cref="IConvertible.ToByte"/>
        public byte ToByte(IFormatProvider provider) => checked((byte)ToInt64(provider));

        /// <inheritdoc cref="IConvertible.ToChar"/>
        public char ToChar(IFormatProvider provider) => ValueType switch
        {
            KVValueType.String => ((string)_ref).Length == 1
                ? ((string)_ref)[0]
                : throw new FormatException($"Cannot convert string \"{_ref}\" to Char."),
            _ => checked((char)ToInt64(provider)),
        };

        /// <inheritdoc cref="IConvertible.ToDecimal"/>
        public decimal ToDecimal(IFormatProvider provider) => ValueType switch
        {
            KVValueType.String => ConvertFromString<decimal>(provider),
            KVValueType.FloatingPoint => (decimal)BitConverter.Int32BitsToSingle((int)_scalar),
            KVValueType.FloatingPoint64 => (decimal)BitConverter.Int64BitsToDouble(_scalar),
            _ => (decimal)ToInt64(provider),
        };

        /// <inheritdoc cref="IConvertible.ToDouble"/>
        public double ToDouble(IFormatProvider provider) => ValueType switch
        {
            KVValueType.FloatingPoint => BitConverter.Int32BitsToSingle((int)_scalar),
            KVValueType.FloatingPoint64 => BitConverter.Int64BitsToDouble(_scalar),
            KVValueType.String => ConvertFromString<double>(provider),
            KVValueType.Null => throw new NotSupportedException("Cannot convert null to Double."),
            KVValueType.Collection or KVValueType.Array or KVValueType.BinaryBlob
                => throw new NotSupportedException($"Cannot convert {ValueType} to Double."),
            _ => (double)_scalar,
        };

        /// <inheritdoc cref="IConvertible.ToInt16"/>
        public short ToInt16(IFormatProvider provider) => checked((short)ToInt64(provider));

        /// <inheritdoc cref="IConvertible.ToInt32"/>
        public int ToInt32(IFormatProvider provider) => ValueType switch
        {
            KVValueType.Int32 or KVValueType.Pointer or KVValueType.Boolean
                or KVValueType.Int16 or KVValueType.UInt16 or KVValueType.UInt32
                => (int)_scalar,
            KVValueType.Int64 or KVValueType.UInt64 => checked((int)_scalar),
            KVValueType.FloatingPoint => (int)BitConverter.Int32BitsToSingle((int)_scalar),
            KVValueType.FloatingPoint64 => (int)BitConverter.Int64BitsToDouble(_scalar),
            KVValueType.String => ConvertFromString<int>(provider),
            KVValueType.Null => throw new NotSupportedException("Cannot convert null to Int32."),
            _ => throw new NotSupportedException($"Cannot convert {ValueType} to Int32."),
        };

        /// <inheritdoc cref="IConvertible.ToInt64"/>
        public long ToInt64(IFormatProvider provider) => ValueType switch
        {
            KVValueType.Int32 or KVValueType.Pointer or KVValueType.Boolean
                or KVValueType.Int16 or KVValueType.UInt16 or KVValueType.UInt32
                or KVValueType.Int64 => _scalar,
            KVValueType.UInt64 => checked((long)(ulong)_scalar),
            KVValueType.FloatingPoint => (long)BitConverter.Int32BitsToSingle((int)_scalar),
            KVValueType.FloatingPoint64 => (long)BitConverter.Int64BitsToDouble(_scalar),
            KVValueType.String => ConvertFromString<long>(provider),
            KVValueType.Null => throw new NotSupportedException("Cannot convert null to Int64."),
            _ => throw new NotSupportedException($"Cannot convert {ValueType} to Int64."),
        };

        /// <inheritdoc cref="IConvertible.ToSByte"/>
        public sbyte ToSByte(IFormatProvider provider) => checked((sbyte)ToInt64(provider));

        /// <inheritdoc cref="IConvertible.ToSingle"/>
        public float ToSingle(IFormatProvider provider) => ValueType switch
        {
            KVValueType.FloatingPoint => BitConverter.Int32BitsToSingle((int)_scalar),
            KVValueType.FloatingPoint64 => (float)BitConverter.Int64BitsToDouble(_scalar),
            KVValueType.String => ConvertFromString<float>(provider),
            KVValueType.Null => throw new NotSupportedException("Cannot convert null to Single."),
            KVValueType.Collection or KVValueType.Array or KVValueType.BinaryBlob
                => throw new NotSupportedException($"Cannot convert {ValueType} to Single."),
            _ => (float)_scalar,
        };

        /// <inheritdoc cref="IConvertible.ToString"/>
        public string ToString(IFormatProvider provider) => ValueType switch
        {
            KVValueType.String => (string)_ref ?? string.Empty,
            KVValueType.Boolean => _scalar != 0 ? "1" : "0",
            KVValueType.Null => string.Empty,
            KVValueType.FloatingPoint => BitConverter.Int32BitsToSingle((int)_scalar).ToString(provider),
            KVValueType.FloatingPoint64 => BitConverter.Int64BitsToDouble(_scalar).ToString(provider),
            KVValueType.Int32 or KVValueType.Pointer => ((int)_scalar).ToString(provider),
            KVValueType.Int64 => _scalar.ToString(provider),
            KVValueType.UInt64 => ((ulong)_scalar).ToString(provider),
            KVValueType.UInt32 => ((uint)_scalar).ToString(provider),
            KVValueType.Int16 => ((short)_scalar).ToString(provider),
            KVValueType.UInt16 => ((ushort)_scalar).ToString(provider),
            KVValueType.BinaryBlob => FormatBlob(),
            KVValueType.Collection => "[Collection]",
            KVValueType.Array => "[Array]",
            _ => string.Empty,
        };

        /// <inheritdoc cref="IConvertible.ToUInt16"/>
        public ushort ToUInt16(IFormatProvider provider) => checked((ushort)ToUInt64(provider));

        /// <inheritdoc cref="IConvertible.ToUInt32"/>
        public uint ToUInt32(IFormatProvider provider) => ValueType switch
        {
            KVValueType.UInt32 or KVValueType.Int32 or KVValueType.Pointer
                or KVValueType.Boolean or KVValueType.Int16 or KVValueType.UInt16
                => unchecked((uint)_scalar),
            KVValueType.String => ConvertFromString<uint>(provider),
            _ => checked((uint)ToUInt64(provider)),
        };

        /// <inheritdoc cref="IConvertible.ToUInt64"/>
        public ulong ToUInt64(IFormatProvider provider) => ValueType switch
        {
            KVValueType.UInt64 or KVValueType.Int64 => unchecked((ulong)_scalar),
            KVValueType.UInt32 or KVValueType.Int32 or KVValueType.Pointer
                or KVValueType.Boolean or KVValueType.Int16 or KVValueType.UInt16
                => unchecked((ulong)_scalar),
            KVValueType.FloatingPoint => (ulong)BitConverter.Int32BitsToSingle((int)_scalar),
            KVValueType.FloatingPoint64 => (ulong)BitConverter.Int64BitsToDouble(_scalar),
            KVValueType.String => ConvertFromString<ulong>(provider),
            KVValueType.Null => throw new NotSupportedException("Cannot convert null to UInt64."),
            _ => throw new NotSupportedException($"Cannot convert {ValueType} to UInt64."),
        };

        /// <inheritdoc cref="IConvertible.ToType"/>
        public object ToType(Type conversionType, IFormatProvider provider)
            => Convert.ChangeType(GetBoxedValue(), conversionType, provider);

        #endregion

        #region Conversion helpers

        private T ConvertFromString<T>(IFormatProvider provider) where T : IParsable<T>
        {
            var str = (string)_ref;
            return T.Parse(str, provider ?? CultureInfo.InvariantCulture);
        }

        private object GetBoxedValue() => ValueType switch
        {
            KVValueType.Boolean => _scalar != 0,
            KVValueType.String => (string)_ref,
            KVValueType.Int16 => (short)_scalar,
            KVValueType.Int32 or KVValueType.Pointer => (int)_scalar,
            KVValueType.Int64 => _scalar,
            KVValueType.UInt16 => (ushort)_scalar,
            KVValueType.UInt32 => (uint)_scalar,
            KVValueType.UInt64 => unchecked((ulong)_scalar),
            KVValueType.FloatingPoint => BitConverter.Int32BitsToSingle((int)_scalar),
            KVValueType.FloatingPoint64 => BitConverter.Int64BitsToDouble(_scalar),
            KVValueType.BinaryBlob => (byte[])_ref,
            KVValueType.Null => null,
            _ => _ref,
        };

        private string FormatBlob()
        {
            var bytes = ((byte[])_ref).AsSpan();
            var builder = new StringBuilder(bytes.Length * 3);

            for (var i = 0; i < bytes.Length; i++)
            {
                var b = bytes[i];
                builder.Append(HexStringHelper.HexToCharUpper(b >> 4));
                builder.Append(HexStringHelper.HexToCharUpper(b));
                builder.Append(' ');
            }

            if (builder.Length > 1)
            {
                builder.Length -= 1;
            }

            return builder.ToString();
        }

        #endregion
    }
}
