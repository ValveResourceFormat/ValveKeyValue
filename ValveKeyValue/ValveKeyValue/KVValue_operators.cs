using System;

namespace ValveKeyValue
{
    /// <summary>
    /// Container type for value of a KeyValues object.
    /// </summary>
    public abstract partial class KVValue
    {
        /// <summary>
        /// Implicit cast operator for <see cref="string"/>  to KVValue.
        /// </summary>
        /// <param name="value">The <see cref="string"/> to cast.</param>
        public static implicit operator KVValue(string value)
        {
            Require.NotNull(value, nameof(value));
            return new KVObjectValue<string>(value, KVValueType.String);
        }

        /// <summary>
        /// Implicit cast operator for <see cref="int"/>  to KVValue.
        /// </summary>
        /// <param name="value">The <see cref="int"/> to cast.</param>
        public static implicit operator KVValue(int value)
        {
            return new KVObjectValue<int>(value, KVValueType.Int32);
        }

        /// <summary>
        /// Implicit cast operator for <see cref="IntPtr"/>  to KVValue.
        /// </summary>
        /// <param name="value">The <see cref="IntPtr"/> to cast.</param>
        public static implicit operator KVValue(IntPtr value)
        {
            return new KVObjectValue<int>(value.ToInt32(), KVValueType.Pointer);
        }

        /// <summary>
        /// Implicit cast operator for <see cref="ulong"/>  to KVValue.
        /// </summary>
        /// <param name="value">The <see cref="ulong"/> to cast.</param>
        public static implicit operator KVValue(ulong value)
        {
            return new KVObjectValue<ulong>(value, KVValueType.UInt64);
        }

        /// <summary>
        /// Implicit cast operator for <see cref="float"/>  to KVValue.
        /// </summary>
        /// <param name="value">The <see cref="float"/> to cast.</param>
        public static implicit operator KVValue(float value)
        {
            return new KVObjectValue<float>(value, KVValueType.FloatingPoint);
        }

        /// <summary>
        /// Implicit cast operator for <see cref="long"/>  to KVValue.
        /// </summary>
        /// <param name="value">The <see cref="long"/> to cast.</param>
        public static implicit operator KVValue(long value)
        {
            return new KVObjectValue<long>(value, KVValueType.Int64);
        }

        /// <summary>
        /// Converts a <see cref="KVValue"/> to a <see cref="string"/>.
        /// </summary>
        /// <param name="value">The <see cref="KVValue"/> to convert.</param>
        public static explicit operator string(KVValue value)
        {
            return value?.ToString(null);
        }

        /// <summary>
        /// Converts a <see cref="KVValue"/> to a <see cref="bool"/>.
        /// </summary>
        /// <param name="value">The <see cref="KVValue"/> to convert.</param>
        public static explicit operator bool(KVValue value)
        {
            return value.ToBoolean(null);
        }

        /// <summary>
        /// Converts a <see cref="KVValue"/> to a <see cref="byte"/>.
        /// </summary>
        /// <param name="value">The <see cref="KVValue"/> to convert.</param>
        public static explicit operator byte(KVValue value)
        {
            return value.ToByte(null);
        }

        /// <summary>
        /// Converts a <see cref="KVValue"/> to a <see cref="char"/>.
        /// </summary>
        /// <param name="value">The <see cref="KVValue"/> to convert.</param>
        public static explicit operator char(KVValue value)
        {
            return value.ToChar(null);
        }

        /// <summary>
        /// Converts a <see cref="KVValue"/> to a <see cref="decimal"/>.
        /// </summary>
        /// <param name="value">The <see cref="KVValue"/> to convert.</param>
        public static explicit operator decimal(KVValue value)
        {
            return value.ToDecimal(null);
        }

        /// <summary>
        /// Converts a <see cref="KVValue"/> to a <see cref="double"/>.
        /// </summary>
        /// <param name="value">The <see cref="KVValue"/> to convert.</param>
        public static explicit operator double(KVValue value)
        {
            return value.ToDouble(null);
        }

        /// <summary>
        /// Converts a <see cref="KVValue"/> to a <see cref="float"/>.
        /// </summary>
        /// <param name="value">The <see cref="KVValue"/> to convert.</param>
        public static explicit operator float(KVValue value)
        {
            return value.ToSingle(null);
        }

        /// <summary>
        /// Converts a <see cref="KVValue"/> to a <see cref="bool"/>.
        /// </summary>
        /// <param name="value">The <see cref="KVValue"/> to convert.</param>
        public static explicit operator int(KVValue value)
        {
            return value.ToInt32(null);
        }

        /// <summary>
        /// Converts a <see cref="KVValue"/> to a <see cref="long"/>.
        /// </summary>
        /// <param name="value">The <see cref="KVValue"/> to convert.</param>
        public static explicit operator long(KVValue value)
        {
            return value.ToInt64(null);
        }

        /// <summary>
        /// Converts a <see cref="KVValue"/> to a <see cref="sbyte"/>.
        /// </summary>
        /// <param name="value">The <see cref="KVValue"/> to convert.</param>
        public static explicit operator sbyte(KVValue value)
        {
            return value.ToSByte(null);
        }

        /// <summary>
        /// Converts a <see cref="KVValue"/> to a <see cref="short"/>.
        /// </summary>
        /// <param name="value">The <see cref="KVValue"/> to convert.</param>
        public static explicit operator short(KVValue value)
        {
            return value.ToInt16(null);
        }

        /// <summary>
        /// Converts a <see cref="KVValue"/> to a <see cref="uint"/>.
        /// </summary>
        /// <param name="value">The <see cref="KVValue"/> to convert.</param>
        public static explicit operator uint(KVValue value)
        {
            return value.ToUInt32(null);
        }

        /// <summary>
        /// Converts a <see cref="KVValue"/> to a <see cref="ulong"/>.
        /// </summary>
        /// <param name="value">The <see cref="KVValue"/> to convert.</param>
        public static explicit operator ulong(KVValue value)
        {
            return value.ToUInt64(null);
        }

        /// <summary>
        /// Converts a <see cref="KVValue"/> to a <see cref="ushort"/>.
        /// </summary>
        /// <param name="value">The <see cref="KVValue"/> to convert.</param>
        public static explicit operator ushort(KVValue value)
        {
            return value.ToUInt16(null);
        }
    }
}
