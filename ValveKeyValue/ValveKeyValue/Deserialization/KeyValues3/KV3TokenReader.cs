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
            Require.NotDisposed(nameof(KV3TokenReader), disposed);
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
                throw new InvalidDataException($"Unrecognized format specifier, expected '{Encoding.Text}' but got '{encoding}'.");
            }

            if (formatType.Equals("generic", StringComparison.OrdinalIgnoreCase) && format != Format.Generic)
            {
                throw new InvalidDataException($"Unrecognized encoding specifier, expected '{Format.Generic}' but got '{format}'.");
            }

            return new KVHeader
            {
                Encoding = encoding,
                Format = format,
            };
        }

        KVToken ReadComment()
        {
            ReadChar(CommentBegin);

            var sb = new StringBuilder();
            var next = Next();
            var isMultiline = false;

            if (next == '*')
            {
                isMultiline = true;
            }
            else if (next != CommentBegin)
            {
                // TODO: Return identifier?
                throw new InvalidDataException("The syntax is incorrect, or is it?");
            }

            if (isMultiline)
            {
                while (true)
                {
                    next = Next();

                    if (next == '*')
                    {
                        var nextNext = Peek();

                        if (nextNext == '/')
                        {
                            Next();
                            break;
                        }
                    }

                    sb.Append(next);
                }
            }
            else
            {
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
            }

            var text = sb.ToString();

            return new KVToken(KVTokenType.Comment, text);
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

            // Is there another quote mark?
            // TODO: Peek() for more than one character
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
                var escapeNext = false;

                // Scan until \n"""
                while (true)
                {
                    var next = Next();

                    if (next == '\\')
                    {
                        // TODO: Is valve keeping the \ character in the string?
                        escapeNext = true;
                    }

                    if (!escapeNext && next == '\n')
                    {
                        var a = Next();
                        var b = Next();
                        var c = Next();

                        if (a == '"' && b == '"' && c == '"')
                        {
                            break;
                        }

                        sb.Append(next);
                        sb.Append(a);
                        sb.Append(b);
                        sb.Append(c);
                    }
                    else
                    {
                        escapeNext = false;

                        sb.Append(next);
                    }
                }

                if (sb.Length > 0 && sb[^1] == '\r')
                {
                    sb.Remove(sb.Length - 1, 1);
                }
            }
            else
            {
                // TODO: Figure out '\' character escapes, does Valve actually unescape anything?
                while (Peek() != quotationMark)
                {
                    var next = Next();
                    sb.Append(next);
                }

                ReadChar(quotationMark);
            }

            return sb.ToString();
        }
    }
}
