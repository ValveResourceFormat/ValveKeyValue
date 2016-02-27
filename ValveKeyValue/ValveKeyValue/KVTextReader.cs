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

        string currentKey;

        public KVObject ReadObject()
        {
            Require.NotDisposed(nameof(KVTextReader), disposed);

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
                        FinalizeCurrentObject();
                        if (stateMachine.IsInObject)
                        {
                            throw new InvalidOperationException();
                        }

                        break;
                }
            }

            return new KVObject(currentKey, objects);
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
                case KVTextReaderState.InObjectBeforeKey:
                case KVTextReaderState.InObjectAfterValue:
                    currentKey = text;
                    stateMachine.Push(KVTextReaderState.InObjectBetweenKeyAndValue);
                    break;

                case KVTextReaderState.InObjectBetweenKeyAndValue:
                    var currentValue = new KVStringValue(text);
                    objects.Add(new KVObject(currentKey, currentValue));
                    stateMachine.Push(KVTextReaderState.InObjectAfterValue);
                    break;

                default:
                    throw new InvalidOperationException();
            }
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

        void FinalizeCurrentObject()
        {
            if (stateMachine.Current != KVTextReaderState.InObjectBeforeKey && stateMachine.Current != KVTextReaderState.InObjectAfterValue)
            {
                throw new InvalidOperationException();
            }

            stateMachine.PopObject();

            if (stateMachine.IsInObject)
            {
                stateMachine.Push(KVTextReaderState.InObjectAfterValue);
            }
        }
    }
}
