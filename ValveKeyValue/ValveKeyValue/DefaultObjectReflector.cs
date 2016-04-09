using System.Collections.Generic;
using System.Reflection;

namespace ValveKeyValue
{
    sealed class DefaultObjectReflector : IObjectReflector
    {
        IEnumerable<IObjectMember> IObjectReflector.GetMembers(object @object)
        {
            Require.NotNull(@object, nameof(@object));
            var properties = @object.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

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
