using System.Collections.Concurrent;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Microsoft.CodeAnalysis;

namespace ValveKeyValue.SourceGenerator
{
    // Describes every type reachable from a root type, following the same rules as the reflection provider in
    // ValveKeyValue (KVReflectionTypeInfo), so that generated type information behaves identically.
    sealed class TypeGraphBuilder
    {
        static readonly ConditionalWeakTable<Compilation, ConcurrentDictionary<ITypeSymbol, TypeGraph>> Cache = new();

        TypeGraphBuilder(Compilation compilation, KnownTypes knownTypes)
        {
            this.compilation = compilation;
            this.knownTypes = knownTypes;
        }

        readonly Compilation compilation;
        readonly KnownTypes knownTypes;
        readonly Dictionary<string, TypeModel> models = new(StringComparer.Ordinal);
        readonly Dictionary<ITypeSymbol, string> renderedTypes = new(SymbolEqualityComparer.Default);
        readonly HashSet<string> objectsInProgress = new(StringComparer.Ordinal);
        HashSet<string> collectionsInProgress = new(StringComparer.Ordinal);

        // Returns the graph of a root type, which is built once per compilation. Root types that differ only in
        // nullable annotations or tuple element names are built separately, so that diagnostics name them as written.
        public static TypeGraph Build(Compilation compilation, KnownTypes knownTypes, ITypeSymbol root)
        {
            var graphs = Cache.GetValue(compilation, static _ => new ConcurrentDictionary<ITypeSymbol, TypeGraph>(SymbolEqualityComparer.IncludeNullability));

            return graphs.TryGetValue(root, out var graph)
                ? graph
                : graphs.GetOrAdd(root, type => new TypeGraphBuilder(compilation, knownTypes).BuildGraph(type));
        }

        TypeGraph BuildGraph(ITypeSymbol root)
        {
            try
            {
                var rootType = Add(root, TypePath.Root(root));
                return new TypeGraph(models[rootType], new EquatableArray<TypeModel>(models.Values), null, []);
            }
            catch (UnsupportedTypeException e)
            {
                return new TypeGraph(null, EquatableArray<TypeModel>.Empty, e.Descriptor, e.Arguments);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                // An unexpected failure leaves the call to reflection instead of failing the build.
                var display = root.ToDisplayString();
                return new TypeGraph(null, EquatableArray<TypeModel>.Empty, DiagnosticDescriptors.Unsupported, [display, display, "the source generator failed: " + e.Message]);
            }
        }

        // Returns the rendered type, after adding it and every type it refers to.
        string Add(ITypeSymbol type, TypePath path)
        {
            var key = Render(type);

            if (models.ContainsKey(key) || objectsInProgress.Contains(key))
            {
                return key;
            }

            if (TypeNames.GetScalarTypeInfo(type) is { } scalar)
            {
                models[key] = new TypeModel(key, TypeModelKind.Scalar, scalar, null, null);
                return key;
            }

            if (ContainsTypeParameter(type))
            {
                throw new UnsupportedTypeException(DiagnosticDescriptors.TypeParameter, type.ToDisplayString());
            }

            CheckAccessible(type, path);

            if (collectionsInProgress.Contains(key))
            {
                throw Unsupported(type, path, "the type contains itself without an object in between");
            }

            TypeModel model;

            collectionsInProgress.Add(key);

            try
            {
                model = Create(type, key, path);
            }
            finally
            {
                collectionsInProgress.Remove(key);
            }

            models[key] = model;
            return key;
        }

        string Render(ITypeSymbol type)
        {
            if (!renderedTypes.TryGetValue(type, out var rendered))
            {
                rendered = TypeNames.Render(type);
                renderedTypes.Add(type, rendered);
            }

            return rendered;
        }

        TypeModel Create(ITypeSymbol type, string key, TypePath path)
        {
            if (type is INamedTypeSymbol { TypeKind: TypeKind.Enum, EnumUnderlyingType: { } underlying })
            {
                return new TypeModel(key, TypeModelKind.Enum, Add(underlying, path), null, null);
            }

            if (type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable)
            {
                return new TypeModel(key, TypeModelKind.Nullable, Add(nullable.TypeArguments[0], path), null, null);
            }

            if (type is INamedTypeSymbol { IsGenericType: true } generic)
            {
                var arguments = generic.TypeArguments;

                if (KnownTypes.Is(generic, knownTypes.ILookup) && arguments[0].SpecialType == SpecialType.System_String)
                {
                    return new TypeModel(key, TypeModelKind.Lookup, null, Add(arguments[1], path.Element()), null);
                }

                if (KnownTypes.Is(generic, knownTypes.Dictionary) || KnownTypes.Is(generic, knownTypes.IDictionary) || KnownTypes.Is(generic, knownTypes.IReadOnlyDictionary))
                {
                    return CreateDictionary(key, arguments[0], arguments[1], path);
                }
            }

            if (IsNonGenericDictionary(type) || type.AllInterfaces.Any(IsNonGenericDictionary))
            {
                if (FindEnumerableElementType(type, path) is INamedTypeSymbol { IsGenericType: true } entry
                    && KnownTypes.Is(entry, knownTypes.KeyValuePair))
                {
                    return CreateDictionary(key, entry.TypeArguments[0], entry.TypeArguments[1], path);
                }

                throw Unsupported(type, path, "non-generic dictionaries are not supported");
            }

            if (type is IArrayTypeSymbol array)
            {
                if (!array.IsSZArray)
                {
                    throw Unsupported(type, path, "multi-dimensional arrays are not supported");
                }

                return new TypeModel(key, TypeModelKind.Array, Add(array.ElementType, path.Element()), null, null);
            }

            if (FindEnumerableElementType(type, path) is { } elementType)
            {
                return new TypeModel(key, TypeModelKind.Collection, Add(elementType, path.Element()), null, null);
            }

            if (type.SpecialType == SpecialType.System_Collections_IEnumerable
                || type.AllInterfaces.Any(i => i.SpecialType == SpecialType.System_Collections_IEnumerable))
            {
                throw Unsupported(type, path, "non-generic enumerables are not supported");
            }

            return CreateObject(type, key, path);
        }

        TypeModel CreateDictionary(string key, ITypeSymbol keyType, ITypeSymbol valueType, TypePath path)
            => new(key, TypeModelKind.Dictionary, Add(keyType, path.Key()), Add(valueType, path.Element()), null);

        ITypeSymbol? FindEnumerableElementType(ITypeSymbol type, TypePath path)
        {
            if (type is INamedTypeSymbol { TypeKind: TypeKind.Interface, OriginalDefinition.SpecialType: SpecialType.System_Collections_Generic_IEnumerable_T } enumerable)
            {
                return enumerable.TypeArguments[0];
            }

            ITypeSymbol? result = null;

            foreach (var candidate in type.AllInterfaces)
            {
                if (candidate.OriginalDefinition.SpecialType != SpecialType.System_Collections_Generic_IEnumerable_T)
                {
                    continue;
                }

                if (result is not null && !SymbolEqualityComparer.Default.Equals(result, candidate.TypeArguments[0]))
                {
                    throw Unsupported(type, path, "the type implements IEnumerable<T> more than once");
                }

                result = candidate.TypeArguments[0];
            }

            return result;
        }

        bool IsNonGenericDictionary(ITypeSymbol type) => KnownTypes.Is(type, knownTypes.NonGenericIDictionary);

        #region Objects

        TypeModel CreateObject(ITypeSymbol type, string key, TypePath path)
        {
            if (type is not INamedTypeSymbol named || type.SpecialType == SpecialType.System_Object)
            {
                throw Unsupported(type, path, "values are serialized using their runtime type");
            }

            switch (named.TypeKind)
            {
                case TypeKind.Class when named.IsAbstract:
                    throw Unsupported(type, path, "abstract types cannot be created");
                case TypeKind.Interface:
                    throw Unsupported(type, path, "interfaces cannot be created");
                case TypeKind.Delegate:
                    throw Unsupported(type, path, "delegates are not supported");
                case TypeKind.Class:
                case TypeKind.Struct:
                    break;
                default:
                    throw Unsupported(type, path, "the kind of type is not supported");
            }

            if (named.IsRefLikeType)
            {
                throw Unsupported(type, path, "ref structs are not supported");
            }

            // Members may refer back to this type, which then resolves to the model that is being created.
            var outerCollections = collectionsInProgress;
            collectionsInProgress = new HashSet<string>(StringComparer.Ordinal);
            objectsInProgress.Add(key);

            try
            {
                var accessors = new List<AccessorModel>();
                var members = IsValueTupleType(named) ? GetTupleMembers(named, path) : GetPropertyMembers(named, path, accessors);
                var model = CreateObjectModel(named, path, members, accessors);

                return new TypeModel(key, TypeModelKind.Object, null, null, model);
            }
            finally
            {
                objectsInProgress.Remove(key);
                collectionsInProgress = outerCollections;
            }
        }

        // Selects the constructor in this order: the one marked with [KVConstructor], a public parameterless
        // constructor, or the single public constructor. A struct that declares no public constructor is created from
        // its default value. Types that cannot be created can still be serialized.
        ObjectModel CreateObjectModel(INamedTypeSymbol type, TypePath path, List<MemberModel> members, List<AccessorModel> accessors)
        {
            ObjectModel Uncreatable(ConstructionKind construction, string? exception = null)
                => new(TypeNames.GetNameHint(type), type.IsValueType, construction, exception, -1, EquatableArray<ParameterModel>.Empty, false, new(members), new(accessors));

            var constructors = type.InstanceConstructors
                .Where(c => !(type.IsValueType && c.Parameters.Length == 0 && c.IsImplicitlyDeclared))
                .ToList();

            var marked = constructors.Where(c => HasAttribute(c, knownTypes.KVConstructorAttribute)).ToList();

            if (marked.Count > 1)
            {
                return Uncreatable(ConstructionKind.Throw, Exception("global::ValveKeyValue.KeyValueException", $"Type '{type.MetadataName}' has more than one constructor marked with [KVConstructor]."));
            }

            var publicConstructors = constructors.Where(c => c.DeclaredAccessibility == Accessibility.Public).ToList();
            var constructor = marked.Count == 1
                ? marked[0]
                : publicConstructors.FirstOrDefault(c => c.Parameters.Length == 0) ?? (publicConstructors.Count == 1 ? publicConstructors[0] : null);

            if (constructor is null)
            {
                return Uncreatable(publicConstructors.Count == 0 && type.IsValueType ? ConstructionKind.Default : ConstructionKind.None);
            }

            foreach (var parameter in constructor.Parameters)
            {
                if (IsSupportedMemberType(parameter.Type))
                {
                    continue;
                }

                if (parameter.Type is not IPointerTypeSymbol and not INamedTypeSymbol)
                {
                    throw Unsupported(parameter.Type, path.Parameter(parameter.Name), "the parameter type cannot be used as a type argument");
                }

                return Uncreatable(ConstructionKind.Throw, Exception("global::System.NotSupportedException", $"Constructor parameter '{parameter.Name}' of type '{GetRuntimeName(parameter.Type)}' on type '{type.MetadataName}' is not supported."));
            }

            var parameters = new List<ParameterModel>();

            foreach (var parameter in constructor.Parameters)
            {
                var parameterPath = path.Parameter(parameter.Name);
                var parameterType = Add(parameter.Type, parameterPath);
                var defaultValue = "default";

                if (parameter.HasExplicitDefaultValue)
                {
                    defaultValue = TypeNames.FormatConstant(parameter.ExplicitDefaultValue, parameter.Type)
                        ?? throw Unsupported(parameter.Type, parameterPath, "the default value cannot be written as a constant");
                }

                var argumentModifier = parameter.RefKind switch
                {
                    RefKind.Ref => "ref ",
                    RefKind.Out => "out ",
                    _ => string.Empty,
                };

                parameters.Add(new ParameterModel(parameter.Name, parameterType, argumentModifier, defaultValue));
            }

            var setsRequiredMembers = HasAttribute(constructor, knownTypes.SetsRequiredMembersAttribute);
            var constructorAccessor = -1;

            if (!IsAccessible(constructor) || (!setsRequiredMembers && HasRequiredMembers(type)))
            {
                var generic = GetGenericAccessor(type, path);
                var declaration = string.Join(", ", constructor.OriginalDefinition.Parameters.Select((p, i) => GetParameterModifier(p.RefKind) + Render(p.Type) + " p" + i));

                constructorAccessor = accessors.Count;
                accessors.Add(new AccessorModel(
                    AccessorKind.Constructor,
                    ".ctor",
                    Render(generic is null ? type : type.OriginalDefinition),
                    string.Empty,
                    declaration,
                    generic));
            }

            return new ObjectModel(
                TypeNames.GetNameHint(type),
                type.IsValueType,
                ConstructionKind.Constructor,
                null,
                constructorAccessor,
                new(parameters),
                setsRequiredMembers,
                new(members),
                new(accessors));
        }

        static string Exception(string type, string message) => "new " + type + "(" + TypeNames.FormatLiteral(message) + ")";

        static string GetParameterModifier(RefKind refKind) => refKind switch
        {
            RefKind.Ref => "ref ",
            RefKind.Out => "out ",
            RefKind.In => "in ",
            RefKind.RefReadOnlyParameter => "ref readonly ",
            _ => string.Empty,
        };

        // The name of a type as reported by Type.Name.
        static string GetRuntimeName(ITypeSymbol type)
            => type is IPointerTypeSymbol pointer ? GetRuntimeName(pointer.PointedAtType) + "*" : type.MetadataName;

        List<MemberModel> GetTupleMembers(INamedTypeSymbol type, TypePath path)
        {
            var underlying = type.TupleUnderlyingType ?? type;
            var members = new List<MemberModel>();

            foreach (var field in underlying.OriginalDefinition.GetMembers().OfType<IFieldSymbol>())
            {
                if (field.IsStatic || field.DeclaredAccessibility != Accessibility.Public || field.Type is not ITypeParameterSymbol typeParameter)
                {
                    continue;
                }

                var fieldType = Add(underlying.TypeArguments[typeParameter.Ordinal], path.Member(field.Name));
                var access = "t." + TypeNames.Escape(field.Name);
                var setter = field.IsReadOnly ? AccessKind.None : AccessKind.Direct;

                members.Add(new MemberModel(field.Name, field.Name, fieldType, false, access, AccessKind.Direct, -1, setter, -1));
            }

            return members;
        }

        List<MemberModel> GetPropertyMembers(INamedTypeSymbol type, TypePath path, List<AccessorModel> accessors)
        {
            var members = new List<MemberModel>();

            foreach (var (property, getter, setter) in GetVisibleProperties(type))
            {
                if (HasInheritedAttribute(property, knownTypes.KVIgnoreAttribute))
                {
                    continue;
                }

                if (IsRecordEqualityContract(property))
                {
                    continue;
                }

                if (property.Parameters.Length > 0 || property.ReturnsByRef || property.ReturnsByRefReadonly || !IsSupportedMemberType(property.Type))
                {
                    continue;
                }

                // A property is visible when one of its accessors is public, or when it is
                // explicitly opted in. Opting in makes any accessor usable regardless of visibility.
                var included = HasInheritedAttribute(property, knownTypes.KVIncludeAttribute);
                var canRead = getter is not null && (included || getter.DeclaredAccessibility == Accessibility.Public);
                var canWrite = setter is not null && (included || setter.DeclaredAccessibility == Accessibility.Public);

                if (!canRead && !canWrite)
                {
                    continue;
                }

                var memberPath = path.Member(property.Name);

                if (!property.ExplicitInterfaceImplementations.IsEmpty)
                {
                    throw Unsupported(type, memberPath, "explicit interface implementations are not supported");
                }

                var name = GetInheritedAttribute(property, knownTypes.KVPropertyAttribute) is { ConstructorArguments: { Length: 1 } arguments }
                    && arguments[0].Value is string propertyName
                    ? propertyName
                    : property.Name;

                var memberType = Add(property.Type, memberPath);
                var declaringType = property.ContainingType;
                var inherited = !SymbolEqualityComparer.Default.Equals(declaringType, type);
                var access = inherited
                    ? "((" + Render(declaringType) + ")t)." + TypeNames.Escape(property.Name)
                    : "t." + TypeNames.Escape(property.Name);

                var (getterKind, getterAccessor) = canRead ? GetAccess(accessors, AccessorKind.Getter, property, getter!, memberPath) : (AccessKind.None, -1);
                var (setterKind, setterAccessor) = canWrite ? GetAccess(accessors, AccessorKind.Setter, property, setter!, memberPath) : (AccessKind.None, -1);

                members.Add(new MemberModel(name, property.Name, memberType, property.IsRequired, access, getterKind, getterAccessor, setterKind, setterAccessor));
            }

            return members;
        }

        // Calls a property accessor directly when generated code can access it, otherwise through an added [UnsafeAccessor].
        (AccessKind Kind, int Accessor) GetAccess(List<AccessorModel> accessors, AccessorKind kind, IPropertySymbol property, IMethodSymbol method, TypePath path)
        {
            if (IsAccessible(method) && !method.IsInitOnly)
            {
                return (AccessKind.Direct, -1);
            }

            var declaringType = property.ContainingType;
            var generic = GetGenericAccessor(declaringType, path);
            var target = generic is null ? declaringType : declaringType.OriginalDefinition;
            var valueType = generic is null ? property.Type : property.OriginalDefinition.Type;

            accessors.Add(new AccessorModel(kind, method.MetadataName, Render(target), Render(valueType), string.Empty, generic));

            return (AccessKind.Accessor, accessors.Count - 1);
        }

        // Returns the generic class that an accessor for a member of the given type must be declared in, or null when
        // neither the type nor its containing types are generic. The class has the type parameters of the containing
        // types followed by those of the type, as the type definition has in metadata.
        GenericAccessorModel? GetGenericAccessor(INamedTypeSymbol type, TypePath path)
        {
            var chain = new List<INamedTypeSymbol>();

            for (var current = type; current is not null; current = current.ContainingType)
            {
                chain.Insert(0, current);
            }

            var typeParameters = chain.SelectMany(t => t.OriginalDefinition.TypeParameters).ToList();

            if (typeParameters.Count == 0)
            {
                return null;
            }

            if (typeParameters.Select(p => p.Name).Distinct(StringComparer.Ordinal).Count() != typeParameters.Count)
            {
                throw Unsupported(type, path, "a type parameter has the same name as a type parameter of a containing type");
            }

            var definition = type.OriginalDefinition;

            return new GenericAccessorModel(
                Render(definition),
                definition.Name,
                "<" + string.Join(", ", typeParameters.Select(p => TypeNames.Escape(p.Name))) + ">",
                string.Concat(chain.Select(t => TypeNames.RenderConstraints(t.OriginalDefinition))),
                "<" + string.Join(", ", chain.SelectMany(t => t.TypeArguments).Select(Render)) + ">");
        }

        // Mirrors Type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic): properties of
        // the type first, then of its base types. Private accessors of base type properties are not visible, a base
        // type property without visible accessors is skipped, and a property hides base type properties with the same
        // name and signature.
        static List<(IPropertySymbol Property, IMethodSymbol? Getter, IMethodSymbol? Setter)> GetVisibleProperties(INamedTypeSymbol type)
        {
            var result = new List<(IPropertySymbol Property, IMethodSymbol? Getter, IMethodSymbol? Setter)>();

            for (var current = type; current is not null; current = current.BaseType)
            {
                var inherited = !SymbolEqualityComparer.Default.Equals(current, type);

                foreach (var property in current.GetMembers().OfType<IPropertySymbol>())
                {
                    if (property.IsStatic)
                    {
                        continue;
                    }

                    var getter = property.GetMethod;
                    var setter = property.SetMethod;

                    if (inherited)
                    {
                        if (getter?.DeclaredAccessibility == Accessibility.Private)
                        {
                            getter = null;
                        }

                        if (setter?.DeclaredAccessibility == Accessibility.Private)
                        {
                            setter = null;
                        }

                        if (getter is null && setter is null)
                        {
                            continue;
                        }
                    }

                    if (result.Any(existing => HasSameSignature(existing.Property, property)))
                    {
                        continue;
                    }

                    result.Add((property, getter, setter));
                }
            }

            return result;
        }

        static bool HasSameSignature(IPropertySymbol left, IPropertySymbol right)
        {
            if (!string.Equals(left.MetadataName, right.MetadataName, StringComparison.Ordinal)
                || !SymbolEqualityComparer.Default.Equals(left.Type, right.Type)
                || left.Parameters.Length != right.Parameters.Length)
            {
                return false;
            }

            for (var i = 0; i < left.Parameters.Length; i++)
            {
                if (!SymbolEqualityComparer.Default.Equals(left.Parameters[i].Type, right.Parameters[i].Type))
                {
                    return false;
                }
            }

            return true;
        }

        bool IsRecordEqualityContract(IPropertySymbol property)
            => property.Name == "EqualityContract"
            && KnownTypes.Is(property.Type, knownTypes.Type)
            && (property.IsImplicitlyDeclared
                || (property.GetMethod is { } getter && (getter.IsImplicitlyDeclared || HasAttribute(getter, knownTypes.CompilerGeneratedAttribute))));

        static bool IsValueTupleType(INamedTypeSymbol type)
            => type.IsGenericType && type.ContainingType is null && type.Name == "ValueTuple"
            && type.ContainingNamespace is { Name: "System", ContainingNamespace.IsGlobalNamespace: true };

        static bool HasRequiredMembers(INamedTypeSymbol type)
        {
            for (var current = type; current is not null; current = current.BaseType)
            {
                foreach (var member in current.GetMembers())
                {
                    if (member is IPropertySymbol { IsRequired: true } or IFieldSymbol { IsRequired: true })
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        #endregion

        static bool IsSupportedMemberType(ITypeSymbol type)
            => type.TypeKind is not (TypeKind.Pointer or TypeKind.FunctionPointer) && !type.IsRefLikeType;

        bool IsAccessible(ISymbol symbol)
            => compilation.IsSymbolAccessibleWithin(symbol, compilation.Assembly) && !IsObsoleteError(symbol);

        bool IsObsoleteError(ISymbol symbol)
            => KnownTypes.GetAttribute(symbol, knownTypes.ObsoleteAttribute) is { ConstructorArguments: { Length: 2 } arguments } && arguments[1].Value is true;

        void CheckAccessible(ITypeSymbol type, TypePath path)
        {
            if (type.IsAnonymousType || !compilation.IsSymbolAccessibleWithin(type, compilation.Assembly))
            {
                throw new UnsupportedTypeException(DiagnosticDescriptors.Inaccessible, type.ToDisplayString(), path.ToString());
            }

            for (var current = type as INamedTypeSymbol; current is not null; current = current.ContainingType)
            {
                if (current.IsFileLocal)
                {
                    throw new UnsupportedTypeException(DiagnosticDescriptors.Inaccessible, type.ToDisplayString(), path.ToString());
                }
            }
        }

        static bool ContainsTypeParameter(ITypeSymbol type) => type switch
        {
            ITypeParameterSymbol => true,
            IArrayTypeSymbol array => ContainsTypeParameter(array.ElementType),
            IPointerTypeSymbol pointer => ContainsTypeParameter(pointer.PointedAtType),
            INamedTypeSymbol named => named.TypeArguments.Any(ContainsTypeParameter) || (named.ContainingType is { } containing && ContainsTypeParameter(containing)),
            _ => false,
        };

        static UnsupportedTypeException Unsupported(ITypeSymbol type, TypePath path, string reason)
            => new(DiagnosticDescriptors.Unsupported, type.ToDisplayString(), path.ToString(), reason);

        static bool HasAttribute(ISymbol symbol, INamedTypeSymbol? attributeType) => KnownTypes.GetAttribute(symbol, attributeType) is not null;

        // Attributes on overridden properties apply to the overriding property, like Attribute.GetCustomAttribute.
        static AttributeData? GetInheritedAttribute(IPropertySymbol property, INamedTypeSymbol? attributeType)
        {
            for (var current = property; current is not null; current = current.OverriddenProperty)
            {
                if (KnownTypes.GetAttribute(current, attributeType) is { } attribute)
                {
                    return attribute;
                }
            }

            return null;
        }

        static bool HasInheritedAttribute(IPropertySymbol property, INamedTypeSymbol? attributeType)
            => GetInheritedAttribute(property, attributeType) is not null;

        // The location of a type within the root type, such as "Settings.Children[].Name", rendered only for diagnostics.
        sealed class TypePath
        {
            TypePath(ITypeSymbol? root, TypePath? parent, string prefix, string name, string suffix)
            {
                this.root = root;
                this.parent = parent;
                this.prefix = prefix;
                this.name = name;
                this.suffix = suffix;
            }

            readonly ITypeSymbol? root;
            readonly TypePath? parent;
            readonly string prefix;
            readonly string name;
            readonly string suffix;

            public static TypePath Root(ITypeSymbol type) => new(type, null, string.Empty, string.Empty, string.Empty);

            public TypePath Member(string memberName) => new(null, this, ".", memberName, string.Empty);

            public TypePath Parameter(string parameterName) => new(null, this, "(", parameterName, ")");

            public TypePath Element() => new(null, this, "[]", string.Empty, string.Empty);

            public TypePath Key() => new(null, this, ".Key", string.Empty, string.Empty);

            public override string ToString()
            {
                var sb = new StringBuilder();
                Append(sb);
                return sb.ToString();
            }

            void Append(StringBuilder sb)
            {
                parent?.Append(sb);

                if (root is not null)
                {
                    sb.Append(root.ToDisplayString(SymbolDisplayFormat.MinimallyQualifiedFormat));
                }

                sb.Append(prefix).Append(name).Append(suffix);
            }
        }
    }

    // The types reachable from a root type, or the diagnostic that explains why the root type is not supported.
    sealed class TypeGraph
    {
        public TypeGraph(TypeModel? root, EquatableArray<TypeModel> types, DiagnosticDescriptor? descriptor, string[] arguments)
        {
            Root = root;
            Types = types;
            Descriptor = descriptor;
            Arguments = arguments;
        }

        public TypeModel? Root { get; }

        public EquatableArray<TypeModel> Types { get; }

        public DiagnosticDescriptor? Descriptor { get; }

        public string[] Arguments { get; }
    }

    // Stops building a type graph; the call site falls back to reflection and reports the given diagnostic.
    sealed class UnsupportedTypeException : Exception
    {
        public UnsupportedTypeException()
        {
            Descriptor = DiagnosticDescriptors.Unsupported;
            Arguments = [];
        }

        public UnsupportedTypeException(string message)
            : base(message)
        {
            Descriptor = DiagnosticDescriptors.Unsupported;
            Arguments = [];
        }

        public UnsupportedTypeException(string message, Exception innerException)
            : base(message, innerException)
        {
            Descriptor = DiagnosticDescriptors.Unsupported;
            Arguments = [];
        }

        public UnsupportedTypeException(DiagnosticDescriptor descriptor, params string[] arguments)
            : base(descriptor.Id)
        {
            Descriptor = descriptor;
            Arguments = arguments;
        }

        public DiagnosticDescriptor Descriptor { get; }

        public string[] Arguments { get; }
    }
}
