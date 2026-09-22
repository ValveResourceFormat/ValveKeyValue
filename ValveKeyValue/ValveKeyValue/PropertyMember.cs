using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace ValveKeyValue
{
    sealed class PropertyMember : IObjectMember
    {
        public PropertyMember(PropertyInfo propertyInfo, bool canRead, bool canWrite)
        {
            ArgumentNullException.ThrowIfNull(propertyInfo);

            this.propertyInfo = propertyInfo;
            CanRead = canRead;
            CanWrite = canWrite;
        }

        readonly PropertyInfo propertyInfo;

        bool IObjectMember.IsExplicitName => PropertyAttribute != null;

        string IObjectMember.Name
            => PropertyAttribute?.PropertyName ?? propertyInfo.Name;

        string IObjectMember.DeclaredName => propertyInfo.Name;

        public bool CanRead { get; }

        public bool CanWrite { get; }

        bool IObjectMember.IsRequired => propertyInfo.GetCustomAttribute<RequiredMemberAttribute>() != null;

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2073", Justification = "PropertyType")]
        [DynamicallyAccessedMembers(Trimming.Properties)]
        Type IObjectMember.MemberType => propertyInfo.PropertyType;

        object? IObjectMember.GetValue(object @object) => propertyInfo.GetValue(@object);

        void IObjectMember.SetValue(object @object, object? value) => propertyInfo.SetValue(@object, value);

        KVPropertyAttribute? PropertyAttribute => propertyInfo.GetCustomAttribute<KVPropertyAttribute>();
    }
}
