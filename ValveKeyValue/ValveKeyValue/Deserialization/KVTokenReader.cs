namespace ValveKeyValue.Deserialization
{
    class KVTokenReader : IDisposable
    {
        public KVTokenReader(TextReader textReader)
        {
            Require.NotNull(textReader, nameof(textReader));

            this.textReader = textReader;
        }

        protected TextReader textReader;
        protected bool disposed;
        protected int? peekedNext;

        int lineOffset;
        int columnOffset;

        public int Line => lineOffset + 1;
        public int Column => columnOffset + 1;

        public int PreviousTokenStartLine { get; protected set; }
        public int PreviousTokenStartColumn { get; protected set; }
        public string PreviousTokenPosition => $"line {PreviousTokenStartLine}, column {PreviousTokenStartColumn}";

        public void Dispose()
        {
            if (!disposed)
            {
                textReader.Dispose();
                textReader = null;

                disposed = true;
            }
        }

        protected char Next() => TryGetNext(out var next) ? next : throw new EndOfStreamException();

        protected bool TryGetNext(out char next)
        {
            int nextValue;

            if (peekedNext.HasValue)
            {
                nextValue = peekedNext.Value;
                peekedNext = null;
            }
            else
            {
                nextValue = textReader.Read();
            }

            if (nextValue == -1)
            {
                next = default;
                return false;
            }

            if (nextValue is '\n')
            {
                lineOffset++;
                columnOffset = 0;
            }
            else
            {
                columnOffset++;
            }

            next = (char)nextValue;
            return true;
        }

        protected int Peek()
        {
            if (peekedNext.HasValue)
            {
                return peekedNext.Value;
            }

            var next = textReader.Read();
            peekedNext = next;

            return next;
        }

        protected void ReadChar(char expectedChar)
        {
            var next = Next();
            if (next != expectedChar)
            {
                throw new InvalidDataException($"The syntax is incorrect, expected '{expectedChar}' but got '{next}' at line {Line}, column {Column}.");
            }
        }

        protected void SwallowWhitespace()
        {
            while (PeekWhitespace())
            {
                Next();
            }
        }

        protected bool PeekWhitespace()
        {
            var next = Peek();
            return !IsEndOfFile(next) && char.IsWhiteSpace((char)next);
        }

        protected bool IsEndOfFile(int value) => value == -1;
    }
}
