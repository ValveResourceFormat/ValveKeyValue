using System;
using System.Collections.Generic;
using ValveKeyValue.Abstraction;

namespace ValveKeyValue.Deserialization
{
    class KVObjectBuilder : IParsingVisitationListener
    {
        readonly IList<KVObjectBuilder> associatedBuilders = new List<KVObjectBuilder>();

        public KVObject GetObject()
        {
            if (stateStack.Count != 1)
            {
                throw new KeyValueException("Builder is not in a fully completed state.");
            }

            foreach (var associatedBuilder in associatedBuilders)
            {
                associatedBuilder.FinalizeState();
            }

            var state = stateStack.Peek();
            return MakeObject(state);
        }

        readonly Stack<KVPartialState> stateStack = new Stack<KVPartialState>();

        public void OnKeyValuePair(string name, KVValue value)
        {
            if (StateStack.Count > 0)
            {
                var state = StateStack.Peek();
                state.Items.Add(new KVObject(name, value));
            }
            else
            {
                var state = new KVPartialState();
                state.Key = name;
                state.Value = value;

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

            var parentState = StateStack.Peek();
            parentState.Items.Add(completedObject);
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

        public void OnObjectStart(string name)
        {
            var state = new KVPartialState();
            state.Key = name;
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
        }

        KVObject MakeObject(KVPartialState state)
        {
            if (state.Discard)
            {
                return null;
            }

            KVObject @object;

            if (state.Value != null)
            {
                @object = new KVObject(state.Key, state.Value);
            }
            else
            {
                @object = new KVObject(state.Key, state.Items);
            }

            return @object;
        }
    }
}
