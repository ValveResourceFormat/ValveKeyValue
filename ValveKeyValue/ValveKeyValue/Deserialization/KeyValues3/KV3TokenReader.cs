using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ValveKeyValue.Deserialization.KeyValues3
{
    class KV3TokenReader : IDisposable
    {
        const char HeaderStart = '<';
        const char QuotationMark = '"';
        const char ObjectStart = '{';
        const char ObjectEnd = '}';
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
                HeaderStart => ReadHeader(),
                ObjectStart => ReadObjectStart(),
                ObjectEnd => ReadObjectEnd(),
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

            return new KVToken(KVTokenType.Identifier, ReadUntilWhitespaceOrQuote());
        }

        KVToken ReadObjectStart()
        {
            ReadChar(ObjectStart);
            return new KVToken(KVTokenType.ObjectStart);
        }

        KVToken ReadAssignment()
        {
            ReadChar(Assignment);
            return new KVToken(KVTokenType.Assignment);
        }

        KVToken ReadObjectEnd()
        {
            ReadChar(ObjectEnd);
            return new KVToken(KVTokenType.ObjectEnd);
        }

        KVToken ReadHeader()
        {
            ReadChar('<');
            ReadChar('!');
            ReadChar('-');
            ReadChar('-');

            var sb = new StringBuilder();
            bool ended;

            while (true)
            {
                var next = Next();

                if (next == '\n')
                {
                    throw new InvalidDataException("Found new line while parsing header.");
                }

                if (next == '>' && sb.Length >= 2 && sb[^1] == '-' && sb[^2] == '-')
                {
                    ended = true;
                    break;
                }

                sb.Append(next);
            }

            if (!ended)
            {
                throw new InvalidDataException("Did not find header comment ending.");
            }

            var text = sb.ToString();

            return new KVToken(KVTokenType.Header, text);
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

        // TODO: Read until delimeter: "{}[]=, \t\n'\":+;"
        string ReadUntilWhitespaceOrQuote()
        {
            var sb = new StringBuilder();

            while (true)
            {
                var next = Peek();
                if (next == -1 || char.IsWhiteSpace((char)next) || next == '"')
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

            // TODO: Single quoted strings may not have new lines
            var integerTerminators = new HashSet<int>
            {
                QuotationMark,
            };

            while (!integerTerminators.Contains(Peek()))
            {
                sb.Append(Next());
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
