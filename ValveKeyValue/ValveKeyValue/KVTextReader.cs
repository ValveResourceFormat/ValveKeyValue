using System;
using System.Collections.Generic;
using System.IO;

namespace ValveKeyValue
{
    class KVTextReader : IDisposable
    {
        public KVTextReader(Stream stream)
        {
            Require.NotNull(stream, nameof(stream));

            tokenReader = new KVTokenReader(stream);
            stateMachine = new KVTextReaderStateMachine();
            objects = new List<KVObject>();
        }

        readonly KVTokenReader tokenReader;
        readonly KVTextReaderStateMachine stateMachine;
        readonly IList<KVObject> objects;
        bool disposed;

        public KVObject ReadObject()
        {
            Require.NotDisposed(nameof(KVTextReader), disposed);

            var @object = default(KVObject);

            while (stateMachine.IsInObject)
            {
                var token = tokenReader.ReadNextToken();
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

                    case KVTokenType.EndOfFile:
                        @object = FinalizeDocument();
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
                throw new InvalidOperationException();
            }

            var @object = stateMachine.PopObject();

            if (stateMachine.IsInObject)
            {
                stateMachine.AddItem(@object);
                stateMachine.Push(KVTextReaderState.InObjectAfterValue);
            }

            return @object;
        }

        KVObject FinalizeDocument()
        {
            var @object = FinalizeCurrentObject();

            if (stateMachine.IsInObject)
            {
                throw new InvalidOperationException();
            }

            return @object;
        }
    }
}
