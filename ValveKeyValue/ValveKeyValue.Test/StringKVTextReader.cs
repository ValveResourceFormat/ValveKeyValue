namespace ValveKeyValue.Test
{
    sealed class StringKVTextReader : IKVTextReader
    {
        KVObject IKVTextReader.Read(string resourceName, KVSerializerOptions options)
        {
            var text = TestDataHelper.ReadTextResource(resourceName);
            return KVSerializer.Deserialize(text, options);
        }

        T IKVTextReader.Read<T>(string resourceName, KVSerializerOptions options)
        {
            var text = TestDataHelper.ReadTextResource(resourceName);
            return KVSerializer.Deserialize<T>(text, options);
        }
    }
}
