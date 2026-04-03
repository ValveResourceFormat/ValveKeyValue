namespace ValveKeyValue
{
#pragma warning disable CA1812 // This is private for now until we actually need it.

    /// <summary>
    /// Represents a KV2/DMX element with a unique identifier and class name.
    /// Used for KeyValues2 format where each element carries a GUID and type descriptor.
    /// </summary>
    class KV2Element : KVObject
    {
        /// <summary>
        /// Gets or sets the unique identifier for this element within its datamodel.
        /// </summary>
        public Guid ElementId { get; set; }

        /// <summary>
        /// Gets or sets the class name (type descriptor) for this element.
        /// </summary>
        public string? ClassName { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="KV2Element"/> class as an empty collection.
        /// </summary>
        public KV2Element() : base()
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KV2Element"/> class as a collection from children.
        /// </summary>
        internal KV2Element(List<KeyValuePair<string, KVObject>> items) : base(KVValueType.Collection, items)
        {
        }
    }
}
