namespace ValveKeyValue
{
    enum KVTokenType
    {
        ObjectStart,
        ObjectEnd,
        String,
        EndOfFile,
        Comment,
        Condition,
        IncludeAndAppend,
        IncludeAndMerge
    }
}
