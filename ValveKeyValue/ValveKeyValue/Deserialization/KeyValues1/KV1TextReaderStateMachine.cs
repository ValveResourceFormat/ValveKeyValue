namespace ValveKeyValue.Deserialization.KeyValues1
{
    class KV1TextReaderStateMachine
    {
        public KV1TextReaderStateMachine()
        {
            PushObject();
            Push(KV1TextReaderState.InObjectBeforeKey);
        }

        readonly Stack<KVPartialState<KV1TextReaderState>> states = new();
        readonly List<string> includedPathsToMerge = new();
        readonly List<string> includedPathsToAppend = new();

        public KV1TextReaderState Current => CurrentObject.States.Peek();

        public bool IsInObject => states.Count > 0;

        public bool IsAtDocumentLevel => states.Count == 1;

        public void PushObject() => states.Push(new KVPartialState<KV1TextReaderState>());

        public void Push(KV1TextReaderState state) => CurrentObject.States.Push(state);

        public void PopObject(out bool discard)
        {
            var state = states.Pop();
            discard = state.Discard;
        }

        public string? CurrentName => CurrentObject.Key;

        public void Pop() => CurrentObject.States.Pop();

        public void SetName(string name) => CurrentObject.Key = name;

        public void SetValue(KVObject value) => CurrentObject.Value = value;

        public void AddItem(string key, KVObject item) => CurrentObject.Items.Add(new KeyValuePair<string, KVObject>(key, item));

        public void SetDiscardCurrent(bool discard) => CurrentObject.Discard = discard;

        public IEnumerable<string> ItemsForMerging => includedPathsToMerge;

        public void AddItemForMerging(string item) => includedPathsToMerge.Add(item);

        public IEnumerable<string> ItemsForAppending => includedPathsToAppend;

        public void AddItemForAppending(string item) => includedPathsToAppend.Add(item);

        KVPartialState<KV1TextReaderState> CurrentObject => states.Peek();
    }
}
