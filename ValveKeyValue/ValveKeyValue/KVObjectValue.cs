using System.Diagnostics;

namespace ValveKeyValue
{
    [DebuggerDisplay("{value}")]
    class KVObjectValue<TObject> : KVValue
        where TObject : IConvertible
    {
        public KVObjectValue(TObject value, KVValueType valueType)
        {
            if (value == null)
            {
                throw new ArgumentNullException(nameof(value));
            }

            this.value = value;
            ValueType = valueType;
        }

        readonly TObject value;

        public override KVValueType ValueType { get; }

        public override TypeCode GetTypeCode()
        {
            return ValueType switch
            {
                KVValueType.Collection => TypeCode.Object,
                KVValueType.FloatingPoint => TypeCode.Single,
                KVValueType.Int32 or KVValueType.Pointer => TypeCode.Int32,
                KVValueType.String => TypeCode.String,
                KVValueType.UInt64 => TypeCode.UInt64,
                _ => throw new NotImplementedException($"No known TypeCode for '{ValueType}'."),
            };
        }

        public override bool ToBoolean(IFormatProvider provider) => ToInt32(provider) != 0;

        public override byte ToByte(IFormatProvider provider) => (byte)Convert.ChangeType(value, typeof(byte), provider);

        public override char ToChar(IFormatProvider provider) => (char)Convert.ChangeType(value, typeof(char), provider);

        public override DateTime ToDateTime(IFormatProvider provider) => throw new InvalidCastException("Casting to DateTime is not supported.");

        public override decimal ToDecimal(IFormatProvider provider) => (decimal)Convert.ChangeType(value, typeof(decimal), provider);

        public override double ToDouble(IFormatProvider provider) => (double)Convert.ChangeType(value, typeof(double), provider);

        public override short ToInt16(IFormatProvider provider) => (short)Convert.ChangeType(value, typeof(short), provider);

        public override int ToInt32(IFormatProvider provider) => (int)Convert.ChangeType(value, typeof(int), provider);

        public override long ToInt64(IFormatProvider provider) => (long)Convert.ChangeType(value, typeof(long), provider);

        public override sbyte ToSByte(IFormatProvider provider) => (sbyte)Convert.ChangeType(value, typeof(sbyte), provider);

        public override float ToSingle(IFormatProvider provider) => (float)Convert.ChangeType(value, typeof(float), provider);

        public override string ToString(IFormatProvider provider) => (string)Convert.ChangeType(value, typeof(string), provider);

        public override object ToType(Type conversionType, IFormatProvider provider) => Convert.ChangeType(value, conversionType, provider);

        public override ushort ToUInt16(IFormatProvider provider) => (ushort)Convert.ChangeType(value, typeof(ushort), provider);

        public override uint ToUInt32(IFormatProvider provider) => (uint)Convert.ChangeType(value, typeof(uint), provider);

        public override ulong ToUInt64(IFormatProvider provider) => (ulong)Convert.ChangeType(value, typeof(ulong), provider);

        public override string ToString() => ToString(null);
    }
}
