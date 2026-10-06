using System.Text;

namespace ValveKeyValue.Deserialization.KeyValues1
{
    class KV1TokenReader : KVTokenReader
    {
        const char QuotationMark = '"';
        const char ObjectStart = '{';
        const char ObjectEnd = '}';
        const char CommentBegin = '/'; // Although Valve uses the double-slash convention, the KV spec allows for single-slash comments.
        const char ConditionBegin = '[';
        const char ConditionEnd = ']';
        const char Assignment = '=';

        public KV1TokenReader(TextReader textReader, KVSerializerOptions options) : base(textReader)
        {
            ArgumentNullException.ThrowIfNull(options);

            this.options = options;
        }

        readonly StringBuilder sb = new();
        readonly KVSerializerOptions options;

        // Whether a backslash-quote (\") was read while escape sequences were disabled. If parsing
        // subsequently fails, this is the likely cause, and the error message suggests enabling them.
        bool encounteredPossibleEscapeSequence;

        // Creates the exception for a syntax error. Messages carry their own precise position;
        // the escape-sequences hint is appended when a stray \" suggests the file needs it.
        public KeyValueException MakeSyntaxException(string message, Exception? innerException = null)
        {
            if (encounteredPossibleEscapeSequence)
            {
                message += " A backslash-escaped quotation mark (\\\") was read, but escape sequences are disabled - consider enabling KVSerializerOptions.HasEscapeSequences.";
            }

            return new KeyValueException(message, innerException);
        }

        protected override KVToken ReadNextTokenInner()
        {
            var nextChar = Peek();
            if (IsEndOfFile(nextChar))
            {
                return new KVToken(KVTokenType.EndOfFile);
            }

            return nextChar switch
            {
                ObjectStart => ReadObjectStart(),
                ObjectEnd => ReadObjectEnd(),
                CommentBegin => ReadComment(),
                ConditionBegin => ReadCondition(),
                Assignment => ReadAssignment(),
                QuotationMark => new KVToken(KVTokenType.String, ReadQuotedString()),
                _ => new KVToken(KVTokenType.String, ReadUnquotedString()),
            };
        }

        KVToken ReadAssignment()
        {
            ReadChar(Assignment);
            return new KVToken(KVTokenType.Assignment, "=");
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

            // Some keyvalues implementations have a bug where only a single slash is needed for a comment.
            // Otherwise the second slash is part of the comment marker.
            if (Peek() == CommentBegin)
            {
                Next();
            }

            // The comment ends at the line break, which is left for the whitespace between tokens.
            // A carriage return before it is not part of the comment either.
            MarkTokenEnd();

            int next;
            while (!IsEndOfFile(next = Peek()) && next != '\n')
            {
                sb.Append(Next());

                if (next != '\r')
                {
                    MarkTokenEnd();
                }
            }

            if (sb.Length > 0 && sb[^1] == '\r')
            {
                sb.Length--;
            }

            var text = sb.ToString();
            sb.Clear();

            return new KVToken(KVTokenType.Comment, text);
        }

        // Conditionals are not quoted, so escape sequences are not translated inside them.
        KVToken ReadCondition()
        {
            ReadChar(ConditionBegin);

            while (Peek() != ConditionEnd)
            {
                sb.Append(Next());
            }

            ReadChar(ConditionEnd);

            var text = sb.ToString();
            sb.Clear();

            return new KVToken(KVTokenType.Condition, text);
        }

        string ReadUntil(Func<int, bool> isTerminator)
        {
            var escapeNext = false;
            var escapeLine = 0;
            var escapeColumn = 0;

            while (escapeNext || !isTerminator(Peek()))
            {
                var next = Next();

                if (options.HasEscapeSequences)
                {
                    if (!escapeNext && next == '\\')
                    {
                        escapeNext = true;
                        // Only the backslash has been consumed, so it sits one column back on this line.
                        escapeLine = Line;
                        escapeColumn = Column - 1;
                        continue;
                    }

                    if (escapeNext)
                    {
                        next = next switch
                        {
                            'n' => '\n',
                            't' => '\t',
                            'v' => '\v',
                            'b' => '\b',
                            'r' => '\r',
                            'f' => '\f',
                            'a' => '\a',
                            '\\' => '\\',
                            '?' => '?',
                            '\'' => '\'',
                            '"' => '"',
                            _ when options.EnableValveNullByteBugBehavior => '\0',
                            _ => throw MakeSyntaxException($"Unknown escape sequence '\\{next}' at line {escapeLine}, column {escapeColumn}."),
                        };

                        escapeNext = false;
                    }
                }
                else if (next == '\\' && Peek() == QuotationMark)
                {
                    encounteredPossibleEscapeSequence = true;
                }

                sb.Append(next);
            }

            var result = sb.ToString();
            sb.Clear();

            // Valve bug-for-bug compatibility with tier1 KeyValues/CUtlBuffer: an invalid escape sequence is a null byte which
            // causes the text to be trimmed to the point of that null byte.
            if (options.EnableValveNullByteBugBehavior && result.IndexOf('\0', StringComparison.Ordinal) is var nullByteIndex && nullByteIndex >= 0)
            {
                result = result[..nullByteIndex];
            }
            return result;
        }

        // An unquoted string ends at whitespace, a quotation mark, a brace, or '='.
        string ReadUnquotedString()
        {
            while (true)
            {
                var next = Peek();
                if (IsEndOfFile(next) || char.IsWhiteSpace((char)next) || next is QuotationMark or ObjectStart or ObjectEnd or Assignment)
                {
                    break;
                }

                sb.Append(Next());
            }

            var result = sb.ToString();
            sb.Clear();

            return result;
        }

        string ReadQuotedString()
        {
            ReadChar(QuotationMark);
            var text = ReadUntil(static (c) => c == QuotationMark);
            ReadChar(QuotationMark);
            return text;
        }
    }
}
