using System.Globalization;
using ValveKeyValue.Abstraction;

namespace ValveKeyValue.Deserialization.KeyValues1
{
    sealed class KV1TextReader : IVisitingReader
    {
        public KV1TextReader(TextReader textReader, IParsingVisitationListener listener, KVSerializerOptions options, List<KvSourceSpan>? sourceMap = null)
        {
            ArgumentNullException.ThrowIfNull(textReader);
            ArgumentNullException.ThrowIfNull(listener);
            ArgumentNullException.ThrowIfNull(options);

            this.listener = listener;
            this.options = options;
            this.sourceMap = sourceMap;

            conditionEvaluator = new KVConditionEvaluator(options.Conditions);
            tokenReader = new KV1TokenReader(textReader, options);
            stateMachine = new KV1TextReaderStateMachine();
        }

#pragma warning disable CA2213 // Not owned by this class
        readonly IParsingVisitationListener listener;
#pragma warning restore CA2213
        readonly KVSerializerOptions options;
        readonly List<KvSourceSpan>? sourceMap;

        readonly KVConditionEvaluator conditionEvaluator;
        readonly KV1TokenReader tokenReader;
        readonly KV1TextReaderStateMachine stateMachine;
        bool disposed;

        // Whether the previous token was a conditional. Only one may appear in each position.
        bool readCondition;

        public KVHeader ReadHeader()
        {
            ObjectDisposedException.ThrowIf(disposed, this);

            while (stateMachine.IsInObject)
            {
                KVToken token;

                try
                {
                    token = tokenReader.ReadNextToken();
                }
                catch (EndOfStreamException ex)
                {
                    throw tokenReader.MakeSyntaxException($"Found end of file while trying to read the token that started at {tokenReader.TokenStartPosition}.", ex);
                }

                token = ClassifyToken(token);

                if (sourceMap != null && token.TokenType != KVTokenType.EndOfFile)
                {
                    sourceMap.Add(new KvSourceSpan(tokenReader.LastTokenStart, tokenReader.LastTokenEnd, token.TokenType));
                }

                switch (token.TokenType)
                {
                    case KVTokenType.Key:
                    case KVTokenType.String:
                        ReadText(token.Value!);
                        break;

                    case KVTokenType.Assignment:
                        ReadAssignment();
                        break;

                    case KVTokenType.ObjectStart:
                        BeginNewObject();
                        break;

                    case KVTokenType.ObjectEnd:
                        // A '}' at document level after the root has closed would otherwise pop the
                        // document pseudo-object and silently ignore all remaining data.
                        if (stateMachine.IsAtDocumentLevel && stateMachine.Current == KV1TextReaderState.InObjectAfterValue)
                        {
                            throw tokenReader.MakeSyntaxException($"Found data after the root object at {tokenReader.TokenStartPosition}, documents with multiple root objects are not supported.");
                        }

                        FinalizeCurrentObject(@explicit: true);
                        break;

                    case KVTokenType.Condition:
                        HandleCondition(token.Value!);
                        break;

                    case KVTokenType.EndOfFile:
                        FinalizeDocument();
                        break;

                    case KVTokenType.Comment:
                        continue;

                    case KVTokenType.IncludeAndAppend:
                        stateMachine.Push(KV1TextReaderState.InDocumentBeforeIncludePath);
                        break;

                    case KVTokenType.IncludeAndMerge:
                        stateMachine.Push(KV1TextReaderState.InDocumentBeforeBasePath);
                        break;

                    default:
                        throw new ArgumentOutOfRangeException(nameof(token.TokenType), token.TokenType, "Unhandled token type.");
                }

                readCondition = token.TokenType == KVTokenType.Condition;
            }

            return new KVHeader();
        }

        // The token reader cannot tell how a string or '=' is used, so this decides it from the state.
        // '=' is an assignment only between a key and its value, anywhere else it is a plain string.
        // Keys appear either at the start of an object (InObjectBeforeKey) or right after the previous
        // value (InObjectAfterValue), and outside the root object "#include" and "#base" are directives.
        KVToken ClassifyToken(KVToken token)
        {
            if (token.TokenType == KVTokenType.Assignment && stateMachine.Current != KV1TextReaderState.InObjectBetweenKeyAndValue)
            {
                token = token with { TokenType = KVTokenType.String };
            }

            if (token.TokenType == KVTokenType.String
                && stateMachine.Current is KV1TextReaderState.InObjectBeforeKey or KV1TextReaderState.InObjectAfterValue)
            {
                if (stateMachine.IsAtDocumentLevel && string.Equals(token.Value, "#include", StringComparison.OrdinalIgnoreCase))
                {
                    return token with { TokenType = KVTokenType.IncludeAndAppend };
                }

                if (stateMachine.IsAtDocumentLevel && string.Equals(token.Value, "#base", StringComparison.OrdinalIgnoreCase))
                {
                    return token with { TokenType = KVTokenType.IncludeAndMerge };
                }

                return token with { TokenType = KVTokenType.Key };
            }

            return token;
        }

        void ReadAssignment()
        {
            if (stateMachine.IsAtDocumentLevel)
            {
                throw tokenReader.MakeSyntaxException($"Found '=' after the root key at {tokenReader.TokenStartPosition}, the root key must be followed by '{{'.");
            }

            stateMachine.Push(KV1TextReaderState.InObjectAfterAssignment);
        }

        void AddInclusion(string filePath)
        {
            if (filePath.Length == 0)
            {
                throw tokenReader.MakeSyntaxException($"Found an inclusion directive with an empty file path at {tokenReader.TokenStartPosition}.");
            }

            if (stateMachine.Current == KV1TextReaderState.InDocumentBeforeBasePath)
            {
                stateMachine.AddItemForMerging(filePath);
            }
            else
            {
                stateMachine.AddItemForAppending(filePath);
            }

            stateMachine.Pop();
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
                    if (stateMachine.IsAtDocumentLevel)
                    {
                        throw tokenReader.MakeSyntaxException($"Found data after the root object at {tokenReader.TokenStartPosition}, documents with multiple root objects are not supported.");
                    }

                    FinalizeCurrentObject(@explicit: false);
                    stateMachine.PushObject();
                    SetObjectKey(text);
                    break;

                case KV1TextReaderState.InObjectBeforeKey:
                    SetObjectKey(text);
                    break;

                case KV1TextReaderState.InObjectBetweenKeyAndValue:
                case KV1TextReaderState.InObjectAfterAssignment:
                    var value = ParseValue(text);
                    var name = stateMachine.CurrentName!;
                    listener.OnKeyValuePair(name, value);

                    stateMachine.Push(KV1TextReaderState.InObjectAfterValue);
                    break;

                case KV1TextReaderState.InDocumentBeforeIncludePath:
                case KV1TextReaderState.InDocumentBeforeBasePath:
                    AddInclusion(text);
                    break;

                default:
                    throw tokenReader.MakeSyntaxException($"Unhandled text reader state: {stateMachine.Current} at {tokenReader.TokenStartPosition}.");
            }
        }

        void SetObjectKey(string name)
        {
            stateMachine.SetName(name);
            stateMachine.Push(KV1TextReaderState.InObjectBetweenKeyAndValue);
        }

        void BeginNewObject()
        {
            if (stateMachine.Current is not (KV1TextReaderState.InObjectBetweenKeyAndValue or KV1TextReaderState.InObjectAfterAssignment))
            {
                throw tokenReader.MakeSyntaxException($"Attempted to begin new object while in state {stateMachine.Current} at {tokenReader.TokenStartPosition}.");
            }

            listener.OnObjectStart(stateMachine.CurrentName, KVFlag.None);

            stateMachine.PushObject();
            stateMachine.Push(KV1TextReaderState.InObjectBeforeKey);
        }

        void FinalizeCurrentObject(bool @explicit)
        {
            if (stateMachine.Current != KV1TextReaderState.InObjectBeforeKey && stateMachine.Current != KV1TextReaderState.InObjectAfterValue)
            {
                throw tokenReader.MakeSyntaxException($"Attempted to finalize object while in state {stateMachine.Current} at {tokenReader.TokenStartPosition}.");
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
            if (!stateMachine.IsAtDocumentLevel
                || (stateMachine.Current != KV1TextReaderState.InObjectBeforeKey && stateMachine.Current != KV1TextReaderState.InObjectAfterValue))
            {
                throw tokenReader.MakeSyntaxException($"Found end of file when another token type was expected at {tokenReader.TokenStartPosition}.");
            }

            FinalizeCurrentObject(@explicit: true);

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
            if (stateMachine.Current is not (KV1TextReaderState.InObjectAfterValue or KV1TextReaderState.InObjectBetweenKeyAndValue or KV1TextReaderState.InObjectAfterAssignment))
            {
                throw tokenReader.MakeSyntaxException($"Found conditional while in state {stateMachine.Current} at {tokenReader.TokenStartPosition}.");
            }

            // A conditional between a root object's name and its '{' is valid and applies to that
            // object, but after the root has closed it would otherwise discard the entire document.
            if (stateMachine.Current == KV1TextReaderState.InObjectAfterValue && stateMachine.IsAtDocumentLevel)
            {
                throw tokenReader.MakeSyntaxException($"Found data after the root object at {tokenReader.TokenStartPosition}, documents with multiple root objects are not supported.");
            }

            if (readCondition)
            {
                throw tokenReader.MakeSyntaxException($"Found a second consecutive conditional at {tokenReader.TokenStartPosition}.");
            }

            bool matches;

            try
            {
                matches = conditionEvaluator.Evaluate(text);
            }
            catch (InvalidOperationException ex)
            {
                throw tokenReader.MakeSyntaxException($"Invalid conditional syntax \"{text}\" at {tokenReader.TokenStartPosition}.", ex);
            }

            // A pair can have a conditional before '=', after '=', and after its value. The last one decides.
            stateMachine.SetDiscardCurrent(!matches);

            // A matching conditional after '=' replaces the earlier pair with the same key.
            if (matches && stateMachine.Current == KV1TextReaderState.InObjectAfterAssignment)
            {
                listener.RemoveItem(stateMachine.CurrentName!);
            }
        }

        void DoIncludeAndMerge(string filePath)
        {
            var mergeListener = listener.GetMergeListener();

            using var stream = OpenFileForInclude(filePath);
            using var reader = new KV1TextReader(new StreamReader(stream), mergeListener, options);
            reader.ReadHeader();
        }

        void DoIncludeAndAppend(string filePath)
        {
            var appendListener = listener.GetAppendListener();

            using var stream = OpenFileForInclude(filePath);
            using var reader = new KV1TextReader(new StreamReader(stream), appendListener, options);
            reader.ReadHeader();
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

        static KVObject ParseValue(string text)
        {
            // "0x" followed by 16 hex digits (2 per byte of a long) is parsed as a uint64.
            // Valve only accepts a lowercase "x", and does not validate the digits: characters
            // outside [0-9A-Fa-f] are run through the same offset math and produce garbage
            // values rather than a parse failure.
            const int HexStringLengthForUnsignedLong = 2 + sizeof(long) * 2;

            if (text.Length == HexStringLengthForUnsignedLong && text.StartsWith("0x", StringComparison.Ordinal))
            {
                var value = 0L;

                for (var i = 2; i < HexStringLengthForUnsignedLong; i++)
                {
                    int digit = text[i];

                    if (digit >= 'a')
                    {
                        digit -= 'a' - ('9' + 1);
                    }
                    else if (digit >= 'A')
                    {
                        digit -= 'A' - ('9' + 1);
                    }

                    value = (value * 16) + (digit - '0');
                }

                return new KVObject((ulong)value);
            }

            const NumberStyles IntegerNumberStyles =
                NumberStyles.AllowLeadingWhite |
                NumberStyles.AllowLeadingSign;

            if (int.TryParse(text, IntegerNumberStyles, CultureInfo.InvariantCulture, out var intValue))
            {
                return new KVObject(intValue);
            }

            const NumberStyles FloatingPointNumberStyles =
                NumberStyles.AllowLeadingWhite |
                NumberStyles.AllowDecimalPoint |
                NumberStyles.AllowExponent |
                NumberStyles.AllowLeadingSign;

            if (!IsStrToLBase10Compatible(text) && float.TryParse(text, FloatingPointNumberStyles, CultureInfo.InvariantCulture, out var floatValue))
            {
                return new KVObject(floatValue);
            }

            return new KVObject(text);
        }

        // The string may begin with an arbitrary amount of white space (as determined by isspace(3)) followed by a single optional
        // '+' or '-' sign.  If base is zero or 16, the string may then include a "0x" prefix, and the number will be read in base 16;
        // otherwise, a zero base is taken as 10 (decimal) unless the next character is '0', in which case it is taken as 8 (octal).
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
