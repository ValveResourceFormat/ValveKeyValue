using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ValveKeyValue.KeyValues3;
using Encoding = ValveKeyValue.KeyValues3.Encoding;

namespace ValveKeyValue.Deserialization.KeyValues3
{
    class KV3TokenReader : IDisposable
    {
        const char ObjectStart = '{';
        const char ObjectEnd = '}';
        const char BinaryArrayMarker = '#';
        const char ArrayStart = '[';
        const char ArrayEnd = ']';
        const char CommentBegin = '/';
        const char Assignment = '=';
        const char Comma = ',';

        public KV3TokenReader(TextReader textReader, KVSerializerOptions options)
        {
            Require.NotNull(textReader, nameof(textReader));
            Require.NotNull(options, nameof(options));

            this.textReader = textReader;
            this.options = options;

            // Dota 2 binary from 2017 used "+" as a terminate (for flagged values), but then they changed it to "|"
            var terminators = "{}[]=, \t\n\r'\":|;".ToCharArray();
            integerTerminators = new HashSet<int>(terminators.Select(t => (int)t));
        }

        readonly KVSerializerOptions options;
        readonly HashSet<int> integerTerminators;
        TextReader textReader;
        bool disposed;
        int? peekedNext;

        public KVToken ReadNextToken()
        {
            Require.NotDisposed(nameof(KV3TokenReader), disposed);
            SwallowWhitespace();

            var nextChar = Peek();
            if (IsEndOfFile(nextChar))
            {
                return new KVToken(KVTokenType.EndOfFile);
            }

            return nextChar switch
            {
                ObjectStart => ReadObjectStart(),
                ObjectEnd => ReadObjectEnd(),
                BinaryArrayMarker when Peek() == ArrayStart => ReadBinaryArrayStart(),
                ArrayStart => ReadArrayStart(),
                ArrayEnd => ReadArrayEnd(),
                CommentBegin => ReadComment(),
                Assignment => ReadAssignment(),
                Comma => ReadComma(),
                _ => ReadStringOrIdentifier(),
            };
        }

        public void Dispose()
        {
            if (!disposed)
            {
                textReader.Dispose();
                textReader = null;

                disposed = true;
            }
        }

        KVToken ReadStringOrIdentifier()
        {
            SwallowWhitespace();

            var token = ReadToken();
            var type = KVTokenType.String;

            if (IsIdentifier(token))
            {
                type = KVTokenType.Identifier;

                var next = Peek();

                if (next == ':' || next == '|')
                {
                    Next();
                    type = KVTokenType.Flag;
                }
            }

            return new KVToken(type, token);
        }

        KVToken ReadAssignment()
        {
            ReadChar(Assignment);
            return new KVToken(KVTokenType.Assignment);
        }

        KVToken ReadComma()
        {
            ReadChar(Comma);
            return new KVToken(KVTokenType.Comma);
        }

        KVToken ReadBinaryArrayStart()
        {
            ReadChar(BinaryArrayMarker);
            ReadChar(ArrayStart);
            return new KVToken(KVTokenType.BinaryArrayStart);
        }

        KVToken ReadArrayStart()
        {
            ReadChar(ArrayStart);
            return new KVToken(KVTokenType.ArrayStart);
        }

        KVToken ReadArrayEnd()
        {
            ReadChar(ArrayEnd);
            return new KVToken(KVTokenType.ArrayEnd);
        }

        KVToken ReadObjectStart()
        {
            ReadChar(ObjectStart);
            return new KVToken(KVTokenType.ObjectStart);
        }

        KVToken ReadObjectEnd()
        {
            ReadChar(ObjectEnd);
            return new KVToken(KVTokenType.ObjectEnd);
        }

        public KVToken ReadHeader()
        {
            var str = ReadToken();

            if (str != "<!--")
            {
                throw new InvalidDataException($"The header is incorrect, expected '<!--' but got '{str}'.");
            }

            SwallowWhitespace();
            str = ReadToken();

            if (!str.Equals("kv3", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException($"The header is incorrect, expected 'kv3' but got '{str}'.");
            }

            SwallowWhitespace();
            str = ReadToken();

            if (!str.Equals("encoding", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException($"The header is incorrect, expected 'encoding' but got '{str}'.");
            }

            ReadChar(':');
            var encodingType = ReadToken();
            ReadChar(':');

            str = ReadToken();

            if (!str.Equals("version", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException($"The header is incorrect, expected 'version' but got '{str}'.");
            }

            ReadChar('{');
            var encoding = new Guid(ReadToken());
            ReadChar('}');

            SwallowWhitespace();

            str = ReadToken();

            if (!str.Equals("format", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException($"The header is incorrect, expected 'format' but got '{str}'.");
            }

            ReadChar(':');
            var formatType = ReadToken();
            ReadChar(':');

            str = ReadToken();

            if (!str.Equals("version", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException($"The header is incorrect, expected 'version' but got '{str}'.");
            }

            ReadChar('{');
            var format = new Guid(ReadToken());
            ReadChar('}');

            SwallowWhitespace();

            str = ReadToken();

            if (str != "-->")
            {
                throw new InvalidDataException($"The header is incorrect, expected '-->' but got '{str}'.");
            }

            if (encodingType.Equals("text", StringComparison.OrdinalIgnoreCase) && encoding != Encoding.Text)
            {
                throw new InvalidDataException($"Unrecognized format specifier, expected '{Encoding.Text}' but got '{encoding}'.");
            }

            if (formatType.Equals("generic", StringComparison.OrdinalIgnoreCase) && format != Format.Generic)
            {
                throw new InvalidDataException($"Unrecognized encoding specifier, expected '{Format.Generic}' but got '{format}'.");
            }

            return new KVToken(KVTokenType.Header, string.Empty);
        }

        KVToken ReadComment()
        {
            ReadChar(CommentBegin);

            var sb = new StringBuilder();
            var next = Next();
            var isMultiline = false;

            // TODO: Read /* */ comments
            if (next == '*')
            {
                isMultiline = true;
            }
            else if (next != CommentBegin)
            {
                // TODO: Return identifier?
                throw new InvalidDataException("The syntax is incorrect, or is it?");
            }

            if (isMultiline)
            {
                while (true)
                {
                    next = Next();

                    if (next == '*')
                    {
                        var nextNext = Peek();

                        if (nextNext == '/')
                        {
                            Next();
                            break;
                        }
                    }

                    sb.Append(next);
                }
            }
            else
            {
                while (true)
                {
                    next = Next();

                    if (next == '\n')
                    {
                        break;
                    }

                    sb.Append(next);
                }

                if (sb.Length > 0 && sb[^1] == '\r')
                {
                    sb.Remove(sb.Length - 1, 1);
                }
            }

            var text = sb.ToString();

            return new KVToken(KVTokenType.Comment, text);
        }

        char Next()
        {
            int next;

            if (peekedNext.HasValue)
            {
                next = peekedNext.Value;
                peekedNext = null;
            }
            else
            {
                next = textReader.Read();
            }

            if (next == -1)
            {
                throw new EndOfStreamException();
            }

            return (char)next;
        }

        int Peek()
        {
            if (peekedNext.HasValue)
            {
                return peekedNext.Value;
            }

            var next = textReader.Read();
            peekedNext = next;

            return next;
        }

        void ReadChar(char expectedChar)
        {
            var next = Next();
            if (next != expectedChar)
            {
                throw new InvalidDataException($"The syntax is incorrect, expected '{expectedChar}' but got '{next}'.");
            }
        }

        bool IsIdentifier(string text)
        {
            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];

                if ((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z'))
                {
                    continue;
                }

                if (c >= '0' && c <= '9')
                {
                    continue;
                }

                if (c == '_' || c == ':' || c == '.')
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        string ReadToken()
        {
            // TODO: while true
            // swallow whitespace
            // swallow comments
            // swallow whitespace

            var next = Peek();

            if (next == '"' || next == '\'')
            {
                return ReadQuotedStringRaw((char)next);
            }

            var sb = new StringBuilder();

            while (true)
            {
                next = Peek();

                if (next <= ' ' || integerTerminators.Contains(next))
                {
                    break;
                }

                sb.Append(Next());
            }

            return sb.ToString();
        }

        void SwallowWhitespace()
        {
            while (PeekWhitespace())
            {
                Next();
            }
        }

        bool PeekWhitespace()
        {
            var next = Peek();
            return !IsEndOfFile(next) && char.IsWhiteSpace((char)next);
        }

        string ReadQuotedStringRaw(char quotationMark)
        {
            ReadChar(quotationMark);

            var isMultiline = false;

            var sb = new StringBuilder();

            // Is there another quote mark?
            // TODO: Peek() for more than one character
            if (quotationMark == '"' && Peek() == '"')
            {
                Next();

                // If the next character is not another quote, it's an empty string
                if (Peek() == '"')
                {
                    isMultiline = true;

                    Next();

                    if (Peek() == '\r')
                    {
                        Next();
                    }

                    if (Peek() == '\n')
                    {
                        Next();
                    }
                }
                else
                {
                    return string.Empty;
                }
            }

            // TODO: Figure out '\' character

            while (Peek() != quotationMark)
            {
                var next = Next();

                if (!isMultiline && next == '\n')
                {
                    throw new InvalidDataException("Found new line while parsing literal string.");
                }

                sb.Append(next);
            }

            ReadChar(quotationMark);

            if (isMultiline)
            {
                ReadChar('"');
                ReadChar('"');

                if (sb.Length > 0 && sb[^1] == '\n')
                {
                    sb.Remove(sb.Length - 1, 1);
                }

                if (sb.Length > 0 && sb[^1] == '\r')
                {
                    sb.Remove(sb.Length - 1, 1);
                }
            }

            return sb.ToString();
        }

        bool IsEndOfFile(int value) => value == -1;
    }
}
