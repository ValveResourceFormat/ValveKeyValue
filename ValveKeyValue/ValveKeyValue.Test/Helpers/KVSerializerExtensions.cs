namespace ValveKeyValue.Test
{
    static class KVSerializerExtensions
    {
        public static KVObject Deserialize(this KVSerializer serializer, byte[] data, KVSerializerOptions options = null)
        {
            using var ms = new MemoryStream(data);
            return serializer.Deserialize(ms, options);
        }

        public static TObject Deserialize<TObject>(this KVSerializer serializer, byte[] data, KVSerializerOptions options = null)
        {
            using var ms = new MemoryStream(data);
            return serializer.Deserialize<TObject>(ms, options);
        }

        public static KVObject Deserialize(this KVSerializer serializer, string text, KVSerializerOptions options = null)
        {
            using var ms = new MemoryStream();
            using var writer = new StreamWriter(ms);
            writer.Write(text);
            writer.Flush();

            ms.Seek(0, SeekOrigin.Begin);

            return serializer.Deserialize(ms, options);
        }

        public static TObject Deserialize<TObject>(this KVSerializer serializer, string text, KVSerializerOptions options = null)
        {
            using var ms = new MemoryStream();
            using var writer = new StreamWriter(ms);
            writer.Write(text);
            writer.Flush();

            ms.Seek(0, SeekOrigin.Begin);

            return serializer.Deserialize<TObject>(ms, options);
        }
    }
}
