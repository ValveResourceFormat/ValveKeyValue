using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace ValveKeyValue
{
    sealed class DefaultObjectReflector : IObjectReflector
    {
        IEnumerable<IObjectMember> IObjectReflector.GetMembers([DynamicallyAccessedMembers(Trimming.Properties)] Type objectType, object @object)
        {
            Require.NotNull(objectType, nameof(objectType));
            Require.NotNull(@object, nameof(@object));

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
