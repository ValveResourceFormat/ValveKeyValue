namespace ValveKeyValue.Test
{
    sealed class StringKVTextReader : IKVTextReader
    {
        public StringKVTextReader()
        {
            serializer = KVSerializer.Create(KVSerializationFormat.KeyValues1Text);
        }

        readonly KVSerializer serializer;

        KVObject IKVTextReader.Read(string resourceName, KVSerializerOptions options)
        {
            var text = TestDataHelper.ReadTextResource(resourceName);
            return serializer.Deserialize(text, options);
        }

        T IKVTextReader.Read<T>(string resourceName, KVSerializerOptions options)
        {
            var text = TestDataHelper.ReadTextResource(resourceName);
            return serializer.Deserialize<T>(text, options);
        }
    }
}
