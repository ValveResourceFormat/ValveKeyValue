namespace ValveKeyValue.Metadata
{
    // Maps a byte array to a binary blob. A KeyValues array, or a collection whose keys are the element indices, is
    // read as a collection of bytes.
    sealed class KVByteArrayTypeInfo : KVTypeInfo<byte[]>
    {
        readonly KVCollectionTypeInfo<byte[], byte> elements = new(KVMetadata.Byte);

        internal override byte[] ReadCore(KVObject value) => value.ValueType switch
        {
            KVValueType.BinaryBlob => value.AsBlob(),
            KVValueType.Collection or KVValueType.Array => elements.ReadCore(value),
            _ => throw ConversionNotSupported(value),
        };

        internal override KVObject Write(byte[] value, KVWriteContext context)
        {
            context.Enter(value);
            return KVObject.Blob(value);
        }
    }
}
