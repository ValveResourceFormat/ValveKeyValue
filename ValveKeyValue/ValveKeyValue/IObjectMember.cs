namespace ValveKeyValue
{
    interface IObjectMember
    {
        bool IsExplicitName { get; }

        string Name { get; }

        bool CanWrite { get; }

        Type MemberType { get; }

        object? Value { get; set; }
    }
}
