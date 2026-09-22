using System.ComponentModel;

namespace ValveKeyValue.Metadata
{
    /// <summary>
    /// Describes a constructor parameter that is bound from KeyValues data.
    /// This type is infrastructure for generated serialization code and is not intended to be used directly.
    /// Instances are created through <see cref="KVMetadata.Parameter{T}"/>.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public sealed class KVParameterInfo
    {
        internal KVParameterInfo(string name, KVTypeInfo type, object? missingValue)
        {
            Name = name;
            Type = type;
            MissingValue = missingValue;
        }

        /// <summary>
        /// Gets the declared name of the parameter.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the type information of the parameter.
        /// </summary>
        public KVTypeInfo Type { get; }

        // The value passed to the constructor when the KeyValues data does not contain the parameter.
        internal object? MissingValue { get; }
    }
}
