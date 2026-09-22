using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ValveKeyValue.SourceGenerator
{
    // Recognizes calls to the reflection-based generic methods of ValveKeyValue.KVSerializer.
    static class KnownMethods
    {
        // Deserialize and GetTypeInfo cannot infer their type argument, so their calls always spell it out.
        public static bool IsCandidateInvocation(SyntaxNode node)
            => node is InvocationExpressionSyntax invocation && GetMethodName(invocation) switch
            {
                GenericNameSyntax { Identifier.ValueText: "Deserialize" or "GetTypeInfo" } => true,
                { Identifier.ValueText: "Serialize" or "SerializeWithSourceMap" } => true,
                _ => false,
            };

        // Returns the name of the called method, or null when the invocation does not call a method by name.
        public static SimpleNameSyntax? GetMethodName(InvocationExpressionSyntax invocation) => invocation.Expression switch
        {
            MemberAccessExpressionSyntax memberAccess => memberAccess.Name,
            MemberBindingExpressionSyntax memberBinding => memberBinding.Name,
            SimpleNameSyntax simpleName => simpleName,
            _ => null,
        };

        // The overloads that take a KVTypeInfo have one parameter more, so the parameter counts exclude them.
        public static bool TryGetInterceptedMethod(IMethodSymbol method, KnownTypes knownTypes, out InterceptedMethod kind)
        {
            kind = default;

            if (method.MethodKind != MethodKind.Ordinary || !method.IsGenericMethod || method.TypeParameters.Length != 1)
            {
                return false;
            }

            var definition = method.OriginalDefinition;

            if (!KnownTypes.Is(definition.ContainingType, knownTypes.KVSerializer))
            {
                return false;
            }

            switch (definition.Name)
            {
                case "Deserialize" when !definition.IsStatic && definition.Parameters.Length == 2:
                    kind = InterceptedMethod.Deserialize;
                    return true;

                case "Serialize" when !definition.IsStatic && definition.Parameters.Length == 4:
                    kind = InterceptedMethod.Serialize;
                    return true;

                case "SerializeWithSourceMap" when !definition.IsStatic && definition.Parameters.Length == 3:
                    kind = InterceptedMethod.SerializeWithSourceMap;
                    return true;

                case "GetTypeInfo" when definition.IsStatic && definition.Parameters.Length == 0:
                    kind = InterceptedMethod.GetTypeInfo;
                    return true;

                default:
                    return false;
            }
        }

        // Returns the invocation that a diagnostic reported on the invocation, its member access or its name refers to.
        public static InvocationExpressionSyntax? FindInvocation(SyntaxNode? node)
        {
            if (node is SimpleNameSyntax && node.Parent is MemberAccessExpressionSyntax or MemberBindingExpressionSyntax)
            {
                node = node.Parent;
            }

            if (node is MemberAccessExpressionSyntax or MemberBindingExpressionSyntax or SimpleNameSyntax)
            {
                node = node.Parent;
            }

            return node as InvocationExpressionSyntax;
        }
    }
}
