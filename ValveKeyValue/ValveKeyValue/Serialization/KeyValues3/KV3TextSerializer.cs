using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using ValveKeyValue.Abstraction;

namespace ValveKeyValue.Serialization.KeyValues3
{
    sealed class KV3TextSerializer : IVisitationListener, IDisposable
    {
        public KV3TextSerializer(Stream stream)
        {
            Require.NotNull(stream, nameof(stream));

            writer = new StreamWriter(stream, new UTF8Encoding(), bufferSize: 1024, leaveOpen: true)
            {
                NewLine = "\n"
            };

            // TODO: Write correct encoding and format
            writer.WriteLine("<!-- kv3 encoding:text:version{e21c7f3c-8a33-41c5-9977-a76d3a32aa0d} format:generic:version{7412167c-06e9-4698-aff2-e63eb59037e7} -->");
        }

        readonly TextWriter writer;
        int indentation = 0;
        Stack<bool> inArray = new();

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

            writer.Write('[');
            indentation++;
            WriteLine();
        }

        public void OnArrayValue(KVValue value)
        {
            WriteIndentation();

            WriteValue(value);

            writer.Write(',');
            writer.WriteLine(); // TODO: If short, no line?
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

            // TODO: Dumb hack, we should not have a root name
            if (indentation != 0 && name != "root")
            {
                WriteKey(name);
            }

            WriteFlag(flag);

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
                case KVValueType.FloatingPoint:
                case KVValueType.Int64:
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
            // TODO: Verify this against Valve
            if (value.Bytes.Length > 32)
            {
                writer.WriteLine();
                WriteIndentation();
            }

            writer.Write('#');
            writer.Write('[');
            writer.WriteLine();
            indentation++;
            WriteIndentation();

            var count = 0;

            foreach (var oneByte in value.Bytes)
            {
                writer.Write(oneByte.ToString("X2"));

                if (++count % 32 == 0)
                {
                    writer.WriteLine();
                    WriteIndentation();
                }
                else if (count != value.Bytes.Length)
                {
                    writer.Write(' ');
                }
            }

            indentation--;

            if (count % 32 != 0)
            {
                writer.WriteLine();
                WriteIndentation();
            }

            writer.Write(']');
        }

        void WriteIndentation()
        {
            if (indentation == 0)
            {
                return;
            }

            var text = new string('\t', indentation);
            writer.Write(text);
        }

        void WriteText(string text)
        {
            var isMultiline = text.Contains("\n", StringComparison.Ordinal);

            if (isMultiline)
            {
                writer.Write("\"\"\"\n");

                text = text.Replace("\"\"\"", "\\\"\"\"");

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
                        case '"':
                            writer.Write("\\\"");
                            break;

                        case '\\':
                            writer.Write("\\");
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

            var escaped = false;
            var sb = new StringBuilder(key.Length + 2);
            sb.Append('"');

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

                    case ' ':
                        escaped = true;
                        sb.Append(' ');
                        break;

                    case '"':
                        escaped = true;
                        sb.Append('\\');
                        sb.Append('"');
                        break;

                    case '\'':
                        escaped = true;
                        sb.Append('\\');
                        sb.Append('\'');
                        break;

                    default:
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

            var flags = (int)kvFlag;
            var i = 0;
            var currentFlag = -1;
            var more = false;

            while (i < flags)
            {
                var flag = (1 << ++currentFlag);

                i += flag;

                if ((flag & flags) == 0)
                {
                    continue;
                }

                var serialized = SerializeFlagName((KVFlag)flag);

                if (serialized == null)
                {
                    continue;
                }

                if (more)
                {
                    writer.Write('|');
                }

                writer.Write(serialized);

                more = true;
            }

            writer.Write(':');
        }

        void WriteLine()
        {
            writer.WriteLine();
        }

        string SerializeFlagName(KVFlag flag)
        {
            return flag switch
            {
                KVFlag.Resource => "resource",
                KVFlag.ResourceName => "resource_name",
                KVFlag.Panorama => "panorama",
                KVFlag.SoundEvent => "soundevent",
                KVFlag.SubClass => "subclass",
                _ => null,
            };
        }
    }
}
