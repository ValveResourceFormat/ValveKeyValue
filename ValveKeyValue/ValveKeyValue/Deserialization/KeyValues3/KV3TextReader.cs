using System.Buffers;
using System.Globalization;
using ValveKeyValue.Abstraction;

namespace ValveKeyValue.Deserialization.KeyValues3
{
    sealed class KV3TextReader : IVisitingReader
    {
        public KV3TextReader(TextReader textReader, IParsingVisitationListener listener, bool skipHeader = false, List<KvSourceSpan>? sourceMap = null)
        {
            ArgumentNullException.ThrowIfNull(textReader);
            ArgumentNullException.ThrowIfNull(listener);

            this.listener = listener;
            this.skipHeader = skipHeader;
            this.sourceMap = sourceMap;

            tokenReader = new KV3TokenReader(textReader);
            stateMachine = new KV3TextReaderStateMachine();
        }

#pragma warning disable CA2213 // Not owned by this class
        readonly IParsingVisitationListener listener;
#pragma warning restore CA2213

        readonly KV3TokenReader tokenReader;
        readonly KV3TextReaderStateMachine stateMachine;
        readonly bool skipHeader;
        readonly List<KvSourceSpan>? sourceMap;
        bool disposed;

        public KVHeader ReadHeader()
        {
            ObjectDisposedException.ThrowIf(disposed, this);

            var headerStart = tokenReader.CharOffset;
            KVHeader header;

            try
            {
                header = skipHeader ? new KVHeader() : tokenReader.ReadHeader();
            }
            catch (EndOfStreamException ex)
            {
                throw new KeyValueException("Found end of file while reading the KV3 header.", ex);
            }

            if (sourceMap != null && !skipHeader)
            {
                sourceMap.Add(new KvSourceSpan(headerStart, tokenReader.CharOffset, KVTokenType.Header));
            }

            while (stateMachine.IsInObject)
            {
                KVToken token;

                try
                {
                    token = tokenReader.ReadNextToken();
                }
                catch (EndOfStreamException ex)
                {
                    throw new KeyValueException($"Found end of file while trying to read the token that started at {tokenReader.TokenStartPosition}.", ex);
                }

                if (sourceMap != null && token.TokenType != KVTokenType.EndOfFile)
                {
                    // Strings and identifiers in key position are recorded as KVTokenType.Key.
                    // The lexer cannot make that distinction; the parser knows from the state.
                    var resolved = (token.TokenType == KVTokenType.String || token.TokenType == KVTokenType.Identifier)
                        && stateMachine.Current == KV3TextReaderState.InObjectBeforeKey
                        ? KVTokenType.Key
                        : token.TokenType;
                    sourceMap.Add(new KvSourceSpan(tokenReader.LastTokenStart, tokenReader.LastTokenEnd, resolved));
                }

                switch (token.TokenType)
                {
                    case KVTokenType.Assignment:
                        ReadAssignment();
                        break;

                    case KVTokenType.Comma:
                        ReadComma();
                        break;

                    case KVTokenType.Flag:
                        ReadFlag(token.Value!);
                        break;

                    case KVTokenType.Identifier:
                    case KVTokenType.String:
                        ReadText(token.Value!, isQuoted: token.TokenType == KVTokenType.String);
                        break;

                    case KVTokenType.BinaryBlob:
                        ReadBinaryBlob(token.Value!);
                        break;

                    case KVTokenType.ObjectStart:
                        BeginNewObject();
                        break;

                    case KVTokenType.ObjectEnd:
                        // A '}' at document level after the root value would otherwise pop the
                        // document pseudo-object and silently ignore all remaining data.
                        ThrowIfAfterRootValue();
                        FinalizeCurrentObject(@explicit: true);
                        break;

                    case KVTokenType.ArrayStart:
                        BeginNewArray();
                        break;

                    case KVTokenType.ArrayEnd:
                        ThrowIfAfterRootValue();
                        FinalizeCurrentArray();
                        break;

                    case KVTokenType.EndOfFile:
                        FinalizeDocument();
                        break;

                    case KVTokenType.Comment:
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(token.TokenType), token.TokenType, "Unhandled token type.");
                }
            }

            return header;
        }

        public void Dispose()
        {
            if (!disposed)
            {
                tokenReader.Dispose();
                disposed = true;
            }
        }

        void ReadAssignment()
        {
            if (stateMachine.Current != KV3TextReaderState.InObjectAfterKey)
            {
                throw new KeyValueException($"Attempted to assign while in state {stateMachine.Current} at {tokenReader.TokenStartPosition}.");
            }

            stateMachine.Set(KV3TextReaderState.InObjectBeforeValue);
        }

        // Array elements must be separated by a comma, and a trailing comma is allowed.
        void ReadComma()
        {
            if (stateMachine.Current != KV3TextReaderState.InArrayAfterValue)
            {
                throw new KeyValueException($"Attempted to have a comma character while in state {stateMachine.Current} at {tokenReader.TokenStartPosition}.");
            }

            stateMachine.Set(KV3TextReaderState.InArray);
        }

        // A value may only start as an array element or after "key =".
        void ThrowIfNotExpectingValue(string what)
        {
            if (stateMachine.Current is KV3TextReaderState.InArray or KV3TextReaderState.InObjectBeforeValue)
            {
                return;
            }

            ThrowIfAfterRootValue();

            var message = stateMachine.Current switch
            {
                KV3TextReaderState.InObjectAfterKey => $"Expected '=' after key '{stateMachine.CurrentName}'",
                KV3TextReaderState.InArrayAfterValue => "Expected ',' or ']' between array elements",
                _ => $"Attempted to read {what} while in state {stateMachine.Current}",
            };

            throw new KeyValueException($"{message} at {tokenReader.TokenStartPosition}.");
        }

        void AddValue(KVObject value)
        {
            if (stateMachine.IsInArray)
            {
                listener.OnArrayValue(value);
            }
            else
            {
                listener.OnKeyValuePair(stateMachine.CurrentName!, value);
            }

            SetStateAfterValue();
        }

        // A finished value, including an array or object, is followed by a comma in an array or by the next key.
        void SetStateAfterValue()
        {
            if (stateMachine.IsInObject)
            {
                stateMachine.Set(stateMachine.IsInArray ? KV3TextReaderState.InArrayAfterValue : KV3TextReaderState.InObjectBeforeKey);
            }
        }

        void ReadFlag(string text)
        {
            ThrowIfNotExpectingValue("flag");

            var flag = ParseFlag(text) ?? throw new KeyValueException($"Unknown flag '{text}' at {tokenReader.TokenStartPosition}.");

            stateMachine.SetFlag(flag);
        }

        // The document pseudo-object is only ever in InObjectBeforeKey once the root value
        // has been fully read, so any further data-bearing token at that point is trailing garbage.
        void ThrowIfAfterRootValue()
        {
            if (stateMachine.IsAtDocumentLevel && stateMachine.Current == KV3TextReaderState.InObjectBeforeKey)
            {
                throw new KeyValueException($"Found data after the root value at {tokenReader.TokenStartPosition}, documents with multiple root values are not supported.");
            }
        }

        void ReadText(string text, bool isQuoted)
        {
            ThrowIfAfterRootValue();

            if (stateMachine.Current == KV3TextReaderState.InObjectBeforeKey)
            {
                if (!isQuoted && !KV3TokenReader.IsIdentifier(text))
                {
                    throw new KeyValueException($"Invalid key '{text}' at {tokenReader.TokenStartPosition}, keys that are not identifiers must be quoted.");
                }

                SetObjectKey(text);
                return;
            }

            ThrowIfNotExpectingValue("value");

            KVObject value;

            if (isQuoted)
            {
                value = new KVObject(text);
            }
            else
            {
                value = ParseValue(text) ?? throw new KeyValueException($"Invalid value '{text}' at {tokenReader.TokenStartPosition}, strings must be quoted.");
            }

            value.Flag = stateMachine.GetAndResetFlag();
            AddValue(value);
        }

        void ReadBinaryBlob(string text)
        {
            ThrowIfNotExpectingValue("binary blob");

            if (!HexStringHelper.TryParseHexStringAsByteArray(text, out var bytes))
            {
                throw new KeyValueException($"Invalid binary blob at {tokenReader.TokenStartPosition}, expected pairs of hexadecimal digits.");
            }

            var value = KVObject.Blob(bytes);
            value.Flag = stateMachine.GetAndResetFlag();
            AddValue(value);
        }

        void BeginNewArray()
        {
            ThrowIfNotExpectingValue("array");

            listener.OnArrayStart(stateMachine.CurrentName, stateMachine.GetAndResetFlag(), 0, false);

            stateMachine.PushObject();
            stateMachine.SetArrayCurrent();
            stateMachine.Set(KV3TextReaderState.InArray);
        }

        void FinalizeCurrentArray()
        {
            if (!stateMachine.IsInArray)
            {
                throw new KeyValueException($"Attempted to finalize array while in state {stateMachine.Current} at {tokenReader.TokenStartPosition}.");
            }

            stateMachine.PopObject();
            SetStateAfterValue();

            listener.OnArrayEnd();
        }

        void SetObjectKey(string name)
        {
            stateMachine.GetAndResetFlag();
            stateMachine.SetName(name);
            stateMachine.Set(KV3TextReaderState.InObjectAfterKey);
        }

        void BeginNewObject()
        {
            ThrowIfNotExpectingValue("object");

            listener.OnObjectStart(stateMachine.CurrentName, stateMachine.GetAndResetFlag());

            stateMachine.PushObject();
            stateMachine.Set(KV3TextReaderState.InObjectBeforeKey);
        }

        void FinalizeCurrentObject(bool @explicit)
        {
            if (stateMachine.Current != KV3TextReaderState.InObjectBeforeKey)
            {
                throw new KeyValueException($"Attempted to finalize object while in state {stateMachine.Current} at {tokenReader.TokenStartPosition}.");
            }

            stateMachine.PopObject();
            SetStateAfterValue();

            if (@explicit)
            {
                listener.OnObjectEnd();
            }
        }

        void FinalizeDocument()
        {
            if (!stateMachine.IsAtDocumentLevel || stateMachine.Current != KV3TextReaderState.InObjectBeforeKey)
            {
                throw new KeyValueException($"Found end of file when another token type was expected at {tokenReader.TokenStartPosition}.");
            }

            FinalizeCurrentObject(@explicit: true);
        }

        static readonly SearchValues<char> FloatCharacters = SearchValues.Create("+-.0123456789Ee");

        // Parses an unquoted value, returns null when it is not a valid literal.
        static KVObject? ParseValue(string text)
        {
            if (text.Equals("false", StringComparison.OrdinalIgnoreCase))
            {
                return new KVObject(false);
            }
            else if (text.Equals("true", StringComparison.OrdinalIgnoreCase))
            {
                return new KVObject(true);
            }
            else if (text.Equals("null", StringComparison.OrdinalIgnoreCase))
            {
                return KVObject.Null();
            }
            else if (text.Equals("nan", StringComparison.OrdinalIgnoreCase))
            {
                return new KVObject(double.NaN);
            }
            else if (text.Equals("inf", StringComparison.OrdinalIgnoreCase) || text.Equals("+inf", StringComparison.OrdinalIgnoreCase))
            {
                return new KVObject(double.PositiveInfinity);
            }
            else if (text.Equals("-inf", StringComparison.OrdinalIgnoreCase))
            {
                return new KVObject(double.NegativeInfinity);
            }

            // A lone plus sign is read as zero
            if (text == "+")
            {
                return new KVObject(0UL);
            }

            const NumberStyles IntegerNumberStyles = NumberStyles.AllowLeadingSign;

            if (text[0] == '-' && long.TryParse(text, IntegerNumberStyles, CultureInfo.InvariantCulture, out var intValue))
            {
                return new KVObject(intValue);
            }
            else if (ulong.TryParse(text, IntegerNumberStyles, CultureInfo.InvariantCulture, out var uintValue))
            {
                return new KVObject(uintValue);
            }

            if (text.AsSpan().ContainsAnyExcept(FloatCharacters))
            {
                return null;
            }

            const NumberStyles FloatingPointNumberStyles =
                NumberStyles.AllowDecimalPoint |
                NumberStyles.AllowExponent |
                NumberStyles.AllowLeadingSign;

            if (double.TryParse(text, FloatingPointNumberStyles, CultureInfo.InvariantCulture, out var floatValue))
            {
                return new KVObject(floatValue);
            }

            return null;
        }

        static KVFlag? ParseFlag(string flag)
        {
            if (flag.Equals("resource", StringComparison.OrdinalIgnoreCase)) return KVFlag.Resource;
            if (flag.Equals("resource_name", StringComparison.OrdinalIgnoreCase)) return KVFlag.ResourceName;
            if (flag.Equals("panorama", StringComparison.OrdinalIgnoreCase)) return KVFlag.Panorama;
            if (flag.Equals("soundevent", StringComparison.OrdinalIgnoreCase)) return KVFlag.SoundEvent;
            if (flag.Equals("subclass", StringComparison.OrdinalIgnoreCase)) return KVFlag.SubClass;
            if (flag.Equals("entity_name", StringComparison.OrdinalIgnoreCase)) return KVFlag.EntityName;
            if (flag.Equals("localize", StringComparison.OrdinalIgnoreCase)) return KVFlag.Localize;
            return null;
        }
    }
}
