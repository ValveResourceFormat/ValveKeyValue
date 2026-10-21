namespace ValveKeyValue.Deserialization.KeyValues1
{
    class KV1TextReaderStateMachine
    {
        public KV1TextReaderStateMachine()
        {
            PushObject();
            Push(KV1TextReaderState.InObjectBeforeKey);
        }

        readonly Stack<ObjectState> states = new();
        readonly Stack<ObjectState> spareStates = new();
        readonly List<string> includedPathsToMerge = new();
        readonly List<string> includedPathsToAppend = new();

        public KV1TextReaderState Current => CurrentObject.States.Peek();

        public bool IsInObject => states.Count > 0;

        public bool IsAtDocumentLevel => states.Count == 1;

        public void PushObject()
        {
            if (!spareStates.TryPop(out var state))
            {
                state = new ObjectState();
            }

            state.Reset();
            states.Push(state);
        }

        public void Push(KV1TextReaderState state) => CurrentObject.States.Push(state);

        public void PopObject(out bool discard)
        {
            var state = states.Pop();
            discard = state.Discard;

            spareStates.Push(state);
        }

        public string? CurrentName => CurrentObject.Key;

        public void Pop() => CurrentObject.States.Pop();

        public void SetName(string name) => CurrentObject.Key = name;

        public void SetDiscardCurrent(bool discard) => CurrentObject.Discard = discard;

        public IEnumerable<string> ItemsForMerging => includedPathsToMerge;

        public void AddItemForMerging(string item) => includedPathsToMerge.Add(item);

        public IEnumerable<string> ItemsForAppending => includedPathsToAppend;

        public void AddItemForAppending(string item) => includedPathsToAppend.Add(item);

        ObjectState CurrentObject => states.Peek();

        sealed class ObjectState
        {
            public string? Key { get; set; }

            public bool Discard { get; set; }

            public Stack<KV1TextReaderState> States { get; } = new();

            public void Reset()
            {
                Key = null;
                Discard = false;
                States.Clear();
            }
        }
    }
}
