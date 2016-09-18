using System.Collections.Generic;

namespace ValveKeyValue.Deserialization
{
    class KVPartialState<TState> : KVPartialState
    {
        public Stack<TState> States { get; } = new Stack<TState>();
    }
}
