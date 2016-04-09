using System.IO;

namespace ValveKeyValue
{
    /// <summary>
    /// Helper class to easily deserialize KeyValue objects.
    /// </summary>
    public static class KVSerializer
    {
        /// <summary>
        /// Deserializes a KeyValue object from a stream.
        /// </summary>
        /// <param name="stream">The stream to deserialize from.</param>
        /// <returns>A <see cref="KVObject"/> representing the KeyValues structure in the stream.</returns>
        public static KVObject Deserialize(Stream stream)
        {
            return Deserialize(stream, null);
        }

        /// <summary>
        /// Deserializes a KeyValue object from a stream.
        /// </summary>
        /// <param name="stream">The stream to deserialize from.</param>
        /// <param name="options">Options to use that can influence the deserialization process.</param>
        /// <returns>A <see cref="KVObject"/> representing the KeyValues structure in the stream.</returns>
        public static KVObject Deserialize(Stream stream, KVSerializerOptions options)
        {
            Require.NotNull(stream, nameof(stream));

            using (var reader = new KVTextReader(stream, options ?? KVSerializerOptions.DefaultOptions))
            {
                return reader.ReadObject();
            }
        }

        /// <summary>
        /// Serializes a KeyValue object into stream in plain text..
        /// </summary>
        /// <param name="stream">The stream to serialize into.</param>
        /// <param name="data">The data to serialize.</param>
        public static void Serialize(Stream stream, KVObject data)
        {
            using (var writer = new KVTextWriter(stream))
            {
                writer.WriteObject(data);
            }
        }

        /// <summary>
        /// Deserializes an object from a KeyValues representation in a stream.
        /// </summary>
        /// <param name="stream">The stream to deserialize from.</param>
        /// <returns>A <typeparamref name="TObject" /> instance representing the KeyValues structure in the stream.</returns>
        /// <typeparam name="TObject">The type of object to deserialize.</typeparam>;
        public static TObject Deserialize<TObject>(Stream stream)
        {
            Require.NotNull(stream, nameof(stream));

            var @object = Deserialize(stream);
            var typedObject = ObjectCopier.MakeObject<TObject>(@object);
            return typedObject;
        }

        /// <summary>
        /// Deserializes an object from a KeyValues representation in a stream.
        /// </summary>
        /// <param name="stream">The stream to deserialize from.</param>
        /// <param name="options">Options to use that can influence the deserialization process.</param>
        /// <returns>A <see cref="KVObject"/> representing the KeyValues structure in the stream.</returns>
        /// <returns>A <typeparamref name="TObject" /> instance representing the KeyValues structure in the stream.</returns>
        /// <typeparam name="TObject">The type of object to deserialize.</typeparam>;
        public static TObject Deserialize<TObject>(Stream stream, KVSerializerOptions options)
        {
            Require.NotNull(stream, nameof(stream));
            Require.NotNull(options, nameof(options));

            var @object = Deserialize(stream, options);
            var typedObject = ObjectCopier.MakeObject<TObject>(@object);
            return typedObject;
        }
    }
}
