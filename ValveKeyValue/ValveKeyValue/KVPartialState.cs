using System.Collections.Generic;

namespace ValveKeyValue
{
    class KVPartialState
    {
        public string Key { get; set; }

        public KVValue Value { get; set; }

        public IList<KVObject> Items { get; } = new List<KVObject>();

        public Stack<KVTextReaderState> States { get; } = new Stack<KVTextReaderState>();
    }
}
