using System;
using System.IO;
using System.Text;
using ValveKeyValue.Abstraction;

namespace ValveKeyValue.Deserialization
{
    class KV1BinaryReader : IVisitingReader
    {
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
            while (true)
            {
                var type = ReadNextNodeType();

                // Break if we hit the end
                if (type == KV1BinaryNodeType.End)
                    break;

                var name = ReadNullTerminatedString();
                if (type == KV1BinaryNodeType.ChildObject)
                {
                    listener.OnObjectStart(name);
                    ReadObjectCore();
                    listener.OnObjectEnd();
                }
                else
                {
                    KVValue value;

                    switch (type)
                    {
                        case KV1BinaryNodeType.String:
                            value = new KVObjectValue<string>(ReadNullTerminatedString(Encoding.UTF8), KVValueType.String);
                            break;

                        case KV1BinaryNodeType.WideString:
                            throw new NotSupportedException("Wide String is not supported.");

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

                        case KV1BinaryNodeType.Int64:
                            value = new KVObjectValue<long>(reader.ReadInt64(), KVValueType.Int64);
                            break;

                        default:
                            throw new ArgumentOutOfRangeException(nameof(type));
                    }

                    listener.OnKeyValuePair(name, value);
                }
            }
        }

        string ReadNullTerminatedString(Encoding encoding = null)
        {
            if (encoding == null) encoding = Encoding.ASCII;
            var b = reader.ReadByte();

            int len;
            for (len = 0; b != 0; len++)
                b = reader.ReadByte();

            reader.BaseStream.Position -= len + 1;
            var str = reader.ReadBytes(len);

            // read the null terminator
            reader.ReadByte();

            return encoding.GetString(str);
        }

        KV1BinaryNodeType ReadNextNodeType()
            => (KV1BinaryNodeType)reader.ReadByte();
    }
}
