using System.Linq;
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

        public KV3TokenReader(TextReader textReader) : base(textReader)
        {
            // Dota 2 binary from 2017 used "+" as a terminate (for flagged values), but then they changed it to "|"
            var terminators = "{}[]=, \t\n\r'\":|;".ToCharArray();
            integerTerminators = new HashSet<int>(terminators.Select(t => (int)t));
        }

        readonly HashSet<int> integerTerminators;

        public KVToken ReadNextToken()
        {
            ObjectDisposedException.ThrowIf(disposed, this);
            SwallowWhitespace();

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

            var token = ReadToken();
            var type = KVTokenType.String;

            if (IsIdentifier(token))
            {
                type = KVTokenType.Identifier;

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

            var sb = new StringBuilder();

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

            return new KVToken(KVTokenType.BinaryBlob, sb.ToString());
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

        bool IsIdentifier(string text)
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

            var sb = new StringBuilder();

            while (true)
            {
                next = Peek();

                if (next <= ' ' || integerTerminators.Contains(next))
                {
                    break;
                }

                sb.Append(Next());
            }

            return sb.ToString();
        }

        string ReadQuotedStringRaw(char quotationMark)
        {
            ReadChar(quotationMark);

            var isMultiline = false;

            var sb = new StringBuilder();

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

            if (isMultiline)
            {
                while (true)
                {
                    var next = Next();

                    if (next == '"' && !IsEscaped(sb))
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

                    sb.Append(next);
                }

                // Strip trailing newline (\n or \r\n)
                if (sb.Length > 0 && sb[^1] == '\n')
                {
                    sb.Remove(sb.Length - 1, 1);

                    if (sb.Length > 0 && sb[^1] == '\r')
                    {
                        sb.Remove(sb.Length - 1, 1);
                    }
                }
            }
            else
            {
                while (true)
                {
                    var next = Next();

                    if (next == quotationMark && !IsEscaped(sb))
                    {
                        break;
                    }

                    sb.Append(next);
                }

                return UnescapeString(sb);
            }

            return sb.ToString();
        }

        static bool IsEscaped(StringBuilder sb)
        {
            var count = 0;

            for (var i = sb.Length - 1; i >= 0 && sb[i] == '\\'; i--)
            {
                count++;
            }

            return count % 2 == 1;
        }

        static string UnescapeString(StringBuilder input)
        {
            if (input.Length == 0)
            {
                return string.Empty;
            }

            var result = new StringBuilder(input.Length);
            var isEscaped = false;

            for (var i = 0; i < input.Length; i++)
            {
                var c = input[i];

                if (c == '\\' && !isEscaped)
                {
                    isEscaped = true;
                    continue;
                }

                if (isEscaped)
                {
                    switch (c)
                    {
                        case 'n':
                            result.Append('\n');
                            break;
                        case 't':
                            result.Append('\t');
                            break;
                        default:
                            result.Append(c);
                            break;
                    }

                    isEscaped = false;
                }
                else
                {
                    result.Append(c);
                }
            }

            return result.ToString();
        }
    }
}
