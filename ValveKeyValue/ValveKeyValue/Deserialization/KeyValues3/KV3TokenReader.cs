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
            SwallowWhitespace();

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

            var type = IsIdentifier(token) ? KVTokenType.Identifier : KVTokenType.String;

            if (type == KVTokenType.Identifier)
            {
                var next = Peek();

                if (next == ':' || next == '|')
                {
                    Next();
                    type = KVTokenType.Flag;
                }
            }

            return new KVToken(type, token);
        }

        KVToken ReadBinaryBlob()
        {
            ReadChar(BinaryBlobMarker);
            ReadChar(ArrayStart); // TODO: Strictly speaking Valve allows bare # without [ to be read as literal value (but what would that be?)

            while (true)
            {
                var next = Next();

                if (char.IsWhiteSpace(next))
                {
                    continue;
                }

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

        public KVHeader ReadHeader()
        {
            var str = ReadToken();

            if (str != "<!--")
            {
                throw new InvalidDataException($"The header is incorrect, expected '<!--' but got '{str}'.");
            }

            SwallowWhitespace();
            str = ReadToken();

            if (!str.Equals("kv3", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException($"The header is incorrect, expected 'kv3' but got '{str}'.");
            }

            SwallowWhitespace();
            str = ReadToken();

            if (!str.Equals("encoding", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException($"The header is incorrect, expected 'encoding' but got '{str}'.");
            }

            ReadChar(':');
            var encodingType = ReadToken();
            ReadChar(':');

            str = ReadToken();

            if (!str.Equals("version", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException($"The header is incorrect, expected 'version' but got '{str}'.");
            }

            ReadChar('{');
            var encoding = new Guid(ReadToken());
            ReadChar('}');

            SwallowWhitespace();

            str = ReadToken();

            if (!str.Equals("format", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException($"The header is incorrect, expected 'format' but got '{str}'.");
            }

            ReadChar(':');
            var formatType = ReadToken();
            ReadChar(':');

            str = ReadToken();

            if (!str.Equals("version", StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidDataException($"The header is incorrect, expected 'version' but got '{str}'.");
            }

            ReadChar('{');
            var format = new Guid(ReadToken());
            ReadChar('}');

            SwallowWhitespace();

            str = ReadToken();

            if (str != "-->")
            {
                throw new InvalidDataException($"The header is incorrect, expected '-->' but got '{str}'.");
            }

            if (encodingType.Equals("text", StringComparison.OrdinalIgnoreCase) && encoding != Encoding.Text)
            {
                throw new InvalidDataException($"Unrecognized encoding version, expected '{Encoding.Text}' but got '{encoding}'.");
            }

            if (formatType.Equals("generic", StringComparison.OrdinalIgnoreCase) && format != Format.Generic)
            {
                throw new InvalidDataException($"Unrecognized format version, expected '{Format.Generic}' but got '{format}'.");
            }

            return new KVHeader
            {
                Encoding = new KV3ID(encodingType, encoding),
                Format = new KV3ID(formatType, format),
            };
        }

        KVToken ReadComment()
        {
            ReadChar(CommentBegin);

            var next = Next();

            if (next == '*')
            {
                while (true)
                {
                    if (IsEndOfFile(Peek()))
                    {
                        throw new InvalidDataException("Unterminated block comment.");
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
                while (true)
                {
                    var peek = Peek();

                    if (IsEndOfFile(peek) || peek == '\n')
                    {
                        break;
                    }

                    Next();
                }
            }
            else
            {
                throw new InvalidDataException($"The syntax is incorrect, expected comment but got '/{next}'.");
            }

            return new KVToken(KVTokenType.Comment);
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
