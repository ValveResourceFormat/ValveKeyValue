using System.Buffers;
using System.Text;
using ValveKeyValue.KeyValues3;
using Encoding = ValveKeyValue.KeyValues3.Encoding;

namespace ValveKeyValue.Deserialization.KeyValues3
{
    class KV3TokenReader : KVTokenReader
    {
        const char ObjectStart = '{';
        const char ObjectEnd = '}';
        const char BinaryBlobMarker = '#';
        const char ArrayStart = '[';
        const char ArrayEnd = ']';
        const char CommentBegin = '/';
        const char Assignment = '=';
        const char Comma = ',';

        // Dota 2 binary from 2017 used "+" as a terminate (for flagged values), but then they changed it to "|"
        static readonly SearchValues<char> TokenTerminators = SearchValues.Create("{}[]=,'\":|;");

        readonly StringBuilder sb = new();

        public KV3TokenReader(TextReader textReader) : base(textReader)
        {
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
                BinaryBlobMarker => ReadBinaryBlob(),
                ArrayStart => ReadArrayStart(),
                ArrayEnd => ReadArrayEnd(),
                CommentBegin => ReadComment(),
                Assignment => ReadAssignment(),
                Comma => ReadComma(),
                _ => ReadStringOrIdentifier(),
            };
        }

        KVToken ReadAssignment()
        {
            ReadChar(Assignment);
            return new KVToken(KVTokenType.Assignment);
        }

        KVToken ReadComma()
        {
            ReadChar(Comma);
            return new KVToken(KVTokenType.Comma);
        }

        KVToken ReadArrayStart()
        {
            ReadChar(ArrayStart);
            return new KVToken(KVTokenType.ArrayStart);
        }

        KVToken ReadArrayEnd()
        {
            ReadChar(ArrayEnd);
            return new KVToken(KVTokenType.ArrayEnd);
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

        KVToken ReadStringOrIdentifier()
        {
            // The token type follows what the source actually looks like, not the contents:
            // a quoted token is always String (so "42" stays a string), any other token is
            // Identifier, and an identifier-shaped token followed by : or | is a Flag.
            var first = Peek();
            var isQuoted = first == '"' || first == '\'';

            var token = ReadToken();

            if (isQuoted)
            {
                return new KVToken(KVTokenType.String, token);
            }

            if (token.Length == 0)
            {
                throw new KeyValueException($"The syntax is incorrect, unexpected character '{(char)first}' at {TokenStartPosition}.");
            }

            var type = KVTokenType.Identifier;

            if (IsIdentifier(token))
            {
                // Whitespace is allowed between a flag and its separator
                MarkTokenEnd();
                SwallowWhitespace();

                var next = Peek();

                if (next == ':' || next == '|')
                {
                    Next();
                    MarkTokenEnd();
                    type = KVTokenType.Flag;
                }
            }

            return new KVToken(type, token);
        }

        KVToken ReadBinaryBlob()
        {
            ReadChar(BinaryBlobMarker);

            // The marker is a token of its own, so it cannot be followed directly by more token characters
            if (IsUnquotedTokenChar(Peek()))
            {
                throw new KeyValueException($"The syntax is incorrect, expected '[' after '#' at {TokenStartPosition}.");
            }

            SwallowWhitespaceAndComments();
            ReadChar(ArrayStart);

            // Each byte is a separate token of exactly two hexadecimal digits
            while (true)
            {
                SwallowWhitespaceAndComments();

                if (Peek() == ArrayEnd)
                {
                    Next();
                    break;
                }

                var start = sb.Length;

                while (IsUnquotedTokenChar(Peek()))
                {
                    sb.Append(Next());
                }

                if (sb.Length - start != 2)
                {
                    throw new KeyValueException($"Invalid binary blob at {TokenStartPosition}, expected bytes as pairs of hexadecimal digits separated by whitespace.");
                }
            }

            var result = sb.ToString();
            sb.Clear();
            return new KVToken(KVTokenType.BinaryBlob, result);
        }

        // Whitespace and comments are allowed between any of the header tokens
        public KVHeader ReadHeader()
        {
            ExpectHeaderToken("<!--");
            ExpectHeaderToken("kv3");
            ExpectHeaderToken("encoding");
            ExpectHeaderChar(':');
            var encodingType = ReadHeaderName();
            ExpectHeaderChar(':');
            ExpectHeaderToken("version");
            ExpectHeaderChar('{');
            var encoding = ReadVersionGuid();
            ExpectHeaderChar('}');

            ExpectHeaderToken("format");
            ExpectHeaderChar(':');
            var formatType = ReadHeaderName();
            ExpectHeaderChar(':');
            ExpectHeaderToken("version");
            ExpectHeaderChar('{');
            var format = ReadVersionGuid();
            ExpectHeaderChar('}');

            ExpectHeaderToken("-->");

            if (!encodingType.Equals("text", StringComparison.OrdinalIgnoreCase))
            {
                throw new KeyValueException($"Unrecognized encoding, expected 'text' but got '{encodingType}'.");
            }

            if (encoding != Encoding.Text)
            {
                throw new KeyValueException($"Unrecognized encoding version, expected '{Encoding.Text}' but got '{encoding}'.");
            }

            if (formatType.Equals("generic", StringComparison.OrdinalIgnoreCase) && format != Format.Generic)
            {
                throw new KeyValueException($"Unrecognized format version, expected '{Format.Generic}' but got '{format}'.");
            }

            return new KVHeader
            {
                Encoding = new KV3ID(encodingType, encoding),
                Format = new KV3ID(formatType, format),
            };
        }

        // Header tokens are never quoted
        string ReadHeaderToken()
        {
            SwallowWhitespaceAndComments();
            MarkTokenStart();
            return ReadUnquotedToken();
        }

        string ReadHeaderName()
        {
            var str = ReadHeaderToken();

            if (!IsIdentifier(str))
            {
                throw new KeyValueException($"The header is incorrect, expected a name but got '{str}' at {TokenStartPosition}.");
            }

            return str;
        }

        void ExpectHeaderToken(string expected)
        {
            var str = ReadHeaderToken();

            if (!str.Equals(expected, StringComparison.OrdinalIgnoreCase))
            {
                throw new KeyValueException($"The header is incorrect, expected '{expected}' but got '{str}' at {TokenStartPosition}.");
            }
        }

        void ExpectHeaderChar(char expected)
        {
            SwallowWhitespaceAndComments();
            ReadChar(expected);
        }

        Guid ReadVersionGuid()
        {
            var str = ReadHeaderToken();

            if (!Guid.TryParse(str, out var guid))
            {
                throw new KeyValueException($"The header is incorrect, expected a version GUID but got '{str}' at {TokenStartPosition}.");
            }

            return guid;
        }

        KVToken ReadComment()
        {
            SkipComment();
            return new KVToken(KVTokenType.Comment);
        }

        void SwallowWhitespaceAndComments()
        {
            while (true)
            {
                SwallowWhitespace();

                if (Peek() != CommentBegin)
                {
                    break;
                }

                SkipComment();
            }
        }

        void SkipComment()
        {
            ReadChar(CommentBegin);

            var next = Next();

            if (next == '*')
            {
                while (true)
                {
                    if (IsEndOfFile(Peek()))
                    {
                        throw new KeyValueException($"Unterminated block comment starting at {TokenStartPosition}.");
                    }

                    next = Next();

                    if (next == '*' && Peek() == '/')
                    {
                        Next();
                        break;
                    }
                }
            }
            else if (next == CommentBegin)
            {
                // A backslash at the end of the line continues the comment onto the next line
                var previous = '\0';
                var beforePrevious = '\0';

                while (true)
                {
                    var peek = Peek();

                    if (IsEndOfFile(peek))
                    {
                        break;
                    }

                    if (peek == '\n' && previous != '\\' && (previous != '\r' || beforePrevious != '\\'))
                    {
                        break;
                    }

                    beforePrevious = previous;
                    previous = Next();
                }
            }
            else
            {
                throw new KeyValueException($"The syntax is incorrect, expected comment but got '/{next}' at {TokenStartPosition}.");
            }
        }

        // Unquoted keys, flags and header names must be identifiers, which cannot start with a digit.
        public static bool IsIdentifier(string text)
        {
            if (text.Length == 0 || char.IsAsciiDigit(text[0]))
            {
                return false;
            }

            foreach (var c in text)
            {
                if (!char.IsAsciiLetterOrDigit(c) && c != '_' && c != '.')
                {
                    return false;
                }
            }

            return true;
        }

        // Non-ASCII characters are not part of unquoted tokens
        static bool IsUnquotedTokenChar(int c) => c > ' ' && c < 0x80 && !TokenTerminators.Contains((char)c);

        string ReadToken()
        {
            MarkTokenStart();

            var next = Peek();

            if (next == '"' || next == '\'')
            {
                return ReadQuotedStringRaw((char)next);
            }

            return ReadUnquotedToken();
        }

        string ReadUnquotedToken()
        {
            while (IsUnquotedTokenChar(Peek()))
            {
                sb.Append(Next());
            }

            var result = sb.ToString();
            sb.Clear();
            return result;
        }

        string ReadQuotedStringRaw(char quotationMark)
        {
            ReadChar(quotationMark);

            if (quotationMark == '"' && Peek() == '"')
            {
                Next();

                // If the next character is not another quote, it's an empty string
                if (Peek() != '"')
                {
                    return string.Empty;
                }

                Next();

                if (Peek() == '\r')
                {
                    Next();
                }

                ReadChar('\n');
                return ReadMultilineStringContents();
            }

            var escaped = false;
            var hasEscapes = false;

            while (true)
            {
                var next = Next();

                if (next == '\\')
                {
                    escaped = !escaped;
                    hasEscapes = true;
                    sb.Append(next);
                    continue;
                }

                if (next == quotationMark && !escaped)
                {
                    break;
                }

                escaped = false;
                sb.Append(next);
            }

            if (!hasEscapes)
            {
                var result = sb.ToString();
                sb.Clear();
                return result;
            }

            return UnescapeString();
        }

        // Reads the contents after the opening """ and its newline. The string only ends at """
        // at the start of a line, and only when the newline is not preceded by a backslash.
        string ReadMultilineStringContents()
        {
            // Stands in for the newline after the opening """
            sb.Append('\n');

            while (true)
            {
                var next = Next();
                sb.Append(next);

                if (next == '"' && sb.Length >= 4 && sb[^2] == '"' && sb[^3] == '"' && sb[^4] == '\n' && (sb.Length == 4 || sb[^5] != '\\'))
                {
                    break;
                }
            }

            // Drop the closing """ and the newlines next to both quote markers
            sb.Length -= 3;
            sb.Replace("\r\n", "\n");

            var result = sb.ToString(1, Math.Max(0, sb.Length - 2)).Replace("\\\"\"\"", "\"\"\"", StringComparison.Ordinal);
            sb.Clear();
            return result;
        }

        string UnescapeString()
        {
            var length = sb.Length;

            if (length == 0)
            {
                return string.Empty;
            }

            // Unescape in-place by reading ahead and writing back
            var write = 0;

            for (var read = 0; read < length; read++)
            {
                var c = sb[read];

                if (c == '\\' && read + 1 < length)
                {
                    read++;
                    sb[write++] = sb[read] switch
                    {
                        'n' => '\n',
                        't' => '\t',
                        var x => x,
                    };
                }
                else
                {
                    sb[write++] = c;
                }
            }

            sb.Length = write;
            var result = sb.ToString();
            sb.Clear();
            return result;
        }
    }
}
