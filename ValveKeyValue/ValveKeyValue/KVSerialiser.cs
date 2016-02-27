using System.IO;

namespace ValveKeyValue
{
    /// <summary>
    /// Helper class to easily deserialize KeyValue objects.
    /// </summary>
    public static class KVSerialiser
    {
        /// <summary>
        /// Deserializes a KeyValue object from a stream.
        /// </summary>
        /// <param name="stream">The stream to deserialize from.</param>
        /// <returns>A <see cref="KVObject"/> representing the KeyValues structure in the stream.</returns>
        public static KVObject Deserialize(Stream stream)
        {
            Require.NotNull(stream, nameof(stream));

            using (var reader = new KVTextReader(stream))
            {
                return reader.ReadObject();
            }
        }
    }
}
