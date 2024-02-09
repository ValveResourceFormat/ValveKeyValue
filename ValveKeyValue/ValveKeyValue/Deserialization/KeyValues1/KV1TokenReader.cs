using System.Linq;
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

            throw new InvalidDataException($"Unrecognized term after '#' symbol (line {Line}, column {Column})");
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
    }
}
