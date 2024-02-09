using System.Globalization;
using ValveKeyValue.Abstraction;

namespace ValveKeyValue.Deserialization.KeyValues1
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
                            throw new KeyValueException($"Inclusions are only valid at the beginning of a file, but found one at {tokenReader.PreviousTokenPosition}.");
                        }

                        stateMachine.AddItemForMerging(token.Value);
                        break;

                    case KVTokenType.IncludeAndAppend:
                        if (!stateMachine.IsAtStart)
                        {
                            throw new KeyValueException($"Inclusions are only valid at the beginning of a file, but found one at {tokenReader.PreviousTokenPosition}.");
                        }

                        stateMachine.AddItemForAppending(token.Value);
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
                    throw new InvalidOperationException($"Unhandled text reader state: {stateMachine.Current} at {tokenReader.PreviousTokenPosition}.");
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
                throw new InvalidOperationException($"Attempted to begin new object while in state {stateMachine.Current} at {tokenReader.PreviousTokenPosition}.");
            }

            listener.OnObjectStart(stateMachine.CurrentName);

            stateMachine.PushObject();
            stateMachine.Push(KV1TextReaderState.InObjectBeforeKey);
        }

        void FinalizeCurrentObject(bool @explicit)
        {
            if (stateMachine.Current != KV1TextReaderState.InObjectBeforeKey && stateMachine.Current != KV1TextReaderState.InObjectAfterValue)
            {
                throw new InvalidOperationException($"Attempted to finalize object while in state {stateMachine.Current} at {tokenReader.PreviousTokenPosition}.");
            }

            stateMachine.PopObject(out var discard);

            if (stateMachine.IsInObject)
            {
                stateMachine.Push(KV1TextReaderState.InObjectAfterValue);
            }

            if (discard)
            {
                listener.DiscardCurrentObject();
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
            if (stateMachine.Current != KV1TextReaderState.InObjectAfterValue && stateMachine.Current != KV1TextReaderState.InObjectBetweenKeyAndValue)
            {
                throw new InvalidDataException($"Found conditional while in state {stateMachine.Current}.");
            }

            if (!conditionEvaluator.Evalute(text))
            {
                stateMachine.SetDiscardCurrent();
            }
        }

        void DoIncludeAndMerge(string filePath)
        {
            var mergeListener = listener.GetMergeListener();

            using var stream = OpenFileForInclude(filePath);
            using var reader = new KV1TextReader(new StreamReader(stream), mergeListener, options);
            reader.ReadObject();
        }

        void DoIncludeAndAppend(string filePath)
        {
            var appendListener = listener.GetAppendListener();

            using var stream = OpenFileForInclude(filePath);
            using var reader = new KV1TextReader(new StreamReader(stream), appendListener, options);
            reader.ReadObject();
        }

        Stream OpenFileForInclude(string filePath)
        {
            if (options.FileLoader == null)
            {
                throw new KeyValueException("Inclusions require a FileLoader to be provided in KVSerializerOptions.");
            }

            var stream = options.FileLoader.OpenFile(filePath) ?? throw new KeyValueException("IIncludedFileLoader returned null for included file path.");

            return stream;
        }

        static KVValue ParseValue(string text)
        {
            // "0x" + 2 digits per byte. Long is 8 bytes, so s + 16 = 18.
            // Expressed this way for readability, rather than using a magic value.
            const int HexStringLengthForUnsignedLong = 2 + sizeof(long) * 2;

            if (text.Length == HexStringLengthForUnsignedLong && text.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                var hexadecimalString = text[2..];
                var data = HexStringHelper.ParseHexStringAsByteArray(hexadecimalString);

                if (BitConverter.IsLittleEndian)
                {
                    Array.Reverse(data);
                }

                var value = BitConverter.ToUInt64(data, 0);
                return new KVObjectValue<ulong>(value, KVValueType.UInt64);
            }

            const NumberStyles IntegerNumberStyles =
                NumberStyles.AllowLeadingWhite |
                NumberStyles.AllowLeadingSign;

            if (int.TryParse(text, IntegerNumberStyles, CultureInfo.InvariantCulture, out var intValue))
            {
                return new KVObjectValue<int>(intValue, KVValueType.Int32);
            }

            const NumberStyles FloatingPointNumberStyles =
                NumberStyles.AllowLeadingWhite |
                NumberStyles.AllowDecimalPoint |
                NumberStyles.AllowExponent |
                NumberStyles.AllowLeadingSign;

            if (!IsStrToLBase10Compatible(text) && float.TryParse(text, FloatingPointNumberStyles, CultureInfo.InvariantCulture, out var floatValue))
            {
                return new KVObjectValue<float>(floatValue, KVValueType.FloatingPoint);
            }

            return new KVObjectValue<string>(text, KVValueType.String);
        }

        // The string may begin with an arbitrary amount of white space (as determined by isspace(3)) followed by a single optional
        // ‘+’ or ‘-’ sign.  If base is zero or 16, the string may then include a “0x” prefix, and the number will be read in base 16;
        // otherwise, a zero base is taken as 10 (decimal) unless the next character is ‘0’, in which case it is taken as 8 (octal).
        // The remainder of the string is converted to a long, long long, intmax_t or quad_t value in the obvious manner, stopping at
        // the first character which is not a valid digit in the given base.
        // - man(3) page for strtol
        static bool IsStrToLBase10Compatible(string str)
        {
            var index = 0;
            while (index < str.Length && char.IsWhiteSpace(str[index]))
            {
                index++;
            }

            if (index < str.Length && str[index] is '+' or '-')
            {
                index++;
            }

            // Ignore 0x as Valve explicitly ignore it in their implementation.
            // Ignore octal (leading zero) as Valve call strtol() for base-10 explicitly.

            while (index < str.Length && str[index] is '0' or '1' or '2' or '3' or '4' or '5' or '6' or '7' or '8' or '9')
            {
                index++;
            }

            return index == str.Length;
        }
    }
}
