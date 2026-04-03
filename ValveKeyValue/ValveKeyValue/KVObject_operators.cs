namespace ValveKeyValue
{
    public partial class KVObject
    {
        #region Implicit operators (FROM primitives)

        /// <summary>Implicit cast from <see cref="string"/> to <see cref="KVObject"/>.</summary>
        public static implicit operator KVObject(string value) => new(value);

        /// <summary>Implicit cast from <see cref="int"/> to <see cref="KVObject"/>.</summary>
        public static implicit operator KVObject(int value) => new(value);

        /// <summary>Implicit cast from <see cref="bool"/> to <see cref="KVObject"/>.</summary>
        public static implicit operator KVObject(bool value) => new(value);

        /// <summary>Implicit cast from <see cref="float"/> to <see cref="KVObject"/>.</summary>
        public static implicit operator KVObject(float value) => new(value);

        /// <summary>Implicit cast from <see cref="double"/> to <see cref="KVObject"/>.</summary>
        public static implicit operator KVObject(double value) => new(value);

        /// <summary>Implicit cast from <see cref="long"/> to <see cref="KVObject"/>.</summary>
        public static implicit operator KVObject(long value) => new(value);

        /// <summary>Implicit cast from <see cref="ulong"/> to <see cref="KVObject"/>.</summary>
        public static implicit operator KVObject(ulong value) => new(value);

        /// <summary>Implicit cast from <see cref="uint"/> to <see cref="KVObject"/>.</summary>
        public static implicit operator KVObject(uint value) => new(value);

        /// <summary>Implicit cast from <see cref="short"/> to <see cref="KVObject"/>.</summary>
        public static implicit operator KVObject(short value) => new(value);

        /// <summary>Implicit cast from <see cref="ushort"/> to <see cref="KVObject"/>.</summary>
        public static implicit operator KVObject(ushort value) => new(value);

        /// <summary>Implicit cast from <see cref="byte"/> to <see cref="KVObject"/>. The value is widened to <see cref="int"/>.</summary>
        public static implicit operator KVObject(byte value) => new((int)value);

        /// <summary>Implicit cast from <see cref="sbyte"/> to <see cref="KVObject"/>. The value is widened to <see cref="int"/>.</summary>
        public static implicit operator KVObject(sbyte value) => new((int)value);

        /// <summary>Implicit cast from <see cref="IntPtr"/> to <see cref="KVObject"/>.</summary>
        public static implicit operator KVObject(IntPtr value) => new(value);

        /// <summary>Implicit cast from <see langword="byte[]"/> to <see cref="KVObject"/>.</summary>
        public static implicit operator KVObject(byte[] value)
            => value is null ? Null() : Blob(value);

        #endregion

        #region Explicit operators (TO primitives)

        /// <summary>Explicit cast from <see cref="KVObject"/> to <see cref="string"/>.</summary>
        public static explicit operator string?(KVObject obj) => obj?.ToString(null); // TODO: Perhaps this should throw like the rest of the operators

        /// <summary>Explicit cast from <see cref="KVObject"/> to <see cref="int"/>.</summary>
        public static explicit operator int(KVObject obj) => obj.ToInt32(null);

        /// <summary>Explicit cast from <see cref="KVObject"/> to <see cref="bool"/>.</summary>
        public static explicit operator bool(KVObject obj) => obj.ToBoolean(null);

        /// <summary>Explicit cast from <see cref="KVObject"/> to <see cref="float"/>.</summary>
        public static explicit operator float(KVObject obj) => obj.ToSingle(null);

        /// <summary>Explicit cast from <see cref="KVObject"/> to <see cref="double"/>.</summary>
        public static explicit operator double(KVObject obj) => obj.ToDouble(null);

        /// <summary>Explicit cast from <see cref="KVObject"/> to <see cref="long"/>.</summary>
        public static explicit operator long(KVObject obj) => obj.ToInt64(null);

        /// <summary>Explicit cast from <see cref="KVObject"/> to <see cref="ulong"/>.</summary>
        public static explicit operator ulong(KVObject obj) => obj.ToUInt64(null);

        /// <summary>Explicit cast from <see cref="KVObject"/> to <see cref="uint"/>.</summary>
        public static explicit operator uint(KVObject obj) => obj.ToUInt32(null);

        /// <summary>Explicit cast from <see cref="KVObject"/> to <see cref="short"/>.</summary>
        public static explicit operator short(KVObject obj) => obj.ToInt16(null);

        /// <summary>Explicit cast from <see cref="KVObject"/> to <see cref="ushort"/>.</summary>
        public static explicit operator ushort(KVObject obj) => obj.ToUInt16(null);

        /// <summary>Explicit cast from <see cref="KVObject"/> to <see cref="char"/>.</summary>
        public static explicit operator char(KVObject obj) => obj.ToChar(null);

        /// <summary>Explicit cast from <see cref="KVObject"/> to <see cref="byte"/>.</summary>
        public static explicit operator byte(KVObject obj) => obj.ToByte(null);

        /// <summary>Explicit cast from <see cref="KVObject"/> to <see cref="sbyte"/>.</summary>
        public static explicit operator sbyte(KVObject obj) => obj.ToSByte(null);

        /// <summary>Explicit cast from <see cref="KVObject"/> to <see cref="decimal"/>.</summary>
        public static explicit operator decimal(KVObject obj) => obj.ToDecimal(null);

        /// <summary>Explicit cast from <see cref="KVObject"/> to <see cref="IntPtr"/>.</summary>
        public static explicit operator IntPtr(KVObject obj) => new(obj.ToInt32(null));

        #endregion
    }
}
