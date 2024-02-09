using System.Reflection;

namespace ValveKeyValue
{
    sealed class DefaultObjectReflector : IObjectReflector
    {
        IEnumerable<IObjectMember> IObjectReflector.GetMembers(object @object)
        {
            Require.NotNull(@object, nameof(@object));

            var objectType = @object.GetType();

            if (IsValueTupleType(objectType))
            {
                var fields = objectType.GetFields(BindingFlags.Instance | BindingFlags.Public);

                foreach (var field in fields)
                {
                    yield return new FieldMember(field, @object);
                }
            }
            else
            {
                var properties = objectType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                foreach (var property in properties)
                {
                    if (property.GetCustomAttribute<KVIgnoreAttribute>() != null)
                    {
                        continue;
                    }

                    yield return new PropertyMember(property, @object);
                }
            }
        }

        static bool IsValueTupleType(Type type)
            => type.IsGenericType && type.FullName.StartsWith("System.ValueTuple`");
    }
}
