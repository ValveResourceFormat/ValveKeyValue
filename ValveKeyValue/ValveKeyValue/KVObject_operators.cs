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
            return (string)obj?.value;
        }
    }
}
