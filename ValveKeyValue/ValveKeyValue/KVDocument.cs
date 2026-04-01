namespace ValveKeyValue
{
    /// <summary>
    /// Represents a KeyValue document with a root key name and an optional header.
    /// </summary>
    public class KVDocument : KVObject
    {
        /// <summary>
        /// Gets the header containing encoding and format identifiers, or <c>null</c> for KV1 documents.
        /// </summary>
        public KVHeader Header { get; }

        /// <summary>
        /// Gets the root key name of this document.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="KVDocument"/> class.
        /// </summary>
        /// <param name="header">Header of the document.</param>
        /// <param name="name">Root key name of the document.</param>
        /// <param name="root">Root value of the document.</param>
        public KVDocument(KVHeader header, string name, KVObject root)
            : base(root.ValueType, root._scalar, root._ref, root.Flag)
        {
            Header = header;
            Name = name;
        }
    }
}
