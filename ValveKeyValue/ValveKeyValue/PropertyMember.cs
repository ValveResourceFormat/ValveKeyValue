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
            propertyAttribute = propertyInfo.GetCustomAttribute<KVPropertyAttribute>();
        }

        readonly PropertyInfo propertyInfo;
        readonly object @object;
        readonly KVPropertyAttribute? propertyAttribute;

        bool IObjectMember.IsExplicitName => propertyAttribute != null;

        string IObjectMember.Name
            => propertyAttribute?.PropertyName ?? propertyInfo.Name;

        bool IObjectMember.CanWrite => propertyInfo.CanWrite;

        Type IObjectMember.MemberType => propertyInfo.PropertyType;

        object? IObjectMember.Value
        {
            get { return propertyInfo.GetValue(@object); }
            set { propertyInfo.SetValue(@object, value); }
        }
    }
}
