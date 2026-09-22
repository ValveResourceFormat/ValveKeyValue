using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace ValveKeyValue
{
    sealed class FieldMember : IObjectMember
    {
        public FieldMember(FieldInfo fieldInfo)
        {
            ArgumentNullException.ThrowIfNull(fieldInfo);

            this.fieldInfo = fieldInfo;
        }

        readonly FieldInfo fieldInfo;

        public bool IsExplicitName => false;

        public string Name => fieldInfo.Name;

        public string DeclaredName => fieldInfo.Name;

        public bool CanRead => true;

        public bool CanWrite => !fieldInfo.IsInitOnly;

        public bool IsRequired => false;

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2073", Justification = "FieldType")]
        [DynamicallyAccessedMembers(Trimming.Properties)]
        public Type MemberType => fieldInfo.FieldType;

        public object? GetValue(object @object) => fieldInfo.GetValue(@object);

        public void SetValue(object @object, object? value) => fieldInfo.SetValue(@object, value);
    }
}
