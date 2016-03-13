using System;

namespace ValveKeyValue
{
    /// <summary>
    /// This attribute is used to tell the deserializer to map a given property to a particular
    /// node in the KeyValue object tree, by name.
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, Inherited = true, AllowMultiple = false)]
    public sealed class KVPropertyAttribute : Attribute
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="KVPropertyAttribute"/> class.
        /// </summary>
        /// <param name="propertyName">The name of the property as it appears in KeyValues serialized data.</param>
        public KVPropertyAttribute(string propertyName)
        {
            Require.NotNull(propertyName, nameof(propertyName));
            PropertyName = propertyName;
        }

        /// <summary>
        /// Gets the name of the property as it appears in KeyValues serialized data.
        /// </summary>
        public string PropertyName { get; }
    }
}
