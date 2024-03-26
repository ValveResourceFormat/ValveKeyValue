namespace ValveKeyValue.Deserialization.KeyValues3
{
    enum KV3TextReaderState
    {
        InObjectBeforeKey,
        InObjectAfterKey,
        InObjectAfterValue,
        InArray,
    }
}
