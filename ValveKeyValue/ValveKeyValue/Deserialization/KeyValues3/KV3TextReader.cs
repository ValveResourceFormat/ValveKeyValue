using System;
using System.Globalization;
using System.IO;
using ValveKeyValue.Abstraction;

namespace ValveKeyValue.Deserialization.KeyValues3
{
    sealed class KV3TextReader : IVisitingReader
    {
        public KV3TextReader(TextReader textReader, IParsingVisitationListener listener)
        {
            Require.NotNull(textReader, nameof(textReader));
            Require.NotNull(listener, nameof(listener));

            this.listener = listener;

            tokenReader = new KV3TokenReader(textReader);
            stateMachine = new KV3TextReaderStateMachine();
        }

        readonly IParsingVisitationListener listener;

        readonly KV3TokenReader tokenReader;
        readonly KV3TextReaderStateMachine stateMachine;
        bool disposed;

        public void ReadObject()
        {
            Require.NotDisposed(nameof(KV3TextReader), disposed);

            tokenReader.ReadHeader();

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
                        ReadFlag(token.Value);
                        break;

                    case KVTokenType.Identifier:
                        ReadText(token.Value);
                        break;

                    case KVTokenType.String:
                        ReadText(token.Value);
                        break;

                    case KVTokenType.BinaryBlob:
                        ReadBinaryBlob(token.Value);
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
                        var name = stateMachine.CurrentName;
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
            var bytes = Utils.ParseHexStringAsByteArray(text);
            //var value = new KVObjectValue<byte[]>(bytes, KVValueType.BinaryBlob);
            var value = new KVObjectValue<byte>(0x00, KVValueType.BinaryBlob); // TODO: wrong
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
                        var name = stateMachine.CurrentName;
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

            listener.OnArrayStart(stateMachine.CurrentName, stateMachine.GetAndResetFlag());

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

        static KVValue ParseValue(string text)
        {
            if (text.Equals("false", StringComparison.Ordinal))
            {
                return new KVObjectValue<bool>(false, KVValueType.Boolean);
            }
            else if (text.Equals("true", StringComparison.Ordinal))
            {
                return new KVObjectValue<bool>(true, KVValueType.Boolean);
            }
            else if (text.Equals("null", StringComparison.Ordinal))
            {
                // TODO: Null is not a string
                // TODO: KVObjectValue does not accept null
                //value = new KVObjectValue<string>(null, KVValueType.Null);
                return new KVObjectValue<string>(string.Empty, KVValueType.Null);
            }
            else if (text.Length > 0 && ((text[0] >= '0' && text[0] <= '9') || text[0] == '-' || text[0] == '+'))
            {
                // TODO: Due to Valve's string to int/double conversion functions, it is possible to have 0x hex values (as well as prefixed with minus like -0x)

                const NumberStyles IntegerNumberStyles = NumberStyles.AllowLeadingSign;

                if (text[0] == '-' && long.TryParse(text, IntegerNumberStyles, CultureInfo.InvariantCulture, out var intValue))
                {
                    return new KVObjectValue<long>(intValue, KVValueType.Int64);
                }
                else if (ulong.TryParse(text, IntegerNumberStyles, CultureInfo.InvariantCulture, out var uintValue))
                {
                    return new KVObjectValue<ulong>(uintValue, KVValueType.UInt64);
                }

                const NumberStyles FloatingPointNumberStyles =
                    NumberStyles.AllowDecimalPoint |
                    NumberStyles.AllowExponent |
                    NumberStyles.AllowLeadingSign;

                // TODO: 
                if (double.TryParse(text, FloatingPointNumberStyles, CultureInfo.InvariantCulture, out var floatValue))
                {
                    return new KVObjectValue<double>(floatValue, KVValueType.FloatingPoint);
                }
            }

            return new KVObjectValue<string>(text, KVValueType.String);
        }

        static byte ParseHexCharacter(string hexadecimalRepresentation)
        {
            if (hexadecimalRepresentation.Length != 2)
            {
                throw new InvalidDataException("Expected hex byte (eg. 00-FF)");
            }

            return byte.Parse(hexadecimalRepresentation, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
        }

        static KVFlag ParseFlag(string flag)
        {
            return flag.ToLowerInvariant() switch
            {
                "resource" => KVFlag.Resource,
                "resource_name" => KVFlag.ResourceName,
                "panorama" => KVFlag.Panorama,
                "soundevent" => KVFlag.SoundEvent,
                "subclass" => KVFlag.SubClass,
                _ => throw new InvalidDataException($"Unknown flag '{flag}'"),
            };
        }
    }
}
