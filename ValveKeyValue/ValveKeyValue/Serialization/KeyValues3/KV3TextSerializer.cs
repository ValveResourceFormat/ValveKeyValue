using System.Globalization;
using System.Text;
using ValveKeyValue.Abstraction;

namespace ValveKeyValue.Serialization.KeyValues3
{
    sealed class KV3TextSerializer : IVisitationListener, IDisposable
    {
        public KV3TextSerializer(Stream stream, KVHeader header = null)
        {
            ArgumentNullException.ThrowIfNull(stream);

            writer = new StreamWriter(stream, new UTF8Encoding(), bufferSize: 1024, leaveOpen: true)
            {
                NewLine = "\n"
            };

            var defaultEncoding = new ValveKeyValue.KeyValues3.KV3ID("text", ValveKeyValue.KeyValues3.Encoding.Text);
            var defaultFormat = new ValveKeyValue.KeyValues3.KV3ID("generic", ValveKeyValue.KeyValues3.Format.Generic);

            var encoding = header?.Encoding.Name != null ? header.Encoding : defaultEncoding;
            var format = header?.Format.Name != null ? header.Format : defaultFormat;

            writer.WriteLine($"<!-- kv3 encoding:{encoding} format:{format} -->");
        }

        readonly TextWriter writer;
        int indentation = 0;
        readonly Stack<bool> inArray = new();

        bool IsInArray => inArray.Count > 0 && inArray.Peek();

        public void Dispose()
        {
            writer.Dispose();
        }

        public void OnObjectStart(string name, KVFlag flag)
        {
            inArray.Push(false);

            WriteStartObject(name, flag);
        }

        public void OnObjectEnd()
        {
            inArray.Pop();

            WriteEndObject();
        }

        public void OnKeyValuePair(string name, KVValue value)
            => WriteKeyValuePair(name, value);

        public void OnArrayStart(string name, KVFlag flag)
        {
            inArray.Push(true);

            WriteIndentation();

            WriteKey(name);
            WriteFlag(flag);

            // After "key = " or "key = flag:", put bracket on next line.
            // TODO: Valve also puts bracket on next line for flagged array elements (name == null, flag != None).
            if (name != null)
            {
                writer.WriteLine();
                WriteIndentation();
            }

            // TODO: Valve writes short arrays (<=4 simple elements) inline as "[ 1, 2, 3 ]",
            // and empty arrays as "[  ]". This requires knowing the element count upfront.
            writer.Write('[');
            indentation++;
            WriteLine();
        }

        public void OnArrayValue(KVValue value)
        {
            WriteIndentation();

            WriteValue(value);

            // TODO: Valve does not write trailing comma on the last element of short inline arrays.
            writer.Write(',');
            // TODO: Valve groups simple array values 4 per line with spaces instead of newlines.
            writer.WriteLine();
        }

        public void OnArrayEnd()
        {
            inArray.Pop();

            indentation--;
            WriteIndentation();
            writer.Write(']');

            if (IsInArray)
            {
                writer.Write(',');
            }

            writer.WriteLine();
        }

        public void DiscardCurrentObject()
        {
            throw new NotSupportedException("Discard not supported when writing.");
        }

        void WriteStartObject(string name, KVFlag flag)
        {
            WriteIndentation();

            if (indentation > 0)
            {
                WriteKey(name);
            }

            WriteFlag(flag);

            // After "key = " or "key = flag:", put bracket on next line.
            // TODO: Valve also puts bracket on next line for flagged object elements (name == null, flag != None).
            if (name != null && indentation > 0)
            {
                writer.WriteLine();
                WriteIndentation();
            }

            writer.Write('{');
            indentation++;
            WriteLine();
        }

        void WriteEndObject()
        {
            indentation--;
            WriteIndentation();
            writer.Write('}');

            if (IsInArray)
            {
                writer.Write(',');
            }

            writer.WriteLine();
        }

        void WriteKeyValuePair(string name, KVValue value)
        {
            WriteIndentation();

            WriteKey(name);

            WriteValue(value);

            WriteLine();
        }

        void WriteValue(KVValue value)
        {
            WriteFlag(value.Flag);

            switch (value.ValueType)
            {
                case KVValueType.BinaryBlob:
                    WriteBinaryBlob((KVBinaryBlob)value);
                    break;
                case KVValueType.Boolean:
                    if ((bool)value)
                    {
                        writer.Write("true");
                    }
                    else
                    {
                        writer.Write("false");
                    }
                    break;
                case KVValueType.Null:
                    writer.Write("null");
                    break;
                // TODO: Valve uses %f with trailing zero stripping, and handles inf/-inf/nan.
                case KVValueType.FloatingPoint:
                    writer.Write(Convert.ToSingle(value, CultureInfo.InvariantCulture).ToString("#0.000000", CultureInfo.InvariantCulture));
                    break;
                case KVValueType.FloatingPoint64:
                    writer.Write(Convert.ToDouble(value, CultureInfo.InvariantCulture).ToString("#0.000000", CultureInfo.InvariantCulture));
                    break;
                case KVValueType.Int16:
                case KVValueType.Int32:
                case KVValueType.Int64:
                case KVValueType.UInt16:
                case KVValueType.UInt32:
                case KVValueType.UInt64:
                    writer.Write(value.ToString(null));
                    break;
                default:
                    WriteText(value.ToString(null));
                    break;
            }
        }

        void WriteBinaryBlob(KVBinaryBlob value)
        {
            var bytes = value.Bytes.Span;

            // TODO: Valve writes small blobs (<=32 bytes) inline as "#[ XX XX ]" with no newlines.
            if (bytes.Length > 32)
            {
                writer.WriteLine();
                WriteIndentation();
            }

            writer.Write('#');
            writer.Write('[');

            if (bytes.Length == 0)
            {
                writer.WriteLine();
                WriteIndentation();
                writer.Write(']');
                return;
            }

            writer.WriteLine();
            indentation++;
            WriteIndentation();

            for (var i = 0; i < bytes.Length - 1; i++)
            {
                var b = bytes[i];
                writer.Write(HexStringHelper.HexToCharUpper(b >> 4));
                writer.Write(HexStringHelper.HexToCharUpper(b));

                if ((i + 1) % 32 == 0)
                {
                    writer.WriteLine();
                    WriteIndentation();
                }
                else
                {
                    writer.Write(' ');
                }
            }

            var last = bytes[bytes.Length - 1];
            writer.Write(HexStringHelper.HexToCharUpper(last >> 4));
            writer.Write(HexStringHelper.HexToCharUpper(last));

            indentation--;

            writer.WriteLine();
            WriteIndentation();
            writer.Write(']');
        }

        void WriteIndentation()
        {
            for (var i = 0; i < indentation; i++)
            {
                writer.Write('\t');
            }
        }

        void WriteText(string text)
        {
            var isMultiline = text.Contains("\n", StringComparison.Ordinal);

            if (isMultiline)
            {
                text = text.Replace("\r\n", "\n");
                text = text.Replace("\"\"\"", "\\\"\"\"");

                writer.Write("\"\"\"\n");
                writer.Write(text);
                writer.Write("\n\"\"\"");
            }
            else
            {
                writer.Write('"');

                foreach (var @char in text)
                {
                    switch (@char)
                    {
                        case '\n':
                            writer.Write("\\n");
                            break;

                        case '\t':
                            writer.Write("\\t");
                            break;

                        case '\\':
                            writer.Write("\\\\");
                            break;

                        case '"':
                            writer.Write("\\\"");
                            break;

                        default:
                            writer.Write(@char);
                            break;
                    }
                }

                writer.Write('"');
            }
        }

        void WriteKey(string key)
        {
            if (key == null)
            {
                return;
            }

            var escaped = key.Length == 0; // Quote empty strings
            var sb = new StringBuilder(key.Length + 2);
            sb.Append('"');

            if (key.Length > 0 && key[0] >= '0' && key[0] <= '9')
            {
                // Quote when first character is a digit
                escaped = true;
            }

            foreach (var @char in key)
            {
                switch (@char)
                {
                    case '\t':
                        escaped = true;
                        sb.Append('\\');
                        sb.Append('t');
                        break;

                    case '\n':
                        escaped = true;
                        sb.Append('\\');
                        sb.Append('n');
                        break;

                    case '"':
                        escaped = true;
                        sb.Append('\\');
                        sb.Append('"');
                        break;

                    case '\\':
                        escaped = true;
                        sb.Append('\\');
                        sb.Append('\\');
                        break;

                    default:
                        // TODO: Use char.IsAscii* functions from newer .NET
                        if (@char != '.' && @char != '_' && !((@char >= 'A' && @char <= 'Z') || (@char >= 'a' && @char <= 'z') || (@char >= '0' && @char <= '9')))
                        {
                            escaped = true;
                        }

                        sb.Append(@char);
                        break;
                }
            }

            if (escaped)
            {
                sb.Append('"');
                writer.Write(sb.ToString());
            }
            else
            {
                writer.Write(key);
            }

            writer.Write(" = ");
        }

        void WriteFlag(KVFlag kvFlag)
        {
            if (kvFlag == KVFlag.None)
            {
                return;
            }

            var name = SerializeFlagName(kvFlag);

            if (name != null)
            {
                writer.Write(name);
                writer.Write(':');
            }
        }

        void WriteLine()
        {
            writer.WriteLine();
        }

        static string SerializeFlagName(KVFlag flag)
        {
            return flag switch
            {
                KVFlag.Resource => "resource",
                KVFlag.ResourceName => "resource_name",
                KVFlag.Panorama => "panorama",
                KVFlag.SoundEvent => "soundevent",
                KVFlag.SubClass => "subclass",
                KVFlag.EntityName => "entity_name",
                _ => null,
            };
        }
    }
}
