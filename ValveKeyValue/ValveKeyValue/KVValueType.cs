namespace ValveKeyValue
{
    /// <summary>
    /// Represents the type of a given <see cref="KVValue"/>
    /// </summary>
    public enum KVValueType
    {
        /// <summary>
        /// This <see cref="KVValue"/> represents a collection of child <see cref="KVObject"/>s.
        /// </summary>
        Collection,

        /// <summary>
        /// This <see cref="KVValue"/> is represented by a <see cref="string"/>.
        /// </summary>
        String
    }
}
