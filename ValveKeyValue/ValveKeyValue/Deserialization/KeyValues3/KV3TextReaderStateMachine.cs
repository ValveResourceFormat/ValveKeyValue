using System.Collections.Generic;

namespace ValveKeyValue.Deserialization.KeyValues3
{
    class KV3TextReaderStateMachine
    {
        public KV3TextReaderStateMachine()
        {
            states = new Stack<KVPartialState<KV3TextReaderState>>();

            // TODO: Get rid of this, kv3 has no root
            // Bare values such as 'null' can be root
            PushObject();
            SetName("root");
            Push(KV3TextReaderState.InObjectAfterKey);
        }

        readonly Stack<KVPartialState<KV3TextReaderState>> states;

        public KV3TextReaderState Current => CurrentObject.States.Peek();

        public bool IsInObject => states.Count > 0;

        public bool IsInArray => states.Count > 0 && CurrentObject.IsArray;

        public void PushObject() => states.Push(new KVPartialState<KV3TextReaderState>());

        public void Push(KV3TextReaderState state) => CurrentObject.States.Push(state);

        public void PopObject()
        {
            states.Pop();
        }

        public string CurrentName => CurrentObject.Key;

        public void SetName(string name) => CurrentObject.Key = name;

        public void SetFlag(KVFlag flag) => CurrentObject.Flag |= flag;

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
