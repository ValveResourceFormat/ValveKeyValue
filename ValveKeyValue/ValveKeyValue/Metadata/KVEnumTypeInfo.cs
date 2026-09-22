using System.Runtime.CompilerServices;

namespace ValveKeyValue.Metadata
{
    // Maps an enum to its underlying value. Strings, including dictionary keys, are parsed as either a number
    // or case-insensitive member names, comma-separated for flags.
    sealed class KVEnumTypeInfo<TEnum, TUnderlying> : KVTypeInfo<TEnum>
        where TEnum : struct, Enum
        where TUnderlying : unmanaged
    {
        public KVEnumTypeInfo(KVScalarTypeInfo<TUnderlying> underlying)
        {
            this.underlying = underlying;
            convert = Convert;
        }

        readonly KVScalarTypeInfo<TUnderlying> underlying;
        readonly Func<KVObject, TEnum> convert;

        internal override TEnum ReadCore(KVObject value) => ReadScalar(value, convert);

        internal override KVObject Write(TEnum value, KVWriteContext context)
            => underlying.Write(Unsafe.BitCast<TEnum, TUnderlying>(value), context);

        TEnum Convert(KVObject value) => value.ValueType == KVValueType.String
            ? Enum.Parse<TEnum>((string)value, ignoreCase: true)
            : Unsafe.BitCast<TUnderlying, TEnum>(underlying.Convert(value));
    }
}
