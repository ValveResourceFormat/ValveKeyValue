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
        IncludeAndMerge,

        // KeyValues3
        Header,
        Identifier,
        Assignment,
        CommentBlock,
        ArrayStart,
        ArrayEnd,
    }
}
