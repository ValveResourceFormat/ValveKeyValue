using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace ValveKeyValue
{
    sealed class DefaultObjectReflector : IObjectReflector
    {
        IEnumerable<IObjectMember> IObjectReflector.GetMembers(
#if NET5_0_OR_GREATER
            [DynamicallyAccessedMembers(Trimming.Properties)]
#endif
            Type type,
            object @object)
        {
            Require.NotNull(type, nameof(type));
            Require.NotNull(@object, nameof(@object));

            var properties = type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return GetMembersFromProperties(@object, properties);

        }

        static IEnumerable<IObjectMember> GetMembersFromProperties(object @object, PropertyInfo[] properties)
        {
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
}
