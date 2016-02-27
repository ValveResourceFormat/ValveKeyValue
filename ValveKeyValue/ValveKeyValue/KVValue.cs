using System;
using System.Globalization;

namespace ValveKeyValue
{
    /// <summary>
    /// Container type for value of a KeyValues object.
    /// </summary>
    public abstract class KVValue
    {
        /// <summary>
        /// Converts a <see cref="KVValue"/> to a <see cref="string"/>.
        /// </summary>
        /// <param name="value">The <see cref="KVValue"/> to convert.</param>
        public static explicit operator string(KVValue value)
        {
            if (value == null)
            {
                return null;
            }

            string retval;
            if (!value.TryConvert(out retval))
            {
                throw MakeCastException(typeof(string));
            }

            return retval;
        }

        internal abstract bool TryConvert(out string value);

        static Exception MakeCastException(Type type)
        {
            return new InvalidCastException(string.Format(CultureInfo.InvariantCulture, "The supplied KVValue object could not cast to type {0}.", type.Name));
        }
    }
}
