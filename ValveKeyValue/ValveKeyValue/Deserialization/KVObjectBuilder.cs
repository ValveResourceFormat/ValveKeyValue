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

        public KVObject GetObject()
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
            return state.IsArray ? MakeArray(state) : MakeObject(state);
        }

        readonly Stack<KVPartialState> stateStack = new();

        public void OnKeyValuePair(string name, KVValue value)
        {
            if (StateStack.Count > 0)
            {
                var state = StateStack.Peek();
                state.Items.Add(new KVObject(name, value));
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

        public void OnArrayValue(KVValue value)
        {
            if (StateStack.Count > 0)
            {
                var state = StateStack.Peek();
                state.Items.Add(new KVObject(null, value));
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
            StateStack.Peek().Items.Add(completedObject);
        }

        public void OnArrayEnd()
        {
            if (StateStack.Count <= 1)
            {
                return;
            }

            var state = StateStack.Pop();
            var completedObject = MakeArray(state);
            StateStack.Peek().Items.Add(completedObject);
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

        KVObject MakeObject(KVPartialState state)
        {
            if (state.Discard)
            {
                return null;
            }

            if (state.IsArray)
            {
                throw new InvalidCastException("Tried to make an object ouf of an array.");
            }

            if (state.Value != null)
            {
                return new KVObject(state.Key, state.Value.Value);
            }

            KVValue collectionValue = useDictionary
                ? KVValue.CreateDictCollection(state.Items)
                : new KVValue(KVValueType.Collection, state.Items);

            if (state.Flag != KVFlag.None)
            {
                collectionValue = collectionValue with { Flag = state.Flag };
            }

            return new KVObject(state.Key, collectionValue);
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
                return new KVObject(state.Key, state.Value.Value);
            }

            var value = new KVValue(KVValueType.Array, state.Items);

            if (state.Flag != KVFlag.None)
            {
                value = value with { Flag = state.Flag };
            }

            return new KVObject(state.Key, value);
        }
    }
}
