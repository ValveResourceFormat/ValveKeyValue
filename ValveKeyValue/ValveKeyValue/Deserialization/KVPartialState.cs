namespace ValveKeyValue.Deserialization
{
    class KVPartialState
    {
        public string Key { get; set; }

        public KVFlag Flag { get; set; }

        public KVObject Value { get; set; }

        public List<KeyValuePair<string, KVObject>> Items { get; } = new List<KeyValuePair<string, KVObject>>();

        public bool Discard { get; set; }

        public bool IsArray { get; set; }
    }
}
