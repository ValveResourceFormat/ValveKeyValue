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
        /// <remarks>
        /// This is a single shared instance appearing wherever a document has a null reference, so
        /// it is immutable: its properties cannot be set and it cannot hold attributes.
        /// </remarks>
        public static new KV2Element Null { get; } = new KV2Element();

        /// <summary>
        /// The class name used for the prefix attribute container that precedes the root
        /// element in binary v9 and keyvalues2 v4 documents.
        /// </summary>
        public const string PrefixElementClassName = "$prefix_element$";

        /// <summary>
        /// The class name Valve gives the second copy of the prefix attributes that binary
        /// version 9 stores as an unreferenced element right after the root. The keyvalues2 text
        /// encoding writes the same data once, under <see cref="PrefixElementClassName"/>.
        /// </summary>
        internal const string PrefixDuplicateClassName = "DmElement";

        /// <summary>
        /// How deeply elements may nest before the readers and writers give up. Recursion is the
        /// natural way to walk the element graph, and a stack overflow cannot be caught, so a
        /// document deeper than any real one is reported instead.
        /// </summary>
        internal const int MaxNestingDepth = 256;

        readonly bool immutable;

        Guid elementId;
        string? className;
        string name;
        bool isStub;

        /// <summary>
        /// Gets or sets the unique identifier for this element within its datamodel.
        /// </summary>
        /// <remarks>
        /// <see cref="Guid.Empty"/> is the null reference sentinel, so an element that is meant to
        /// be written out needs a real identifier.
        /// </remarks>
        public Guid ElementId
        {
            get => elementId;
            set { ThrowIfImmutable(); elementId = value; }
        }

        /// <summary>
        /// Gets or sets the class name (type descriptor) for this element.
        /// </summary>
        public string? ClassName
        {
            get => className;
            set { ThrowIfImmutable(); className = value; }
        }

        /// <summary>
        /// Gets or sets the instance name of this element.
        /// </summary>
        public string Name
        {
            get => name;
            set { ThrowIfImmutable(); name = value; }
        }

        /// <summary>
        /// Gets a value indicating whether this element is a stub, that is, a reference to an
        /// element that is owned by another file. A stub only carries its <see cref="ElementId"/>,
        /// has no class name and no attributes, and is never written inline by the serializers.
        /// </summary>
        public bool IsStub
        {
            get => isStub;
            internal set => isStub = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KV2Element"/> class as an empty dictionary-backed collection.
        /// </summary>
        public KV2Element(string className, string name, Guid id) : base()
        {
            this.className = className;
            this.name = name;
            elementId = id;
        }

        /// <summary>
        /// Initializes an immutable element. It has no backing collection at all, so the inherited
        /// mutating members reject it rather than silently sharing state.
        /// </summary>
        KV2Element(bool isStub, Guid id) : base(KVValueType.Collection, 0L, null, KVFlag.None)
        {
            className = string.Empty;
            name = string.Empty;
            elementId = id;
            this.isStub = isStub;
            immutable = true;
        }

        KV2Element() : this(isStub: false, Guid.Empty)
        {
        }

        /// <summary>
        /// Creates a stub element referencing an element owned by another file. A stub is
        /// immutable: it carries an identifier and nothing else.
        /// </summary>
        /// <param name="id">The unique identifier of the referenced element.</param>
        public static KV2Element Stub(Guid id) => new(isStub: true, id);

        /// <summary>
        /// Gets a value indicating whether an element reads as a null reference. DMX has no
        /// separate null, so an element without an identifier is indistinguishable from one.
        /// </summary>
        internal static bool IsNullReference(KV2Element? element)
            => element is null || ReferenceEquals(element, Null) || element.ElementId == Guid.Empty;

        /// <summary>
        /// Rejects an element that reads as a null reference but is not one of the sentinels.
        /// Writing it would silently discard the element and everything below it, so a missing
        /// identifier is reported rather than treated as a deliberate null.
        /// </summary>
        internal static void ThrowIfUnwritable(KV2Element? element)
        {
            if (element is null || ReferenceEquals(element, Null) || element.IsStub || element.ElementId != Guid.Empty)
            {
                return;
            }

            throw new KeyValueException(
                $"Element \"{element.ClassName}\" has no {nameof(ElementId)}, which is the null reference sentinel, so it would be written as null. Assign it one.");
        }

        /// <summary>
        /// Gets a value indicating whether an attribute is really the element name. Valve's
        /// datamodel keeps the name as a built-in member, so an attribute literally called
        /// "name" is that member rather than a child, and is written from <see cref="Name"/>.
        /// </summary>
        internal static bool IsElementName(string key, KVObject value)
            => key == "name" && value.ValueType == KVValueType.String;

        /// <summary>
        /// Gets a value indicating whether any element uses a type that only exists from binary
        /// version 9 and keyvalues2 version 4 onwards, which forces the output version up.
        /// </summary>
        internal static bool NeedsSourceTwoTypes(IEnumerable<KV2Element> elements)
        {
            foreach (var element in elements)
            {
                foreach (var (_, child) in element.Children)
                {
                    if (child.ValueType is KVValueType.Byte or KVValueType.UInt64
                        or KVValueType.ByteArray or KVValueType.UInt64Array)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        void ThrowIfImmutable()
        {
            if (immutable)
            {
                throw new InvalidOperationException(
                    ReferenceEquals(this, Null)
                        ? $"{nameof(KV2Element)}.{nameof(Null)} is a shared sentinel and cannot be modified."
                        : "A stub element cannot be modified.");
            }
        }
    }
}
