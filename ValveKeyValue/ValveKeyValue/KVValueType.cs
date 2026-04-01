namespace ValveKeyValue
{
    /// <summary>
    /// Represents the type of a given <see cref="KVObject"/>.
    /// </summary>
    public enum KVValueType
    {
        /// <summary>
        /// This <see cref="KVObject"/> is represented by a <see langword="null"/>.
        /// </summary>
        Null,

        /// <summary>
        /// This <see cref="KVObject"/> represents a collection of named key-value pairs (like a dictionary or table).
        /// </summary>
        Collection,

        /// <summary>
        /// This <see cref="KVObject"/> represents an ordered array of unnamed child <see cref="KVObject"/>s.
        /// </summary>
        Array,

        /// <summary>
        /// This <see cref="KVObject"/> represents a binary blob (raw byte data).
        /// </summary>
        BinaryBlob,

        /// <summary>
        /// This <see cref="KVObject"/> is represented by a <see cref="bool"/>.
        /// </summary>
        Boolean,

        /// <summary>
        /// This <see cref="KVObject"/> is represented by a <see cref="string"/>.
        /// </summary>
        String,

        /// <summary>
        /// This <see cref="KVObject"/> is represented by a <see cref="short"/>.
        /// </summary>
        Int16,

        /// <summary>
        /// This <see cref="KVObject"/> is represented by a <see cref="int"/>.
        /// </summary>
        Int32,

        /// <summary>
        /// This <see cref="KVObject"/> is represented by a <see cref="long"/>.
        /// </summary>
        Int64,

        /// <summary>
        /// This <see cref="KVObject"/> is represented by a <see cref="ushort"/>.
        /// </summary>
        UInt16,

        /// <summary>
        /// This <see cref="KVObject"/> is represented by a <see cref="uint"/>.
        /// </summary>
        UInt32,

        /// <summary>
        /// This <see cref="KVObject"/> is represented by a <see cref="ulong"/>.
        /// </summary>
        UInt64,

        /// <summary>
        /// This <see cref="KVObject"/> is represented by a <see cref="float"/>.
        /// </summary>
        FloatingPoint,

        /// <summary>
        /// This <see cref="KVObject"/> is represented by a <see cref="double"/>.
        /// </summary>
        FloatingPoint64,

        /// <summary>
        /// This <see cref="KVObject"/> is represented by a <see cref="int"/>, but represents a pointer.
        /// </summary>
        Pointer,

        // DMX scalar types

        /// <summary>
        /// This <see cref="KVObject"/> is represented by a <see cref="byte"/>.
        /// </summary>
        Byte,

        /// <summary>
        /// This <see cref="KVObject"/> is represented by a <see cref="DmxColor"/>.
        /// </summary>
        Color,

        /// <summary>
        /// This <see cref="KVObject"/> is represented by a <see cref="DmxTime"/>.
        /// </summary>
        TimeSpan,

        /// <summary>
        /// This <see cref="KVObject"/> is represented by a <see cref="System.Numerics.Vector2"/>.
        /// </summary>
        Vector2,

        /// <summary>
        /// This <see cref="KVObject"/> is represented by a <see cref="System.Numerics.Vector3"/>.
        /// </summary>
        Vector3,

        /// <summary>
        /// This <see cref="KVObject"/> is represented by a <see cref="System.Numerics.Vector4"/>.
        /// </summary>
        Vector4,

        /// <summary>
        /// This <see cref="KVObject"/> is represented by a <see cref="ValveKeyValue.QAngle"/>.
        /// </summary>
        QAngle,

        /// <summary>
        /// This <see cref="KVObject"/> is represented by a <see cref="System.Numerics.Quaternion"/>.
        /// </summary>
        Quaternion,

        /// <summary>
        /// This <see cref="KVObject"/> is represented by a <see cref="System.Numerics.Matrix4x4"/>.
        /// </summary>
        Matrix4x4,

        // DMX array types

        /// <summary>DMX element array (<see cref="System.Collections.Generic.List{T}"/> of <see cref="KV2Element"/>).</summary>
        ElementArray,

        /// <summary>DMX int array (<see cref="System.Collections.Generic.List{T}"/> of <see cref="int"/>).</summary>
        Int32Array,

        /// <summary>DMX float array (<see cref="System.Collections.Generic.List{T}"/> of <see cref="float"/>).</summary>
        FloatArray,

        /// <summary>DMX bool array (<see cref="System.Collections.Generic.List{T}"/> of <see cref="bool"/>).</summary>
        BooleanArray,

        /// <summary>DMX string array (<see cref="System.Collections.Generic.List{T}"/> of <see cref="string"/>).</summary>
        StringArray,

        /// <summary>DMX binary blob array (<see cref="System.Collections.Generic.List{T}"/> of byte arrays).</summary>
        BinaryBlobArray,

        /// <summary>DMX time array (<see cref="System.Collections.Generic.List{T}"/> of <see cref="DmxTime"/>).</summary>
        TimeSpanArray,

        /// <summary>DMX color array (<see cref="System.Collections.Generic.List{T}"/> of <see cref="DmxColor"/>).</summary>
        ColorArray,

        /// <summary>DMX Vector2 array (<see cref="System.Collections.Generic.List{T}"/> of <see cref="System.Numerics.Vector2"/>).</summary>
        Vector2Array,

        /// <summary>DMX Vector3 array (<see cref="System.Collections.Generic.List{T}"/> of <see cref="System.Numerics.Vector3"/>).</summary>
        Vector3Array,

        /// <summary>DMX Vector4 array (<see cref="System.Collections.Generic.List{T}"/> of <see cref="System.Numerics.Vector4"/>).</summary>
        Vector4Array,

        /// <summary>DMX QAngle array (<see cref="System.Collections.Generic.List{T}"/> of <see cref="ValveKeyValue.QAngle"/>).</summary>
        QAngleArray,

        /// <summary>DMX Quaternion array (<see cref="System.Collections.Generic.List{T}"/> of <see cref="System.Numerics.Quaternion"/>).</summary>
        QuaternionArray,

        /// <summary>DMX Matrix4x4 array (<see cref="System.Collections.Generic.List{T}"/> of <see cref="System.Numerics.Matrix4x4"/>).</summary>
        Matrix4x4Array,

        /// <summary>DMX byte array (<see cref="System.Collections.Generic.List{T}"/> of <see cref="byte"/>).</summary>
        ByteArray,

        /// <summary>DMX uint64 array (<see cref="System.Collections.Generic.List{T}"/> of <see cref="ulong"/>).</summary>
        UInt64Array,
    }
}
