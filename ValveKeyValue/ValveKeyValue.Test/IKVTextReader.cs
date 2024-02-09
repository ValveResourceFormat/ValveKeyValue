namespace ValveKeyValue.Test
{
    interface IKVTextReader
    {
        KVObject Read(string resourceName, KVSerializerOptions options = null);

        T Read<T>(string resourceName, KVSerializerOptions options = null);
    }
}
