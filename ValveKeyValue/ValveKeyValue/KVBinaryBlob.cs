using System.Text;

namespace ValveKeyValue
{
    /// <summary>
    /// Represents a binary blob value.
    /// </summary>
    public class KVBinaryBlob : KVValue
    {
        /// <summary>
        /// Gets the binary data.
        /// </summary>
        public Memory<byte> Bytes { get; }

        /// <inheritdoc/>
        public override KVValueType ValueType => KVValueType.BinaryBlob;

        /// <summary>
        /// Initializes a new instance of the <see cref="KVBinaryBlob"/> class.
        /// </summary>
        /// <param name="value">The binary data.</param>
        public KVBinaryBlob(byte[] value)
        {
            Bytes = value;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="KVBinaryBlob"/> class.
        /// </summary>
        /// <param name="value">The binary data.</param>
        public KVBinaryBlob(Memory<byte> value)
        {
            Bytes = value;
        }

        #region IConvertible

        /// <inheritdoc/>
        public override TypeCode GetTypeCode()
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public override bool ToBoolean(IFormatProvider provider)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public override byte ToByte(IFormatProvider provider)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public override char ToChar(IFormatProvider provider)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public override DateTime ToDateTime(IFormatProvider provider)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public override decimal ToDecimal(IFormatProvider provider)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public override double ToDouble(IFormatProvider provider)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public override short ToInt16(IFormatProvider provider)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public override int ToInt32(IFormatProvider provider)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public override long ToInt64(IFormatProvider provider)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public override sbyte ToSByte(IFormatProvider provider)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public override float ToSingle(IFormatProvider provider)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public override string ToString(IFormatProvider provider)
             => ToString();

        /// <inheritdoc/>
        public override object ToType(Type conversionType, IFormatProvider provider)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public override ushort ToUInt16(IFormatProvider provider)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public override uint ToUInt32(IFormatProvider provider)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public override ulong ToUInt64(IFormatProvider provider)
        {
            throw new NotSupportedException();
        }

        #endregion

        /// <inheritdoc/>
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
