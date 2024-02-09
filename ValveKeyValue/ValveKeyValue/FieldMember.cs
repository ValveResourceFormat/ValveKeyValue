using System.Reflection;

namespace ValveKeyValue
{
    sealed class FieldMember : IObjectMember
    {
        public FieldMember(FieldInfo fieldInfo, object @object)
        {
            Require.NotNull(fieldInfo, nameof(fieldInfo));
            Require.NotNull(@object, nameof(@object));

            this.fieldInfo = fieldInfo;
            this.@object = @object;
        }

        readonly FieldInfo fieldInfo;
        readonly object @object;

        public bool IsExplicitName => false;

        public string Name => fieldInfo.Name;

        public Type MemberType => fieldInfo.FieldType;

        public object Value
        {
            get => fieldInfo.GetValue(@object);
            set => fieldInfo.SetValue(@object, value);
        }
    }
}
