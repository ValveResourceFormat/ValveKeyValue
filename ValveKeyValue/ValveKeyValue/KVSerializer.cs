using System.Diagnostics.CodeAnalysis;
using System.Text;
using ValveKeyValue.Abstraction;
using ValveKeyValue.Deserialization;
using ValveKeyValue.Deserialization.KeyValues1;
using ValveKeyValue.Deserialization.KeyValues3;
using ValveKeyValue.Serialization.KeyValues1;
using ValveKeyValue.Serialization.KeyValues3;

namespace ValveKeyValue
{
    /// <summary>
    /// Helper class to serialize and deserialize KeyValue objects.
    /// </summary>
    public class KVSerializer
    {
        KVSerializer(KVSerializationFormat format)
        {
            this.format = format;
        }

        readonly KVSerializationFormat format;

        /// <summary>
        /// Creates a new <see cref="KVSerializer"/> for the given format.
        /// </summary>
        /// <param name="format">The <see cref="KVSerializationFormat"/> to use when (de)serializing.</param>
        /// <returns>A new <see cref="KVSerializer"/> that (de)serializes with the given format.</returns>
        public static KVSerializer Create(KVSerializationFormat format)
            => new(format);

        /// <summary>
        /// Deserializes a KeyValue object from a stream.
        /// </summary>
        /// <param name="stream">The stream to deserialize from.</param>
        /// <param name="options">Options to use that can influence the deserialization process.</param>
        /// <returns>A <see cref="KVDocument"/> representing the KeyValues structure encoded in the stream.</returns>
        public KVDocument Deserialize(Stream stream, KVSerializerOptions? options = null)
        {
            ArgumentNullException.ThrowIfNull(stream);

            var builder = new KVObjectBuilder(useDictionaryForCollections: format == KVSerializationFormat.KeyValues3Text);

            using var reader = MakeReader(stream, builder, options ?? KVSerializerOptions.DefaultOptions);

            var header = reader.ReadHeader();
            var result = builder.GetObject();

            return new KVDocument(header, result.Key, result.Value);
        }

        /// <summary>
        /// Deserializes an object from a KeyValues representation in a stream.
        /// </summary>
        /// <param name="stream">The stream to deserialize from.</param>
        /// <param name="options">Options to use that can influence the deserialization process.</param>
        /// <returns>A <typeparamref name="TObject" /> instance representing the KeyValues structure in the stream.</returns>
        /// <typeparam name="TObject">The type of object to deserialize.</typeparam>
        public TObject Deserialize<[DynamicallyAccessedMembers(Trimming.Constructors | Trimming.Properties)] TObject>(Stream stream, KVSerializerOptions? options = null)
        {
            ArgumentNullException.ThrowIfNull(stream);

            var @object = Deserialize(stream, options ?? KVSerializerOptions.DefaultOptions);
            var typedObject = ObjectCopier.MakeObject<TObject>(@object.Root);
            return typedObject;
        }

        /// <summary>
        /// Serializes a KeyValue object into stream.
        /// </summary>
        /// <param name="stream">The stream to serialize into.</param>
        /// <param name="data">The data to serialize.</param>
        /// <param name="name">The top-level object name.</param>
        /// <param name="options">Options to use that can influence the serialization process.</param>
        public void Serialize(Stream stream, KVObject data, string name, KVSerializerOptions? options = null)
        {
            ArgumentNullException.ThrowIfNull(stream);
            ArgumentNullException.ThrowIfNull(data);

            using var serializer = MakeSerializer(stream, options ?? KVSerializerOptions.DefaultOptions);
            var visitor = new KVObjectVisitor(serializer);
            visitor.Visit(name, data);
        }

        /// <summary>
        /// Serializes a KeyValue document into stream, preserving header encoding and format.
        /// </summary>
        /// <param name="stream">The stream to serialize into.</param>
        /// <param name="data">The data to serialize.</param>
        /// <param name="options">Options to use that can influence the serialization process.</param>
        public void Serialize(Stream stream, KVDocument data, KVSerializerOptions? options = null)
        {
            ArgumentNullException.ThrowIfNull(stream);
            ArgumentNullException.ThrowIfNull(data);

            using var serializer = MakeSerializer(stream, options ?? KVSerializerOptions.DefaultOptions, data.Header);
            var visitor = new KVObjectVisitor(serializer);
            visitor.Visit(data.Name, data.Root);
        }

        /// <summary>
        /// Serializes a KeyValue object into stream in plain text.
        /// </summary>
        /// <param name="stream">The stream to serialize into.</param>
        /// <param name="data">The data to serialize.</param>
        /// <param name="name">The top-level object name.</param>
        /// <param name="options">Options to use that can influence the serialization process.</param>
        /// <typeparam name="TData">The type of object to serialize.</typeparam>
        public void Serialize<[DynamicallyAccessedMembers(Trimming.Properties)] TData>(Stream stream, TData data, string name, KVSerializerOptions? options = null)
        {
            ArgumentNullException.ThrowIfNull(stream);
            ArgumentNullException.ThrowIfNull(data);
            ArgumentNullException.ThrowIfNull(name);

            var kvObjectTree = ObjectCopier.FromObject(typeof(TData), data);

            using var serializer = MakeSerializer(stream, options ?? KVSerializerOptions.DefaultOptions);
            var visitor = new KVObjectVisitor(serializer);
            visitor.Visit(name, kvObjectTree);
        }

        /// <summary>
        /// Deserializes a KeyValues document from a text string and produces a per-token source
        /// map describing where each token sits in the input. The span offsets index directly
        /// into the supplied <paramref name="text"/>, so the same string can be displayed
        /// verbatim and highlighted using the returned spans.
        /// Only text formats are supported.
        /// </summary>
        /// <param name="text">The KeyValues text to parse.</param>
        /// <param name="options">Options to use that can influence the deserialization process.</param>
        /// <returns>The parsed document and a list of <see cref="KvSourceSpan"/> records covering each token in the input.</returns>
        public (KVDocument Document, IReadOnlyList<KvSourceSpan> Spans) DeserializeWithSourceMap(string text, KVSerializerOptions? options = null)
        {
            ArgumentNullException.ThrowIfNull(text);

            var resolvedOptions = options ?? KVSerializerOptions.DefaultOptions;
            var spans = new List<KvSourceSpan>();
            var builder = new KVObjectBuilder(useDictionaryForCollections: format == KVSerializationFormat.KeyValues3Text);

            using var stringReader = new StringReader(text);
            using var reader = MakeSourceMapReader(stringReader, builder, resolvedOptions, spans);

            var header = reader.ReadHeader();
            var result = builder.GetObject();
            var document = new KVDocument(header, result.Key, result.Value);

            return (document, spans);
        }

        IVisitingReader MakeSourceMapReader(TextReader textReader, IParsingVisitationListener listener, KVSerializerOptions options, List<KvSourceSpan> spans)
        {
            return format switch
            {
                KVSerializationFormat.KeyValues1Text => new KV1TextReader(textReader, listener, options, spans),
                KVSerializationFormat.KeyValues3Text => new KV3TextReader(textReader, listener, options.SkipHeader, spans),
                _ => throw new InvalidOperationException($"Source maps are only supported for text formats, not {format}."),
            };
        }

        /// <summary>
        /// Serializes a KeyValue document to text and produces a per-token source map alongside it.
        /// Intended for syntax highlighters that want exact token boundaries instead of regex
        /// approximations. Only text formats are supported. Header (for KV3) is preserved.
        /// </summary>
        /// <param name="data">The document to serialize.</param>
        /// <param name="options">Options to use that can influence the serialization process.</param>
        /// <returns>The serialized text and a list of <see cref="KvSourceSpan"/> records covering each token.</returns>
        public (string Text, IReadOnlyList<KvSourceSpan> Spans) SerializeWithSourceMap(KVDocument data, KVSerializerOptions? options = null)
        {
            ArgumentNullException.ThrowIfNull(data);
            return SerializeWithSourceMapCore(data.Root, data.Name, data.Header, options);
        }

        /// <summary>
        /// Serializes a KeyValue object to text and produces a per-token source map alongside it.
        /// Intended for syntax highlighters that want exact token boundaries instead of regex
        /// approximations. Only text formats are supported.
        /// </summary>
        /// <param name="data">The object to serialize.</param>
        /// <param name="name">The top-level object name.</param>
        /// <param name="options">Options to use that can influence the serialization process.</param>
        /// <returns>The serialized text and a list of <see cref="KvSourceSpan"/> records covering each token.</returns>
        public (string Text, IReadOnlyList<KvSourceSpan> Spans) SerializeWithSourceMap(KVObject data, string? name = null, KVSerializerOptions? options = null)
        {
            ArgumentNullException.ThrowIfNull(data);
            return SerializeWithSourceMapCore(data, name, header: null, options);
        }

        /// <summary>
        /// Serializes a typed object to KeyValues text and produces a per-token source map alongside it.
        /// Intended for syntax highlighters that want exact token boundaries instead of regex
        /// approximations. Only text formats are supported.
        /// </summary>
        /// <param name="data">The object to serialize.</param>
        /// <param name="name">The top-level object name.</param>
        /// <param name="options">Options to use that can influence the serialization process.</param>
        /// <typeparam name="TData">The type of object to serialize.</typeparam>
        /// <returns>The serialized text and a list of <see cref="KvSourceSpan"/> records covering each token.</returns>
        public (string Text, IReadOnlyList<KvSourceSpan> Spans) SerializeWithSourceMap<[DynamicallyAccessedMembers(Trimming.Properties)] TData>(TData data, string name, KVSerializerOptions? options = null)
        {
            ArgumentNullException.ThrowIfNull(data);
            ArgumentNullException.ThrowIfNull(name);

            var kvObjectTree = ObjectCopier.FromObject(typeof(TData), data);
            return SerializeWithSourceMapCore(kvObjectTree, name, header: null, options);
        }

        (string Text, IReadOnlyList<KvSourceSpan> Spans) SerializeWithSourceMapCore(KVObject root, string? name, KVHeader? header, KVSerializerOptions? options)
        {
            var resolvedOptions = options ?? KVSerializerOptions.DefaultOptions;
            var sb = new StringBuilder();
            var spans = new List<KvSourceSpan>();

            using var serializer = MakeSourceMapSerializer(sb, spans, resolvedOptions, header);
            var visitor = new KVObjectVisitor(serializer);
            visitor.Visit(name, root);

            return (sb.ToString(), spans);
        }

        IVisitationListener MakeSourceMapSerializer(StringBuilder sb, List<KvSourceSpan> spans, KVSerializerOptions options, KVHeader? header)
        {
            return format switch
            {
                KVSerializationFormat.KeyValues1Text => new KV1TextSerializer(sb, spans, options),
                KVSerializationFormat.KeyValues3Text => new KV3TextSerializer(sb, spans, header, options.SkipHeader),
                _ => throw new InvalidOperationException($"Source maps are only supported for text formats, not {format}."),
            };
        }

        IVisitingReader MakeReader(Stream stream, IParsingVisitationListener listener, KVSerializerOptions options)
        {
            ArgumentNullException.ThrowIfNull(stream);
            ArgumentNullException.ThrowIfNull(listener);
            ArgumentNullException.ThrowIfNull(options);

            return format switch
            {
                KVSerializationFormat.KeyValues1Text => new KV1TextReader(new StreamReader(stream, null, true, -1, leaveOpen: true), listener, options),
                KVSerializationFormat.KeyValues1Binary => new KV1BinaryReader(stream, listener, options.StringTable!),
                KVSerializationFormat.KeyValues3Text => new KV3TextReader(new StreamReader(stream, null, true, -1, leaveOpen: true), listener, options.SkipHeader),
                _ => throw new InvalidOperationException($"Invalid serialization format: {format}"),
            };
        }

        IVisitationListener MakeSerializer(Stream stream, KVSerializerOptions options, KVHeader? header = null)
        {
            ArgumentNullException.ThrowIfNull(stream);
            ArgumentNullException.ThrowIfNull(options);

            return format switch
            {
                KVSerializationFormat.KeyValues1Text => new KV1TextSerializer(stream, options),
                KVSerializationFormat.KeyValues1Binary => new KV1BinarySerializer(stream, options.StringTable!),
                KVSerializationFormat.KeyValues3Text => new KV3TextSerializer(stream, header, options.SkipHeader),
                _ => throw new InvalidOperationException($"Invalid serialization format: {format}"),
            };
        }
    }
}
