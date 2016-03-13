using System;
using System.Linq;
using System.Reflection;

namespace ValveKeyValue
{
   sealed class DefaultMapper : IPropertyMapper
    {
        PropertyInfo IPropertyMapper.MapFromKeyValue(Type objectType, string propertyName)
        {
            var properties = objectType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

            // First search for properties with a [KVProperty("...")] attribute.
            var property = properties
                .FirstOrDefault(p => p.GetCustomAttribute<KVPropertyAttribute>()?.PropertyName == propertyName);
            if (property != null)
            {
                return property;
            }

            // Next, search by name.
            property = properties
                .FirstOrDefault(p => string.Equals(p.Name, propertyName, StringComparison.OrdinalIgnoreCase));

            // Drop it if it has a [KVIgnore].
            if (property?.GetCustomAttribute<KVIgnoreAttribute>() != null)
            {
                return null;
            }

            return property;
        }
    }
}
