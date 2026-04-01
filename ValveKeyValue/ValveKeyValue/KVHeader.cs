using ValveKeyValue.KeyValues3;

namespace ValveKeyValue
{
    /// <summary>
    /// Represents the header of a KeyValues3 document containing encoding and format identifiers.
    /// KV3 uses GUIDs instead of version integers to uniquely identify formats even across branched codebases.
    /// </summary>
    public class KVHeader
    {
        /// <summary>
        /// Gets or sets the encoding identifier (e.g. text, binary, binary block-compressed).
        /// </summary>
        public KV3ID Encoding { get; set; }

        /// <summary>
        /// Gets or sets the format identifier describing how the data should be interpreted.
        /// </summary>
        public KV3ID Format { get; set; }
    }
}
