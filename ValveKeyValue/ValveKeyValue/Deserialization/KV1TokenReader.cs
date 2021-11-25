using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace ValveKeyValue.Deserialization
{
    class KV1TokenReader : IDisposable
    {
        const char QuotationMark = '"';
        const char ObjectStart = '{';
        const char ObjectEnd = '}';
        const char CommentBegin = '/'; // Although Valve uses the double-slash convention, the KV spec allows for single-slash comments.
        const char ConditionBegin = '[';
        const char ConditionEnd = ']';
        const char InclusionMark = '#';

        public KV1TokenReader(TextReader textReader, KVSerializerOptions options)
        {
            Require.NotNull(textReader, nameof(textReader));
            Require.NotNull(options, nameof(options));

            this.textReader = textReader;
            this.options = options;
        }

        readonly KVSerializerOptions options;
        TextReader textReader;
        bool disposed;

        public KVToken ReadNextToken()
        {
            Require.NotDisposed(nameof(KV1TokenReader), disposed);
            SwallowWhitespace();

            var nextChar = Peek();
            if (IsEndOfFile(nextChar))
            {
                return new KVToken(KVTokenType.EndOfFile);
            }

            switch (nextChar)
            {
                case ObjectStart:
                    return ReadObjectStart();

                case ObjectEnd:
                    return ReadObjectEnd();

                case CommentBegin:
                    return ReadComment();

                case ConditionBegin:
                    return ReadCondition();

                case InclusionMark:
                    return ReadInclusion();

                case QuotationMark:
                default:
                    return ReadString();
            }
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

        KVToken ReadString()
        {
            var text = ReadStringRaw();
            return new KVToken(KVTokenType.String, text);
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

        KVToken ReadComment()
        {
            ReadChar(CommentBegin);

            if (Peek() == (char)CommentBegin)
            {
                Next();
            }

            var text = textReader.ReadLine();
            return new KVToken(KVTokenType.Comment, text);
        }

        KVToken ReadCondition()
        {
            ReadChar(ConditionBegin);
            var text = ReadUntil(ConditionEnd);
            ReadChar(ConditionEnd);

            return new KVToken(KVTokenType.Condition, text);
        }

        KVToken ReadInclusion()
        {
            ReadChar(InclusionMark);
            var term = ReadUntil(new[] { ' ', '\t' });
            var value = ReadStringRaw();

            if (string.Equals(term, "include", StringComparison.Ordinal))
            {
                return new KVToken(KVTokenType.IncludeAndAppend, value);
            }
            else if (string.Equals(term, "base", StringComparison.Ordinal))
            {
                return new KVToken(KVTokenType.IncludeAndMerge, value);
            }

            throw new InvalidDataException("Unrecognized term after '#' symbol.");
        }

        char Next()
        {
            var next = textReader.Read();
            if (next == -1)
            {
                throw new EndOfStreamException();
            }

            return (char)next;
        }

        int Peek() => textReader.Peek();

        void ReadChar(char expectedChar)
        {
            var next = Next();
            if (next != expectedChar)
            {
                throw MakeSyntaxException();
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
                result = result.Substring(0, nullByteIndex);
            }
            return result;
        }

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

        string ReadStringRaw()
        {
            SwallowWhitespace();
            if (Peek() == '"')
            {
                return ReadQuotedStringRaw();
            }
            else
            {
                return ReadUntilWhitespaceOrQuote();
            }
        }

        string ReadQuotedStringRaw()
        {
            ReadChar(QuotationMark);
            var text = ReadUntil(QuotationMark);
            ReadChar(QuotationMark);
            return text;
        }

        bool IsEndOfFile(int value) => value == -1;

        static InvalidDataException MakeSyntaxException()
        {
            return new InvalidDataException("The syntax is incorrect.");
        }
    }
}
