namespace ValveKeyValue.Deserialization.KeyValues3
{
    enum KV3TextReaderState
    {
        InObjectBeforeKey,
        InObjectAfterKey,
        InObjectBeforeValue,
        InObjectAfterValue,
        InArray,
    }
}
