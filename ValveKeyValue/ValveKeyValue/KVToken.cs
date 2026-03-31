namespace ValveKeyValue
{
    readonly record struct KVToken(KVTokenType TokenType, string Value = null);
}
