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

            if (Peek() == '"')
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

            // Some keyvalues implementations have a bug where only a single slash is needed for a comment
            if (next != CommentBegin)
            {
                sb.Append(next);
            }

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

        string ReadUntil(params char[] terminators)
        {
            var sb = new StringBuilder();
            var escapeNext = false;

            var integerTerminators = new HashSet<int>(terminators.Select(t => (int)t));
            while (!integerTerminators.Contains(Peek()) || escapeNext)
            {
                var next = Next();

                if (options.HasEscapeSequences)
                {
                    if (!escapeNext && next == '\\')
                    {
                        escapeNext = true;
                        continue;
                    }

                    if (escapeNext)
                    {
                        next = next switch
                        {
                            'r' => '\r',
                            'n' => '\n',
                            't' => '\t',
                            '\\' => '\\',
                            '"' => '"',
                            _ when options.EnableValveNullByteBugBehavior => '\0',
                            _ => throw new InvalidDataException($"Unknown escape sequence '\\{next}'."),
                        };

                        escapeNext = false;
                    }
                }

                sb.Append(next);
            }

            var result = sb.ToString();

            // Valve bug-for-bug compatibility with tier1 KeyValues/CUtlBuffer: an invalid escape sequence is a null byte which
            // causes the text to be trimmed to the point of that null byte.
            if (options.EnableValveNullByteBugBehavior && result.IndexOf('\0') is var nullByteIndex && nullByteIndex >= 0)
            {
                result = result[..nullByteIndex];
            }
            return result;
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
            var text = ReadUntil(QuotationMark);
            ReadChar(QuotationMark);
            return text;
        }

        bool IsEndOfFile(int value) => value == -1;
    }
}
