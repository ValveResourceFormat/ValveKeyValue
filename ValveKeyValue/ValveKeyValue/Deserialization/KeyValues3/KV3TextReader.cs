using System.Globalization;
using ValveKeyValue.Abstraction;

namespace ValveKeyValue.Deserialization.KeyValues3
{
    sealed class KV3TextReader : IVisitingReader
    {
        public KV3TextReader(TextReader textReader, IParsingVisitationListener listener, bool skipHeader = false)
        {
            ArgumentNullException.ThrowIfNull(textReader);
            ArgumentNullException.ThrowIfNull(listener);

            this.listener = listener;
            this.skipHeader = skipHeader;

            tokenReader = new KV3TokenReader(textReader);
            stateMachine = new KV3TextReaderStateMachine();
        }

#pragma warning disable CA2213 // Not owned by this class
        readonly IParsingVisitationListener listener;
#pragma warning restore CA2213

        readonly KV3TokenReader tokenReader;
        readonly KV3TextReaderStateMachine stateMachine;
        readonly bool skipHeader;
        bool disposed;

        public KVHeader ReadHeader()
        {
            ObjectDisposedException.ThrowIf(disposed, this);

            var header = skipHeader ? new KVHeader() : tokenReader.ReadHeader();

            while (stateMachine.IsInObject)
            {
                KVToken token;

                try
                {
                    token = tokenReader.ReadNextToken();
                }
                catch (InvalidDataException ex)
                {
                    throw new KeyValueException(ex.Message, ex);
                }
                catch (EndOfStreamException ex)
                {
                    throw new KeyValueException("Found end of file while trying to read token.", ex);
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
                        ReadText(token.Value!);
                        break;

                    case KVTokenType.BinaryBlob:
                        ReadBinaryBlob(token.Value!);
                        break;

                    case KVTokenType.ObjectStart:
                        BeginNewObject();
                        break;

                    case KVTokenType.ObjectEnd:
                        FinalizeCurrentObject(@explicit: true);
                        break;

                    case KVTokenType.ArrayStart:
                        BeginNewArray();
                        break;

                    case KVTokenType.ArrayEnd:
                        FinalizeCurrentArray();
                        break;

                    case KVTokenType.EndOfFile:
                        try
                        {
                            FinalizeDocument();
                        }
                        catch (InvalidOperationException ex)
                        {
                            throw new KeyValueException("Found end of file when another token type was expected.", ex);
                        }

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
                throw new InvalidOperationException($"Attempted to assign while in state {stateMachine.Current}.");
            }
        }

        void ReadComma()
        {
            if (stateMachine.Current != KV3TextReaderState.InArray)
            {
                throw new InvalidOperationException($"Attempted to have a comma character while in state {stateMachine.Current}.");
            }
        }

        void ReadFlag(string text)
        {
            if (stateMachine.Current != KV3TextReaderState.InArray && stateMachine.Current != KV3TextReaderState.InObjectAfterKey)
            {
                throw new InvalidOperationException($"Attempted to read flag while in state {stateMachine.Current}.");
            }

            var flag = ParseFlag(text);

            stateMachine.SetFlag(flag);
        }

        void ReadText(string text)
        {
            switch (stateMachine.Current)
            {
                case KV3TextReaderState.InArray:
                    {
                        var value = ParseValue(text);
                        value.Flag = stateMachine.GetAndResetFlag();
                        listener.OnArrayValue(value);
                        break;
                    }

                case KV3TextReaderState.InObjectBeforeKey:
                    SetObjectKey(text);
                    break;

                case KV3TextReaderState.InObjectAfterKey:
                    {
                        var name = stateMachine.CurrentName!;
                        var value = ParseValue(text);
                        value.Flag = stateMachine.GetAndResetFlag();
                        listener.OnKeyValuePair(name, value);

                        stateMachine.Push(KV3TextReaderState.InObjectBeforeKey);
                        break;
                    }

                default:
                    throw new InvalidOperationException($"Unhandled text reader state: {stateMachine.Current}.");
            }
        }

        void ReadBinaryBlob(string text)
        {
            var bytes = HexStringHelper.ParseHexStringAsByteArray(text);
            var value = KVObject.Blob(bytes);
            value.Flag = stateMachine.GetAndResetFlag();

            switch (stateMachine.Current)
            {
                case KV3TextReaderState.InArray:
                    {
                        listener.OnArrayValue(value);
                        break;
                    }

                case KV3TextReaderState.InObjectAfterKey:
                    {
                        var name = stateMachine.CurrentName!;
                        listener.OnKeyValuePair(name, value);

                        stateMachine.Push(KV3TextReaderState.InObjectBeforeKey);
                        break;
                    }

                default:
                    throw new InvalidOperationException($"Unhandled text reader state: {stateMachine.Current}.");
            }
        }

        void BeginNewArray()
        {
            if (stateMachine.Current != KV3TextReaderState.InArray && stateMachine.Current != KV3TextReaderState.InObjectAfterKey)
            {
                throw new InvalidOperationException($"Attempted to begin new array while in state {stateMachine.Current}.");
            }

            listener.OnArrayStart(stateMachine.CurrentName, stateMachine.GetAndResetFlag(), 0, false);

            stateMachine.PushObject();
            stateMachine.SetArrayCurrent();
            stateMachine.Push(KV3TextReaderState.InArray);
        }

        void FinalizeCurrentArray()
        {
            if (stateMachine.Current != KV3TextReaderState.InArray)
            {
                throw new InvalidOperationException($"Attempted to finalize array while in state {stateMachine.Current}.");
            }

            stateMachine.PopObject();

            if (stateMachine.IsInObject && !stateMachine.IsInArray)
            {
                stateMachine.Push(KV3TextReaderState.InObjectBeforeKey);
            }

            listener.OnArrayEnd();
        }

        void SetObjectKey(string name)
        {
            stateMachine.GetAndResetFlag();
            stateMachine.SetName(name);
            stateMachine.Push(KV3TextReaderState.InObjectAfterKey);
        }

        void BeginNewObject()
        {
            if (stateMachine.Current != KV3TextReaderState.InArray && stateMachine.Current != KV3TextReaderState.InObjectAfterKey)
            {
                throw new InvalidOperationException($"Attempted to begin new object while in state {stateMachine.Current}.");
            }

            listener.OnObjectStart(stateMachine.CurrentName, stateMachine.GetAndResetFlag());

            stateMachine.PushObject();
            stateMachine.Push(KV3TextReaderState.InObjectBeforeKey);
        }

        void FinalizeCurrentObject(bool @explicit)
        {
            if (stateMachine.Current != KV3TextReaderState.InObjectBeforeKey)
            {
                throw new InvalidOperationException($"Attempted to finalize object while in state {stateMachine.Current}.");
            }

            stateMachine.PopObject();

            if (stateMachine.IsInObject && !stateMachine.IsInArray)
            {
                stateMachine.Push(KV3TextReaderState.InObjectBeforeKey);
            }

            if (@explicit)
            {
                listener.OnObjectEnd();
            }
        }

        void FinalizeDocument()
        {
            FinalizeCurrentObject(@explicit: true);

            if (stateMachine.IsInObject)
            {
                throw new InvalidOperationException("Inconsistent state - at end of file whilst inside an object.");
            }
        }

        static KVObject ParseValue(string text)
        {
            if (text.Equals("false", StringComparison.Ordinal))
            {
                return new KVObject(false);
            }
            else if (text.Equals("true", StringComparison.Ordinal))
            {
                return new KVObject(true);
            }
            else if (text.Equals("null", StringComparison.Ordinal))
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
            else if (text.Length > 0 && ((text[0] >= '0' && text[0] <= '9') || text[0] == '-' || text[0] == '+'))
            {
                // TODO: Due to Valve's string to int/double conversion functions, it is possible to have 0x hex values (as well as prefixed with minus like -0x)

                const NumberStyles IntegerNumberStyles = NumberStyles.AllowLeadingSign;

                if (text[0] == '-' && long.TryParse(text, IntegerNumberStyles, CultureInfo.InvariantCulture, out var intValue))
                {
                    return new KVObject(intValue);
                }
                else if (ulong.TryParse(text, IntegerNumberStyles, CultureInfo.InvariantCulture, out var uintValue))
                {
                    return new KVObject(uintValue);
                }

                const NumberStyles FloatingPointNumberStyles =
                    NumberStyles.AllowDecimalPoint |
                    NumberStyles.AllowExponent |
                    NumberStyles.AllowLeadingSign;

                if (double.TryParse(text, FloatingPointNumberStyles, CultureInfo.InvariantCulture, out var floatValue))
                {
                    return new KVObject(floatValue);
                }
            }

            return new KVObject(text);
        }

        static KVFlag ParseFlag(string flag)
        {
            if (flag.Equals("resource", StringComparison.OrdinalIgnoreCase)) return KVFlag.Resource;
            if (flag.Equals("resource_name", StringComparison.OrdinalIgnoreCase)) return KVFlag.ResourceName;
            if (flag.Equals("panorama", StringComparison.OrdinalIgnoreCase)) return KVFlag.Panorama;
            if (flag.Equals("soundevent", StringComparison.OrdinalIgnoreCase)) return KVFlag.SoundEvent;
            if (flag.Equals("subclass", StringComparison.OrdinalIgnoreCase)) return KVFlag.SubClass;
            if (flag.Equals("entity_name", StringComparison.OrdinalIgnoreCase)) return KVFlag.EntityName;
            throw new InvalidDataException($"Unknown flag '{flag}'");
        }
    }
}
