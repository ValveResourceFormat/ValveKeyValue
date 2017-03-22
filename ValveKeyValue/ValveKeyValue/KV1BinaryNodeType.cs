namespace ValveKeyValue
{
    enum KV1BinaryNodeType : byte
    {
        ChildObject = 0,
        String = 1,
        Int32 = 2,
        Float32 = 3,
        Pointer = 4,
        WideString = 5,
        Color = 6,
        UInt64 = 7,
        End = 8,
        Int64 = 10,
    }
}
