namespace ValveKeyValue.Deserialization.KeyValues3
{
    enum KV3TextReaderState
    {
        Header,
        InObjectBeforeKey,
        InObjectBetweenKeyAndValue,
        InObjectBeforeValue,
        InObjectAfterValue
    }
}
