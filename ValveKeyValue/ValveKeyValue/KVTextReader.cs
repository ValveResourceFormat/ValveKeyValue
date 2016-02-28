using System;
using System.Globalization;
using System.IO;

namespace ValveKeyValue
{
    class KVTextReader : IDisposable
    {
        public KVTextReader(Stream stream, string[] conditions)
        {
            Require.NotNull(stream, nameof(stream));
            Require.NotNull(conditions, nameof(conditions));

            this.conditions = conditions;
            tokenReader = new KVTokenReader(stream);
            stateMachine = new KVTextReaderStateMachine();
        }

        readonly string[] conditions;
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
                    var value = new KVStringValue(text);
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

            if (text.Length < 2 || text[0] != '$' || (text[1] == '!' && text.Length < 3))
            {
                throw new InvalidDataException(
                    string.Format(
                        CultureInfo.InvariantCulture,
                        "Invalid conditional '{0}'.",
                        text));
            }

            bool negate = text[1] == '!';
            var variable = negate ? text.Substring(2) : text.Substring(1);
            var hasVariable = Array.IndexOf(conditions, variable) < 0;
            var matchesCondition = negate ? !hasVariable : hasVariable;

            if (matchesCondition)
            {
                stateMachine.SetDiscardCurrent();
            }
        }
    }
}
