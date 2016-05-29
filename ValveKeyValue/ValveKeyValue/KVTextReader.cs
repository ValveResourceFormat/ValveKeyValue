using System;
using System.Globalization;
using System.IO;
using System.Linq;

namespace ValveKeyValue
{
    class KVTextReader : IDisposable
    {
        public KVTextReader(TextReader textReader, KVSerializerOptions options)
        {
            Require.NotNull(textReader, nameof(textReader));
            Require.NotNull(options, nameof(options));

            this.options = options;
            conditionEvaluator = new KVConditionEvaluator(options.Conditions);
            tokenReader = new KVTokenReader(textReader, options);
            stateMachine = new KVTextReaderStateMachine();
        }

        readonly KVSerializerOptions options;
        readonly KVConditionEvaluator conditionEvaluator;
        readonly KVTokenReader tokenReader;
        readonly KVTextReaderStateMachine stateMachine;
        bool disposed;

        public KVObject ReadObject()
        {
            Require.NotDisposed(nameof(KVTextReader), disposed);

            var @object = default(KVObject);

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
                        FinalizeCurrentObject();
                        break;

                    case KVTokenType.Condition:
                        HandleCondition(token.Value);
                        break;

                    case KVTokenType.EndOfFile:
                        try
                        {
                            @object = FinalizeDocument();
                        }
                        catch (InvalidOperationException ex)
                        {
                            throw new KeyValueException("Found end of file when another token type was expected.", ex);
                        }

                        break;

                    case KVTokenType.Comment:
                        break;

                    case KVTokenType.IncludeAndMerge:
                        HandleIncludeAndMerge(token.Value);
                        break;

                    case KVTokenType.IncludeAndAppend:
                        HandleIncludeAndAppend(token.Value);
                        break;

                    default:
                        throw new NotImplementedException("The developer forgot to handle a KVTokenType.");
                }
            }

            if (@object == null)
            {
                throw new InvalidOperationException(); // Should be unreachable.
            }

            return @object;
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
                case KVTextReaderState.InObjectAfterValue:
                    FinalizeCurrentObject();
                    stateMachine.PushObject();
                    SetObjectKey(text);
                    break;

                case KVTextReaderState.InObjectBeforeKey:
                    SetObjectKey(text);
                    break;

                case KVTextReaderState.InObjectBetweenKeyAndValue:
                    var value = ParseValue(text);
                    stateMachine.SetValue(value);
                    stateMachine.Push(KVTextReaderState.InObjectAfterValue);
                    break;

                default:
                    throw new InvalidOperationException();
            }
        }

        void SetObjectKey(string name)
        {
            stateMachine.SetName(name);
            stateMachine.Push(KVTextReaderState.InObjectBetweenKeyAndValue);
        }

        void BeginNewObject()
        {
            if (stateMachine.Current != KVTextReaderState.InObjectBetweenKeyAndValue)
            {
                throw new InvalidOperationException();
            }

            stateMachine.PushObject();
            stateMachine.Push(KVTextReaderState.InObjectBeforeKey);
        }

        KVObject FinalizeCurrentObject()
        {
            if (stateMachine.Current != KVTextReaderState.InObjectBeforeKey && stateMachine.Current != KVTextReaderState.InObjectAfterValue)
            {
                throw new InvalidOperationException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Attempted to finalize object while in state {0}.",
                        stateMachine.Current));
            }

            var @object = stateMachine.PopObject();

            if (stateMachine.IsInObject)
            {
                if (@object != null)
                {
                    stateMachine.AddItem(@object);
                }

                stateMachine.Push(KVTextReaderState.InObjectAfterValue);
            }

            return @object;
        }

        KVObject FinalizeDocument()
        {
            var @object = FinalizeCurrentObject();

            if (stateMachine.IsInObject)
            {
                throw new InvalidOperationException("Inconsistent state - at end of file whilst inside an object.");
            }

            foreach (var includedForMerge in stateMachine.ItemsForMerging)
            {
                Merge(from: includedForMerge, into: @object);
            }

            foreach (var includedDocument in stateMachine.ItemsForAppending)
            {
                foreach (var child in includedDocument.Children)
                {
                    @object.Add(child);
                }
            }

            return @object;
        }

        void HandleCondition(string text)
        {
            if (stateMachine.Current != KVTextReaderState.InObjectAfterValue)
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

        void HandleIncludeAndMerge(string filePath)
        {
            KVObject includedKeyValues;

            using (var stream = OpenFileForInclude(filePath))
            using (var reader = new KVTextReader(new StreamReader(stream), options))
            {
                includedKeyValues = reader.ReadObject();
            }

            stateMachine.AddItemForMerging(includedKeyValues);
        }

        void HandleIncludeAndAppend(string filePath)
        {
            KVObject includedKeyValues;

            using (var stream = OpenFileForInclude(filePath))
            using (var reader = new KVTextReader(new StreamReader(stream), options))
            {
                includedKeyValues = reader.ReadObject();
            }

            stateMachine.AddItemForAppending(includedKeyValues);
        }

        Stream OpenFileForInclude(string filePath)
        {
            if (!stateMachine.IsAtStart)
            {
                throw new KeyValueException("Inclusions are only valid at the beginning of a file.");
            }

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

        static void Merge(KVObject from, KVObject into)
        {
            foreach (var child in from)
            {
                var matchingChild = into.Children.FirstOrDefault(c => c.Name == child.Name);
                if (matchingChild == null && into.Value.ValueType == KVValueType.Collection)
                {
                    into.Add(child);
                }
                else
                {
                    Merge(from: child, into: matchingChild);
                }
            }
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
