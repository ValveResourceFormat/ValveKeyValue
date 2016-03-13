using System;

namespace ValveKeyValue
{
    interface IPropertyMapper
    {
        string MapFromKeyValue(Type objectType, string propertyName);
    }
}
