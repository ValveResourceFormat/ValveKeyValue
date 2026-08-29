namespace ValveKeyValue
{
    /// <summary>
    /// Represents a KeyValues2 (DMX) document. In addition to the root element it can carry a
    /// prefix attribute container, which precedes the root in binary v9 and keyvalues2 v4 documents.
    /// </summary>
    public class KV2Document : KVDocument
    {
        /// <summary>
        /// Gets the prefix attribute container (binary v9 / keyvalues2 v4), or <c>null</c> when the
        /// document does not have one.
        /// </summary>
        public KV2Element? PrefixElement { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="KV2Document"/> class.
        /// </summary>
        /// <param name="header">Header of the document.</param>
        /// <param name="name">Root key name of the document.</param>
        /// <param name="root">Root element of the document.</param>
        /// <param name="prefixElement">Prefix attribute container, or <c>null</c>.</param>
        public KV2Document(KVHeader? header, string? name, KVObject root, KV2Element? prefixElement = null)
            : base(header, name, root)
        {
            PrefixElement = prefixElement;
        }
    }
}
