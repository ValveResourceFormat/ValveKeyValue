using System.Linq;
using System.Runtime.CompilerServices;
using Microsoft.CodeAnalysis;

namespace ValveKeyValue.SourceGenerator
{
    // The types that the generator recognizes, resolved once per compilation. A type that the compilation does not
    // contain is null and matches nothing.
    sealed class KnownTypes
    {
        static readonly ConditionalWeakTable<Compilation, KnownTypes> Cache = new();

        KnownTypes(Compilation compilation)
        {
            Dictionary = compilation.GetTypeByMetadataName("System.Collections.Generic.Dictionary`2");
            IDictionary = compilation.GetTypeByMetadataName("System.Collections.Generic.IDictionary`2");
            IReadOnlyDictionary = compilation.GetTypeByMetadataName("System.Collections.Generic.IReadOnlyDictionary`2");
            KeyValuePair = compilation.GetTypeByMetadataName("System.Collections.Generic.KeyValuePair`2");
            ILookup = compilation.GetTypeByMetadataName("System.Linq.ILookup`2");
            NonGenericIDictionary = compilation.GetTypeByMetadataName("System.Collections.IDictionary");
            Type = compilation.GetTypeByMetadataName("System.Type");
            LambdaExpression = compilation.GetTypeByMetadataName("System.Linq.Expressions.LambdaExpression");
            ObsoleteAttribute = compilation.GetTypeByMetadataName("System.ObsoleteAttribute");
            CompilerGeneratedAttribute = compilation.GetTypeByMetadataName("System.Runtime.CompilerServices.CompilerGeneratedAttribute");
            SetsRequiredMembersAttribute = compilation.GetTypeByMetadataName("System.Diagnostics.CodeAnalysis.SetsRequiredMembersAttribute");
            KVSerializer = compilation.GetTypeByMetadataName("ValveKeyValue.KVSerializer");
            KVConstructorAttribute = compilation.GetTypeByMetadataName("ValveKeyValue.KVConstructorAttribute");
            KVIgnoreAttribute = compilation.GetTypeByMetadataName("ValveKeyValue.KVIgnoreAttribute");
            KVIncludeAttribute = compilation.GetTypeByMetadataName("ValveKeyValue.KVIncludeAttribute");
            KVPropertyAttribute = compilation.GetTypeByMetadataName("ValveKeyValue.KVPropertyAttribute");
        }

        public INamedTypeSymbol? Dictionary { get; }

        public INamedTypeSymbol? IDictionary { get; }

        public INamedTypeSymbol? IReadOnlyDictionary { get; }

        public INamedTypeSymbol? KeyValuePair { get; }

        public INamedTypeSymbol? ILookup { get; }

        public INamedTypeSymbol? NonGenericIDictionary { get; }

        public INamedTypeSymbol? Type { get; }

        public INamedTypeSymbol? LambdaExpression { get; }

        public INamedTypeSymbol? ObsoleteAttribute { get; }

        public INamedTypeSymbol? CompilerGeneratedAttribute { get; }

        public INamedTypeSymbol? SetsRequiredMembersAttribute { get; }

        public INamedTypeSymbol? KVSerializer { get; }

        public INamedTypeSymbol? KVConstructorAttribute { get; }

        public INamedTypeSymbol? KVIgnoreAttribute { get; }

        public INamedTypeSymbol? KVIncludeAttribute { get; }

        public INamedTypeSymbol? KVPropertyAttribute { get; }

        public static KnownTypes Get(Compilation compilation) => Cache.GetValue(compilation, static c => new KnownTypes(c));

        // Compares the definition of a possibly constructed type with a known type.
        public static bool Is(ITypeSymbol? type, INamedTypeSymbol? known)
            => type is not null && known is not null && SymbolEqualityComparer.Default.Equals(type.OriginalDefinition, known);

        public static AttributeData? GetAttribute(ISymbol symbol, INamedTypeSymbol? attributeType)
            => attributeType is null ? null : symbol.GetAttributes().FirstOrDefault(attribute => Is(attribute.AttributeClass, attributeType));
    }
}
