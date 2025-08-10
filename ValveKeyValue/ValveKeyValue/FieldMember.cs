using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace ValveKeyValue
{
    sealed class FieldMember : IObjectMember
    {
        public FieldMember(FieldInfo fieldInfo, object @object)
        {
            ArgumentNullException.ThrowIfNull(fieldInfo);
            ArgumentNullException.ThrowIfNull(@object);

            this.fieldInfo = fieldInfo;
            this.@object = @object;
        }

        readonly FieldInfo fieldInfo;
        readonly object @object;

        public bool IsExplicitName => false;

        public string Name => fieldInfo.Name;

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2073", Justification = "FieldType")]
        [DynamicallyAccessedMembers(Trimming.Properties)]
        public Type MemberType => fieldInfo.FieldType;

        public object Value
        {
            get => fieldInfo.GetValue(@object);
            set => fieldInfo.SetValue(@object, value);
        }
    }
}
