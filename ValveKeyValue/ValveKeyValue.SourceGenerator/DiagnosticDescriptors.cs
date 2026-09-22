using Microsoft.CodeAnalysis;

namespace ValveKeyValue.SourceGenerator
{
    // Every diagnostic is informational: a call that cannot be intercepted keeps working through reflection,
    // and trimming or AOT compilation reports IL2026/IL3050 for it. The identifiers VKV0004 and VKV0005 are not used.
    static class DiagnosticDescriptors
    {
        const string Category = "ValveKeyValue";

        public static DiagnosticDescriptor TypeParameter { get; } = new(
            "VKV0001",
            "Type argument contains a type parameter",
            "The type argument '{0}' contains a type parameter; this call uses reflection-based object mapping. Pass a KVTypeInfo<T> obtained from KVSerializer.GetTypeInfo<T>() at a call site with a concrete type argument instead.",
            Category,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor Inaccessible { get; } = new(
            "VKV0002",
            "Type is not accessible from generated code",
            "The type '{0}' at '{1}' is not accessible from generated code; this call uses reflection-based object mapping",
            Category,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor Unsupported { get; } = new(
            "VKV0003",
            "Type is not supported by the source generator",
            "The type '{0}' at '{1}' is not supported by the source generator ({2}); this call uses reflection-based object mapping",
            Category,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor LanguageVersion { get; } = new(
            "VKV0006",
            "Language version does not support interceptors",
            "Interceptors require C# 12 or later; this call uses reflection-based object mapping",
            Category,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true);

        public static DiagnosticDescriptor InterceptorsNotEnabled { get; } = new(
            "VKV0007",
            "Interceptors are not enabled for the generated namespace",
            "Interceptors are not enabled for the 'ValveKeyValue.Generated' namespace; this call uses reflection-based object mapping. Add '<InterceptorsNamespaces>$(InterceptorsNamespaces);ValveKeyValue.Generated</InterceptorsNamespaces>' to the project file.",
            Category,
            DiagnosticSeverity.Info,
            isEnabledByDefault: true);
    }
}
