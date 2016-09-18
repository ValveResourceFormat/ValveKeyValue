using System;
using System.Globalization;
using System.IO;
using ValveKeyValue.Abstraction;

namespace ValveKeyValue.Deserialization
{
    sealed class KV1TextReader : IVisitingReader
    {
        public KV1TextReader(TextReader textReader, IParsingVisitationListener listener, KVSerializerOptions options)
        {
            Require.NotNull(textReader, nameof(textReader));
            Require.NotNull(listener, nameof(listener));
            Require.NotNull(options, nameof(options));

            this.listener = listener;
            this.options = options;

            conditionEvaluator = new KVConditionEvaluator(options.Conditions);
            tokenReader = new KV1TokenReader(textReader, options);
            stateMachine = new KV1TextReaderStateMachine();
        }

        readonly IParsingVisitationListener listener;
        readonly KVSerializerOptions options;

        readonly KVConditionEvaluator conditionEvaluator;
        readonly KV1TokenReader tokenReader;
        readonly KV1TextReaderStateMachine stateMachine;
        bool disposed;

        public void ReadObject()
        {
            Require.NotDisposed(nameof(KV1TextReader), disposed);

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
                    case KVTokenType.String:
                        ReadText(token.Value);
                        break;

                    case KVTokenType.ObjectStart:
                        BeginNewObject();
                        break;

                    case KVTokenType.ObjectEnd:
                        FinalizeCurrentObject(@explicit: true);
                        break;

                    case KVTokenType.Condition:
                        HandleCondition(token.Value);
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

                    case KVTokenType.IncludeAndMerge:
                        if (!stateMachine.IsAtStart)
                        {
                            throw new KeyValueException("Inclusions are only valid at the beginning of a file.");
                        }

                        stateMachine.AddItemForMerging(token.Value);
                        break;

                    case KVTokenType.IncludeAndAppend:
                        if (!stateMachine.IsAtStart)
                        {
                            throw new KeyValueException("Inclusions are only valid at the beginning of a file.");
                        }

                        stateMachine.AddItemForAppending(token.Value);
                        break;

                    default:
                        throw new NotImplementedException("The developer forgot to handle a KVTokenType.");
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

        void ReadText(string text)
        {
            switch (stateMachine.Current)
            {
                // If we're after a value when we find more text, then we must be starting a new key/value pair.
                case KV1TextReaderState.InObjectAfterValue:
                    FinalizeCurrentObject(@explicit: false);
                    stateMachine.PushObject();
                    SetObjectKey(text);
                    break;

                case KV1TextReaderState.InObjectBeforeKey:
                    SetObjectKey(text);
                    break;

                case KV1TextReaderState.InObjectBetweenKeyAndValue:
                    var value = ParseValue(text);
                    var name = stateMachine.CurrentName;
                    listener.OnKeyValuePair(name, value);

                    stateMachine.Push(KV1TextReaderState.InObjectAfterValue);
                    break;

                default:
                    throw new InvalidOperationException();
            }
        }

        void SetObjectKey(string name)
        {
            stateMachine.SetName(name);
            stateMachine.Push(KV1TextReaderState.InObjectBetweenKeyAndValue);
        }

        void BeginNewObject()
        {
            if (stateMachine.Current != KV1TextReaderState.InObjectBetweenKeyAndValue)
            {
                throw new InvalidOperationException();
            }

            listener.OnObjectStart(stateMachine.CurrentName);

            stateMachine.PushObject();
            stateMachine.Push(KV1TextReaderState.InObjectBeforeKey);
        }

        void FinalizeCurrentObject(bool @explicit)
        {
            if (stateMachine.Current != KV1TextReaderState.InObjectBeforeKey && stateMachine.Current != KV1TextReaderState.InObjectAfterValue)
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Attempted to finalize object while in state {0}.",
                        stateMachine.Current));
            }

            bool discard;
            stateMachine.PopObject(out discard);

            if (stateMachine.IsInObject)
            {
                stateMachine.Push(KV1TextReaderState.InObjectAfterValue);
            }

            if (discard)
            {
                listener.DiscardCurrentObject();
            }
            else if (@explicit)
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

            foreach (var includedForMerge in stateMachine.ItemsForMerging)
            {
                DoIncludeAndMerge(includedForMerge);
            }

            foreach (var includedDocument in stateMachine.ItemsForAppending)
            {
                DoIncludeAndAppend(includedDocument);
            }
        }

        void HandleCondition(string text)
        {
            if (stateMachine.Current != KV1TextReaderState.InObjectAfterValue)
            {
                throw new InvalidDataException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Found conditional while in state {0}.",
                        stateMachine.Current));
            }

            if (!conditionEvaluator.Evalute(text))
            {
                stateMachine.SetDiscardCurrent();
            }
        }

        void DoIncludeAndMerge(string filePath)
        {
            var mergeListener = listener.GetMergeListener();

            using (var stream = OpenFileForInclude(filePath))
            using (var reader = new KV1TextReader(new StreamReader(stream), mergeListener, options))
            {
                reader.ReadObject();
            }
        }

        void DoIncludeAndAppend(string filePath)
        {
            var appendListener = listener.GetAppendListener();

            using (var stream = OpenFileForInclude(filePath))
            using (var reader = new KV1TextReader(new StreamReader(stream), appendListener, options))
            {
                reader.ReadObject();
            }
        }

        Stream OpenFileForInclude(string filePath)
        {
            if (options.FileLoader == null)
            {
                throw new KeyValueException("Inclusions requirer a FileLoader to be provided in KVSerializerOptions.");
            }

            var stream = options.FileLoader.OpenFile(filePath);
            if (stream == null)
            {
                throw new KeyValueException("IIncludedFileLoader returned null for included file path.");
            }

            return stream;
        }

        static KVValue ParseValue(string text)
        {
            // "0x" + 2 digits per byte. Long is 8 bytes, so s + 16 = 18.
            // Expressed this way for readability, rather than using a magic value.
            const int HexStringLengthForUnsignedLong = 2 + (sizeof(long) * 2);

            if (text.Length == HexStringLengthForUnsignedLong && text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                var hexadecimalString = text.Substring(2);
                var data = ParseHexStringAsByteArray(hexadecimalString);

                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(data);
                }

                var value = BitConverter.ToUInt64(data, 0);
                return new KVObjectValue<ulong>(value, KVValueType.UInt64);
            }

            int intValue;
            if (int.TryParse(text, out intValue))
            {
                return new KVObjectValue<int>(intValue, KVValueType.Int32);
            }

            const NumberStyles FloatingPointNumberStyles =
                NumberStyles.AllowDecimalPoint |
                NumberStyles.AllowExponent |
                NumberStyles.AllowLeadingSign;

            float floatValue;
            if (float.TryParse(text, FloatingPointNumberStyles, CultureInfo.InvariantCulture, out floatValue))
            {
                return new KVObjectValue<float>(floatValue, KVValueType.FloatingPoint);
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
    }
}
