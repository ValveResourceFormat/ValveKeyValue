using System.Buffers;
using System.Globalization;
using System.Text;
using ValveKeyValue.Abstraction;

namespace ValveKeyValue.Serialization.KeyValues1
{
    sealed class KV1TextSerializer : KVTextSerializerBase, IVisitationListener
    {
        static readonly SearchValues<char> CharsToEscape = SearchValues.Create("\"\\");

        // Fits any integer and the shortest round-trip form of any float or double.
        const int NumberBufferLength = 32;

        public KV1TextSerializer(Stream stream, KVSerializerOptions options)
            : base(stream)
        {
            ArgumentNullException.ThrowIfNull(options);
            this.options = options;
        }

        public KV1TextSerializer(StringBuilder sb, List<KvSourceSpan> sourceMap, KVSerializerOptions options)
            : base(sb, sourceMap)
        {
            ArgumentNullException.ThrowIfNull(options);
            this.options = options;
        }

        readonly KVSerializerOptions options;
        readonly Stack<int> arrayCount = new();

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

            Span<char> buffer = stackalloc char[NumberBufferLength];
            WriteKeyValuePair(FormatIndex(count, buffer), value);

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

        static ReadOnlySpan<char> FormatIndex(int index, Span<char> buffer)
        {
            if (index.TryFormat(buffer, out var charsWritten, default, CultureInfo.InvariantCulture))
            {
                return buffer[..charsWritten];
            }

            return index.ToString(CultureInfo.InvariantCulture);
        }

        void WriteStartObject(string? name)
        {
            if (name != null)
            {
                WriteStartObjectCore(name);
            }
            else if (arrayCount.Count > 0)
            {
                var count = arrayCount.Pop();

                Span<char> buffer = stackalloc char[NumberBufferLength];
                WriteStartObjectCore(FormatIndex(count, buffer));

                arrayCount.Push(count + 1);
            }
            else
            {
                WriteStartObjectCore(string.Empty);
            }
        }

        void WriteStartObjectCore(ReadOnlySpan<char> name)
        {
            WriteIndentation();
            var keyStart = Position;
            WriteText(name);
            Record(keyStart, KVTokenType.Key);
            WriteLine();
            WriteIndentation();
            Record(KVTokenType.ObjectStart, '{');
            indentation++;
            WriteLine();
        }

        void WriteEndObject()
        {
            indentation--;
            WriteIndentation();
            Record(KVTokenType.ObjectEnd, '}');
            writer.WriteLine();
        }

        void WriteKeyValuePair(ReadOnlySpan<char> name, KVObject value)
        {
            WriteIndentation();
            var keyStart = Position;
            WriteText(name);
            Record(keyStart, KVTokenType.Key);
            writer.Write('\t');

            var valueStart = Position;
            if (value.IsNull)
            {
                WriteText(string.Empty);
            }
            else if (value.ValueType == KVValueType.Boolean)
            {
                WriteText(value.ToBoolean(null) ? "1" : "0");
            }
            else if (value.ValueType == KVValueType.String)
            {
                WriteText(value.ToString(CultureInfo.InvariantCulture));
            }
            else
            {
                WriteValue(value);
            }
            Record(valueStart, KVTokenType.String);

            WriteLine();
        }

        void WriteValue(KVObject value)
        {
            Span<char> buffer = stackalloc char[NumberBufferLength];

            if (value.TryFormatNumber(buffer, out var charsWritten, CultureInfo.InvariantCulture))
            {
                WriteText(buffer[..charsWritten]);
            }
            else
            {
                WriteText(value.ToString(CultureInfo.InvariantCulture));
            }
        }

        void WriteText(ReadOnlySpan<char> text)
        {
            writer.Write('"');

            if (!text.ContainsAny(CharsToEscape))
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
