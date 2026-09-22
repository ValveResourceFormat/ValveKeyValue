using System.Globalization;
using System.Linq;
using System.Text;

namespace ValveKeyValue.SourceGenerator
{
    // Writes the class that holds the type information of every type used by an interceptor, and the accessors for
    // members that generated code cannot access directly. Each type information property is named after the mangled
    // name of its type, so that interceptors can refer to it without knowing the other types.
    sealed class TypeInfoEmitter
    {
        const string UnsafeAccessor = "[global::System.Runtime.CompilerServices.UnsafeAccessor(global::System.Runtime.CompilerServices.UnsafeAccessorKind.";

        TypeInfoEmitter(EquatableArray<TypeModel> types, string className)
        {
            this.types = types;
            this.className = className;

            usedNames.Add(className);

            foreach (var type in types)
            {
                typesByName.Add(type.Type, type);

                if (type.Kind != TypeModelKind.Scalar)
                {
                    var name = TypeNames.Mangle(type.Type);
                    typeInfoNames.Add(type.Type, name);
                    usedNames.Add(name);
                    usedNames.Add("_" + name);
                }
            }
        }

        readonly EquatableArray<TypeModel> types;
        readonly string className;
        readonly Dictionary<string, TypeModel> typesByName = new(StringComparer.Ordinal);
        readonly Dictionary<string, string> typeInfoNames = new(StringComparer.Ordinal);
        readonly HashSet<string> usedNames = new(StringComparer.Ordinal) { "t", "v", "args", "_" };
        readonly CodeWriter writer = new();

        // The types must be ordered by name, so that the generated source does not depend on the order of call sites.
        public static string Emit(EquatableArray<TypeModel> types, string className) => new TypeInfoEmitter(types, className).Emit();

        // Returns the expression that refers to the type information of a type from any generated code.
        public static string GetReference(TypeModel type, string className)
            => type.Kind == TypeModelKind.Scalar ? type.Element! : $"global::{Parser.GeneratedNamespace}.{className}.{TypeNames.Mangle(type.Type)}";

        string Emit()
        {
            writer.WriteLines(CodeWriter.Header);
            writer.WriteLine($"namespace {Parser.GeneratedNamespace}");
            writer.OpenBlock();
            writer.WriteLine(CodeWriter.GeneratedCodeAttribute);
            writer.WriteLine("[global::System.ComponentModel.EditorBrowsable(global::System.ComponentModel.EditorBrowsableState.Never)]");
            writer.WriteLine($"internal static class {className}");
            writer.OpenBlock();

            var accessors = new AccessorWriter(this);
            var first = true;

            foreach (var type in types)
            {
                if (type.Kind == TypeModelKind.Scalar)
                {
                    continue;
                }

                if (!first)
                {
                    writer.WriteLine();
                }

                first = false;
                WriteTypeInfo(type, typeInfoNames[type.Type], accessors);
            }

            accessors.Write();

            writer.CloseBlock();
            writer.CloseBlock();

            return writer.ToString();
        }

        void WriteTypeInfo(TypeModel type, string name, AccessorWriter accessors)
        {
            writer.WriteLine($"static {TypeNames.KVTypeInfo}<{type.Type}>? _{name};");
            writer.WriteLine();

            var expression = type.Kind switch
            {
                TypeModelKind.Enum => $"{TypeNames.KVMetadata}.Enum<{type.Type}, {type.Element}>({GetTypeInfo(type.Element!)})",
                TypeModelKind.Nullable => $"{TypeNames.KVMetadata}.Nullable<{type.Element}>({GetTypeInfo(type.Element!)})",
                TypeModelKind.Array => $"{TypeNames.KVMetadata}.Array<{type.Element}>({GetTypeInfo(type.Element!)})",
                TypeModelKind.Collection => $"{TypeNames.KVMetadata}.Collection<{type.Type}, {type.Element}>({GetTypeInfo(type.Element!)})",
                TypeModelKind.Dictionary => $"{TypeNames.KVMetadata}.Dictionary<{type.Type}, {type.Element}, {type.Value}>({GetTypeInfo(type.Element!)}, {GetTypeInfo(type.Value!)})",
                TypeModelKind.Lookup => $"{TypeNames.KVMetadata}.Lookup<{type.Value}>({GetTypeInfo(type.Value!)})",
                _ => null,
            };

            if (expression is not null)
            {
                writer.WriteLine($"internal static {TypeNames.KVTypeInfo}<{type.Type}> {name} => _{name} ??= {expression};");
                return;
            }

            WriteObjectTypeInfo(type.Type, name, type.Object!, accessors);
        }

        void WriteObjectTypeInfo(string type, string name, ObjectModel model, AccessorWriter accessors)
        {
            var accessorNames = model.Accessors.Select(accessor => accessors.Add(accessor, model)).ToList();
            var target = model.IsValueType ? "ref t" : "t";

            writer.WriteLine($"internal static {TypeNames.KVTypeInfo}<{type}> {name} => _{name} ??= {TypeNames.KVMetadata}.Object<{type}>(");
            writer.Indent();
            writer.WriteLine($"create: {GetCreate(type, model, accessorNames)},");

            if (model.Parameters.Count > 0)
            {
                writer.WriteLine($"parameters: static () => new {TypeNames.MetadataNamespace}.KVParameterInfo[]");
                writer.OpenBlock();

                foreach (var parameter in model.Parameters)
                {
                    writer.WriteLine($"{TypeNames.KVMetadata}.Parameter<{parameter.Type}>({Literal(parameter.Name)}, {GetTypeInfo(parameter.Type)}, defaultValue: {parameter.DefaultValue}),");
                }

                writer.CloseBlock(",");
            }
            else
            {
                writer.WriteLine("parameters: null,");
            }

            writer.WriteLine($"members: static () => new {TypeNames.MetadataNamespace}.KVMemberInfo<{type}>[]");
            writer.OpenBlock();

            foreach (var member in model.Members)
            {
                var getter = member.Getter switch
                {
                    AccessKind.Direct => $"static ({type} t) => {member.DirectAccess}",
                    AccessKind.Accessor => $"static ({type} t) => {accessorNames[member.GetterAccessor]}({target})",
                    _ => "getter: null",
                };

                var setter = member.Setter switch
                {
                    AccessKind.Direct => $"static (ref {type} t, {member.Type} v) => {member.DirectAccess} = v",
                    AccessKind.Accessor => $"static (ref {type} t, {member.Type} v) => {accessorNames[member.SetterAccessor]}({target}, v)",
                    _ => "setter: null",
                };

                writer.WriteLine($"{TypeNames.KVMetadata}.Member<{type}, {member.Type}>({Literal(member.Name)}, {Literal(member.DeclaredName)}, {GetTypeInfo(member.Type)}, {getter}, {setter}, isRequired: {Literal(member.IsRequired)}),");
            }

            writer.CloseBlock(",");
            writer.WriteLine($"constructorSetsRequiredMembers: {Literal(model.SetsRequiredMembers)});");
            writer.Unindent();
        }

        // Arguments of parameters that are passed by reference are copied to variables first.
        static string GetCreate(string type, ObjectModel model, List<string> accessorNames)
        {
            switch (model.Construction)
            {
                case ConstructionKind.None:
                    return "null";
                case ConstructionKind.Default:
                    return $"static _ => default({type})";
                case ConstructionKind.Throw:
                    return $"static _ => throw {model.Exception}";
            }

            var constructor = model.ConstructorAccessor >= 0 ? accessorNames[model.ConstructorAccessor] : "new " + type;

            if (model.Parameters.Count == 0)
            {
                return $"static _ => {constructor}()";
            }

            var variables = new StringBuilder();
            var arguments = new List<string>();

            for (var i = 0; i < model.Parameters.Count; i++)
            {
                var parameter = model.Parameters[i];
                var index = i.ToString(CultureInfo.InvariantCulture);
                var value = $"({parameter.Type})args[{index}]!";

                if (parameter.ArgumentModifier.Length == 0)
                {
                    arguments.Add(value);
                    continue;
                }

                variables.Append($"var a{index} = {value}; ");
                arguments.Add(parameter.ArgumentModifier + "a" + index);
            }

            var call = $"{constructor}({string.Join(", ", arguments)})";

            return variables.Length == 0 ? $"static args => {call}" : $"static args => {{ {variables}return {call}; }}";
        }

        string GetTypeInfo(string type)
            => typesByName[type] is { Kind: TypeModelKind.Scalar } scalar ? scalar.Element! : typeInfoNames[type];

        string AllocateName(string hint)
        {
            var candidate = TypeNames.Escape(hint);
            var suffix = 2;

            while (usedNames.Contains(candidate))
            {
                candidate = TypeNames.Escape(hint + "_" + suffix.ToString(CultureInfo.InvariantCulture));
                suffix++;
            }

            usedNames.Add(candidate);
            return candidate;
        }

        static string Literal(object value) => TypeNames.FormatLiteral(value)!;

        // Collects [UnsafeAccessor] externs. Accessors for members of generic types are declared in a generic class
        // with the same type parameters and constraints as the type definition, as UnsafeAccessor requires.
        sealed class AccessorWriter
        {
            public AccessorWriter(TypeInfoEmitter emitter)
            {
                this.emitter = emitter;
            }

            readonly TypeInfoEmitter emitter;
            readonly List<string> declarations = [];
            readonly List<GenericClass> genericClasses = [];

            // Returns the expression that invokes the accessor.
            public string Add(AccessorModel accessor, ObjectModel owner)
            {
                var hint = accessor.Kind switch
                {
                    AccessorKind.Constructor => "Create_" + owner.NameHint,
                    _ => owner.NameHint + "_" + accessor.MetadataName,
                };

                var name = emitter.AllocateName(TypeNames.Sanitize(hint));
                var target = owner.IsValueType ? "ref " + accessor.TargetType : accessor.TargetType;
                var sb = new StringBuilder();

                if (accessor.Kind == AccessorKind.Constructor)
                {
                    sb.Append(UnsafeAccessor).Append("Constructor)]\n");
                }
                else
                {
                    sb.Append(UnsafeAccessor).Append("Method, Name = ").Append(Literal(accessor.MetadataName)).Append(")]\n");
                }

                sb.Append(accessor.Generic is null ? "static extern " : "public static extern ");

                switch (accessor.Kind)
                {
                    case AccessorKind.Getter:
                        sb.Append(accessor.ValueType).Append(' ').Append(name).Append('(').Append(target).Append(" target);");
                        break;

                    case AccessorKind.Setter:
                        sb.Append("void ").Append(name).Append('(').Append(target).Append(" target, ").Append(accessor.ValueType).Append(" value);");
                        break;

                    case AccessorKind.Constructor:
                        sb.Append(accessor.TargetType).Append(' ').Append(name).Append('(').Append(accessor.Parameters).Append(");");
                        break;
                }

                if (accessor.Generic is not { } generic)
                {
                    declarations.Add(sb.ToString());
                    return name;
                }

                var genericClass = genericClasses.Find(c => c.Model.Definition == generic.Definition && c.Model.Constraints == generic.Constraints);

                if (genericClass is null)
                {
                    genericClass = new GenericClass(emitter.AllocateName(TypeNames.Sanitize(generic.Name) + "Accessors"), generic);
                    genericClasses.Add(genericClass);
                }

                genericClass.Declarations.Add(sb.ToString());
                return genericClass.Name + generic.TypeArguments + "." + name;
            }

            public void Write()
            {
                var writer = emitter.writer;

                foreach (var declaration in declarations)
                {
                    writer.WriteLine();
                    writer.WriteLines(declaration);
                }

                foreach (var genericClass in genericClasses)
                {
                    writer.WriteLine();
                    writer.WriteLine($"static class {genericClass.Name}{genericClass.Model.TypeParameters}{genericClass.Model.Constraints}");
                    writer.OpenBlock();

                    for (var i = 0; i < genericClass.Declarations.Count; i++)
                    {
                        if (i > 0)
                        {
                            writer.WriteLine();
                        }

                        writer.WriteLines(genericClass.Declarations[i]);
                    }

                    writer.CloseBlock();
                }
            }

            sealed record GenericClass(string Name, GenericAccessorModel Model)
            {
                public List<string> Declarations { get; } = [];
            }
        }
    }
}
