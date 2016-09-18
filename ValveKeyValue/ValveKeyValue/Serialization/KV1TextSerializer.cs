using System;
using System.IO;
using System.Text;
using ValveKeyValue.Abstraction;

namespace ValveKeyValue.Serialization
{
    sealed class KV1TextSerializer : IVisitationListener, IDisposable
    {
        public KV1TextSerializer(Stream stream, KVSerializerOptions options)
        {
            Require.NotNull(stream, nameof(stream));
            Require.NotNull(options, nameof(options));

            this.options = options;
            writer = new StreamWriter(stream, Encoding.UTF8, bufferSize: 1024, leaveOpen: true);
            writer.NewLine = "\n";
        }

        readonly KVSerializerOptions options;
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

        public void DiscardCurrentObject()
        {
            throw new NotSupportedException("Discard not supported when writing.");
        }

        void WriteStartObject(string name)
        {
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
            WriteIndentation();
            WriteText(name);
            writer.Write('\t');
            WriteText(value.ToString(null));
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
