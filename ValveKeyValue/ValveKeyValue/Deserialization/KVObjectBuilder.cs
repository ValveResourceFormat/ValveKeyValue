using ValveKeyValue.Abstraction;

namespace ValveKeyValue.Deserialization
{
    class KVObjectBuilder : IParsingVisitationListener
    {
        readonly bool useDictionary;
        readonly List<KVObjectBuilder> associatedBuilders = new();

        public KVObjectBuilder(bool useDictionaryForCollections = false)
        {
            useDictionary = useDictionaryForCollections;
        }

        public KeyValuePair<string, KVObject> GetObject()
        {
            if (stateStack.Count != 1)
            {
                throw new KeyValueException($"Builder is not in a fully completed state (stack count is {stateStack.Count}).");
            }

            foreach (var associatedBuilder in associatedBuilders)
            {
                associatedBuilder.FinalizeState();
            }

            var state = stateStack.Peek();
            return state.IsArray ? MakeResult(state, MakeArray(state)) : MakeResult(state, MakeObject(state));
        }

        readonly Stack<KVPartialState> stateStack = new();

        public void OnKeyValuePair(string name, KVObject value)
        {
            if (StateStack.Count > 0)
            {
                var state = StateStack.Peek();
                state.Items.Add(new KeyValuePair<string, KVObject>(name, value));
            }
            else
            {
                var state = new KVPartialState
                {
                    Key = name,
                    Value = value
                };

                StateStack.Push(state);
            }
        }

        public void OnArrayValue(KVObject value)
        {
            if (StateStack.Count > 0)
            {
                var state = StateStack.Peek();
                state.Items.Add(new KeyValuePair<string, KVObject>(null, value));
            }
            else
            {
                var state = new KVPartialState
                {
                    Value = value
                };

                StateStack.Push(state);
            }
        }

        public void OnObjectEnd()
        {
            if (StateStack.Count <= 1)
            {
                return;
            }

            var state = StateStack.Pop();
            var completedObject = MakeObject(state);
            StateStack.Peek().Items.Add(new KeyValuePair<string, KVObject>(state.Key, completedObject));
        }

        public void OnArrayEnd()
        {
            if (StateStack.Count <= 1)
            {
                return;
            }

            var state = StateStack.Pop();
            var completedObject = MakeArray(state);
            StateStack.Peek().Items.Add(new KeyValuePair<string, KVObject>(state.Key, completedObject));
        }

        public void DiscardCurrentObject()
        {
            var state = StateStack.Peek();
            if (state.Items?.Count > 0)
            {
                state.Items.RemoveAt(state.Items.Count - 1);
            }
            else
            {
                StateStack.Pop();
            }
        }

        public void OnObjectStart(string name, KVFlag flag)
        {
            var state = new KVPartialState
            {
                Key = name,
                Flag = flag,
            };
            StateStack.Push(state);
        }

        public void OnArrayStart(string name, KVFlag flag, int elementCount, bool allSimpleElements)
        {
            var state = new KVPartialState
            {
                Key = name,
                Flag = flag,
                IsArray = true,
            };
            StateStack.Push(state);
        }

        public IParsingVisitationListener GetMergeListener()
        {
            var builder = new KVMergingObjectBuilder(this);
            associatedBuilders.Add(builder);
            return builder;
        }

        public IParsingVisitationListener GetAppendListener()
        {
            var builder = new KVAppendingObjectBuilder(this);
            associatedBuilders.Add(builder);
            return builder;
        }

        public void Dispose()
        {
        }

        internal Stack<KVPartialState> StateStack => stateStack;

        protected virtual void FinalizeState()
        {
            foreach (var associatedBuilder in associatedBuilders)
            {
                associatedBuilder.FinalizeState();
            }
        }

        static KeyValuePair<string, KVObject> MakeResult(KVPartialState state, KVObject obj)
        {
            return new KeyValuePair<string, KVObject>(state.Key, obj);
        }

        KVObject MakeObject(KVPartialState state)
        {
            if (state.Discard)
            {
                return null;
            }

            if (state.IsArray)
            {
                throw new InvalidCastException("Tried to make an object out of an array.");
            }

            if (state.Value != null)
            {
                return state.Value;
            }

            KVObject result = useDictionary
                ? KVObject.Collection(state.Items)
                : KVObject.ListCollection(state.Items);

            if (state.Flag != KVFlag.None)
            {
                result.Flag = state.Flag;
            }

            return result;
        }

        static KVObject MakeArray(KVPartialState state)
        {
            if (state.Discard)
            {
                return null;
            }

            if (!state.IsArray)
            {
                throw new InvalidCastException("Tried to make an array out of an object.");
            }

            if (state.Value != null)
            {
                return state.Value;
            }

            var arrayItems = new List<KVObject>(state.Items.Count);
            foreach (var kvp in state.Items)
            {
                arrayItems.Add(kvp.Value);
            }

            var result = new KVObject(KVValueType.Array, arrayItems);

            if (state.Flag != KVFlag.None)
            {
                result.Flag = state.Flag;
            }

            return result;
        }
    }
}
