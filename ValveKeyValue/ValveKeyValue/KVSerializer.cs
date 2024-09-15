using ValveKeyValue.Abstraction;
using ValveKeyValue.Deserialization;
using ValveKeyValue.Deserialization.KeyValues1;
using ValveKeyValue.Serialization.KeyValues1;

namespace ValveKeyValue
{
    /// <summary>
    /// Helper class to easily deserialize KeyValue objects.
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
        /// <param name="format">The <see cref="KVSerializationFormat"/> to use when (de)serializing. </param>
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
            Require.NotNull(stream, nameof(stream));
            var builder = new KVObjectBuilder();

            using (var reader = MakeReader(stream, builder, options ?? KVSerializerOptions.DefaultOptions))
            {
                reader.ReadObject();
            }

            var root = builder.GetObject();
            return new KVDocument(root.Name, root.Value);
        }

        /// <summary>
        /// Deserializes an object from a KeyValues representation in a stream.
        /// </summary>
        /// <param name="stream">The stream to deserialize from.</param>
        /// <param name="options">Options to use that can influence the deserialization process.</param>
        /// <returns>A <typeparamref name="TObject" /> instance representing the KeyValues structure in the stream.</returns>
        /// <typeparam name="TObject">The type of object to deserialize.</typeparam>;
        public TObject Deserialize<TObject>(Stream stream, KVSerializerOptions options = null)
        {
            Require.NotNull(stream, nameof(stream));

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
            using var serializer = MakeSerializer(stream, options ?? KVSerializerOptions.DefaultOptions);
            var visitor = new KVObjectVisitor(serializer);
            visitor.Visit(data);
        }

        /// <summary>
        /// Serializes a KeyValue object into stream.
        /// </summary>
        /// <param name="stream">The stream to serialize into.</param>
        /// <param name="data">The data to serialize.</param>
        /// <param name="options">Options to use that can influence the serialization process.</param>
        public void Serialize(Stream stream, KVDocument data, KVSerializerOptions options = null) =>
            Serialize(stream, (KVObject)data, options);

        /// <summary>
        /// Serializes a KeyValue object into stream in plain text..
        /// </summary>
        /// <param name="stream">The stream to serialize into.</param>
        /// <param name="data">The data to serialize.</param>
        /// <param name="name">The top-level object name</param>
        /// <param name="options">Options to use that can influence the serialization process.</param>
        /// <typeparam name="TData">The type of object to serialize.</typeparam>
        public void Serialize<TData>(Stream stream, TData data, string name, KVSerializerOptions options = null)
        {
            Require.NotNull(stream, nameof(stream));

            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            Require.NotNull(name, nameof(name));

            var kvObjectTree = ObjectCopier.FromObject(typeof(TData), data, name);
            Serialize(stream, kvObjectTree, options);
        }

        IVisitingReader MakeReader(Stream stream, IParsingVisitationListener listener, KVSerializerOptions options)
        {
            Require.NotNull(stream, nameof(stream));
            Require.NotNull(listener, nameof(listener));
            Require.NotNull(options, nameof(options));

            return format switch
            {
                KVSerializationFormat.KeyValues1Text => new KV1TextReader(new StreamReader(stream, null, true, -1, leaveOpen: true), listener, options),
                KVSerializationFormat.KeyValues1Binary => new KV1BinaryReader(stream, listener, options.StringTable),
                _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Invalid serialization format."),
            };
        }

        IVisitationListener MakeSerializer(Stream stream, KVSerializerOptions options)
        {
            Require.NotNull(stream, nameof(stream));
            Require.NotNull(options, nameof(options));

            return format switch
            {
                KVSerializationFormat.KeyValues1Text => new KV1TextSerializer(stream, options),
                KVSerializationFormat.KeyValues1Binary => new KV1BinarySerializer(stream, options.StringTable),
                _ => throw new ArgumentOutOfRangeException(nameof(format), format, "Invalid serialization format."),
            };
            ;
        }
    }
}
