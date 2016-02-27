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
            this.type = type;
            this.value = value;
        }

        readonly KVTokenType type;
        readonly string value;

        public KVTokenType TokenType => type;

        public string Value => value;
    }
}
