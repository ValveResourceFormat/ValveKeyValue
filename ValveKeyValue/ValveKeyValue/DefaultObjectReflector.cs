using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace ValveKeyValue
{
    sealed class DefaultObjectReflector : IObjectReflector
    {
        IEnumerable<IObjectMember> IObjectReflector.GetMembers([DynamicallyAccessedMembers(Trimming.Properties)] Type objectType, object @object)
        {
            ArgumentNullException.ThrowIfNull(objectType);
            ArgumentNullException.ThrowIfNull(@object);

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

                    if (property.GetIndexParameters().Length > 0 || IsRecordEqualityContract(property))
                    {
                        continue;
                    }

                    yield return new PropertyMember(property, @object);
                }
            }
        }

        static bool IsValueTupleType(Type type)
            => type.IsGenericType && type.FullName!.StartsWith("System.ValueTuple`", StringComparison.Ordinal);

        // Records synthesize a protected "Type EqualityContract" property which must not be treated as data.
        static bool IsRecordEqualityContract(PropertyInfo property)
            => property.Name == "EqualityContract"
            && property.PropertyType == typeof(Type)
            && property.GetMethod?.GetCustomAttribute<System.Runtime.CompilerServices.CompilerGeneratedAttribute>() != null;
    }
}
