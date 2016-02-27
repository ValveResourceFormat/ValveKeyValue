using System.Collections.Generic;

namespace ValveKeyValue
{
    class KVTextReaderStateMachine
    {
        public KVTextReaderStateMachine()
        {
            states = new Stack<Stack<KVTextReaderState>>();
            PushObject();
            Push(KVTextReaderState.InObjectBeforeKey);
        }

        readonly Stack<Stack<KVTextReaderState>> states;

        public KVTextReaderState Current => CurrentObject.Peek();

        public bool IsInObject => states.Count > 0;

        public void PushObject() => states.Push(new Stack<KVTextReaderState>());

        public void Push(KVTextReaderState state) => CurrentObject.Push(state);

        public void PopObject() => states.Pop();

        public void Pop() => CurrentObject.Pop();

        Stack<KVTextReaderState> CurrentObject => states.Peek();
    }
}
