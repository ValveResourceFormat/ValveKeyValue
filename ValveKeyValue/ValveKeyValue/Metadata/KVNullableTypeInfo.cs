namespace ValveKeyValue.Metadata
{
    sealed class KVNullableTypeInfo<T> : KVTypeInfo<T?>
        where T : struct
    {
        public KVNullableTypeInfo(KVTypeInfo<T> underlying)
        {
            this.underlying = underlying;
        }

        readonly KVTypeInfo<T> underlying;

        internal override T? ReadCore(KVObject value) => underlying.Read(value);

        internal override KVObject Write(T? value, KVWriteContext context) => underlying.Write(value!.Value, context);
    }
}
