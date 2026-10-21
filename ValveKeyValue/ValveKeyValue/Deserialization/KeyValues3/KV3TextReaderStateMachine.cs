namespace ValveKeyValue.Deserialization.KeyValues3
{
    class KV3TextReaderStateMachine
    {
        public KV3TextReaderStateMachine()
        {
            PushObject();
            Set(KV3TextReaderState.InObjectBeforeValue);
        }

        readonly Stack<ObjectState> states = new();
        readonly Stack<ObjectState> spareStates = new();

        public KV3TextReaderState Current => CurrentObject.State;

        public bool IsInObject => states.Count > 0;

        public bool IsAtDocumentLevel => states.Count == 1;

        public bool IsInArray => states.Count > 0 && CurrentObject.IsArray;

        public void PushObject()
        {
            if (!spareStates.TryPop(out var state))
            {
                state = new ObjectState();
            }

            state.Reset();
            states.Push(state);
        }

        // Only the current state of each object is ever needed, so it replaces the previous one
        public void Set(KV3TextReaderState state) => CurrentObject.State = state;

        public void PopObject()
        {
            var state = states.Pop();
            spareStates.Push(state);
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

        ObjectState CurrentObject => states.Peek();

        sealed class ObjectState
        {
            public string? Key { get; set; }

            public KVFlag Flag { get; set; }

            public bool IsArray { get; set; }

            public KV3TextReaderState State { get; set; }

            public void Reset()
            {
                Key = null;
                Flag = KVFlag.None;
                IsArray = false;
                State = default;
            }
        }
    }
}
