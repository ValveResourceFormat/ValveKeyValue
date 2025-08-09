using System.Diagnostics.CodeAnalysis;

namespace ValveKeyValue
{
    interface IObjectMember
    {
        bool IsExplicitName { get; }

        string Name { get; }

        [DynamicallyAccessedMembers(Trimming.Properties)]
        Type MemberType { get; }

        object Value { get; set; }
    }
}
