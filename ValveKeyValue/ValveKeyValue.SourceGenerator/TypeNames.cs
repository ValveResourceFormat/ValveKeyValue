using System.Globalization;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace ValveKeyValue.SourceGenerator
{
    static class TypeNames
    {
        public const string MetadataNamespace = "global::ValveKeyValue.Metadata";

        public const string KVMetadata = MetadataNamespace + ".KVMetadata";

        public const string KVTypeInfo = MetadataNamespace + ".KVTypeInfo";

        // Fully qualified C# syntax without nullable reference annotations and tuple element names, so that every
        // spelling of the same runtime type produces the same text.
        static readonly SymbolDisplayFormat Format = SymbolDisplayFormat.FullyQualifiedFormat
            .AddMiscellaneousOptions(SymbolDisplayMiscellaneousOptions.ExpandValueTuple);

        static readonly SymbolDisplayFormat ConstraintsFormat = Format
            .AddGenericsOptions(SymbolDisplayGenericsOptions.IncludeTypeConstraints);

        public static string Render(ITypeSymbol type) => type.ToDisplayString(Format);

        // The constraint clauses of the type parameters that a type definition declares, such as " where T : class".
        public static string RenderConstraints(INamedTypeSymbol definition)
            => definition.TypeParameters.IsEmpty ? string.Empty : definition.ToDisplayString(ConstraintsFormat).Substring(Render(definition).Length);

        // A short identifier fragment that describes the type, used to name generated members.
        public static string GetNameHint(ITypeSymbol type)
        {
            switch (type)
            {
                case IArrayTypeSymbol array:
                    return GetNameHint(array.ElementType) + "Array";

                case INamedTypeSymbol named:
                    if (named.IsTupleType && named.TupleUnderlyingType is { } underlying)
                    {
                        named = underlying;
                    }

                    if (named.SpecialType != SpecialType.None && named.TypeArguments.Length == 0)
                    {
                        return named.MetadataName;
                    }

                    if (named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T)
                    {
                        return "Nullable" + GetNameHint(named.TypeArguments[0]);
                    }

                    if (named.TypeArguments.Length == 0)
                    {
                        return named.Name;
                    }

                    return named.Name + "_" + string.Join("_", named.TypeArguments.Select(GetNameHint));

                default:
                    return type.Name;
            }
        }

        public static string Escape(string identifier)
            => SyntaxFacts.GetKeywordKind(identifier) != SyntaxKind.None ? "@" + identifier : identifier;

        // The expression that returns the type information of a scalar type, or null when the type is not a scalar.
        public static string? GetScalarTypeInfo(ITypeSymbol type)
            => GetScalarProperty(type) is { } property ? KVMetadata + "." + property : null;

        static string? GetScalarProperty(ITypeSymbol type)
        {
            if (type is IArrayTypeSymbol { IsSZArray: true, ElementType.SpecialType: SpecialType.System_Byte })
            {
                return "ByteArray";
            }

            return type.SpecialType switch
            {
                SpecialType.System_Boolean => "Boolean",
                SpecialType.System_Byte => "Byte",
                SpecialType.System_SByte => "SByte",
                SpecialType.System_Char => "Char",
                SpecialType.System_Int16 => "Int16",
                SpecialType.System_UInt16 => "UInt16",
                SpecialType.System_Int32 => "Int32",
                SpecialType.System_UInt32 => "UInt32",
                SpecialType.System_Int64 => "Int64",
                SpecialType.System_UInt64 => "UInt64",
                SpecialType.System_Single => "Single",
                SpecialType.System_Double => "Double",
                SpecialType.System_Decimal => "Decimal",
                SpecialType.System_String => "String",
                SpecialType.System_IntPtr => "IntPtr",
                _ => null,
            };
        }

        // Formats a constant as a C# expression that converts implicitly to the given type, or returns null when the
        // constant cannot be written as an expression. Integer constants convert to any integer type that can hold
        // them, so only enums need a cast.
        public static string? FormatConstant(object? value, ITypeSymbol type)
        {
            if (value is null)
            {
                return "default";
            }

            if (type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable)
            {
                type = nullable.TypeArguments[0];
            }

            if (type is INamedTypeSymbol { TypeKind: TypeKind.Enum, EnumUnderlyingType: { } underlyingType })
            {
                var underlyingValue = FormatConstant(value, underlyingType);

                if (underlyingValue is null)
                {
                    return null;
                }

                // Negative numbers are parenthesized so that they can follow a cast.
                return "(" + Render(type) + ")" + (underlyingValue.StartsWith("-", StringComparison.Ordinal) ? "(" + underlyingValue + ")" : underlyingValue);
            }

            return FormatLiteral(value);
        }

        // Formats a constant as a C# literal, or returns null when the constant has no literal form.
        public static string? FormatLiteral(object value)
            => value switch
            {
                bool b => b ? "true" : "false",
                string s => SymbolDisplay.FormatLiteral(s, quote: true),
                char c => SymbolDisplay.FormatLiteral(c, quote: true),
                sbyte n => SyntaxFactory.Literal(n).Text,
                byte n => SyntaxFactory.Literal(n).Text,
                short n => SyntaxFactory.Literal(n).Text,
                ushort n => SyntaxFactory.Literal(n).Text,
                int n => n == int.MinValue ? "int.MinValue" : SyntaxFactory.Literal(n).Text,
                uint n => SyntaxFactory.Literal(n).Text,
                long n => n == long.MinValue ? "long.MinValue" : SyntaxFactory.Literal(n).Text,
                ulong n => SyntaxFactory.Literal(n).Text,
                float f => FormatNonFinite("float", f) ?? SyntaxFactory.Literal(f).Text,
                double d => FormatNonFinite("double", d) ?? d.ToString("R", CultureInfo.InvariantCulture) + "D",
                decimal m => SyntaxFactory.Literal(m).Text,
                _ => null,
            };

        // Returns an identifier that is distinct for every rendered type. Identifier characters other than '_' are kept,
        // '.' becomes '_', and '_' followed by a digit encodes a character that cannot appear in an identifier. A '.' is
        // always followed by the start of an identifier, which is never a digit, so the result decodes unambiguously.
        // The global:: alias and spaces are removed because rendered types always contain them in the same places.
        public static string Mangle(string renderedType)
        {
            var text = renderedType.Replace("global::", string.Empty);
            var sb = new StringBuilder(text.Length);

            foreach (var c in text)
            {
                switch (c)
                {
                    case ' ':
                        break;
                    case '.':
                        sb.Append('_');
                        break;
                    case '_':
                        sb.Append("_0");
                        break;
                    case '<':
                        sb.Append("_1");
                        break;
                    case '>':
                        sb.Append("_2");
                        break;
                    case ',':
                        sb.Append("_3");
                        break;
                    case '?':
                        sb.Append("_4");
                        break;
                    case '[':
                        sb.Append("_5");
                        break;
                    case ']':
                        sb.Append("_6");
                        break;
                    case '@':
                        sb.Append("_7");
                        break;
                    default:
                        if (SyntaxFacts.IsIdentifierPartCharacter(c))
                        {
                            sb.Append(c);
                        }
                        else
                        {
                            sb.Append("_9").Append(((int)c).ToString("X4", CultureInfo.InvariantCulture));
                        }

                        break;
                }
            }

            return Escape(sb.ToString());
        }

        // The name of the class that holds the generated type information of an assembly. It is distinct for every
        // assembly, so that assemblies that see each other's internal types do not declare conflicting classes, and it
        // contains "__" followed by a letter, which never occurs in a mangled type name.
        public static string GetTypeInfoClassName(string? assemblyName)
        {
            assemblyName ??= string.Empty;

            var sb = new StringBuilder("KVGeneratedTypeInfo__For_").Append(Sanitize(assemblyName));

            // FNV-1a, which is stable across processes unlike string.GetHashCode.
            var hash = 2166136261u;

            foreach (var c in assemblyName)
            {
                hash = unchecked((hash ^ c) * 16777619u);
            }

            return sb.Append('_').Append(hash.ToString("X8", CultureInfo.InvariantCulture)).ToString();
        }

        // Replaces every character that is not an ASCII identifier character with '_'.
        public static string Sanitize(string text)
        {
            var sb = new StringBuilder(text.Length);

            foreach (var c in text)
            {
                sb.Append(c < 128 && SyntaxFacts.IsIdentifierPartCharacter(c) ? c : '_');
            }

            return sb.ToString();
        }

        static string? FormatNonFinite(string keyword, double value)
        {
            if (double.IsNaN(value))
            {
                return keyword + ".NaN";
            }

            if (double.IsPositiveInfinity(value))
            {
                return keyword + ".PositiveInfinity";
            }

            if (double.IsNegativeInfinity(value))
            {
                return keyword + ".NegativeInfinity";
            }

            return null;
        }
    }
}
