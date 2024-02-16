using System.Text;

namespace ValveKeyValue
{
    public class KVBinaryBlob : KVValue
    {
        public Memory<byte> Bytes { get; }

        public override KVValueType ValueType => KVValueType.BinaryBlob;

        public KVBinaryBlob(byte[] value)
        {
            Bytes = value;
        }

        public KVBinaryBlob(Memory<byte> value)
        {
            Bytes = value;
        }

        #region IConvertible

        public override TypeCode GetTypeCode()
        {
            throw new NotSupportedException();
        }

        public override bool ToBoolean(IFormatProvider provider)
        {
            throw new NotSupportedException();
        }

        public override byte ToByte(IFormatProvider provider)
        {
            throw new NotSupportedException();
        }

        public override char ToChar(IFormatProvider provider)
        {
            throw new NotSupportedException();
        }

        public override DateTime ToDateTime(IFormatProvider provider)
        {
            throw new NotSupportedException();
        }

        public override decimal ToDecimal(IFormatProvider provider)
        {
            throw new NotSupportedException();
        }

        public override double ToDouble(IFormatProvider provider)
        {
            throw new NotSupportedException();
        }

        public override short ToInt16(IFormatProvider provider)
        {
            throw new NotSupportedException();
        }

        public override int ToInt32(IFormatProvider provider)
        {
            throw new NotSupportedException();
        }

        public override long ToInt64(IFormatProvider provider)
        {
            throw new NotSupportedException();
        }

        public override sbyte ToSByte(IFormatProvider provider)
        {
            throw new NotSupportedException();
        }

        public override float ToSingle(IFormatProvider provider)
        {
            throw new NotSupportedException();
        }

        public override string ToString(IFormatProvider provider)
             => ToString();

        public override object ToType(Type conversionType, IFormatProvider provider)
        {
            throw new NotSupportedException();
        }

        public override ushort ToUInt16(IFormatProvider provider)
        {
            throw new NotSupportedException();
        }

        public override uint ToUInt32(IFormatProvider provider)
        {
            throw new NotSupportedException();
        }

        public override ulong ToUInt64(IFormatProvider provider)
        {
            throw new NotSupportedException();
        }

        #endregion

        public override string ToString()
        {
            var bytes = Bytes.Span;
            var builder = new StringBuilder(bytes.Length * 3);

            for (var i = 0; i < Bytes.Length; i++)
            {
                var b = bytes[i];
                builder.Append(HexStringHelper.HexToCharUpper(b >> 4));
                builder.Append(HexStringHelper.HexToCharUpper(b));
                builder.Append(' ');
            }

            // Remove final space
            if (builder.Length > 1)
            {
                builder.Length -= 1;
            }

            return builder.ToString();
        }
    }
}
