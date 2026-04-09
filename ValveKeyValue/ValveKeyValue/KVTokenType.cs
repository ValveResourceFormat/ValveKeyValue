namespace ValveKeyValue
{
    /// <summary>
    /// Lexer-level token kinds shared between KV1 and KV3, also used by source maps to
    /// describe each emitted/parsed token's role.
    /// </summary>
    public enum KVTokenType
    {
        /// <summary>The default/unset value. Never produced by the lexer or source maps.</summary>
        Invalid = 0,

        /// <summary>Object opening brace <c>{</c>.</summary>
        ObjectStart,
        /// <summary>Object closing brace <c>}</c>.</summary>
        ObjectEnd,
        /// <summary>
        /// A token in key position. The raw lexer sees keys as <see cref="String"/> or
        /// <see cref="Identifier"/>; source maps tag them as <see cref="Key"/> because the
        /// serializer and parser both know which side of the assignment a token sits on.
        /// </summary>
        Key,
        /// <summary>Quoted string in value position.</summary>
        String,
        /// <summary>End of input. Not produced by source maps.</summary>
        EndOfFile,
        /// <summary>A line comment (<c>//</c>).</summary>
        Comment,
        /// <summary>A KV1 conditional (e.g. <c>[$X360]</c>), including the surrounding brackets.</summary>
        Condition,
        /// <summary>A KV1 <c>#include</c> directive.</summary>
        IncludeAndAppend,
        /// <summary>A KV1 <c>#base</c> directive.</summary>
        IncludeAndMerge,

        // KeyValues3

        /// <summary>The KV3 header line (<c>&lt;!-- kv3 ... --&gt;</c>).</summary>
        Header,
        /// <summary>An unquoted identifier. May be a key or a bare value literal (true/false/null/numeric) in KV3.</summary>
        Identifier,
        /// <summary>A KV3 flag prefix (e.g. <c>resource:</c>, <c>entity_name:</c>).</summary>
        Flag,
        /// <summary>The KV3 assignment operator <c>=</c>.</summary>
        Assignment,
        /// <summary>The KV3 array element separator <c>,</c>.</summary>
        Comma,
        /// <summary>A block comment (<c>/* ... */</c>).</summary>
        CommentBlock,
        /// <summary>Array opening bracket <c>[</c>.</summary>
        ArrayStart,
        /// <summary>Array closing bracket <c>]</c>.</summary>
        ArrayEnd,
        /// <summary>A KV3 binary blob value <c>#[ FF AA ... ]</c>.</summary>
        BinaryBlob,
    }
}
