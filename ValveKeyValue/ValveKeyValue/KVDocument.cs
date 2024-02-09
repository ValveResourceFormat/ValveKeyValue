namespace ValveKeyValue
{
    /// <summary>
    /// Represents a KeyValue document.
    /// </summary>
    public class KVDocument : KVObject
    {
        public KVHeader Header { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="KVDocument"/> class.
        /// </summary>
        /// <param name="name">Name of the document.</param>
        /// <param name="value">Root value of the document.</param>
        public KVDocument(KVHeader header, string name, KVValue value) : base(name, value)
        {
            Header = header;
        }
    }
}
