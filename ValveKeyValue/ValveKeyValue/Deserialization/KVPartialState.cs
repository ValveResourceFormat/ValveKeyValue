using System.Collections.Generic;

namespace ValveKeyValue.Deserialization
{
    class KVPartialState
    {
        public string Key { get; set; }

        public KVValue Value { get; set; }

        public IList<KVObject> Items { get; } = new List<KVObject>();

        public bool Discard { get; set; }
    }
}
