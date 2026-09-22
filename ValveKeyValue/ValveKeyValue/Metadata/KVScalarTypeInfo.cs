namespace ValveKeyValue.Metadata
{
    // Maps a type to a single KeyValues value, converted with the conversions of KVObject.
    sealed class KVScalarTypeInfo<T> : KVTypeInfo<T>
    {
        public KVScalarTypeInfo(Func<KVObject, T> convert, Func<T, KVObject> write)
        {
            this.convert = convert;
            this.write = write;
        }

        readonly Func<KVObject, T> convert;
        readonly Func<T, KVObject> write;

        internal override T ReadCore(KVObject value) => ReadScalar(value, convert);

        internal override KVObject Write(T value, KVWriteContext context) => write(value);

        // Converts a single value without handling collections, arrays or conversion failures.
        internal T Convert(KVObject value) => convert(value);
    }
}
