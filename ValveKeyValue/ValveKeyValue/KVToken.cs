namespace ValveKeyValue
{
    class KVToken
    {
        public KVToken(KVTokenType type)
            : this(type, null)
        {
        }

        public KVToken(KVTokenType type, string value)
        {
            TokenType = type;
            Value = value;
        }

        public KVTokenType TokenType { get; }

        public string Value { get; }
    }
}
