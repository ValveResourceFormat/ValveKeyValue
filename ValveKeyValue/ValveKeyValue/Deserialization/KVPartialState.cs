namespace ValveKeyValue.Deserialization
{
    class KVPartialState
    {
        public string Key { get; set; }

        public KVFlag Flag { get; set; }

        public KVValue? Value { get; set; }

        public List<KVObject> Items { get; } = new List<KVObject>();

        public bool Discard { get; set; }

        public bool IsArray { get; set; }
    }
}
