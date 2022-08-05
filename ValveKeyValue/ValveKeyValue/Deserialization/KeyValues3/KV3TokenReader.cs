using System;
using System.IO;
using System.Text;
using ValveKeyValue.KeyValues3;
using Encoding = ValveKeyValue.KeyValues3.Encoding;

namespace ValveKeyValue.Deserialization.KeyValues3
{
    class KV3TokenReader : IDisposable
    {
        const char QuotationMark = '"';
        const char ObjectStart = '{';
        const char ObjectEnd = '}';
        const char ArrayStart = '[';
        const char ArrayEnd = ']';
        const char CommentBegin = '/';
        const char Assignment = '=';

        public KV3TokenReader(TextReader textReader, KVSerializerOptions options)
        {
            Require.NotNull(textReader, nameof(textReader));
            Require.NotNull(options, nameof(options));

            this.textReader = textReader;
            this.options = options;
        }

        readonly KVSerializerOptions options;
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
                ArrayStart => ReadArrayStart(),
                ArrayEnd => ReadArrayEnd(),
                CommentBegin => ReadComment(),
                Assignment => ReadAssignment(),
                _ => ReadStringOrIdentifier(), // TODO: This should read identifiers, strings should only be read as values, keys can't be quoted
                // TODO: #[] byte array
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

            if (Peek() == QuotationMark)
            {
                return new KVToken(KVTokenType.String, ReadQuotedStringRaw());
            }

            return new KVToken(KVTokenType.Identifier, ReadUntilWhitespaceOrDelimeter(QuotationMark));
        }

        KVToken ReadAssignment()
        {
            ReadChar(Assignment);
            return new KVToken(KVTokenType.Assignment);
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
            var str = ReadUntilWhitespaceOrDelimeter((char)0);

            if (str != "<!--")
            {
                throw new InvalidDataException($"The header is incorrect, expected '<!--' but got '{str}'.");
            }

            SwallowWhitespace();
            str = ReadUntilWhitespaceOrDelimeter((char)0);

            if (str != "kv3")
            {
                throw new InvalidDataException($"The header is incorrect, expected 'kv3' but got '{str}'.");
            }

            SwallowWhitespace();
            str = ReadUntil(':');

            if (str != "encoding")
            {
                throw new InvalidDataException($"The header is incorrect, expected 'encoding' but got '{str}'.");
            }

            ReadChar(':');
            var encodingType = ReadUntil(':');
            ReadChar(':');

            str = ReadUntil('{');

            if (str != "version")
            {
                throw new InvalidDataException($"The header is incorrect, expected 'version' but got '{str}'.");
            }

            ReadChar('{');
            var encoding = new Guid(ReadUntil('}'));
            ReadChar('}');

            SwallowWhitespace();

            str = ReadUntil(':');

            if (str != "format")
            {
                throw new InvalidDataException($"The header is incorrect, expected 'format' but got '{str}'.");
            }

            ReadChar(':');
            var formatType = ReadUntil(':');
            ReadChar(':');

            str = ReadUntil('{');

            if (str != "version")
            {
                throw new InvalidDataException($"The header is incorrect, expected 'version' but got '{str}'.");
            }

            ReadChar('{');
            var format = new Guid(ReadUntil('}'));
            ReadChar('}');

            SwallowWhitespace();

            str = ReadUntilWhitespaceOrDelimeter((char)0);

            if (str != "-->")
            {
                throw new InvalidDataException($"The header is incorrect, expected '-->' but got '{str}'.");
            }

            if (encodingType.Equals("text", StringComparison.OrdinalIgnoreCase) && encoding != Encoding.Text)
            {
                throw new InvalidDataException($"Unrecognized format specifier, expected '{Encoding.Text}' but got '{format}'.");
            }

            if (encodingType.Equals("generic", StringComparison.OrdinalIgnoreCase) && format != Format.Generic)
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

        string ReadUntil(char delimeter)
        {
            var sb = new StringBuilder();

            while (true)
            {
                var next = Peek();

                if (next == delimeter)
                {
                    break;
                }

                sb.Append(Next());
            }

            return sb.ToString();
        }

        // TODO: Read until delimeter: "{}[]=, \t\n'\":+;"
        string ReadUntilWhitespaceOrDelimeter(char delimeter)
        {
            var sb = new StringBuilder();

            while (true)
            {
                var next = Peek();
                if (next == -1 || char.IsWhiteSpace((char)next) || next == delimeter)
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

        string ReadQuotedStringRaw()
        {
            ReadChar(QuotationMark);

            var isMultiline = false;

            var sb = new StringBuilder();

            // Is there another quote mark?
            // TODO: Peek() for more than one character
            if (Peek() == QuotationMark)
            {
                Next();

                // If the next character is not another quote, it's an empty string
                if (Peek() == QuotationMark)
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

            while (Peek() != QuotationMark)
            {
                var next = Next();

                if (!isMultiline && next == '\n')
                {
                    throw new InvalidDataException("Found new line while parsing literal string.");
                }

                sb.Append(next);
            }

            ReadChar(QuotationMark);

            if (isMultiline)
            {
                ReadChar(QuotationMark);
                ReadChar(QuotationMark);
            }

            if (sb.Length > 0 && sb[^1] == '\n')
            {
                sb.Remove(sb.Length - 1, 1);
            }

            if (sb.Length > 0 && sb[^1] == '\r')
            {
                sb.Remove(sb.Length - 1, 1);
            }

            return sb.ToString();
        }

        bool IsEndOfFile(int value) => value == -1;
    }
}
