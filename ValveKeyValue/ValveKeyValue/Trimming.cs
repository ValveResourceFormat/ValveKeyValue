using System.Diagnostics.CodeAnalysis;

namespace ValveKeyValue
{
    internal static class Trimming
    {
        public const DynamicallyAccessedMemberTypes Constructors =
            DynamicallyAccessedMemberTypes.PublicParameterlessConstructor |
            DynamicallyAccessedMemberTypes.PublicConstructors |
            DynamicallyAccessedMemberTypes.NonPublicConstructors;

        public const DynamicallyAccessedMemberTypes Properties =
            DynamicallyAccessedMemberTypes.PublicProperties |
            DynamicallyAccessedMemberTypes.NonPublicProperties |
            DynamicallyAccessedMemberTypes.PublicFields;

        public const string ObjectMappingMessage =
            "Mapping objects reflects over the members of nested types, which may be trimmed. Use KVObject or KVDocument instead.";

        public const string ObjectDeserializationMessage =
            "Deserializing into collections and dictionaries creates generic instantiations at runtime. Use KVObject or KVDocument instead.";
    }
}
