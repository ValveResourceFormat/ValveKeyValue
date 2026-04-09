namespace ValveKeyValue.Deserialization
{
    abstract class KVTokenReader : IDisposable
    {
        protected KVTokenReader(TextReader textReader)
        {
            ArgumentNullException.ThrowIfNull(textReader);

            this.textReader = textReader;
        }

        protected TextReader textReader;
        protected bool disposed;
        int peekedNext = -1;

        int lineOffset;
        int columnOffset;
        int charOffset;

        public int Line => lineOffset + 1;
        public int Column => columnOffset + 1;

        // Character offset of the next character to read, advancing by exactly one per character
        // consumed from the input. Source-map deserialization indexes into the same text as a
        // string, so these offsets line up with substring boundaries on the caller side.
        public int CharOffset => charOffset;

        // Character range [LastTokenStart, LastTokenEnd) of the most recently returned token,
        // measured after leading whitespace was skipped. Source-map deserializers read these
        // after each ReadNextToken() call to index into the same text the parser saw.
        public int LastTokenStart { get; private set; }
        public int LastTokenEnd { get; private set; }

        public int PreviousTokenStartLine { get; private set; }
        public int PreviousTokenStartColumn { get; private set; }
        public string PreviousTokenPosition => $"line {PreviousTokenStartLine}, column {PreviousTokenStartColumn}";

        public KVToken ReadNextToken()
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            SwallowWhitespace();

            PreviousTokenStartLine = Line;
            PreviousTokenStartColumn = Column;

            LastTokenStart = charOffset;
            var token = ReadNextTokenInner();
            LastTokenEnd = charOffset;
            return token;
        }

        protected abstract KVToken ReadNextTokenInner();

        public void Dispose()
        {
            if (!disposed)
            {
                textReader.Dispose();
                disposed = true;
            }
        }

        protected char Next() => TryGetNext(out var next) ? next : throw new EndOfStreamException();

        protected bool TryGetNext(out char next)
        {
            int nextValue;

            if (peekedNext != -1)
            {
                nextValue = peekedNext;
                peekedNext = -1;
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

            charOffset++;
            next = (char)nextValue;
            return true;
        }

        protected int Peek()
        {
            if (peekedNext != -1)
            {
                return peekedNext;
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

        protected static bool IsEndOfFile(int value) => value == -1;
    }
}
