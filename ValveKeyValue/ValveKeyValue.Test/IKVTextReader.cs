namespace ValveKeyValue.Test
{
    interface IKVTextReader
    {
        KVDocument Read(string resourceName, KVSerializerOptions? options = null);

        T Read<T>(string resourceName, KVSerializerOptions? options = null);
    }
}
