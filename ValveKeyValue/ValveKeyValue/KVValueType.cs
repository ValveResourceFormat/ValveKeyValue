namespace ValveKeyValue
{
    /// <summary>
    /// Represents the type of a given <see cref="KVValue"/>
    /// </summary>
    public enum KVValueType
    {
        /// <summary>
        /// This <see cref="KVValue"/> contains <see cref="KVObject"/>s.
        /// </summary>
        Children,

        /// <summary>
        /// This <see cref="KVValue"/> is represented by a <see cref="string"/>.
        /// </summary>
        String
    }
}
