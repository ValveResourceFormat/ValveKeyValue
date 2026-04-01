namespace ValveKeyValue
{
    /// <summary>
    /// Represents a KV2/DMX element with a unique identifier and class name.
    /// Used for KeyValues2 format where each element carries a GUID and type descriptor.
    /// </summary>
    public class KV2Element : KVObject
    {
        /// <summary>
        /// A null element sentinel used for null element references in DMX.
        /// </summary>
        public static new KV2Element Null { get; } = new KV2Element(string.Empty, string.Empty, Guid.Empty);

        /// <summary>
        /// Gets or sets the unique identifier for this element within its datamodel.
        /// </summary>
        public Guid ElementId { get; set; }

        /// <summary>
        /// Gets or sets the class name (type descriptor) for this element.
        /// </summary>
        public string ClassName { get; set; }

        /// <summary>
        /// Gets or sets the instance name of this element.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="KV2Element"/> class as an empty dictionary-backed collection.
        /// </summary>
        public KV2Element(string className, string name, Guid id) : base()
        {
            ClassName = className;
            Name = name;
            ElementId = id;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KV2Element"/> class as a collection from children.
        /// </summary>
        internal KV2Element(string className, string name, Guid id, Dictionary<string, KVObject> items)
            : base(KVValueType.Collection, items)
        {
            ClassName = className;
            Name = name;
            ElementId = id;
        }
    }
}
