using System.Text;

namespace ValveKeyValue.Test.BinaryKV2
{
    /// <summary>
    /// Builds binary DMX documents byte by byte, so that layouts without a real world fixture
    /// can still be exercised.
    /// </summary>
    sealed class DmxBinaryBuilder
    {
        readonly List<byte> bytes = [];

        public DmxBinaryBuilder Header(string encoding, int encodingVersion, string format = "dmx", int formatVersion = 1)
            => RawHeader($"<!-- dmx encoding {encoding} {encodingVersion} format {format} {formatVersion} -->");

        public DmxBinaryBuilder RawHeader(string header)
        {
            bytes.AddRange(Encoding.UTF8.GetBytes(header));
            bytes.Add((byte)'\n');
            bytes.Add(0);
            return this;
        }

        public DmxBinaryBuilder Int(int value)
        {
            bytes.AddRange(BitConverter.GetBytes(value));
            return this;
        }

        public DmxBinaryBuilder Short(short value)
        {
            bytes.AddRange(BitConverter.GetBytes(value));
            return this;
        }

        public DmxBinaryBuilder Float(float value)
        {
            bytes.AddRange(BitConverter.GetBytes(value));
            return this;
        }

        public DmxBinaryBuilder U8(byte value)
        {
            bytes.Add(value);
            return this;
        }

        /// <summary>Writes an inline, null terminated string.</summary>
        public DmxBinaryBuilder Str(string value)
        {
            bytes.AddRange(Encoding.UTF8.GetBytes(value));
            bytes.Add(0);
            return this;
        }

        public DmxBinaryBuilder Id(Guid value)
        {
            bytes.AddRange(value.ToByteArray());
            return this;
        }

        public DmxBinaryBuilder Raw(params byte[] value)
        {
            bytes.AddRange(value);
            return this;
        }

        public byte[] ToArray() => [.. bytes];

        public MemoryStream ToStream() => new(ToArray(), writable: false);
    }
}
