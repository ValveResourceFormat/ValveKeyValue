namespace ValveKeyValue
{
    /// <summary>
    /// Represents a KeyValue document.
    /// </summary>
    public class KVDocument : KVObject
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="KVDocument"/> class.
        /// </summary>
        /// <param name="name">Name of the document.</param>
        /// <param name="value">Root value of the document.</param>
        public KVDocument(string name, KVValue value) : base(name, value)
        {
            // KV3 will require a header field that contains format/encoding here.
        }
    }
}
