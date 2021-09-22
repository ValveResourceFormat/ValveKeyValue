#if NET5_0_OR_GREATER
using System.Diagnostics.CodeAnalysis;

namespace ValveKeyValue
{
    internal static class Trimming
    {
        public const DynamicallyAccessedMemberTypes Constructors = DynamicallyAccessedMemberTypes.PublicParameterlessConstructor | DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.NonPublicConstructors;
        public const DynamicallyAccessedMemberTypes Properties = DynamicallyAccessedMemberTypes.PublicProperties | DynamicallyAccessedMemberTypes.NonPublicProperties;
    }
}
#endif
