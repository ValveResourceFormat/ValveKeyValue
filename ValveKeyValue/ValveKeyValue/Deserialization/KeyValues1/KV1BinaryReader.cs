using System.Buffers;
using System.Text;
using ValveKeyValue.Abstraction;
using ValveKeyValue.KeyValues1;

namespace ValveKeyValue.Deserialization.KeyValues1
{
    class KV1BinaryReader : IVisitingReader
    {
        public const int BinaryMagicHeader = 0x564B4256; // VBKV

        public KV1BinaryReader(Stream stream, IVisitationListener listener, StringTable stringTable)
        {
            ArgumentNullException.ThrowIfNull(stream);
            ArgumentNullException.ThrowIfNull(listener);

            if (!stream.CanSeek)
            {
                throw new ArgumentException("Stream must be seekable.", nameof(stream));
            }

            this.stream = stream;
            this.listener = listener;
            this.stringTable = stringTable;
            reader = new BinaryReader(stream, Encoding.UTF8, leaveOpen: true);
        }

#pragma warning disable CA2213 // Not owned by this class
        readonly Stream stream;
        readonly IVisitationListener listener;
#pragma warning restore CA2213
        readonly BinaryReader reader;
        readonly StringTable stringTable;
        bool disposed;
        bool hasMagicHeader;

        public KVHeader ReadHeader()
        {
            ObjectDisposedException.ThrowIf(disposed, this);

            DetectMagicHeader();

            try
            {
                ReadRootObject();
            }
            catch (IOException ex)
            {
                throw new KeyValueException($"Error while reading binary KeyValues at offset {stream.Position}.", ex);
            }
            catch (ArgumentOutOfRangeException ex)
            {
                throw new KeyValueException($"Error while parsing binary KeyValues at offset {stream.Position}.", ex);
            }
            catch (NotSupportedException ex)
            {
                throw new KeyValueException($"Error while parsing binary KeyValues at offset {stream.Position}.", ex);
            }

            return new KVHeader();
        }

        public void Dispose()
        {
            if (!disposed)
            {
                reader.Dispose();
                disposed = true;
            }
        }

        void ReadRootObject()
        {
            var type = ReadNextNodeType();

            if (IsEndMarker(type))
            {
                return;
            }

            ReadValue(type);

            if (!IsEndMarker(ReadNextNodeType()))
            {
                throw new KeyValueException($"Found data after the root object at offset {stream.Position}, documents with multiple root objects are not supported.");
            }
        }

        void ReadObjectCore()
        {
            KV1BinaryNodeType type = ReadNextNodeType();

            // Keep reading values, until we reach the terminator
            while (!IsEndMarker(type))
            {
                ReadValue(type);
                type = ReadNextNodeType();
            }
        }

        string ReadKeyForNextValue()
        {
            if (stringTable is not null)
            {
                var index = reader.ReadInt32();
                return stringTable[index];
            }

            return ReadNullTerminatedUtf8String();
        }

        void ReadValue(KV1BinaryNodeType type)
        {
            var name = ReadKeyForNextValue();
            KVObject value;

            switch (type)
            {
                case KV1BinaryNodeType.ChildObject:
                    listener.OnObjectStart(name, KVFlag.None);
                    ReadObjectCore();
                    listener.OnObjectEnd();
                    return;

                case KV1BinaryNodeType.String:
                    // UTF8 encoding is used for string values
                    value = new KVObject(ReadNullTerminatedUtf8String());
                    break;

                case KV1BinaryNodeType.WideString:
                    value = new KVObject(ReadWideString());
                    break;

                case KV1BinaryNodeType.Int32:
                case KV1BinaryNodeType.Color:
                    value = new KVObject(reader.ReadInt32());
                    break;

                case KV1BinaryNodeType.Pointer:
                    // Source 1 writes pointers as 4 bytes, Steam and Source 2 write them as 8 bytes.
                    // No known files contain pointers, so only the Source 1 size is supported.
                    value = new KVObject(KVValueType.Pointer, (long)reader.ReadInt32());
                    break;

                case KV1BinaryNodeType.UInt64:
                    value = new KVObject(reader.ReadUInt64());
                    break;

                case KV1BinaryNodeType.Float32:
                    value = new KVObject(reader.ReadSingle());
                    break;

                case KV1BinaryNodeType.BinaryBlob:
                    var blob = new byte[reader.ReadUInt32()];
                    stream.ReadExactly(blob);
                    value = KVObject.Blob(blob);
                    break;

                case KV1BinaryNodeType.Int64:
                    value = new KVObject(reader.ReadInt64());
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(type), type, "Unhandled binary node type.");
            }

            listener.OnKeyValuePair(name, value);
        }

        string ReadNullTerminatedUtf8String()
        {
            var buffer = ArrayPool<byte>.Shared.Rent(32);

            try
            {
                var position = 0;

                do
                {
                    var b = reader.ReadByte();

                    if (b <= 0) // null byte or stream ended
                    {
                        break;
                    }

                    if (position >= buffer.Length)
                    {
                        var newBuffer = ArrayPool<byte>.Shared.Rent(buffer.Length * 2);
                        Buffer.BlockCopy(buffer, 0, newBuffer, 0, buffer.Length);
                        ArrayPool<byte>.Shared.Return(buffer);
                        buffer = newBuffer;
                    }

                    buffer[position++] = b;
                }
                while (true);

                return Encoding.UTF8.GetString(buffer, 0, position);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        string ReadWideString()
        {
            // Source 2 writes an int16 character count without a terminator,
            // Steam writes an int32 character count that includes the null terminator.
            var length = hasMagicHeader ? reader.ReadInt16() : reader.ReadInt32();

            if (length < 0)
            {
                throw new KeyValueException($"Invalid wide string length {length} at offset {stream.Position}.");
            }

            var data = new byte[length * 2];
            stream.ReadExactly(data);

            var text = Encoding.Unicode.GetString(data);
            var terminator = text.IndexOf('\0', StringComparison.Ordinal);

            return terminator >= 0 ? text[..terminator] : text;
        }

        void DetectMagicHeader()
        {
            if (stream.Length - stream.Position < 8)
            {
                return;
            }

            if (reader.ReadUInt32() == BinaryMagicHeader)
            {
                // Skip CRC32 of the remaining data
                stream.Position += 4;
                hasMagicHeader = true;
            }
            else
            {
                stream.Position -= 4; // Go back as we did not detect the header
            }
        }

        // Steam uses End, Source 2 uses AlternateEnd. Each format rejects the other's marker as a node type,
        // so both are accepted regardless of whether the magic header is present.
        static bool IsEndMarker(KV1BinaryNodeType type)
            => type is KV1BinaryNodeType.End or KV1BinaryNodeType.AlternateEnd;

        KV1BinaryNodeType ReadNextNodeType()
            => (KV1BinaryNodeType)reader.ReadByte();
    }
}
