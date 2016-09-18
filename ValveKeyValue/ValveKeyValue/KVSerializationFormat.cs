namespace ValveKeyValue
{
    /// <summary>
    /// Represents the type of (de)serialization to use.
    /// </summary>
    public enum KVSerializationFormat
    {
        /// <summary>
        /// KeyValues 1 textual format. Used often in Steam and the Source engine.
        /// </summary>
        KeyValues1Text,

        /// <summary>
        /// KeyValues 1 binary format. Used occasionally in Steam.
        /// </summary>
        KeyValues1Binary
    }
}
