namespace ValveKeyValue.Deserialization
{
    class KVPartialState
    {
        public string Key { get; set; }

        public KVValue Value { get; set; }

        public IList<KVObject> Items { get; } = new List<KVObject>();

        // TODO: Somehow merge with Items?
        public IList<KVValue> Children { get; } = new List<KVValue>();

        public bool Discard { get; set; }
    }
}
