using System.Diagnostics.CodeAnalysis;
using ValveKeyValue.Abstraction;
using ValveKeyValue.Deserialization;
using ValveKeyValue.Deserialization.KeyValues1;
using ValveKeyValue.Deserialization.KeyValues2;
using ValveKeyValue.Deserialization.KeyValues3;
using ValveKeyValue.Serialization.KeyValues1;
using ValveKeyValue.Serialization.KeyValues2;
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
        public KVDocument Deserialize(Stream stream, KVSerializerOptions options = null)
        {
            ArgumentNullException.ThrowIfNull(stream);

            if (format == KVSerializationFormat.KeyValues2Text)
            {
                using var kv2Reader = new KV2TextReader(new StreamReader(stream, null, true, -1, leaveOpen: true));
                return kv2Reader.Read();
            }

            if (format == KVSerializationFormat.KeyValues2Binary)
            {
                using var kv2BinaryReader = new KV2BinaryReader(stream);
                return kv2BinaryReader.Read();
            }

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
        public TObject Deserialize<[DynamicallyAccessedMembers(Trimming.Constructors | Trimming.Properties)] TObject>(Stream stream, KVSerializerOptions options = null)
        {
            ArgumentNullException.ThrowIfNull(stream);

            var @object = Deserialize(stream, options ?? KVSerializerOptions.DefaultOptions);
            var typedObject = ObjectCopier.MakeObject<TObject>(@object);
            return typedObject;
        }

        /// <summary>
        /// Serializes a KeyValue object into stream.
        /// </summary>
        /// <param name="stream">The stream to serialize into.</param>
        /// <param name="data">The data to serialize.</param>
        /// <param name="options">Options to use that can influence the serialization process.</param>
        public void Serialize(Stream stream, KVObject data, KVSerializerOptions options = null)
        {
            ArgumentNullException.ThrowIfNull(data);

            var doc = data as KVDocument;

            // KV2 binary is standalone — flat element index doesn't fit the visitor pattern
            if (format == KVSerializationFormat.KeyValues2Binary)
            {
                using var kv2Writer = new KV2BinaryWriter(stream, doc?.Header);
                kv2Writer.Write(doc ?? new KVDocument(null, null, data));
                return;
            }

            using var serializer = MakeSerializer(stream, options ?? KVSerializerOptions.DefaultOptions, doc?.Header);
            var visitor = new KVObjectVisitor(serializer);
            visitor.Visit(doc?.Name, data);
        }

        /// <summary>
        /// Serializes a KeyValue document into stream, preserving header encoding and format.
        /// </summary>
        /// <param name="stream">The stream to serialize into.</param>
        /// <param name="data">The data to serialize.</param>
        /// <param name="options">Options to use that can influence the serialization process.</param>
        public void Serialize(Stream stream, KVDocument data, KVSerializerOptions options = null)
        {
            ArgumentNullException.ThrowIfNull(data);

            if (format == KVSerializationFormat.KeyValues2Binary)
            {
                using var kv2Writer = new KV2BinaryWriter(stream, data.Header);
                kv2Writer.Write(data);
                return;
            }

            using var serializer = MakeSerializer(stream, options ?? KVSerializerOptions.DefaultOptions, data.Header);
            var visitor = new KVObjectVisitor(serializer);
            visitor.Visit(data.Name, data);
        }

        /// <summary>
        /// Serializes a KeyValue object into stream in plain text.
        /// </summary>
        /// <param name="stream">The stream to serialize into.</param>
        /// <param name="data">The data to serialize.</param>
        /// <param name="name">The top-level object name.</param>
        /// <param name="options">Options to use that can influence the serialization process.</param>
        /// <typeparam name="TData">The type of object to serialize.</typeparam>
        public void Serialize<[DynamicallyAccessedMembers(Trimming.Properties)] TData>(Stream stream, TData data, string name, KVSerializerOptions options = null)
        {
            ArgumentNullException.ThrowIfNull(stream);
            ArgumentNullException.ThrowIfNull(data);
            ArgumentNullException.ThrowIfNull(name);

            var kvObjectTree = ObjectCopier.FromObject(typeof(TData), data);

            using var serializer = MakeSerializer(stream, options ?? KVSerializerOptions.DefaultOptions);
            var visitor = new KVObjectVisitor(serializer);
            visitor.Visit(name, kvObjectTree);
        }

        IVisitingReader MakeReader(Stream stream, IParsingVisitationListener listener, KVSerializerOptions options)
        {
            ArgumentNullException.ThrowIfNull(stream);
            ArgumentNullException.ThrowIfNull(listener);
            ArgumentNullException.ThrowIfNull(options);

            return format switch
            {
                KVSerializationFormat.KeyValues1Text => new KV1TextReader(new StreamReader(stream, null, true, -1, leaveOpen: true), listener, options),
                KVSerializationFormat.KeyValues1Binary => new KV1BinaryReader(stream, listener, options.StringTable),
                KVSerializationFormat.KeyValues3Text => new KV3TextReader(new StreamReader(stream, null, true, -1, leaveOpen: true), listener),
                _ => throw new InvalidOperationException($"Invalid serialization format: {format}"),
            };
        }

        IVisitationListener MakeSerializer(Stream stream, KVSerializerOptions options, KVHeader header = null)
        {
            ArgumentNullException.ThrowIfNull(stream);
            ArgumentNullException.ThrowIfNull(options);

            return format switch
            {
                KVSerializationFormat.KeyValues1Text => new KV1TextSerializer(stream, options),
                KVSerializationFormat.KeyValues1Binary => new KV1BinarySerializer(stream, options.StringTable),
                KVSerializationFormat.KeyValues3Text => new KV3TextSerializer(stream, header),
                KVSerializationFormat.KeyValues2Text => new KV2TextWriter(stream, header),
                _ => throw new InvalidOperationException($"Invalid serialization format: {format}"),
            };
        }
    }
}
