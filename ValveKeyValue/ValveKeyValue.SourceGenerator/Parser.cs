using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

namespace ValveKeyValue.SourceGenerator
{
    static class Parser
    {
        public const string GeneratedNamespace = "ValveKeyValue.Generated";

        public static CallSiteResult? Parse(GeneratorSyntaxContext context, CancellationToken cancellationToken)
        {
            var invocation = (InvocationExpressionSyntax)context.Node;
            var semanticModel = context.SemanticModel;
            var knownTypes = KnownTypes.Get(semanticModel.Compilation);

            if (semanticModel.GetOperation(invocation, cancellationToken) is not IInvocationOperation operation
                || !KnownMethods.TryGetInterceptedMethod(operation.TargetMethod, knownTypes, out var method))
            {
                return null;
            }

            var typeArgument = operation.TargetMethod.TypeArguments[0];

            if (typeArgument.TypeKind == TypeKind.Error || IsInExpressionTree(invocation, semanticModel, knownTypes, cancellationToken))
            {
                return null;
            }

            var location = LocationInfo.From(GetDiagnosticLocation(invocation));
            var parseOptions = (CSharpParseOptions)invocation.SyntaxTree.Options;

            if (parseOptions.LanguageVersion < LanguageVersion.CSharp12)
            {
                return Fail(DiagnosticDescriptors.LanguageVersion, location);
            }

            if (!AreInterceptorsEnabled(parseOptions))
            {
                return Fail(DiagnosticDescriptors.InterceptorsNotEnabled, location);
            }

            var interceptableLocation = semanticModel.GetInterceptableLocation(invocation, cancellationToken);

            if (interceptableLocation is null)
            {
                return null;
            }

            var graph = TypeGraphBuilder.Build(semanticModel.Compilation, knownTypes, typeArgument);

            if (graph.Root is not { } root)
            {
                return Fail(graph.Descriptor!, location, graph.Arguments);
            }

            var lineSpan = invocation.SyntaxTree.GetLineSpan(invocation.Span, cancellationToken);
            var displayLocation = $"{System.IO.Path.GetFileName(invocation.SyntaxTree.FilePath)}({lineSpan.StartLinePosition.Line + 1},{lineSpan.StartLinePosition.Character + 1})";

            var interceptor = new InterceptorModel(
                method,
                root.Type,
                TypeInfoEmitter.GetReference(root, TypeNames.GetTypeInfoClassName(semanticModel.Compilation.AssemblyName)),
                interceptableLocation.GetInterceptsLocationAttributeSyntax(),
                displayLocation);

            return new CallSiteResult(new CallSiteModel(interceptor, graph.Types), null);
        }

        static CallSiteResult Fail(DiagnosticDescriptor descriptor, LocationInfo? location, params string[] arguments)
            => new(null, new DiagnosticInfo(descriptor, location, new EquatableArray<string>(arguments)));

        // Reports diagnostics on the name of the called method, where the compiler reports errors for the call.
        static Location GetDiagnosticLocation(InvocationExpressionSyntax invocation)
            => ((ExpressionSyntax?)KnownMethods.GetMethodName(invocation) ?? invocation.Expression).GetLocation();

        // Interceptors must be declared in a namespace that the project lists in the InterceptorsNamespaces property.
        static bool AreInterceptorsEnabled(CSharpParseOptions options)
        {
            foreach (var feature in new[] { "InterceptorsNamespaces", "InterceptorsPreviewNamespaces" })
            {
                if (!options.Features.TryGetValue(feature, out var namespaces))
                {
                    continue;
                }

                foreach (var ns in namespaces.Split(';'))
                {
                    var trimmed = ns.Trim();

                    if (trimmed is "ValveKeyValue" or GeneratedNamespace)
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        static bool IsInExpressionTree(InvocationExpressionSyntax invocation, SemanticModel semanticModel, KnownTypes knownTypes, CancellationToken cancellationToken)
        {
            foreach (var lambda in invocation.Ancestors().OfType<AnonymousFunctionExpressionSyntax>())
            {
                for (var type = semanticModel.GetTypeInfo(lambda, cancellationToken).ConvertedType; type is not null; type = type.BaseType)
                {
                    if (KnownTypes.Is(type, knownTypes.LambdaExpression))
                    {
                        return true;
                    }
                }
            }

            return false;
        }
    }
}
