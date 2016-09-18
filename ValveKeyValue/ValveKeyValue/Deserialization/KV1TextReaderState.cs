namespace ValveKeyValue.Deserialization
{
    enum KV1TextReaderState
    {
       InObjectBeforeKey,
       InObjectBetweenKeyAndValue,
       InObjectAfterValue
    }
}
