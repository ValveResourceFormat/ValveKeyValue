using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace ValveKeyValue
{
    sealed class DefaultObjectReflector : IObjectReflector
    {
        IEnumerable<IObjectMember> IObjectReflector.GetMembers([DynamicallyAccessedMembers(Trimming.Properties)] Type objectType)
        {
            ArgumentNullException.ThrowIfNull(objectType);

            if (IsValueTupleType(objectType))
            {
                var fields = objectType.GetFields(BindingFlags.Instance | BindingFlags.Public);

                foreach (var field in fields)
                {
                    yield return new FieldMember(field);
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

                    if (IsRecordEqualityContract(property))
                    {
                        continue;
                    }

                    // A property is visible when one of its accessors is public, or when it is
                    // explicitly opted in. Opting in makes any accessor usable regardless of visibility.
                    var included = property.GetCustomAttribute<KVIncludeAttribute>() != null;
                    var canRead = property.GetMethod is { } getter && (included || getter.IsPublic);
                    var canWrite = property.SetMethod is { } setter && (included || setter.IsPublic);

                    if (!canRead && !canWrite)
                    {
                        continue;
                    }

                    yield return new PropertyMember(property, canRead, canWrite);
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
