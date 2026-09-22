using System.Diagnostics.CodeAnalysis;

namespace ValveKeyValue
{
    interface IObjectMember
    {
        bool IsExplicitName { get; }

        /// <summary>
        /// Gets the name of the member as it appears in KeyValues data.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets the name of the member as it is declared on the type.
        /// </summary>
        string DeclaredName { get; }

        bool CanRead { get; }

        bool CanWrite { get; }

        bool IsRequired { get; }

        [DynamicallyAccessedMembers(Trimming.Properties)]
        Type MemberType { get; }

        object? GetValue(object @object);

        void SetValue(object @object, object? value);
    }
}
