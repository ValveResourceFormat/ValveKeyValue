using System.Text;
using ValveKeyValue.Abstraction;
using ValveKeyValue.KeyValues1;

namespace ValveKeyValue.Deserialization.KeyValues1
{
    class KV1BinaryReader : IVisitingReader
    {
        public const int BinaryMagicHeader = 0x564B4256; // VBKV

        public KV1BinaryReader(Stream stream, IParsingVisitationListener listener)
        {
            Require.NotNull(stream, nameof(stream));
            Require.NotNull(listener, nameof(listener));

            if (!stream.CanSeek)
            {
                throw new ArgumentException("Stream must be seekable", nameof(stream));
            }

            this.stream = stream;
            this.listener = listener;
            reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
        }

        readonly Stream stream;
        readonly BinaryReader reader;
        readonly IParsingVisitationListener listener;
        bool disposed;
        KV1BinaryNodeType endMarker = KV1BinaryNodeType.End;

        public void ReadObject()
        {
            Require.NotDisposed(nameof(KV1TextReader), disposed);

            DetectMagicHeader();

            try
            {
                ReadObjectCore();
            }
            catch (IOException ex)
            {
                throw new KeyValueException("Error while reading binary KeyValues.", ex);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                throw new KeyValueException("Error while parsing binary KeyValues.", ex);
            }
            catch (NotSupportedException ex)
            {
                throw new KeyValueException("Error while parsing binary KeyValues.", ex);
            }
        }

        public void Dispose()
        {
            if (!disposed)
            {
                reader.Dispose();
                disposed = true;
            }
        }

        void ReadObjectCore()
        {
            KV1BinaryNodeType type = ReadNextNodeType();

            // Keep reading values, until we reach the terminator
            while (type != endMarker)
            {
                ReadValue(type);
                type = ReadNextNodeType();
            }
        }

        void ReadValue(KV1BinaryNodeType type)
        {
            string name;
            KVValue value;

            if (listener.StringPool != null)
            {
                name = listener.StringPool[reader.ReadInt32()];
            }
            else
            {
                name = Encoding.UTF8.GetString(ReadNullTerminatedBytes());
            }

            switch (type)
            {
                case KV1BinaryNodeType.ChildObject:
                    listener.OnObjectStart(name);
                    ReadObjectCore();
                    listener.OnObjectEnd();
                    return;

                case KV1BinaryNodeType.String:
                    // UTF8 encoding is used for string values
                    value = new KVObjectValue<string>(Encoding.UTF8.GetString(ReadNullTerminatedBytes()), KVValueType.String);
                    break;

                case KV1BinaryNodeType.WideString:
                    throw new NotSupportedException("Wide String is not supported, please create an issue saying where you found it: https://github.com/ValveResourceFormat/ValveKeyValue/issues");

                case KV1BinaryNodeType.Int32:
                case KV1BinaryNodeType.Color:
                case KV1BinaryNodeType.Pointer:
                    value = new KVObjectValue<int>(reader.ReadInt32(), KVValueType.Int32);
                    break;

                case KV1BinaryNodeType.UInt64:
                    value = new KVObjectValue<ulong>(reader.ReadUInt64(), KVValueType.UInt64);
                    break;

                case KV1BinaryNodeType.Float32:
                    var floatValue = BitConverter.ToSingle(reader.ReadBytes(4), 0);
                    value = new KVObjectValue<float>(floatValue, KVValueType.FloatingPoint);
                    break;

                case KV1BinaryNodeType.ProbablyBinary:
                    throw new NotSupportedException("Hit kv type 9, please create an issue saying where you found it: https://github.com/ValveResourceFormat/ValveKeyValue/issues");

                case KV1BinaryNodeType.Int64:
                    value = new KVObjectValue<long>(reader.ReadInt64(), KVValueType.Int64);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, "Unhandled binary node type.");
            }

            listener.OnKeyValuePair(name, value);
        }

        byte[] ReadNullTerminatedBytes()
        {
            using var mem = new MemoryStream();
            byte nextByte;
            while ((nextByte = reader.ReadByte()) != 0)
            {
                mem.WriteByte(nextByte);
            }

            return mem.ToArray();
        }

        void DetectMagicHeader()
        {
            if (stream.Length - stream.Position < 8)
            {
                return;
            }

            if (reader.ReadUInt32() == BinaryMagicHeader)
            {
                stream.Position += 4; // Skip crc32

                // There is likely to reason to handle this separately
                // as the types do not conflict between Steam or Dota 2
                endMarker = KV1BinaryNodeType.AlternateEnd;
            }
            else
            {
                stream.Position -= 4; // Go back as we did not detect the header
            }
        }

        KV1BinaryNodeType ReadNextNodeType()
            => (KV1BinaryNodeType)reader.ReadByte();
    }
}
