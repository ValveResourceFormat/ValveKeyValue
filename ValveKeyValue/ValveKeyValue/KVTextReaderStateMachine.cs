using System.Collections.Generic;

namespace ValveKeyValue
{
    class KVTextReaderStateMachine
    {
        public KVTextReaderStateMachine()
        {
            states = new Stack<KVPartialState>();
            includedObjectsToMerge = new List<KVObject>();
            includedObjectsToAppend = new List<KVObject>();

            PushObject();
            Push(KVTextReaderState.InObjectBeforeKey);
        }

        readonly Stack<KVPartialState> states;
        readonly IList<KVObject> includedObjectsToMerge;
        readonly IList<KVObject> includedObjectsToAppend;

        public KVTextReaderState Current => CurrentObject.States.Peek();

        public bool IsInObject => states.Count > 0;

        public bool IsAtStart => states.Count == 1 && CurrentObject.States.Count == 1 && Current == KVTextReaderState.InObjectBeforeKey;

        public void PushObject() => states.Push(new KVPartialState());

        public void Push(KVTextReaderState state) => CurrentObject.States.Push(state);

        public KVObject PopObject()
        {
            var state = states.Pop();

            if (state.Key == null)
            {
                throw new KeyValueException("Attempted to finish object construction without an object name.");
            }

            if (state.Discard)
            {
                return null;
            }

            if (state.Value != null)
            {
                return new KVObject(state.Key, state.Value);
            }
            else
            {
                return new KVObject(state.Key, state.Items);
            }
        }

        public void Pop() => CurrentObject.States.Pop();

        public void SetName(string name) => CurrentObject.Key = name;

        public void SetValue(KVValue value) => CurrentObject.Value = value;

        public void AddItem(KVObject item) => CurrentObject.Items.Add(item);

        public void SetDiscardCurrent() => CurrentObject.Discard = true;

        public IEnumerable<KVObject> ItemsForMerging => includedObjectsToMerge;

        public void AddItemForMerging(KVObject item) => includedObjectsToMerge.Add(item);

        public IEnumerable<KVObject> ItemsForAppending => includedObjectsToAppend;

        public void AddItemForAppending(KVObject item) => includedObjectsToAppend.Add(item);

        KVPartialState CurrentObject => states.Peek();
    }
}
