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
        public string ClassName { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="KV2Element"/> class with a null value.
        /// </summary>
        /// <param name="name">Name of this element.</param>
        public KV2Element(string name) : base(name)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KV2Element"/> class with a scalar or blob value.
        /// </summary>
        /// <param name="name">Name of this element.</param>
        /// <param name="value">Value of this element.</param>
        public KV2Element(string name, KVValue value) : base(name, value)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KV2Element"/> class as a collection of named children.
        /// </summary>
        /// <param name="name">Name of this element.</param>
        /// <param name="items">Child items of this element.</param>
        public KV2Element(string name, IEnumerable<KVObject> items) : base(name, items)
        {
        }
    }
}
