using System.Collections.Generic;

namespace ValveKeyValue.Deserialization
{
    class KV1TextReaderStateMachine
    {
        public KV1TextReaderStateMachine()
        {
            states = new Stack<KVPartialState<KV1TextReaderState>>();
            includedPathsToMerge = new List<string>();
            includedPathsToAppend = new List<string>();

            PushObject();
            Push(KV1TextReaderState.InObjectBeforeKey);
        }

        readonly Stack<KVPartialState<KV1TextReaderState>> states;
        readonly IList<string> includedPathsToMerge;
        readonly IList<string> includedPathsToAppend;

        public KV1TextReaderState Current => CurrentObject.States.Peek();

        public bool IsInObject => states.Count > 0;

        public bool IsAtStart => states.Count == 1 && CurrentObject.States.Count == 1 && Current == KV1TextReaderState.InObjectBeforeKey;

        public void PushObject() => states.Push(new KVPartialState<KV1TextReaderState>());

        public void Push(KV1TextReaderState state) => CurrentObject.States.Push(state);

        public void PopObject(out bool discard)
        {
            var state = states.Pop();
            discard = state.Discard;
        }

        public string CurrentName => CurrentObject.Key;

        public void Pop() => CurrentObject.States.Pop();

        public void SetName(string name) => CurrentObject.Key = name;

        public void SetValue(KVValue value) => CurrentObject.Value = value;

        public void AddItem(KVObject item) => CurrentObject.Items.Add(item);

        public void SetDiscardCurrent() => CurrentObject.Discard = true;

        public IEnumerable<string> ItemsForMerging => includedPathsToMerge;

        public void AddItemForMerging(string item) => includedPathsToMerge.Add(item);

        public IEnumerable<string> ItemsForAppending => includedPathsToAppend;

        public void AddItemForAppending(string item) => includedPathsToAppend.Add(item);

        KVPartialState<KV1TextReaderState> CurrentObject => states.Peek();
    }
}
