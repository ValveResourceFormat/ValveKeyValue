namespace ValveKeyValue
{
    /// <summary>
    /// This attribute is used to tell the serializer and deserializer to map a given property
    /// even when the property, or one of its accessors, is not public.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public sealed class KVIncludeAttribute : Attribute
    {
    }
}
