namespace ValveKeyValue
{
    public readonly partial record struct KVValue
    {
        #region Implicit operators (FROM primitives)

        /// <summary>Implicit cast operator for <see cref="string"/> to <see cref="KVValue"/>.</summary>
        public static implicit operator KVValue(string value)
            => value is null ? default : new(KVValueType.String, value);

        /// <summary>Implicit cast operator for <see cref="int"/> to <see cref="KVValue"/>.</summary>
        public static implicit operator KVValue(int value)
            => new(KVValueType.Int32, value);

        /// <summary>Implicit cast operator for <see cref="bool"/> to <see cref="KVValue"/>.</summary>
        public static implicit operator KVValue(bool value)
            => new(KVValueType.Boolean, value ? 1L : 0L);

        /// <summary>Implicit cast operator for <see cref="float"/> to <see cref="KVValue"/>.</summary>
        public static implicit operator KVValue(float value)
            => new(KVValueType.FloatingPoint, BitConverter.SingleToInt32Bits(value));

        /// <summary>Implicit cast operator for <see cref="double"/> to <see cref="KVValue"/>.</summary>
        public static implicit operator KVValue(double value)
            => new(KVValueType.FloatingPoint64, BitConverter.DoubleToInt64Bits(value));

        /// <summary>Implicit cast operator for <see cref="long"/> to <see cref="KVValue"/>.</summary>
        public static implicit operator KVValue(long value)
            => new(KVValueType.Int64, value);

        /// <summary>Implicit cast operator for <see cref="ulong"/> to <see cref="KVValue"/>.</summary>
        public static implicit operator KVValue(ulong value)
            => new(KVValueType.UInt64, unchecked((long)value));

        /// <summary>Implicit cast operator for <see cref="uint"/> to <see cref="KVValue"/>.</summary>
        public static implicit operator KVValue(uint value)
            => new(KVValueType.UInt32, value);

        /// <summary>Implicit cast operator for <see cref="short"/> to <see cref="KVValue"/>.</summary>
        public static implicit operator KVValue(short value)
            => new(KVValueType.Int16, value);

        /// <summary>Implicit cast operator for <see cref="ushort"/> to <see cref="KVValue"/>.</summary>
        public static implicit operator KVValue(ushort value)
            => new(KVValueType.UInt16, value);

        /// <summary>Implicit cast operator for <see cref="IntPtr"/> to <see cref="KVValue"/>.</summary>
        /// <remarks>
        /// Valve's KV format defines pointers as 32-bit. Values exceeding <see cref="int.MaxValue"/> will throw <see cref="OverflowException"/>.
        /// </remarks>
        public static implicit operator KVValue(IntPtr value)
            => new(KVValueType.Pointer, value.ToInt32());

        /// <summary>Implicit cast operator for <see langword="byte[]"/> to <see cref="KVValue"/>.</summary>
        public static implicit operator KVValue(byte[] value)
            => value is null ? default : new(KVValueType.BinaryBlob, value);

        #endregion

        #region Explicit operators (TO primitives)

        /// <summary>Converts a <see cref="KVValue"/> to a <see cref="string"/>.</summary>
        public static explicit operator string(KVValue value)
            => value.ToString(null);

        /// <summary>Converts a <see cref="KVValue"/> to an <see cref="int"/>.</summary>
        public static explicit operator int(KVValue value)
            => value.ToInt32(null);

        /// <summary>Converts a <see cref="KVValue"/> to a <see cref="bool"/>.</summary>
        public static explicit operator bool(KVValue value)
            => value.ToBoolean(null);

        /// <summary>Converts a <see cref="KVValue"/> to a <see cref="float"/>.</summary>
        public static explicit operator float(KVValue value)
            => value.ToSingle(null);

        /// <summary>Converts a <see cref="KVValue"/> to a <see cref="double"/>.</summary>
        public static explicit operator double(KVValue value)
            => value.ToDouble(null);

        /// <summary>Converts a <see cref="KVValue"/> to a <see cref="long"/>.</summary>
        public static explicit operator long(KVValue value)
            => value.ToInt64(null);

        /// <summary>Converts a <see cref="KVValue"/> to a <see cref="ulong"/>.</summary>
        public static explicit operator ulong(KVValue value)
            => value.ToUInt64(null);

        /// <summary>Converts a <see cref="KVValue"/> to a <see cref="uint"/>.</summary>
        public static explicit operator uint(KVValue value)
            => value.ToUInt32(null);

        /// <summary>Converts a <see cref="KVValue"/> to a <see cref="short"/>.</summary>
        public static explicit operator short(KVValue value)
            => value.ToInt16(null);

        /// <summary>Converts a <see cref="KVValue"/> to a <see cref="ushort"/>.</summary>
        public static explicit operator ushort(KVValue value)
            => value.ToUInt16(null);

        /// <summary>Converts a <see cref="KVValue"/> to a <see cref="char"/>.</summary>
        public static explicit operator char(KVValue value)
            => value.ToChar(null);

        /// <summary>Converts a <see cref="KVValue"/> to a <see cref="byte"/>.</summary>
        public static explicit operator byte(KVValue value)
            => value.ToByte(null);

        /// <summary>Converts a <see cref="KVValue"/> to a <see cref="sbyte"/>.</summary>
        public static explicit operator sbyte(KVValue value)
            => value.ToSByte(null);

        /// <summary>Converts a <see cref="KVValue"/> to a <see cref="decimal"/>.</summary>
        public static explicit operator decimal(KVValue value)
            => value.ToDecimal(null);

        /// <summary>Converts a <see cref="KVValue"/> to an <see cref="IntPtr"/>.</summary>
        public static explicit operator IntPtr(KVValue value)
            => new(value.ToInt32(null));

        #endregion
    }
}
