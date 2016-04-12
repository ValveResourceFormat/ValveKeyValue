namespace ValveKeyValue.Test
{
    sealed class StreamKVTextReader : IKVTextReader
    {
        KVObject IKVTextReader.Read(string resourceName, KVSerializerOptions options)
        {
            using (var stream = TestDataHelper.OpenResource(resourceName))
            {
                return KVSerializer.Deserialize(stream, options);
            }
        }

        T IKVTextReader.Read<T>(string resourceName, KVSerializerOptions options)
        {
            using (var stream = TestDataHelper.OpenResource(resourceName))
            {
                return KVSerializer.Deserialize<T>(stream, options);
            }
        }
    }
}
