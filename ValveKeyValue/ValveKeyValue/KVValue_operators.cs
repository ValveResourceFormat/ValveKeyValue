namespace ValveKeyValue
{
    /// <summary>
    /// Container type for value of a KeyValues object.
    /// </summary>
    public abstract partial class KVValue
    {
        /// <summary>
        /// Implicit cast operator for string to KVValue.
        /// </summary>
        /// <param name="value">The <see cref="string"/> to cast.</param>
        public static implicit operator KVValue(string value)
        {
            Require.NotNull(value, nameof(value));
            return new KVStringValue(value);
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
        /// Converts a <see cref="KVValue"/> to a <see cref="bool"/>.
        /// </summary>
        /// <param name="value">The <see cref="KVValue"/> to convert.</param>
        public static explicit operator int(KVValue value)
        {
            return value.ToInt32(null);
        }
    }
}
