using System;
using System.Globalization;
using System.IO;
using ValveKeyValue.Abstraction;
using ValveKeyValue.KeyValues3;

namespace ValveKeyValue.Deserialization.KeyValues3
{
    sealed class KV3TextReader : IVisitingReader
    {
        public KV3TextReader(TextReader textReader, IParsingVisitationListener listener, KVSerializerOptions options)
        {
            Require.NotNull(textReader, nameof(textReader));
            Require.NotNull(listener, nameof(listener));
            Require.NotNull(options, nameof(options));

            this.listener = listener;
            this.options = options;

            tokenReader = new KV3TokenReader(textReader, options);
            stateMachine = new KV3TextReaderStateMachine();
        }

        readonly IParsingVisitationListener listener;
        readonly KVSerializerOptions options;

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
                        ReadIdentifier(token.Value);
                        break;

                    case KVTokenType.String:
                        ReadText(token.Value);
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

            stateMachine.Push(KV3TextReaderState.InObjectBeforeValue);
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
            if (stateMachine.Current != KV3TextReaderState.InArray && stateMachine.Current != KV3TextReaderState.InObjectBeforeValue)
            {
                throw new InvalidOperationException($"Attempted to read flag while in state {stateMachine.Current}.");
            }

            var flag = ParseFlag(text);
        }

        void ReadIdentifier(string text)
        {
            switch (stateMachine.Current)
            {
                case KV3TextReaderState.InArray:
                    new KVObjectValue<string>(text, KVValueType.String);
                    break;

                // If we're after a value when we find more text, then we must be starting a new key/value pair.
                case KV3TextReaderState.InObjectAfterValue:
                    FinalizeCurrentObject(@explicit: false);
                    stateMachine.PushObject();
                    SetObjectKey(text);
                    break;

                case KV3TextReaderState.InObjectBeforeKey:
                    SetObjectKey(text);
                    break;

                case KV3TextReaderState.InObjectBeforeValue:
                    KVValue value = ParseValue(text);

                    if (value != null)
                    {
                        var name = stateMachine.CurrentName;
                        listener.OnKeyValuePair(name, value);

                        stateMachine.Push(KV3TextReaderState.InObjectAfterValue);
                    }

                    break;

                default:
                    throw new InvalidOperationException($"Unhandled identifier reader state: {stateMachine.Current}.");
            }
        }

        void ReadText(string text)
        {
            switch (stateMachine.Current)
            {
                case KV3TextReaderState.InArray:
                    {
                        var value = ParseValue(text);
                        listener.OnArrayValue(value);
                        break;
                    }

                case KV3TextReaderState.InObjectAfterValue:
                    FinalizeCurrentObject(@explicit: false);
                    stateMachine.PushObject();
                    SetObjectKey(text);
                    break;

                case KV3TextReaderState.InObjectBeforeKey:
                    SetObjectKey(text);
                    break;

                case KV3TextReaderState.InObjectBeforeValue:
                    {
                        var name = stateMachine.CurrentName;
                        var value = ParseValue(text);
                        listener.OnKeyValuePair(name, value);

                        stateMachine.Push(KV3TextReaderState.InObjectAfterValue);
                        break;
                    }

                default:
                    throw new InvalidOperationException($"Unhandled text reader state: {stateMachine.Current}.");
            }
        }

        void BeginNewArray()
        {
            if (stateMachine.Current != KV3TextReaderState.InObjectBeforeValue)
            {
                throw new InvalidOperationException($"Attempted to begin new array while in state {stateMachine.Current}.");
            }

            stateMachine.PushObject();
            stateMachine.Push(KV3TextReaderState.InArray);
        }

        void FinalizeCurrentArray()
        {
            if (stateMachine.Current != KV3TextReaderState.InArray)
            {
                throw new InvalidOperationException($"Attempted to finalize array while in state {stateMachine.Current}.");
            }

            stateMachine.PopObject();

            if (stateMachine.IsInObject)
            {
                stateMachine.Push(KV3TextReaderState.InObjectAfterValue);
            }
        }

        void SetObjectKey(string name)
        {
            stateMachine.SetName(name);
            stateMachine.Push(KV3TextReaderState.InObjectAfterKey);
        }

        void BeginNewObject()
        {
            if (stateMachine.Current != KV3TextReaderState.InObjectAfterKey)
            {
                throw new InvalidOperationException($"Attempted to begin new object while in state {stateMachine.Current}.");
            }

            listener.OnObjectStart(stateMachine.CurrentName);

            stateMachine.PushObject();
            stateMachine.Push(KV3TextReaderState.InObjectBeforeKey);
        }

        void FinalizeCurrentObject(bool @explicit)
        {
            if (stateMachine.Current != KV3TextReaderState.InObjectBeforeKey && stateMachine.Current != KV3TextReaderState.InObjectAfterValue)
            {
                throw new InvalidOperationException($"Attempted to finalize object while in state {stateMachine.Current}.");
            }

            stateMachine.PopObject();

            if (stateMachine.IsInObject)
            {
                stateMachine.Push(KV3TextReaderState.InObjectAfterValue);
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

        static byte[] ParseHexStringAsByteArray(string hexadecimalRepresentation)
        {
            Require.NotNull(hexadecimalRepresentation, nameof(hexadecimalRepresentation));

            var data = new byte[hexadecimalRepresentation.Length / 2];
            for (var i = 0; i < data.Length; i++)
            {
                var currentByteText = hexadecimalRepresentation.Substring(i * 2, 2);
                data[i] = byte.Parse(currentByteText, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            }

            return data;
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
