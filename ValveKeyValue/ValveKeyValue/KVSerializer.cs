using System;
using System.IO;
using ValveKeyValue.Abstraction;
using ValveKeyValue.Deserialization;
using ValveKeyValue.Serialization;

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
        /// <param name="options">Options to use that can influence the deserialization process.</param>
        /// <returns>A <see cref="KVObject"/> representing the KeyValues structure encoded in the stream.</returns>
        public static KVObject Deserialize(Stream stream, KVSerializerOptions options = null)
        {
            Require.NotNull(stream, nameof(stream));

            var builder = new KVObjectBuilder();

            using (var reader = new KV1TextReader(new StreamReader(stream), builder, options ?? KVSerializerOptions.DefaultOptions))
            {
                reader.ReadObject();
            }

            return builder.GetObject();
        }

        /// <summary>
        /// Deserializes an object from a textual KeyValues representation.
        /// </summary>
        /// <param name="text">The text to deserialize.</param>
        /// <param name="options">Options to use that can influence the deserialization process.</param>
        /// <returns>A <see cref="KVObject"/> representing the KeyValues structure encoded in the text.</returns>
        public static KVObject Deserialize(string text, KVSerializerOptions options = null)
        {
            Require.NotNull(text, nameof(text));

            var builder = new KVObjectBuilder();

            using (var reader = new KV1TextReader(new StringReader(text), builder, options ?? KVSerializerOptions.DefaultOptions))
            {
                reader.ReadObject();
            }

            return builder.GetObject();
        }

        /// <summary>
        /// Deserializes an object from a binary KeyValues representation.
        /// </summary>
        /// <param name="data">The data to deserialize.</param>
        /// <returns>A <see cref="KVObject"/> representing the KeyValues structure encoded in the data.</returns>
        public static KVObject Deserialize(byte[] data)
        {
            Require.NotNull(data, nameof(data));

            var builder = new KVObjectBuilder();

            using (var ms = new MemoryStream(data))
            using (var reader = new KV1BinaryReader(ms, builder))
            {
                reader.ReadObject();
            }

            return builder.GetObject();
        }

        /// <summary>
        /// Deserializes an object from a KeyValues representation in a stream.
        /// </summary>
        /// <param name="stream">The stream to deserialize from.</param>
        /// <param name="options">Options to use that can influence the deserialization process.</param>
        /// <returns>A <typeparamref name="TObject" /> instance representing the KeyValues structure in the stream.</returns>
        /// <typeparam name="TObject">The type of object to deserialize.</typeparam>;
        public static TObject Deserialize<TObject>(Stream stream, KVSerializerOptions options = null)
        {
            Require.NotNull(stream, nameof(stream));

            var @object = Deserialize(stream, options ?? KVSerializerOptions.DefaultOptions);
            var typedObject = ObjectCopier.MakeObject<TObject>(@object);
            return typedObject;
        }

        /// <summary>
        /// Deserializes an object from a textual KeyValues representation.
        /// </summary>
        /// <param name="text">The text to deserialize.</param>
        /// <param name="options">Options to use that can influence the deserialization process.</param>
        /// <returns>A <typeparamref name="TObject" /> instance representing the KeyValues structure encoded in the stream.</returns>
        /// <typeparam name="TObject">The type of object to deserialize.</typeparam>;
        public static TObject Deserialize<TObject>(string text, KVSerializerOptions options = null)
        {
            Require.NotNull(text, nameof(text));

            var @object = Deserialize(text, options ?? KVSerializerOptions.DefaultOptions);
            var typedObject = ObjectCopier.MakeObject<TObject>(@object);
            return typedObject;
        }

        /// <summary>
        /// Deserializes an object from a binary KeyValues representation.
        /// </summary>
        /// <param name="data">The data to deserialize.</param>
        /// <returns>A <typeparamref name="TObject" /> instance representing the KeyValues structure encoded in the stream.</returns>
        /// <typeparam name="TObject">The type of object to deserialize.</typeparam>;
        public static TObject Deserialize<TObject>(byte[] data)
        {
            Require.NotNull(data, nameof(data));

            var @object = Deserialize(data);
            var typedObject = ObjectCopier.MakeObject<TObject>(@object);
            return typedObject;
        }

        /// <summary>
        /// Serializes a KeyValue object into stream in plain text..
        /// </summary>
        /// <param name="stream">The stream to serialize into.</param>
        /// <param name="data">The data to serialize.</param>
        /// <param name="options">Options to use that can influence the serialization process.</param>
        public static void Serialize(Stream stream, KVObject data, KVSerializerOptions options = null)
        {
            using (var serializer = new KV1TextSerializer(stream, options ?? KVSerializerOptions.DefaultOptions))
            {
                var visitor = new KVObjectVisitor(serializer);
                visitor.Visit(data);
            }
        }

        /// <summary>
        /// Serializes a KeyValue object into stream in plain text..
        /// </summary>
        /// <param name="stream">The stream to serialize into.</param>
        /// <param name="data">The data to serialize.</param>
        /// <param name="name">The top-level object name</param>
        /// <param name="options">Options to use that can influence the serialization process.</param>
        /// <typeparam name="TData">The type of object to serialize.</typeparam>
        public static void Serialize<TData>(Stream stream, TData data, string name, KVSerializerOptions options = null)
        {
            Require.NotNull(stream, nameof(stream));

            if (data == null)
            {
                throw new ArgumentNullException(nameof(data));
            }

            Require.NotNull(name, nameof(name));

            var kvObjectTree = ObjectCopier.FromObject<TData>(data, name);
            Serialize(stream, kvObjectTree, options);
        }
    }
}
