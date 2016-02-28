using System;
using System.IO;
using System.Text;

namespace ValveKeyValue
{
    class KVTokenReader : IDisposable
    {
        const char QuotationMark = '"';
        const char ObjectStart = '{';
        const char ObjectEnd = '}';
        const char CommentBegin = '/'; // Although Valve uses the double-slash convention, the KV spec allows for single-slash comments.
        const char ConditionBegin = '[';
        const char ConditionEnd = ']';

        public KVTokenReader(Stream stream)
        {
            Require.NotNull(stream, nameof(stream));
            textReader = new StreamReader(stream);
        }

        TextReader textReader;
        bool disposed;

        public KVToken ReadNextToken()
        {
            Require.NotDisposed(nameof(KVTokenReader), disposed);
            SwallowWhitespace();

            var nextChar = Peek();
            if (IsEndOfFile(nextChar))
            {
                return new KVToken(KVTokenType.EndOfFile);
            }

            switch (nextChar)
            {
                case QuotationMark:
                    return ReadString();

                case ObjectStart:
                    return ReadObjectStart();

                case ObjectEnd:
                    return ReadObjectEnd();

                case CommentBegin:
                    return ReadComment();

                case ConditionBegin:
                    return ReadCondition();

                default:
                    throw new InvalidDataException();
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
            SwallowWhitespace();
            ReadChar(QuotationMark);
            var text = ReadUntil(QuotationMark);
            ReadChar(QuotationMark);

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

        char Next()
        {
            var next = textReader.Read();
            if (next == -1)
            {
                throw new EndOfStreamException();
            }

            return (char)next;
        }

        int Peek()
        {
            return textReader.Peek();
        }

        void ReadChar(char expectedChar)
        {
            var next = Next();
            if (next != expectedChar)
            {
                throw MakeSyntaxException();
            }
        }

        string ReadUntil(char terminator)
        {
            var sb = new StringBuilder();
            var escapeNext = false;

            while (Peek() != terminator || escapeNext)
            {
                var next = Next();

                if (next == '\\' && !escapeNext)
                {
                    escapeNext = true;
                    continue;
                }
                else if (escapeNext)
                {
                    escapeNext = false;
                }

                sb.Append(next);
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

        bool IsEndOfFile(int value) => value == -1;

        static InvalidDataException MakeSyntaxException()
        {
            return new InvalidDataException("The syntax is incorrect.");
        }
    }
}
