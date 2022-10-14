namespace ValveKeyValue.Deserialization.KeyValues1
{
    enum KV1TextReaderState
    {
        InObjectBeforeKey,
        InObjectBetweenKeyAndValue,
        InObjectAfterValue
    }
}
