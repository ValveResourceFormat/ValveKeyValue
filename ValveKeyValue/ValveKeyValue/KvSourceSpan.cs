namespace ValveKeyValue
{
    /// <summary>
    /// Describes a single token's character range within KeyValues text.
    /// Produced by source-map serializers (offsets refer to freshly serialized text) or
    /// by source-map parsers (offsets refer to the input text being parsed).
    /// </summary>
    /// <param name="Start">Inclusive start character offset.</param>
    /// <param name="End">Exclusive end character offset.</param>
    /// <param name="TokenType">The lexer-level role of the span.</param>
    public readonly record struct KvSourceSpan(int Start, int End, KVTokenType TokenType);
}
