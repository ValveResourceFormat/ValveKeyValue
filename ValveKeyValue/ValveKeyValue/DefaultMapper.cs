using System;
using System.Linq;
using System.Reflection;

namespace ValveKeyValue
{
   sealed class DefaultMapper : IPropertyMapper
    {
        string IPropertyMapper.MapFromKeyValue(Type objectType, string propertyName)
        {
            var property = objectType.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                .FirstOrDefault(p => string.Equals(p.Name, propertyName, StringComparison.OrdinalIgnoreCase));

            return property?.Name ?? propertyName;
        }
    }
}
