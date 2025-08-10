using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace ValveKeyValue
{
    sealed class PropertyMember : IObjectMember
    {
        public PropertyMember(PropertyInfo propertyInfo, object @object)
        {
            Require.NotNull(propertyInfo, nameof(propertyInfo));
            Require.NotNull(@object, nameof(@object));

            this.propertyInfo = propertyInfo;
            this.@object = @object;
        }

        readonly PropertyInfo propertyInfo;
        readonly object @object;

        bool IObjectMember.IsExplicitName => PropertyAttribute != null;

        string IObjectMember.Name
            => PropertyAttribute?.PropertyName ?? propertyInfo.Name;

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2073", Justification = "PropertyType")]
        [DynamicallyAccessedMembers(Trimming.Properties)]
        Type IObjectMember.MemberType => propertyInfo.PropertyType;

        object IObjectMember.Value
        {
            get { return propertyInfo.GetValue(@object); }
            set { propertyInfo.SetValue(@object, value); }
        }

        KVPropertyAttribute PropertyAttribute => propertyInfo.GetCustomAttribute<KVPropertyAttribute>();
    }
}
