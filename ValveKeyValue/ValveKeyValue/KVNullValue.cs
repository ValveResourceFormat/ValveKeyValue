namespace ValveKeyValue
{
    /// <summary>
    /// Represents a null KeyValue value.
    /// </summary>
    public class KVNullValue : KVValue
    {
        /// <inheritdoc/>
        public override KVValueType ValueType => KVValueType.Null;

        /// <inheritdoc/>
        public override TypeCode GetTypeCode() => TypeCode.Empty;

        /// <inheritdoc/>
        public override bool ToBoolean(IFormatProvider provider) => throw new NotSupportedException("Cannot convert null to Boolean.");

        /// <inheritdoc/>
        public override byte ToByte(IFormatProvider provider) => throw new NotSupportedException("Cannot convert null to Byte.");

        /// <inheritdoc/>
        public override char ToChar(IFormatProvider provider) => throw new NotSupportedException("Cannot convert null to Char.");

        /// <inheritdoc/>
        public override DateTime ToDateTime(IFormatProvider provider) => throw new NotSupportedException("Cannot convert null to DateTime.");

        /// <inheritdoc/>
        public override decimal ToDecimal(IFormatProvider provider) => throw new NotSupportedException("Cannot convert null to Decimal.");

        /// <inheritdoc/>
        public override double ToDouble(IFormatProvider provider) => throw new NotSupportedException("Cannot convert null to Double.");

        /// <inheritdoc/>
        public override short ToInt16(IFormatProvider provider) => throw new NotSupportedException("Cannot convert null to Int16.");

        /// <inheritdoc/>
        public override int ToInt32(IFormatProvider provider) => throw new NotSupportedException("Cannot convert null to Int32.");

        /// <inheritdoc/>
        public override long ToInt64(IFormatProvider provider) => throw new NotSupportedException("Cannot convert null to Int64.");

        /// <inheritdoc/>
        public override sbyte ToSByte(IFormatProvider provider) => throw new NotSupportedException("Cannot convert null to SByte.");

        /// <inheritdoc/>
        public override float ToSingle(IFormatProvider provider) => throw new NotSupportedException("Cannot convert null to Single.");

        /// <inheritdoc/>
        public override string ToString(IFormatProvider provider) => string.Empty;

        /// <inheritdoc/>
        public override object ToType(Type conversionType, IFormatProvider provider) => throw new NotSupportedException($"Cannot convert null to {conversionType}.");

        /// <inheritdoc/>
        public override ushort ToUInt16(IFormatProvider provider) => throw new NotSupportedException("Cannot convert null to UInt16.");

        /// <inheritdoc/>
        public override uint ToUInt32(IFormatProvider provider) => throw new NotSupportedException("Cannot convert null to UInt32.");

        /// <inheritdoc/>
        public override ulong ToUInt64(IFormatProvider provider) => throw new NotSupportedException("Cannot convert null to UInt64.");

        /// <inheritdoc/>
        public override string ToString() => string.Empty;
    }
}
