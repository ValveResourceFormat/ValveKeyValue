namespace ValveKeyValue.KeyValues2
{
    /// <summary>
    /// What a binary DMX encoding version changes about the layout. Shared by the reader and the
    /// writer so the two cannot disagree about a version — if they did, round trips would corrupt
    /// silently rather than fail.
    /// </summary>
    /// <remarks>
    /// Versions 6 to 8 never existed; Valve went from 5 to 9. Version 0 is the legacy
    /// <c>&lt;!-- DMXVersion name_vN --&gt;</c> header, which has the same layout as version 1.
    /// </remarks>
    internal readonly struct KV2BinaryVersion
    {
        /// <summary>First version that can encode uint8, uint64 and a prefix element.</summary>
        public const int SourceTwo = 9;

        /// <summary>Version used when the document did not come from a binary document.</summary>
        public const int Default = 5;

        KV2BinaryVersion(int version)
        {
            Version = version;
            IdVersion = version < 3 ? IDVersion.V1 : version < SourceTwo ? IDVersion.V2 : IDVersion.V3;
        }

        public int Version { get; }

        public IDVersion IdVersion { get; }

        /// <summary>Version 1 has no string table; every string is inline.</summary>
        public bool HasStringTable => Version > 1;

        /// <summary>
        /// Before version 4 the table holds only class names and attribute names, so element names
        /// and scalar string values stay inline.
        /// </summary>
        public bool NamesInStringTable => Version > 3;

        /// <summary>The string table count widens from a short to an int at version 4.</summary>
        public bool StringCountIsInt => Version >= 4;

        /// <summary>String table indices widen from a short to an int at version 5.</summary>
        public bool StringIndicesAreInt => Version >= 5;

        /// <summary>Prefix attribute containers precede the string table from version 9.</summary>
        public bool HasPrefixAttributes => Version > 5;

        /// <summary>
        /// Validates an encoding version and derives its layout flags.
        /// </summary>
        /// <param name="version">The encoding version from the document header.</param>
        /// <param name="allowLegacy">Whether version 0, the legacy header form, is acceptable.</param>
        public static KV2BinaryVersion For(int version, bool allowLegacy)
        {
            var valid = version is 1 or 2 or 3 or 4 or 5 or SourceTwo || (allowLegacy && version is 0);

            if (!valid)
            {
                throw new KeyValueException($"Unsupported DMX binary encoding version: {version}");
            }

            return new KV2BinaryVersion(version);
        }
    }
}
