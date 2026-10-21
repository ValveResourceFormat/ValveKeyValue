namespace ValveKeyValue.Deserialization
{
    class KVPartialState
    {
        public string? Key { get; set; }

        public KVFlag Flag { get; set; }

        public KVObject? Value { get; set; }

        public List<KeyValuePair<string, KVObject>> Items { get; private set; } = new List<KeyValuePair<string, KVObject>>();

        public bool Discard { get; set; }

        public bool IsArray { get; set; }

        // Hands the items over to a new owner and starts this state on a fresh list.
        public List<KeyValuePair<string, KVObject>> DetachItems()
        {
            var items = Items;
            Items = new List<KeyValuePair<string, KVObject>>();
            return items;
        }

        public void Reset()
        {
            Key = null;
            Flag = KVFlag.None;
            Value = null;
            Items.Clear();
            Discard = false;
            IsArray = false;
        }
    }
}
