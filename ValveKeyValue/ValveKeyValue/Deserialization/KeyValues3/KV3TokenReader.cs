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
        static readonly SearchValues<char> TokenTerminators = SearchValues.Create("{}[]=, \t\n\r'\":|;");

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
            // a quoted token is always String (so "42" stays a String for the source map),
            // an unquoted identifier-shaped token is Identifier, and an unquoted token with
            // a trailing : or | is a Flag.
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

            var type = IsIdentifier(token) ? KVTokenType.Identifier : KVTokenType.String;

            if (type == KVTokenType.Identifier)
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
            SwallowWhitespaceAndComments();
            ReadChar(ArrayStart);

            while (true)
            {
                SwallowWhitespaceAndComments();

                var next = Next();

                if (next == ArrayEnd)
                {
                    break;
                }

                sb.Append(next);
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
            var encodingType = ReadHeaderToken();
            ExpectHeaderChar(':');
            ExpectHeaderToken("version");
            ExpectHeaderChar('{');
            var encoding = ReadVersionGuid();
            ExpectHeaderChar('}');

            ExpectHeaderToken("format");
            ExpectHeaderChar(':');
            var formatType = ReadHeaderToken();
            ExpectHeaderChar(':');
            ExpectHeaderToken("version");
            ExpectHeaderChar('{');
            var format = ReadVersionGuid();
            ExpectHeaderChar('}');

            ExpectHeaderToken("-->");

            if (encodingType.Equals("text", StringComparison.OrdinalIgnoreCase) && encoding != Encoding.Text)
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

        string ReadHeaderToken()
        {
            SwallowWhitespaceAndComments();
            return ReadToken();
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

        static bool IsIdentifier(string text)
        {
            for (var i = 0; i < text.Length; i++)
            {
                var c = text[i];

                if ((c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z'))
                {
                    continue;
                }

                if (c >= '0' && c <= '9')
                {
                    continue;
                }

                // TODO: Disallow : because it's a token terminator?
                if (c == '_' || c == ':' || c == '.')
                {
                    continue;
                }

                return false;
            }

            return true;
        }

        string ReadToken()
        {
            MarkTokenStart();

            var next = Peek();

            if (next == '"' || next == '\'')
            {
                return ReadQuotedStringRaw((char)next);
            }

            while (true)
            {
                next = Peek();

                if (next <= ' ' || TokenTerminators.Contains((char)next))
                {
                    break;
                }

                sb.Append(Next());
            }

            var result = sb.ToString();
            sb.Clear();
            return result;
        }

        string ReadQuotedStringRaw(char quotationMark)
        {
            ReadChar(quotationMark);

            var isMultiline = false;

            if (quotationMark == '"' && Peek() == '"')
            {
                Next();

                // If the next character is not another quote, it's an empty string
                if (Peek() == '"')
                {
                    isMultiline = true;

                    Next();

                    if (Peek() == '\r')
                    {
                        Next();
                    }

                    ReadChar('\n');
                }
                else
                {
                    return string.Empty;
                }
            }

            var escaped = false;

            if (isMultiline)
            {
                while (true)
                {
                    var next = Next();

                    if (next == '\\')
                    {
                        escaped = !escaped;
                        sb.Append(next);
                        continue;
                    }

                    if (next == '"' && !escaped)
                    {
                        // Check if this is the start of """
                        if (Peek() == '"')
                        {
                            Next();

                            if (Peek() == '"')
                            {
                                Next();
                                break;
                            }

                            // Only two quotes, append both
                            sb.Append(next);
                            sb.Append('"');
                            continue;
                        }
                    }

                    escaped = false;
                    sb.Append(next);
                }

                // Strip trailing newline (\n or \r\n)
                if (sb.Length > 0 && sb[^1] == '\n')
                {
                    sb.Length--;

                    if (sb.Length > 0 && sb[^1] == '\r')
                    {
                        sb.Length--;
                    }
                }

                var result = sb.ToString();
                sb.Clear();
                return result;
            }
            else
            {
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
