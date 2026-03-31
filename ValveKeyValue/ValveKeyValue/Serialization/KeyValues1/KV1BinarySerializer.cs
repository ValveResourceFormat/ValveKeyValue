using System.Text;
using ValveKeyValue.Abstraction;
using ValveKeyValue.KeyValues1;

namespace ValveKeyValue.Serialization.KeyValues1
{
    sealed class KV1BinarySerializer : IVisitationListener, IDisposable
    {
        public KV1BinarySerializer(Stream stream, StringTable stringTable)
        {
            ArgumentNullException.ThrowIfNull(stream);

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

        public void OnObjectStart(string name, KVFlag flag)
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

        public void OnKeyValuePair(string name, KVObject value)
        {
            Write(GetNodeType(value.ValueType));
            WriteKeyForNextValue(name);

            switch (value.ValueType)
            {
                case KVValueType.FloatingPoint:
                case KVValueType.FloatingPoint64:
                    writer.Write((float)value);
                    break;

                case KVValueType.Boolean:
                case KVValueType.Int16:
                case KVValueType.UInt16:
                case KVValueType.Int32:
                case KVValueType.Pointer:
                    writer.Write((int)value);
                    break;

                case KVValueType.String:
                    WriteNullTerminatedString((string)value);
                    break;

                case KVValueType.UInt32:
                case KVValueType.UInt64:
                    writer.Write((ulong)value);
                    break;

                case KVValueType.Int64:
                    writer.Write((long)value);
                    break;

                case KVValueType.Null:
                    writer.Write((byte)0);
                    break;

                case KVValueType.BinaryBlob:
                    WriteNullTerminatedString(value.ToString(null));
                    break;

                default:
                    throw new InvalidOperationException($"Unhandled value type: {value.ValueType}");
            }
        }

        public void OnArrayStart(string name, KVFlag flag, int elementCount, bool allSimpleElements) => throw new NotImplementedException();
        public void OnArrayValue(KVObject value) => throw new NotImplementedException();
        public void OnArrayEnd() => throw new NotImplementedException();

        void Write(KV1BinaryNodeType nodeType)
        {
            writer.Write((byte)nodeType);
        }

        void WriteNullTerminatedString(string value)
        {
            writer.Write(value.AsSpan());
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
                WriteNullTerminatedString(name);
            }
        }

        static KV1BinaryNodeType GetNodeType(KVValueType type)
        {
            return type switch
            {
                KVValueType.FloatingPoint or KVValueType.FloatingPoint64 => KV1BinaryNodeType.Float32,
                KVValueType.Boolean or KVValueType.Int16 or KVValueType.UInt16
                    or KVValueType.Int32 => KV1BinaryNodeType.Int32,
                KVValueType.Pointer => KV1BinaryNodeType.Pointer,
                KVValueType.Null or KVValueType.BinaryBlob
                    or KVValueType.String => KV1BinaryNodeType.String,
                KVValueType.UInt32 or KVValueType.UInt64 => KV1BinaryNodeType.UInt64,
                KVValueType.Int64 => KV1BinaryNodeType.Int64,
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, "Unhandled value type."),
            };
        }
    }
}
