namespace ValveKeyValue
{
    class KVNullValue : KVValue
    {
        public override KVValueType ValueType => KVValueType.Null;

        public override TypeCode GetTypeCode() => TypeCode.Empty;

        public override bool ToBoolean(IFormatProvider provider) => throw new NotSupportedException("Cannot convert null to Boolean.");

        public override byte ToByte(IFormatProvider provider) => throw new NotSupportedException("Cannot convert null to Byte.");

        public override char ToChar(IFormatProvider provider) => throw new NotSupportedException("Cannot convert null to Char.");

        public override DateTime ToDateTime(IFormatProvider provider) => throw new NotSupportedException("Cannot convert null to DateTime.");

        public override decimal ToDecimal(IFormatProvider provider) => throw new NotSupportedException("Cannot convert null to Decimal.");

        public override double ToDouble(IFormatProvider provider) => throw new NotSupportedException("Cannot convert null to Double.");

        public override short ToInt16(IFormatProvider provider) => throw new NotSupportedException("Cannot convert null to Int16.");

        public override int ToInt32(IFormatProvider provider) => throw new NotSupportedException("Cannot convert null to Int32.");

        public override long ToInt64(IFormatProvider provider) => throw new NotSupportedException("Cannot convert null to Int64.");

        public override sbyte ToSByte(IFormatProvider provider) => throw new NotSupportedException("Cannot convert null to SByte.");

        public override float ToSingle(IFormatProvider provider) => throw new NotSupportedException("Cannot convert null to Single.");

        public override string ToString(IFormatProvider provider) => string.Empty;

        public override object ToType(Type conversionType, IFormatProvider provider) => throw new NotSupportedException($"Cannot convert null to {conversionType}.");

        public override ushort ToUInt16(IFormatProvider provider) => throw new NotSupportedException("Cannot convert null to UInt16.");

        public override uint ToUInt32(IFormatProvider provider) => throw new NotSupportedException("Cannot convert null to UInt32.");

        public override ulong ToUInt64(IFormatProvider provider) => throw new NotSupportedException("Cannot convert null to UInt64.");

        public override string ToString() => string.Empty;
    }
}
