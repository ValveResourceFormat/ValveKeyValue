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
        const char InclusionMark = '#';

        public KV1TokenReader(TextReader textReader, KVSerializerOptions options) : base(textReader)
        {
            Require.NotNull(options, nameof(options));

            this.options = options;
        }

        readonly StringBuilder sb = new();
        readonly KVSerializerOptions options;

        public KVToken ReadNextToken()
        {
            Require.NotDisposed(nameof(KV1TokenReader), disposed);
            SwallowWhitespace();

            PreviousTokenStartLine = Line;
            PreviousTokenStartColumn = Column;

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
                InclusionMark => ReadInclusion(),
                _ => ReadString(),
            };
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

            // Some keyvalues implementations have a bug where only a single slash is needed for a comment
            // If the file ends with a single slash then we have an empty comment, bail out
            if (!TryGetNext(out var next))
            {
                return new KVToken(KVTokenType.Comment, string.Empty);
            }

            // If the next character is not a slash, then we have a comment that starts with a single slash
            // Otherwise pretend the comment is a double-slash and ignore this new second slash.
            if (next != CommentBegin)
            {
                sb.Append(next);
            }

            // Be more permissive here than in other places, as comments can be the last token in a file.
            while (TryGetNext(out next))
            {
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
            sb.Clear();

            return new KVToken(KVTokenType.Comment, text);
        }

        KVToken ReadCondition()
        {
            ReadChar(ConditionBegin);
            var text = ReadUntil(static (c) => c == ConditionEnd);
            ReadChar(ConditionEnd);

            return new KVToken(KVTokenType.Condition, text);
        }

        KVToken ReadInclusion()
        {
            ReadChar(InclusionMark);
            var term = ReadUntil(static c => c is ' ' or '\t');
            var value = ReadStringRaw();

            if (string.Equals(term, "include", StringComparison.Ordinal))
            {
                return new KVToken(KVTokenType.IncludeAndAppend, value);
            }
            else if (string.Equals(term, "base", StringComparison.Ordinal))
            {
                return new KVToken(KVTokenType.IncludeAndMerge, value);
            }

            throw new InvalidDataException($"Unrecognized term after '#' symbol (line {Line}, column {Column})");
        }

        string ReadUntil(Func<int, bool> isTerminator)
        {
            var escapeNext = false;

            while (escapeNext || !isTerminator(Peek()))
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
                            _ => throw new InvalidDataException($"Unknown escape sequence '\\{next}' at line {Line}, column {Column - 2}."),
                        };

                        escapeNext = false;
                    }
                }

                sb.Append(next);
            }

            var result = sb.ToString();
            sb.Clear();

            // Valve bug-for-bug compatibility with tier1 KeyValues/CUtlBuffer: an invalid escape sequence is a null byte which
            // causes the text to be trimmed to the point of that null byte.
            if (options.EnableValveNullByteBugBehavior && result.IndexOf('\0') is var nullByteIndex && nullByteIndex >= 0)
            {
                result = result[..nullByteIndex];
            }
            return result;
        }

        string ReadUntilWhitespaceOrQuote()
        {
            while (true)
            {
                var next = Peek();
                if (next == -1 || char.IsWhiteSpace((char)next) || next == '"')
                {
                    break;
                }

                sb.Append(Next());
            }

            var result = sb.ToString();
            sb.Clear();

            return result;
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
            var text = ReadUntil(static (c) => c == QuotationMark);
            ReadChar(QuotationMark);
            return text;
        }
    }
}
