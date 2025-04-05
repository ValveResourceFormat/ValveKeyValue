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
        Flag,
        Assignment,
        Comma,
        CommentBlock,
        ArrayStart,
        ArrayEnd,
        BinaryBlob,
    }
}
