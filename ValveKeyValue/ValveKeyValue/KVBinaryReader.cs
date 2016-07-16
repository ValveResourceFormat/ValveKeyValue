using System;
using System.IO;
using System.Text;

namespace ValveKeyValue
{
    class KVBinaryReader : IDisposable
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

        public KVBinaryReader(Stream stream)
        {
            Require.NotNull(stream, nameof(stream));
            if (!stream.CanSeek)
            {
                throw new ArgumentException("Stream must be seekable", nameof(stream));
            }

            this.stream = stream;
            reader = new BinaryReader(stream);
        }

        readonly Stream stream;
        readonly BinaryReader reader;
        bool disposed;

        public KVObject ReadObject()
        {
            Require.NotDisposed(nameof(KVTextReader), disposed);

            try
            {
                return ReadObjectCore();
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

        KVObject ReadObjectCore()
        {
            var type = ReadNextNodeType();
            var name = ReadNullTerminatedString();
            var value = ReadValue(type);
            return new KVObject(name, value);
        }

        KVValue ReadValue(BinaryNodeType type)
        {
            switch (type)
            {
                case BinaryNodeType.ChildObject:
                    {
                        var children = new KVCollectionValue();
                        do
                        {
                            var child = ReadObjectCore();
                            children.Add(child);
                        }
                        while (PeekNextNodeType() != BinaryNodeType.End);

                        return children;
                    }

                case BinaryNodeType.String:
                    return ReadNullTerminatedString();

                case BinaryNodeType.WideString:
                    throw new NotSupportedException("Wide String is not supported.");

                case BinaryNodeType.Int32:
                case BinaryNodeType.Color:
                case BinaryNodeType.Pointer:
                    return new KVObjectValue<int>(reader.ReadInt32(), KVValueType.Int32);

                case BinaryNodeType.UInt64:
                    return new KVObjectValue<ulong>(reader.ReadUInt64(), KVValueType.UInt64);

                case BinaryNodeType.Float32:
                    var floatValue = BitConverter.ToSingle(reader.ReadBytes(4), 0);
                    return new KVObjectValue<float>(floatValue, KVValueType.FloatingPoint);

                default:
                    throw new ArgumentOutOfRangeException(nameof(type));
            }
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
