using System;
using System.Reflection;

namespace ValveKeyValue
{
    interface IPropertyMapper
    {
        PropertyInfo MapFromKeyValue(Type objectType, string propertyName);
    }
}
