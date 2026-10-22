using System.Reflection;

namespace ValveKeyValue
{
    sealed class PropertyMember : IObjectMember
    {
        public PropertyMember(PropertyInfo propertyInfo, object @object)
        {
            ArgumentNullException.ThrowIfNull(propertyInfo);
            ArgumentNullException.ThrowIfNull(@object);

            this.propertyInfo = propertyInfo;
            this.@object = @object;
        }

        readonly PropertyInfo propertyInfo;
        readonly object @object;

        bool IObjectMember.IsExplicitName => PropertyAttribute != null;

        string IObjectMember.Name
            => PropertyAttribute?.PropertyName ?? propertyInfo.Name;

        bool IObjectMember.CanWrite => propertyInfo.CanWrite;

        Type IObjectMember.MemberType => propertyInfo.PropertyType;

        object? IObjectMember.Value
        {
            get { return propertyInfo.GetValue(@object); }
            set { propertyInfo.SetValue(@object, value); }
        }

        KVPropertyAttribute? PropertyAttribute => propertyInfo.GetCustomAttribute<KVPropertyAttribute>();
    }
}
