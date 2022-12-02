using System.Globalization;
using System.Text;
using ValveKeyValue.Abstraction;

namespace ValveKeyValue.Serialization.KeyValues1
{
    sealed class KV1TextSerializer : IVisitationListener, IDisposable
    {
        public KV1TextSerializer(Stream stream, KVSerializerOptions options)
        {
            Require.NotNull(stream, nameof(stream));
            Require.NotNull(options, nameof(options));

            this.options = options;
            writer = new StreamWriter(stream, new UTF8Encoding(), bufferSize: 1024, leaveOpen: true)
            {
                NewLine = "\n"
            };
        }

        readonly KVSerializerOptions options;
        readonly TextWriter writer;
        int indentation = 0;
        Stack<int> arrayCount = new();

        public void Dispose()
        {
            writer.Dispose();
        }

        public void OnObjectStart(string name, KVFlag flag)
            => WriteStartObject(name);

        public void OnObjectEnd()
            => WriteEndObject();

        public void OnKeyValuePair(string name, KVValue value)
            => WriteKeyValuePair(name, value);

        public void OnArrayStart(string name, KVFlag flag)
        {
            WriteStartObject(name);
            arrayCount.Push(0);
        }

        public void OnArrayValue(KVValue value)
        {
            var count = arrayCount.Pop();

            WriteKeyValuePair(count.ToString(), value);

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

        void WriteStartObject(string name)
        {
            if (name == null)
            {
                var count = arrayCount.Pop();

                name = count.ToString();

                arrayCount.Push(count + 1);
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

        void WriteKeyValuePair(string name, IConvertible value)
        {
            // TODO: Handle true, false, null value types

            WriteIndentation();
            WriteText(name);
            writer.Write('\t');
            WriteText(value.ToString(CultureInfo.InvariantCulture));
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
            writer.Write('"');

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

            writer.Write('"');
        }

        void WriteLine()
        {
            writer.WriteLine();
        }
    }
}
