using System.Diagnostics.CodeAnalysis;

namespace ValveKeyValue
{
    interface IObjectMember
    {
        bool IsExplicitName { get; }

        string Name { get; }

        bool CanRead { get; }

        bool CanWrite { get; }

        [DynamicallyAccessedMembers(Trimming.Properties)]
        Type MemberType { get; }

        object? Value { get; set; }
    }
}
