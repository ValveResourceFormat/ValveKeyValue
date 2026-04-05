namespace ValveKeyValue
{
    /// <summary>
    /// Represents a KeyValue document with an optional root key name and an optional header.
    /// </summary>
    public class KVDocument
    {
        /// <summary>
        /// Gets the header containing encoding and format identifiers, or <c>null</c> for KV1 documents.
        /// </summary>
        public KVHeader? Header { get; }

        /// <summary>
        /// Gets the root key name of this document.
        /// </summary>
        public string? Name { get; }

        /// <summary>
        /// Gets the root object.
        /// </summary>
        public KVObject Root { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="KVDocument"/> class.
        /// </summary>
        /// <param name="header">Header of the document.</param>
        /// <param name="name">Root key name of the document.</param>
        /// <param name="root">Root object of the document.</param>
        public KVDocument(KVHeader? header, string? name, KVObject root)
        {
            Header = header;
            Name = name;
            Root = root;
        }

        /// <summary>
        /// Gets a child by key.
        /// Setting a value creates or replaces the child with the given key.
        /// </summary>
        /// <exception cref="KeyNotFoundException">The key was not found (getter only).</exception>
        /// <remarks>
        /// The indexer exists for backwards compatibility, you should use <see cref="Root" /> instead,
        /// as it may be removed in the future.
        /// </remarks>
        public KVObject this[string key] => Root[key];

        /// <summary>
        /// Implicitly converts a <see cref="KVDocument"/> to <see cref="KVObject"/> by returning the <see cref="Root"/>.
        /// </summary>
        public static implicit operator KVObject(KVDocument document) => document.Root;
    }
}
