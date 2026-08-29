namespace ValveKeyValue
{
    /// <summary>
    /// Represents the type of (de)serialization to use.
    /// </summary>
    public enum KVSerializationFormat
    {
        /// <summary>
        /// KeyValues 1 textual format. Used often in Steam and the Source engine.
        /// Supports <c>#include</c>, <c>#base</c> directives and conditional expressions (e.g. <c>[$WIN32]</c>).
        /// </summary>
        KeyValues1Text,

        /// <summary>
        /// KeyValues 1 binary format. Used occasionally in Steam.
        /// </summary>
        KeyValues1Binary,

        /// <summary>
        /// KeyValues 3 textual format. Used in the Source 2 engine.
        /// </summary>
        KeyValues3Text,

        /// <summary>
        /// KeyValues 2 (DMX) textual format. Used by Source engine tools (SFM, Hammer, model compiler, particle editor).
        /// </summary>
        /// <remarks>
        /// None of the <see cref="KVSerializerOptions"/> apply to this format: it has no includes or
        /// conditionals, escape sequences are always on, and its header is mandatory. Options passed
        /// alongside it are ignored.
        /// </remarks>
        KeyValues2Text,

        /// <summary>
        /// KeyValues 2 (DMX) binary format. Used by Source engine tools.
        /// </summary>
        /// <remarks>
        /// None of the <see cref="KVSerializerOptions"/> apply to this format, including
        /// <see cref="KVSerializerOptions.StringTable"/> — DMX carries its own string table in the
        /// file. Options passed alongside it are ignored.
        /// </remarks>
        KeyValues2Binary,
    }
}
