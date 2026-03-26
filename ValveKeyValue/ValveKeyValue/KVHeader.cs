using ValveKeyValue.KeyValues3;

namespace ValveKeyValue
{
    /// <summary>
    /// Represents the header of a KeyValues3 document containing encoding and format identifiers.
    /// </summary>
    public class KVHeader
    {
        /// <summary>
        /// Gets or sets the encoding identifier.
        /// </summary>
        public KV3ID Encoding { get; set; }

        /// <summary>
        /// Gets or sets the format identifier.
        /// </summary>
        public KV3ID Format { get; set; }
    }
}
