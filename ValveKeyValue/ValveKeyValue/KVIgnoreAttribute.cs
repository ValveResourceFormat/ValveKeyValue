using System;

namespace ValveKeyValue
{
    /// <summary>
    /// This attribute is used to tell the deserializer to ignore a given property.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public sealed class KVIgnoreAttribute : Attribute
    {
    }
}
