using System.Buffers;
using System.Globalization;
using System.Text;
using ValveKeyValue.Abstraction;

namespace ValveKeyValue.Serialization.KeyValues1
{
    sealed class KV1TextSerializer : IVisitationListener, IDisposable
    {
        static readonly SearchValues<char> CharsToEscape = SearchValues.Create("\"\\");

        public KV1TextSerializer(Stream stream, KVSerializerOptions options)
        {
            ArgumentNullException.ThrowIfNull(stream);
            ArgumentNullException.ThrowIfNull(options);

            this.options = options;
            writer = new StreamWriter(stream, new UTF8Encoding(), bufferSize: 1024, leaveOpen: true)
            {
                NewLine = "\n"
            };
        }

        readonly KVSerializerOptions options;
        readonly StreamWriter writer;
        int indentation;
        readonly Stack<int> arrayCount = new();

        public void Dispose()
        {
            writer.Dispose();
        }

        public void OnObjectStart(string? name, KVFlag flag)
            => WriteStartObject(name);

        public void OnObjectEnd()
            => WriteEndObject();

        public void OnKeyValuePair(string name, KVObject value)
            => WriteKeyValuePair(name, value);

        public void OnArrayStart(string? name, KVFlag flag, int elementCount, bool allSimpleElements)
        {
            WriteStartObject(name);
            arrayCount.Push(0);
        }

        public void OnArrayValue(KVObject value)
        {
            var count = arrayCount.Pop();

            WriteKeyValuePair(count.ToString(CultureInfo.InvariantCulture), value);

            arrayCount.Push(count + 1);
        }

        public void OnArrayEnd()
        {
            WriteEndObject();
            arrayCount.Pop();
        }

        public void DiscardCurrentObject()
        {
            throw new NotSupportedException("Discard not supported when writing.");
        }

        void WriteStartObject(string? name)
        {
            if (name == null)
            {
                if (arrayCount.Count > 0)
                {
                    var count = arrayCount.Pop();

                    name = count.ToString(CultureInfo.InvariantCulture);

                    arrayCount.Push(count + 1);
                }
                else
                {
                    name = string.Empty;
                }
            }

            WriteIndentation();
            WriteText(name);
            WriteLine();
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

        void WriteKeyValuePair(string name, KVObject value)
        {
            WriteIndentation();
            WriteText(name);
            writer.Write('\t');

            if (value.IsNull)
            {
                WriteText(string.Empty);
            }
            else if (value.ValueType == KVValueType.Boolean)
            {
                WriteText(value.ToBoolean(null) ? "1" : "0");
            }
            else
            {
                WriteText(value.ToString(CultureInfo.InvariantCulture));
            }

            WriteLine();
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
            writer.Write('"');

            if (!text.AsSpan().ContainsAny(CharsToEscape))
            {
                writer.Write(text);
            }
            else
            {
                foreach (var @char in text)
                {
                    switch (@char)
                    {
                        case '"':
                            writer.Write("\\\"");
                            break;

                        case '\\':
                            writer.Write(options.HasEscapeSequences ? "\\\\" : "\\");
                            break;

                        default:
                            writer.Write(@char);
                            break;
                    }
                }
            }

            writer.Write('"');
        }

        void WriteLine()
        {
            writer.WriteLine();
        }
    }
}
