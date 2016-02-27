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

            while (Peek() != terminator)
            {
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

        bool IsEndOfFile(int value) => value == -1;

        static InvalidDataException MakeSyntaxException()
        {
            return new InvalidDataException("The syntax is incorrect.");
        }
    }
}
