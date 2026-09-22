using System.Collections.Immutable;
using System.Linq;
using System.Threading;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace ValveKeyValue.SourceGenerator
{
    /// <summary>
    /// Suppresses the trimming and AOT warnings of <c>ValveKeyValue.KVSerializer</c> calls that the generator intercepts,
    /// because the trim analyzer does not know that the reflection-based method is never called.
    /// </summary>
    [DiagnosticAnalyzer(LanguageNames.CSharp)]
    public sealed class KVInterceptedCallSuppressor : DiagnosticSuppressor
    {
        const string Justification = "The call is intercepted by source-generated code that does not use reflection.";

        static readonly SuppressionDescriptor RequiresUnreferencedCode = new("VKVSUPPRESS0001", "IL2026", Justification);

        static readonly SuppressionDescriptor RequiresDynamicCode = new("VKVSUPPRESS0002", "IL3050", Justification);

        /// <inheritdoc/>
        public override ImmutableArray<SuppressionDescriptor> SupportedSuppressions { get; } = [RequiresUnreferencedCode, RequiresDynamicCode];

        /// <inheritdoc/>
        public override void ReportSuppressions(SuppressionAnalysisContext context)
        {
            HashSet<string>? intercepted = null;

            foreach (var diagnostic in context.ReportedDiagnostics)
            {
                var descriptor = diagnostic.Id switch
                {
                    "IL2026" => RequiresUnreferencedCode,
                    "IL3050" => RequiresDynamicCode,
                    _ => null,
                };

                if (descriptor is null)
                {
                    continue;
                }

                var location = diagnostic.AdditionalLocations.Count > 0 ? diagnostic.AdditionalLocations[0] : diagnostic.Location;

                if (location.SourceTree is not { } tree)
                {
                    continue;
                }

                var node = tree.GetRoot(context.CancellationToken).FindNode(location.SourceSpan, getInnermostNodeForTie: true);

                if (KnownMethods.FindInvocation(node) is not { } invocation || !KnownMethods.IsCandidateInvocation(invocation))
                {
                    continue;
                }

                var semanticModel = context.GetSemanticModel(tree);

                if (semanticModel.GetOperation(invocation, context.CancellationToken) is not IInvocationOperation operation
                    || !KnownMethods.TryGetInterceptedMethod(operation.TargetMethod, KnownTypes.Get(context.Compilation), out _))
                {
                    continue;
                }

                intercepted ??= CollectInterceptedLocations(context.Compilation, context.CancellationToken);

                if (semanticModel.GetInterceptableLocation(invocation, context.CancellationToken) is { } interceptableLocation
                    && intercepted.Contains(interceptableLocation.Data))
                {
                    context.ReportSuppression(Suppression.Create(descriptor, diagnostic));
                }
            }
        }

        // Reads the locations that the generated file intercepts, so that only calls that are actually intercepted
        // have their warnings suppressed.
        static HashSet<string> CollectInterceptedLocations(Compilation compilation, CancellationToken cancellationToken)
        {
            const string suffix = "/" + KVSerializerInterceptorGenerator.FileName;
            var result = new HashSet<string>(StringComparer.Ordinal);

            foreach (var tree in compilation.SyntaxTrees)
            {
                var path = tree.FilePath.Replace('\\', '/');

                if (!path.EndsWith(suffix, StringComparison.Ordinal))
                {
                    continue;
                }

                foreach (var attribute in tree.GetRoot(cancellationToken).DescendantNodes().OfType<AttributeSyntax>())
                {
                    if (attribute.Name.ToString() is not ("global::System.Runtime.CompilerServices.InterceptsLocationAttribute" or "global::System.Runtime.CompilerServices.InterceptsLocation")
                        || attribute.ArgumentList is not { Arguments.Count: 2 } argumentList)
                    {
                        continue;
                    }

                    if (argumentList.Arguments[0].Expression is LiteralExpressionSyntax { Token.Value: 1 }
                        && argumentList.Arguments[1].Expression is LiteralExpressionSyntax dataLiteral
                        && dataLiteral.IsKind(SyntaxKind.StringLiteralExpression))
                    {
                        result.Add(dataLiteral.Token.ValueText);
                    }
                }
            }

            return result;
        }
    }
}
