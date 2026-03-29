using System.Globalization;

namespace ValveKeyValue
{
    public partial class KVObject : IConvertible
    {
        TypeCode IConvertible.GetTypeCode() => Value.ValueType switch
        {
            KVValueType.Boolean => TypeCode.Boolean,
            KVValueType.String => TypeCode.String,
            KVValueType.Int16 => TypeCode.Int16,
            KVValueType.Int32 or KVValueType.Pointer => TypeCode.Int32,
            KVValueType.Int64 => TypeCode.Int64,
            KVValueType.UInt16 => TypeCode.UInt16,
            KVValueType.UInt32 => TypeCode.UInt32,
            KVValueType.UInt64 => TypeCode.UInt64,
            KVValueType.FloatingPoint => TypeCode.Single,
            KVValueType.FloatingPoint64 => TypeCode.Double,
            KVValueType.Null => TypeCode.Empty,
            _ => TypeCode.Object,
        };
        bool IConvertible.ToBoolean(IFormatProvider provider) => Value.ToBoolean(provider);
        byte IConvertible.ToByte(IFormatProvider provider) => Value.ToByte(provider);
        char IConvertible.ToChar(IFormatProvider provider) => Value.ToChar(provider);
        DateTime IConvertible.ToDateTime(IFormatProvider provider) => throw new NotSupportedException("Cannot convert KeyValue to DateTime.");
        decimal IConvertible.ToDecimal(IFormatProvider provider) => Value.ToDecimal(provider);
        double IConvertible.ToDouble(IFormatProvider provider) => Value.ToDouble(provider);
        short IConvertible.ToInt16(IFormatProvider provider) => Value.ToInt16(provider);
        int IConvertible.ToInt32(IFormatProvider provider) => Value.ToInt32(provider);
        long IConvertible.ToInt64(IFormatProvider provider) => Value.ToInt64(provider);
        sbyte IConvertible.ToSByte(IFormatProvider provider) => Value.ToSByte(provider);
        float IConvertible.ToSingle(IFormatProvider provider) => Value.ToSingle(provider);
        string IConvertible.ToString(IFormatProvider provider) => Value.ToString(provider);
        object IConvertible.ToType(Type conversionType, IFormatProvider provider) => Value.ToType(conversionType, provider);
        ushort IConvertible.ToUInt16(IFormatProvider provider) => Value.ToUInt16(provider);
        uint IConvertible.ToUInt32(IFormatProvider provider) => Value.ToUInt32(provider);
        ulong IConvertible.ToUInt64(IFormatProvider provider) => Value.ToUInt64(provider);
    }
}
