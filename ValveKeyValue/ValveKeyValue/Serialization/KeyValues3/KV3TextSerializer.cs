using System.Buffers;
using System.Globalization;
using System.Text;
using ValveKeyValue.Abstraction;

namespace ValveKeyValue.Serialization.KeyValues3
{
    sealed class KV3TextSerializer : KVTextSerializerBase, IVisitationListener
    {
        static readonly SearchValues<char> CharsToEscape = SearchValues.Create("\n\t\\\"");

        public KV3TextSerializer(Stream stream, KVHeader? header = null, bool skipHeader = false)
            : base(stream)
        {
            WriteHeaderIfNeeded(header, skipHeader);
        }

        public KV3TextSerializer(StringBuilder sb, List<KvSourceSpan> sourceMap, KVHeader? header = null, bool skipHeader = false)
            : base(sb, sourceMap)
        {
            WriteHeaderIfNeeded(header, skipHeader);
        }

        void WriteHeaderIfNeeded(KVHeader? header, bool skipHeader)
        {
            if (skipHeader)
            {
                return;
            }

            var defaultEncoding = new ValveKeyValue.KeyValues3.KV3ID("text", ValveKeyValue.KeyValues3.Encoding.Text);
            var defaultFormat = new ValveKeyValue.KeyValues3.KV3ID("generic", ValveKeyValue.KeyValues3.Format.Generic);

            var encoding = header?.Encoding.Name != null ? header.Encoding : defaultEncoding;
            var format = header?.Format.Name != null ? header.Format : defaultFormat;

            var s = Position;
            writer.Write($"<!-- kv3 encoding:{encoding} format:{format} -->");
            Record(s, KVTokenType.Header);
            writer.WriteLine();
        }

        // Tracks nesting: null for objects, tuple for arrays
        readonly Stack<(bool isShort, bool allSimple, int index, int count)?> context = new();

        bool IsInArray => context.Count > 0 && context.Peek() != null;

        public void OnObjectStart(string? name, KVFlag flag)
        {
            context.Push(null);

            WriteStartObject(name, flag);
        }

        public void OnObjectEnd()
        {
            context.Pop();

            WriteEndObject();
        }

        public void OnKeyValuePair(string name, KVObject value)
            => WriteKeyValuePair(name, value);

        public void OnArrayStart(string? name, KVFlag flag, int elementCount, bool allSimpleElements)
        {
            var isShort = elementCount <= 4 && allSimpleElements;
            context.Push((isShort, allSimpleElements, 0, elementCount));

            WriteIndentation();

            WriteKey(name);
            WriteFlag(flag);

            if (isShort)
            {
                Record(KVTokenType.ArrayStart, '[');
                writer.Write(' ');
            }
            else
            {
                // After "key = " or "key = flag:", put bracket on next line.
                // Also for flagged array elements.
                if (name != null || flag != KVFlag.None)
                {
                    writer.WriteLine();
                    WriteIndentation();
                }

                Record(KVTokenType.ArrayStart, '[');
                indentation++;
                writer.WriteLine();
            }
        }

        public void OnArrayValue(KVObject value)
        {
            var (isShort, allSimple, index, count) = context.Pop()!.Value;
            var isLast = index == count - 1;

            // Push back before WriteValue so nested code can see the array context
            context.Push((isShort, allSimple, index + 1, count));

            if (isShort)
            {
                WriteValue(value);

                if (!isLast)
                    writer.Write(", ");
            }
            else if (allSimple)
            {
                // Group 4 simple values per line
                if (index % 4 == 0)
                    WriteIndentation();

                WriteValue(value);
                writer.Write(',');

                if (!isLast && (index + 1) % 4 != 0)
                    writer.Write(' ');
                else
                    writer.WriteLine();
            }
            else
            {
                WriteIndentation();
                WriteValue(value);
                writer.Write(',');
                writer.WriteLine();
            }
        }

        public void OnArrayEnd()
        {
            var (isShort, _, _, _) = context.Pop()!.Value;

            if (isShort)
            {
                writer.Write(' ');
                Record(KVTokenType.ArrayEnd, ']');
            }
            else
            {
                indentation--;
                WriteIndentation();
                Record(KVTokenType.ArrayEnd, ']');
            }

            if (IsInArray)
            {
                writer.Write(',');
            }

            writer.WriteLine();
        }

        public void DiscardCurrentObject()
        {
            throw new NotSupportedException("Discard not supported when writing.");
        }

        void WriteStartObject(string? name, KVFlag flag)
        {
            WriteIndentation();

            if (indentation > 0)
            {
                WriteKey(name);
            }

            WriteFlag(flag);

            // After "key = " or "key = flag:", put bracket on next line.
            // Also for flagged object elements.
            if ((name != null || flag != KVFlag.None) && indentation > 0)
            {
                writer.WriteLine();
                WriteIndentation();
            }

            Record(KVTokenType.ObjectStart, '{');
            indentation++;
            WriteLine();
        }

        void WriteEndObject()
        {
            indentation--;
            WriteIndentation();
            Record(KVTokenType.ObjectEnd, '}');

            if (IsInArray)
            {
                writer.Write(',');
            }

            writer.WriteLine();
        }

        void WriteKeyValuePair(string name, KVObject value)
        {
            WriteIndentation();

            WriteKey(name);

            WriteValue(value);

            WriteLine();
        }

        void WriteValue(KVObject value)
        {
            WriteFlag(value.Flag);

            var s = Position;
            var type = KVTokenType.Identifier;

            switch (value.ValueType)
            {
                case KVValueType.BinaryBlob:
                    WriteBinaryBlob(value);
                    type = KVTokenType.BinaryBlob;
                    break;
                case KVValueType.Boolean:
                    if ((bool)value)
                    {
                        writer.Write("true");
                    }
                    else
                    {
                        writer.Write("false");
                    }
                    break;
                case KVValueType.Null:
                    writer.Write("null");
                    break;
                case KVValueType.FloatingPoint:
                    WriteFloat(value.ToSingle(CultureInfo.InvariantCulture));
                    break;
                case KVValueType.FloatingPoint64:
                    WriteFloat(value.ToDouble(CultureInfo.InvariantCulture));
                    break;
                case KVValueType.Int16:
                case KVValueType.Int32:
                case KVValueType.Int64:
                case KVValueType.UInt16:
                case KVValueType.UInt32:
                case KVValueType.UInt64:
                case KVValueType.Pointer:
                    writer.Write(value.ToString(null));
                    break;
                default:
                    WriteText(value.ToString(null));
                    type = KVTokenType.String;
                    break;
            }

            Record(s, type);
        }

        void WriteFloat(float value)
        {
            if (float.IsNaN(value))
            {
                writer.Write("nan");
            }
            else if (float.IsPositiveInfinity(value))
            {
                writer.Write("inf");
            }
            else if (float.IsNegativeInfinity(value))
            {
                writer.Write("-inf");
            }
            else
            {
                WriteFloatFormatted(value);
            }
        }

        void WriteFloat(double value)
        {
            if (double.IsNaN(value))
            {
                writer.Write("nan");
            }
            else if (double.IsPositiveInfinity(value))
            {
                writer.Write("inf");
            }
            else if (double.IsNegativeInfinity(value))
            {
                writer.Write("-inf");
            }
            else
            {
                WriteFloatFormatted(value);
            }
        }

        // Matches Valve's %f (6 decimal places) + V_StripExcessTrailingZeros.
        // "0.0#####" can't be used here because it rounds differently than %f for edge cases.
        void WriteFloatFormatted(float value) => WriteFloatTrimmed(value.ToString("F6", CultureInfo.InvariantCulture));
        void WriteFloatFormatted(double value) => WriteFloatTrimmed(value.ToString("F6", CultureInfo.InvariantCulture));

        void WriteFloatTrimmed(string formatted)
        {
            // Strip trailing zeros but keep at least one digit after the decimal point
            var span = formatted.AsSpan().TrimEnd('0');
            if (span[^1] == '.')
                writer.Write(formatted.AsSpan(0, span.Length + 1));
            else
                writer.Write(span);
        }

        void WriteBinaryBlob(KVObject value)
        {
            var bytes = value.AsBlob();

            if (bytes.Length <= 32)
            {
                // Small and empty blobs are written inline: "#[ XX XX ]" or "#[  ]"
                writer.Write("#[ ");

                for (var i = 0; i < bytes.Length; i++)
                {
                    var b = bytes[i];
                    WriteHexByte(b);

                    if (i < bytes.Length - 1)
                        writer.Write(' ');
                }

                writer.Write(" ]");
            }
            else
            {
                // Large blobs are written multiline with 32 bytes per line.
                // In array context, the caller already wrote indentation, so just open inline.
                // In object context, put #[ on a new indented line.
                if (IsInArray)
                {
                    writer.Write("#[ ");
                    writer.WriteLine();
                }
                else
                {
                    writer.WriteLine();
                    WriteIndentation();
                    writer.Write('#');
                    writer.Write('[');
                    writer.WriteLine();
                }
                indentation++;
                WriteIndentation();

                for (var i = 0; i < bytes.Length - 1; i++)
                {
                    var b = bytes[i];
                    WriteHexByte(b);

                    if ((i + 1) % 32 == 0)
                    {
                        writer.WriteLine();
                        WriteIndentation();
                    }
                    else
                    {
                        writer.Write(' ');
                    }
                }

                WriteHexByte(bytes[bytes.Length - 1]);

                indentation--;

                writer.WriteLine();
                WriteIndentation();
                writer.Write(']');
            }
        }

        void WriteHexByte(byte b)
        {
            writer.Write(HexStringHelper.HexToCharUpper(b >> 4));
            writer.Write(HexStringHelper.HexToCharUpper(b));
        }

        void WriteText(string text)
        {
            if (text.Contains('\n', StringComparison.Ordinal))
            {
                text = text.Replace("\r\n", "\n", StringComparison.Ordinal);
                text = text.Replace("\"\"\"", "\\\"\"\"", StringComparison.Ordinal);

                writer.Write("\"\"\"\n");
                writer.Write(text);
                writer.Write("\n\"\"\"");
            }
            else if (!text.AsSpan().ContainsAny(CharsToEscape))
            {
                writer.Write('"');
                writer.Write(text);
                writer.Write('"');
            }
            else
            {
                writer.Write('"');

                foreach (var @char in text)
                {
                    switch (@char)
                    {
                        case '\t':
                            writer.Write("\\t");
                            break;

                        case '\\':
                            writer.Write("\\\\");
                            break;

                        case '"':
                            writer.Write("\\\"");
                            break;

                        default:
                            writer.Write(@char);
                            break;
                    }
                }

                writer.Write('"');
            }
        }

        void WriteKey(string? key)
        {
            if (key == null)
            {
                return;
            }

            var s = Position;

            if (key.Length > 0 && !char.IsAsciiDigit(key[0]) && !NeedsQuoting(key))
            {
                writer.Write(key);
            }
            else
            {
                writer.Write('"');

                foreach (var @char in key)
                {
                    switch (@char)
                    {
                        case '\t':
                            writer.Write("\\t");
                            break;

                        case '\n':
                            writer.Write("\\n");
                            break;

                        case '\'':
                            writer.Write("\\'");
                            break;

                        case '"':
                            writer.Write("\\\"");
                            break;

                        case '\\':
                            writer.Write("\\\\");
                            break;

                        default:
                            writer.Write(@char);
                            break;
                    }
                }

                writer.Write('"');
            }

            Record(s, KVTokenType.Key);

            writer.Write(' ');
            Record(KVTokenType.Assignment, '=');
            writer.Write(' ');
        }

        static bool NeedsQuoting(string key)
        {
            foreach (var c in key)
            {
                if (c != '.' && c != '_' && !char.IsAsciiLetterOrDigit(c))
                {
                    return true;
                }
            }

            return false;
        }

        void WriteFlag(KVFlag kvFlag)
        {
            if (kvFlag == KVFlag.None)
            {
                return;
            }

            var name = SerializeFlagName(kvFlag);

            if (name != null)
            {
                var s = Position;
                writer.Write(name);
                writer.Write(':');
                Record(s, KVTokenType.Flag);
            }
        }

        void WriteLine()
        {
            writer.WriteLine();
        }

        static string? SerializeFlagName(KVFlag flag)
        {
            return flag switch
            {
                KVFlag.Resource => "resource",
                KVFlag.ResourceName => "resource_name",
                KVFlag.Panorama => "panorama",
                KVFlag.SoundEvent => "soundevent",
                KVFlag.SubClass => "subclass",
                KVFlag.EntityName => "entity_name",
                _ => null,
            };
        }
    }
}
