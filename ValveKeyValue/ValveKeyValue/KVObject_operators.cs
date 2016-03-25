namespace ValveKeyValue
{
    /// <summary>
    /// Represents a dynamic KeyValue object.
    /// </summary>
    public partial class KVObject
    {
        /// <summary>
        /// Explicit cast operator for KVObject to string.
        /// </summary>
        /// <param name="obj">The <see cref="KVObject"/> to cast.</param>
        public static explicit operator string(KVObject obj)
        {
            return (string)obj?.Value;
        }

        /// <summary>
        /// Explicit cast operator for KVObject to bool.
        /// </summary>
        /// <param name="obj">The <see cref="KVObject"/> to cast.</param>
        public static explicit operator bool(KVObject obj)
        {
            return (bool)obj?.Value;
        }

        /// <summary>
        /// Explicit cast operator for KVObject to byte.
        /// </summary>
        /// <param name="obj">The <see cref="KVObject"/> to cast.</param>
        public static explicit operator byte(KVObject obj)
        {
            return (byte)obj?.Value;
        }

        /// <summary>
        /// Explicit cast operator for KVObject to char.
        /// </summary>
        /// <param name="obj">The <see cref="KVObject"/> to cast.</param>
        public static explicit operator char(KVObject obj)
        {
            return (char)obj?.Value;
        }

        /// <summary>
        /// Explicit cast operator for KVObject to double.
        /// </summary>
        /// <param name="obj">The <see cref="KVObject"/> to cast.</param>
        public static explicit operator double(KVObject obj)
        {
            return (double)obj?.Value;
        }

        /// <summary>
        /// Explicit cast operator for KVObject to float.
        /// </summary>
        /// <param name="obj">The <see cref="KVObject"/> to cast.</param>
        public static explicit operator float(KVObject obj)
        {
            return (float)obj?.Value;
        }

        /// <summary>
        /// Explicit cast operator for KVObject to int.
        /// </summary>
        /// <param name="obj">The <see cref="KVObject"/> to cast.</param>
        public static explicit operator int(KVObject obj)
        {
            return (int)obj?.Value;
        }

        /// <summary>
        /// Explicit cast operator for KVObject to sbyte.
        /// </summary>
        /// <param name="obj">The <see cref="KVObject"/> to cast.</param>
        public static explicit operator sbyte(KVObject obj)
        {
            return (sbyte)obj?.Value;
        }

        /// <summary>
        /// Explicit cast operator for KVObject to long.
        /// </summary>
        /// <param name="obj">The <see cref="KVObject"/> to cast.</param>
        public static explicit operator long(KVObject obj)
        {
            return (long)obj?.Value;
        }

        /// <summary>
        /// Explicit cast operator for KVObject to uint.
        /// </summary>
        /// <param name="obj">The <see cref="KVObject"/> to cast.</param>
        public static explicit operator uint(KVObject obj)
        {
            return (uint)obj?.Value;
        }

        /// <summary>
        /// Explicit cast operator for KVObject to ulong.
        /// </summary>
        /// <param name="obj">The <see cref="KVObject"/> to cast.</param>
        public static explicit operator ulong(KVObject obj)
        {
            return (ulong)obj?.Value;
        }

        /// <summary>
        /// Explicit cast operator for KVObject to ushort.
        /// </summary>
        /// <param name="obj">The <see cref="KVObject"/> to cast.</param>
        public static explicit operator ushort(KVObject obj)
        {
            return (ushort)obj?.Value;
        }
    }
}
