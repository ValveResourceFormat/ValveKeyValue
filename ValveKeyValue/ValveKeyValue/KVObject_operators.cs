namespace ValveKeyValue
{
    public partial class KVObject
    {
        #region Implicit operators (FROM primitives)

        /// <summary>Implicit cast from <see cref="string"/> to <see cref="KVObject"/>.</summary>
        public static implicit operator KVObject(string value) => new(null, (KVValue)value);

        /// <summary>Implicit cast from <see cref="int"/> to <see cref="KVObject"/>.</summary>
        public static implicit operator KVObject(int value) => new(null, (KVValue)value);

        /// <summary>Implicit cast from <see cref="bool"/> to <see cref="KVObject"/>.</summary>
        public static implicit operator KVObject(bool value) => new(null, (KVValue)value);

        /// <summary>Implicit cast from <see cref="float"/> to <see cref="KVObject"/>.</summary>
        public static implicit operator KVObject(float value) => new(null, (KVValue)value);

        /// <summary>Implicit cast from <see cref="double"/> to <see cref="KVObject"/>.</summary>
        public static implicit operator KVObject(double value) => new(null, (KVValue)value);

        /// <summary>Implicit cast from <see cref="long"/> to <see cref="KVObject"/>.</summary>
        public static implicit operator KVObject(long value) => new(null, (KVValue)value);

        /// <summary>Implicit cast from <see cref="ulong"/> to <see cref="KVObject"/>.</summary>
        public static implicit operator KVObject(ulong value) => new(null, (KVValue)value);

        /// <summary>Implicit cast from <see cref="uint"/> to <see cref="KVObject"/>.</summary>
        public static implicit operator KVObject(uint value) => new(null, (KVValue)value);

        /// <summary>Implicit cast from <see cref="short"/> to <see cref="KVObject"/>.</summary>
        public static implicit operator KVObject(short value) => new(null, (KVValue)value);

        /// <summary>Implicit cast from <see cref="ushort"/> to <see cref="KVObject"/>.</summary>
        public static implicit operator KVObject(ushort value) => new(null, (KVValue)value);

        #endregion

        #region Explicit operators (TO primitives, delegate to Value)

        /// <summary>Explicit cast from <see cref="KVObject"/> to <see cref="string"/>.</summary>
        public static explicit operator string(KVObject obj) => obj != null ? (string)obj.Value : null;

        /// <summary>Explicit cast from <see cref="KVObject"/> to <see cref="int"/>.</summary>
        public static explicit operator int(KVObject obj) => (int)obj.Value;

        /// <summary>Explicit cast from <see cref="KVObject"/> to <see cref="bool"/>.</summary>
        public static explicit operator bool(KVObject obj) => (bool)obj.Value;

        /// <summary>Explicit cast from <see cref="KVObject"/> to <see cref="float"/>.</summary>
        public static explicit operator float(KVObject obj) => (float)obj.Value;

        /// <summary>Explicit cast from <see cref="KVObject"/> to <see cref="double"/>.</summary>
        public static explicit operator double(KVObject obj) => (double)obj.Value;

        /// <summary>Explicit cast from <see cref="KVObject"/> to <see cref="long"/>.</summary>
        public static explicit operator long(KVObject obj) => (long)obj.Value;

        /// <summary>Explicit cast from <see cref="KVObject"/> to <see cref="ulong"/>.</summary>
        public static explicit operator ulong(KVObject obj) => (ulong)obj.Value;

        /// <summary>Explicit cast from <see cref="KVObject"/> to <see cref="uint"/>.</summary>
        public static explicit operator uint(KVObject obj) => (uint)obj.Value;

        /// <summary>Explicit cast from <see cref="KVObject"/> to <see cref="short"/>.</summary>
        public static explicit operator short(KVObject obj) => (short)obj.Value;

        /// <summary>Explicit cast from <see cref="KVObject"/> to <see cref="ushort"/>.</summary>
        public static explicit operator ushort(KVObject obj) => (ushort)obj.Value;

        /// <summary>Explicit cast from <see cref="KVObject"/> to <see cref="byte"/>.</summary>
        public static explicit operator byte(KVObject obj) => (byte)obj.Value;

        /// <summary>Explicit cast from <see cref="KVObject"/> to <see cref="IntPtr"/>.</summary>
        public static explicit operator IntPtr(KVObject obj) => (IntPtr)obj.Value;

        #endregion
    }
}
