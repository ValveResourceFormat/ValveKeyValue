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
        String,

        /// <summary>
        /// This <see cref="KVValue"/> is represented by a <see cref="int"/>
        /// </summary>
        Int32,

        /// <summary>
        /// This <see cref="KVValue"/> is represented by a <see cref="ulong"/>
        /// </summary>
        UInt64,

        /// <summary>
        /// This <see cref="KVValue"/> is represented by a <see cref="float"/>
        /// </summary>
        FloatingPoint,

        /// <summary>
        /// This <see cref="KVValue"/> is represented by a <see cref="int"/>, but represents a pointer.
        /// </summary>
        Pointer,

        /// <summary>
        /// This <see cref="KVValue"/> is represented by a <see cref="long"/>.
        /// </summary>
        Int64
    }
}
