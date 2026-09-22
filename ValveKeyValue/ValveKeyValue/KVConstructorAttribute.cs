namespace ValveKeyValue
{
    /// <summary>
    /// This attribute is used to tell the deserializer which constructor to use when creating
    /// an instance of a type. Constructor parameters are bound to KeyValues children by name.
    /// </summary>
    [AttributeUsage(AttributeTargets.Constructor, Inherited = false, AllowMultiple = false)]
    public sealed class KVConstructorAttribute : Attribute
    {
    }
}
