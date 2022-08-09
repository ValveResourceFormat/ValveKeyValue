using System;
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

        public void Dispose()
        {
            writer.Dispose();
        }

        public void OnObjectStart(string name)
            => WriteStartObject(name);

        public void OnObjectEnd()
            => WriteEndObject();

        public void OnKeyValuePair(string name, KVValue value)
            => WriteKeyValuePair(name, value);

        public void OnArrayValue(KVValue value) => throw new NotImplementedException();

        public void DiscardCurrentObject()
        {
            throw new NotSupportedException("Discard not supported when writing.");
        }

        void WriteStartObject(string name)
        {
            WriteIndentation();

            // TODO: Dumb hack, we should not have a root name
            if (indentation != 0 && name != "root")
            {
                WriteText(name);
                WriteLine();
            }

            WriteIndentation();
            writer.Write('{');
            indentation++;
            WriteLine();
        }

        void WriteEndObject()
        {
            indentation--;
            WriteIndentation();
            writer.Write('}');
            writer.WriteLine();
        }

        void WriteKeyValuePair(string name, KVValue value)
        {
            WriteIndentation();

            WriteKey(name);
            writer.Write(" = ");

            switch (value.ValueType)
            {
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

            WriteLine();
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
            }
            else
            {
                writer.Write('"');
            }

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

            if (isMultiline)
            {
                writer.Write("\n\"\"\"");
            }
            else
            {
                writer.Write('"');
            }
        }

        void WriteKey(string key)
        {
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
        }

        void WriteLine()
        {
            writer.WriteLine();
        }
    }
}
