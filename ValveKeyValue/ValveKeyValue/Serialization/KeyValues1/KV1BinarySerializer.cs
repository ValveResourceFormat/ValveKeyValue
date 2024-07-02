using System.Linq;
using System.Text;
using ValveKeyValue.Abstraction;
using ValveKeyValue.KeyValues1;

namespace ValveKeyValue.Serialization.KeyValues1
{
    sealed class KV1BinarySerializer : IVisitationListener, IDisposable
    {
        public KV1BinarySerializer(Stream stream, StringTable stringTable)
        {
            Require.NotNull(stream, nameof(stream));

            writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);
            this.stringTable = stringTable;
        }

        readonly BinaryWriter writer;
        readonly StringTable stringTable;
        int objectDepth;

        public void Dispose()
        {
            writer.Dispose();
        }

        public void OnObjectStart(string name)
        {
            objectDepth++;
            Write(KV1BinaryNodeType.ChildObject);
            WriteKeyForNextValue(name);
        }

        public void OnObjectEnd()
        {
            Write(KV1BinaryNodeType.End);

            objectDepth--;
            if (objectDepth == 0)
            {
                Write(KV1BinaryNodeType.End);
            }
        }

        public void OnKeyValuePair(string name, KVValue value)
        {
            Write(GetNodeType(value.ValueType));
            WriteKeyForNextValue(name);

            switch (value.ValueType)
            {
                case KVValueType.FloatingPoint:
                    writer.Write((float)value);
                    break;

                case KVValueType.Int32:
                case KVValueType.Pointer:
                    writer.Write((int)value);
                    break;

                case KVValueType.String:
                    WriteNullTerminatedBytes(Encoding.UTF8.GetBytes((string)value));
                    break;

                case KVValueType.UInt64:
                    writer.Write((ulong)value);
                    break;

                case KVValueType.Int64:
                    writer.Write((long)value);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(value.ValueType), value.ValueType, "Unhandled value type.");
            }
        }

        void Write(KV1BinaryNodeType nodeType)
        {
            writer.Write((byte)nodeType);
        }

        void WriteNullTerminatedBytes(byte[] value)
        {
            writer.Write(value);
            writer.Write((byte)0);
        }

        void WriteKeyForNextValue(string name)
        {
            if (stringTable is not null)
            {
                writer.Write(stringTable.GetOrAdd(name));
            }
            else
            {
                WriteNullTerminatedBytes(Encoding.UTF8.GetBytes(name));
            }
        }

        static KV1BinaryNodeType GetNodeType(KVValueType type)
        {
            return type switch
            {
                KVValueType.FloatingPoint => KV1BinaryNodeType.Float32,
                KVValueType.Int32 => KV1BinaryNodeType.Int32,
                KVValueType.Pointer => KV1BinaryNodeType.Pointer,
                KVValueType.String => KV1BinaryNodeType.String,
                KVValueType.UInt64 => KV1BinaryNodeType.UInt64,
                KVValueType.Int64 => KV1BinaryNodeType.Int64,
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unhandled value type."),
            };
        }
    }
}
