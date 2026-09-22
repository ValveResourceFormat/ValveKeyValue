using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Text;

namespace ValveKeyValue.SourceGenerator
{
    enum InterceptedMethod
    {
        Deserialize,
        Serialize,
        SerializeWithSourceMap,
        GetTypeInfo,
    }

    enum TypeModelKind
    {
        Scalar,
        Enum,
        Nullable,
        Array,
        Collection,
        Dictionary,
        Lookup,
        Object,
    }

    enum ConstructionKind
    {
        // The type cannot be created.
        None,

        // The type is created from its default value.
        Default,

        // The type is created through a constructor.
        Constructor,

        // Creating the type throws an exception.
        Throw,
    }

    enum AccessKind
    {
        None,
        Direct,
        Accessor,
    }

    enum AccessorKind
    {
        Getter,
        Setter,
        Constructor,
    }

    // The outcome of inspecting one call site: either a model to intercept it, or a diagnostic explaining why not.
    sealed record CallSiteResult(CallSiteModel? Model, DiagnosticInfo? Diagnostic);

    // An intercepted call site and every type that its type argument refers to.
    sealed record CallSiteModel(InterceptorModel Interceptor, EquatableArray<TypeModel> Types);

    // TypeInfo is the expression that returns the type information of the type argument.
    sealed record InterceptorModel(
        InterceptedMethod Method,
        string RootType,
        string TypeInfo,
        string InterceptsLocationAttribute,
        string DisplayLocation);

    // A type that needs a generated KVTypeInfo property, or a scalar that is referenced through KVMetadata directly.
    // Element holds the KVMetadata property expression of a scalar or the underlying, element or key type, and Value
    // the dictionary value type.
    sealed record TypeModel(
        string Type,
        TypeModelKind Kind,
        string? Element,
        string? Value,
        ObjectModel? Object);

    // NameHint is a short description of the type, used to name accessors. Exception is the exception that creating
    // the type throws when Construction is Throw, and ConstructorAccessor indexes into Accessors when the constructor
    // is called through an accessor.
    sealed record ObjectModel(
        string NameHint,
        bool IsValueType,
        ConstructionKind Construction,
        string? Exception,
        int ConstructorAccessor,
        EquatableArray<ParameterModel> Parameters,
        bool SetsRequiredMembers,
        EquatableArray<MemberModel> Members,
        EquatableArray<AccessorModel> Accessors);

    // ArgumentModifier is "ref " or "out " for a parameter whose argument must be a variable, otherwise empty.
    sealed record ParameterModel(string Name, string Type, string ArgumentModifier, string DefaultValue);

    // DirectAccess is the member access expression on a target named "t", used when an accessor is accessed directly.
    // GetterAccessor and SetterAccessor index into the accessors of the declaring object model.
    sealed record MemberModel(
        string Name,
        string DeclaredName,
        string Type,
        bool IsRequired,
        string DirectAccess,
        AccessKind Getter,
        int GetterAccessor,
        AccessKind Setter,
        int SetterAccessor);

    // An [UnsafeAccessor] extern. Types are written in terms of the generic parameters of the target definition
    // when Generic is set, in which case the extern is declared in a generic class instantiated with its type arguments.
    // The target is passed by reference when the declaring object model is a value type. Parameters is the parameter
    // list of a constructor accessor.
    sealed record AccessorModel(
        AccessorKind Kind,
        string MetadataName,
        string TargetType,
        string ValueType,
        string Parameters,
        GenericAccessorModel? Generic);

    // Name is the name of the target type definition, without its namespace, containing types and type parameters.
    // The type parameters, constraints and type arguments include those of the containing types, outermost first.
    sealed record GenericAccessorModel(string Definition, string Name, string TypeParameters, string Constraints, string TypeArguments);

    sealed record LocationInfo(string FilePath, TextSpan TextSpan, LinePositionSpan LineSpan)
    {
        public Location ToLocation() => Location.Create(FilePath, TextSpan, LineSpan);

        public static LocationInfo? From(Location location)
        {
            if (location.SourceTree is null)
            {
                return null;
            }

            return new LocationInfo(location.SourceTree.FilePath, location.SourceSpan, location.GetLineSpan().Span);
        }
    }

    sealed record DiagnosticInfo(DiagnosticDescriptor Descriptor, LocationInfo? Location, EquatableArray<string> Arguments)
    {
        public Diagnostic ToDiagnostic()
            => Diagnostic.Create(Descriptor, Location?.ToLocation() ?? Microsoft.CodeAnalysis.Location.None, [.. Arguments]);
    }
}
