namespace ValveKeyValue
{
    /// <summary>
    /// Represents the type of a given <see cref="KVValue"/>.
    /// </summary>
    public enum KVValueType
    {
        /// <summary>
        /// This <see cref="KVValue"/> is represented by a <see langword="null"/>.
        /// </summary>
        Null,

        /// <summary>
        /// This <see cref="KVValue"/> represents a collection of child <see cref="KVObject"/>s.
        /// </summary>
        Collection,

        /// <summary>
        /// This <see cref="KVValue"/> represents an array of child <see cref="KVValue"/>s.
        /// </summary>
        Array,

        /// <summary>
        /// This <see cref="KVValue"/> represents a blob of binary bytes.
        /// </summary>
        BinaryBlob,

        /// <summary>
        /// This <see cref="KVValue"/> is represented by a <see cref="bool"/>.
        /// </summary>
        Boolean,

        /// <summary>
        /// This <see cref="KVValue"/> is represented by a <see cref="string"/>.
        /// </summary>
        String,

        /// <summary>
        /// This <see cref="KVValue"/> is represented by a <see cref="short"/>.
        /// </summary>
        Int16,

        /// <summary>
        /// This <see cref="KVValue"/> is represented by a <see cref="int"/>.
        /// </summary>
        Int32,

        /// <summary>
        /// This <see cref="KVValue"/> is represented by a <see cref="long"/>.
        /// </summary>
        Int64,

        /// <summary>
        /// This <see cref="KVValue"/> is represented by a <see cref="ushort"/>.
        /// </summary>
        UInt16,

        /// <summary>
        /// This <see cref="KVValue"/> is represented by a <see cref="uint"/>.
        /// </summary>
        UInt32,

        /// <summary>
        /// This <see cref="KVValue"/> is represented by a <see cref="ulong"/>.
        /// </summary>
        UInt64,

        /// <summary>
        /// This <see cref="KVValue"/> is represented by a <see cref="float"/>.
        /// </summary>
        FloatingPoint,

        /// <summary>
        /// This <see cref="KVValue"/> is represented by a <see cref="double"/>.
        /// </summary>
        FloatingPoint64,

        /// <summary>
        /// This <see cref="KVValue"/> is represented by a <see cref="int"/>, but represents a pointer.
        /// </summary>
        Pointer,
    }
}
