using System;
using System.IO;
using System.Text;
using ValveKeyValue.Abstraction;

namespace ValveKeyValue.Deserialization
{
    class KV1BinaryReader : IDisposable
    {
        enum BinaryNodeType : byte
        {
            ChildObject = 0,
            String = 1,
            Int32 = 2,
            Float32 = 3,
            Pointer = 4,
            WideString = 5,
            Color = 6,
            UInt64 = 7,
            End = 8,
        }

        public KV1BinaryReader(Stream stream, IVisitationListener listener)
        {
            Require.NotNull(stream, nameof(stream));
            Require.NotNull(listener, nameof(listener));

            if (!stream.CanSeek)
            {
                throw new ArgumentException("Stream must be seekable", nameof(stream));
            }

            this.stream = stream;
            this.listener = listener;
            reader = new BinaryReader(stream);
        }

        readonly Stream stream;
        readonly BinaryReader reader;
        readonly IVisitationListener listener;
        bool disposed;

        public void ReadObject()
        {
            Require.NotDisposed(nameof(KV1TextReader), disposed);

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
                stream.Dispose();
                disposed = true;
            }
        }

        void ReadObjectCore()
        {
            var type = ReadNextNodeType();
            var name = ReadNullTerminatedString();
            ReadValue(name, type);
        }

        void ReadValue(string name, BinaryNodeType type)
        {
            KVValue value;

            switch (type)
            {
                case BinaryNodeType.ChildObject:
                    {
                        listener.OnObjectStart(name);
                        do
                        {
                            ReadObjectCore();
                        }
                        while (PeekNextNodeType() != BinaryNodeType.End);
                        listener.OnObjectEnd();
                        return;
                    }

                case BinaryNodeType.String:
                    value = new KVObjectValue<string>(ReadNullTerminatedString(), KVValueType.String);
                    break;

                case BinaryNodeType.WideString:
                    throw new NotSupportedException("Wide String is not supported.");

                case BinaryNodeType.Int32:
                case BinaryNodeType.Color:
                case BinaryNodeType.Pointer:
                    value = new KVObjectValue<int>(reader.ReadInt32(), KVValueType.Int32);
                    break;

                case BinaryNodeType.UInt64:
                    value = new KVObjectValue<ulong>(reader.ReadUInt64(), KVValueType.UInt64);
                    break;

                case BinaryNodeType.Float32:
                    var floatValue = BitConverter.ToSingle(reader.ReadBytes(4), 0);
                    value = new KVObjectValue<float>(floatValue, KVValueType.FloatingPoint);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }

            listener.OnKeyValuePair(name, value);
        }

        string ReadNullTerminatedString()
        {
            var sb = new StringBuilder();
            byte nextByte;
            while ((nextByte = reader.ReadByte()) != 0)
            {
                sb.Append((char)nextByte);
            }

            return sb.ToString();
        }

        BinaryNodeType ReadNextNodeType()
            => (BinaryNodeType)reader.ReadByte();

        BinaryNodeType PeekNextNodeType()
        {
            var value = ReadNextNodeType();
            stream.Seek(-1, SeekOrigin.Current);
            return value;
        }
    }
}
