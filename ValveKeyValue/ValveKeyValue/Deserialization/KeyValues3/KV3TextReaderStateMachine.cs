namespace ValveKeyValue.Deserialization.KeyValues3
{
    class KV3TextReaderStateMachine
    {
        public KV3TextReaderStateMachine()
        {
            states = new Stack<KVPartialState<KV3TextReaderState>>();

            PushObject();
            Set(KV3TextReaderState.InObjectBeforeValue);
        }

        readonly Stack<KVPartialState<KV3TextReaderState>> states;

        public KV3TextReaderState Current => CurrentObject.States.Peek();

        public bool IsInObject => states.Count > 0;

        public bool IsAtDocumentLevel => states.Count == 1;

        public bool IsInArray => states.Count > 0 && CurrentObject.IsArray;

        public void PushObject() => states.Push(new KVPartialState<KV3TextReaderState>());

        // Only the current state of each object is ever needed, so it replaces the previous one
        public void Set(KV3TextReaderState state)
        {
            var states = CurrentObject.States;
            states.TryPop(out _);
            states.Push(state);
        }

        public void PopObject()
        {
            states.Pop();
        }

        public string? CurrentName => CurrentObject.Key;

        public void SetName(string name) => CurrentObject.Key = name;

        public void SetFlag(KVFlag flag) => CurrentObject.Flag = flag;

        public KVFlag GetAndResetFlag()
        {
            var flag = CurrentObject.Flag;

            CurrentObject.Flag = KVFlag.None;

            return flag;
        }

        public void SetArrayCurrent() => CurrentObject.IsArray = true;

        KVPartialState<KV3TextReaderState> CurrentObject => states.Peek();
    }
}
