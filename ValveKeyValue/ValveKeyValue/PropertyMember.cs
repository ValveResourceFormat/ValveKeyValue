using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace ValveKeyValue
{
    sealed class PropertyMember : IObjectMember
    {
        public PropertyMember(PropertyInfo propertyInfo, object @object, bool canRead, bool canWrite)
        {
            ArgumentNullException.ThrowIfNull(propertyInfo);
            ArgumentNullException.ThrowIfNull(@object);

            this.propertyInfo = propertyInfo;
            this.@object = @object;
            CanRead = canRead;
            CanWrite = canWrite;
        }

        readonly PropertyInfo propertyInfo;
        readonly object @object;

        bool IObjectMember.IsExplicitName => PropertyAttribute != null;

        string IObjectMember.Name
            => PropertyAttribute?.PropertyName ?? propertyInfo.Name;

        public bool CanRead { get; }

        public bool CanWrite { get; }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2073", Justification = "PropertyType")]
        [DynamicallyAccessedMembers(Trimming.Properties)]
        Type IObjectMember.MemberType => propertyInfo.PropertyType;

        object? IObjectMember.Value
        {
            get { return propertyInfo.GetValue(@object); }
            set { propertyInfo.SetValue(@object, value); }
        }

        KVPropertyAttribute? PropertyAttribute => propertyInfo.GetCustomAttribute<KVPropertyAttribute>();
    }
}
