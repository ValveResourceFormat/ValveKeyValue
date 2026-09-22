using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace ValveKeyValue.SourceGenerator.Test
{
    class SuppressorTestCase
    {
        const string Source = """
            using System.IO;
            using ValveKeyValue;
            using ValveKeyValue.Metadata;

            class Settings
            {
                public string? Name { get; set; }
            }

            class WithObject
            {
                public object? Value { get; set; }
            }

            static class Program
            {
                static readonly KVSerializer Serializer = KVSerializer.Create(KVSerializationFormat.KeyValues1Text);

                public static Settings Load(Stream stream) => Serializer.Deserialize<Settings>(stream);

                public static void Save(Stream stream, Settings settings) => Serializer.Serialize(stream, settings, "root");

                public static string Show(Settings settings) => Serializer.SerializeWithSourceMap(settings, "root").Text;

                public static KVTypeInfo<Settings> Info() => KVSerializer.GetTypeInfo<Settings>();

                public static T LoadAny<T>(Stream stream) => Serializer.Deserialize<T>(stream);

                public static WithObject LoadUnsupported(Stream stream) => Serializer.Deserialize<WithObject>(stream);
            }
            """;

        static readonly string[] Warnings = ["IL2026", "IL3050"];

        static readonly string[] InterceptedMethods = ["Load", "Save", "Show", "Info"];

        static readonly string[] ReflectionMethods = ["LoadAny", "LoadUnsupported"];

        [Test]
        public async Task InterceptedCallsAreSuppressed()
        {
            var result = GeneratorHarness.Run(Source);

            var analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(new RequiresCodeAnalyzer(), new KVInterceptedCallSuppressor());
            var options = new CompilationWithAnalyzersOptions(new AnalyzerOptions([]), onAnalyzerException: null, concurrentAnalysis: false, logAnalyzerExecutionTime: false, reportSuppressedDiagnostics: true);
            var diagnostics = await result.Output.WithAnalyzers(analyzers, options).GetAnalyzerDiagnosticsAsync().ConfigureAwait(false);

            var byMethod = diagnostics
                .Where(d => d.Id is "IL2026" or "IL3050")
                .GroupBy(d => GetContainingMethod(d))
                .ToDictionary(g => g.Key, g => g.ToList());

            using (Assert.EnterMultipleScope())
            {
                foreach (var method in InterceptedMethods)
                {
                    Assert.That(byMethod[method].Select(d => d.Id), Is.EquivalentTo(Warnings), method);
                    Assert.That(byMethod[method].Select(d => d.IsSuppressed), Is.All.True, method);
                }

                foreach (var method in ReflectionMethods)
                {
                    Assert.That(byMethod[method].Select(d => d.Id), Is.EquivalentTo(Warnings), method);
                    Assert.That(byMethod[method].Select(d => d.IsSuppressed), Is.All.False, method);
                }

                Assert.That(byMethod.Keys, Is.EquivalentTo(InterceptedMethods.Concat(ReflectionMethods)), "the generated code calls no method that requires unreferenced code");
            }
        }

        static string GetContainingMethod(Diagnostic diagnostic)
        {
            var node = diagnostic.Location.SourceTree!.GetRoot().FindNode(diagnostic.Location.SourceSpan);
            return node.AncestorsAndSelf().OfType<MethodDeclarationSyntax>().First().Identifier.ValueText;
        }

        // Reports IL2026 and IL3050 on the member access of calls to methods marked with [RequiresUnreferencedCode] and
        // [RequiresDynamicCode], at the location used by the trim analyzer.
        [DiagnosticAnalyzer(LanguageNames.CSharp)]
        sealed class RequiresCodeAnalyzer : DiagnosticAnalyzer
        {
            static readonly DiagnosticDescriptor RequiresUnreferencedCode = new("IL2026", "Requires unreferenced code", "Requires unreferenced code", "Trimming", DiagnosticSeverity.Warning, isEnabledByDefault: true);

            static readonly DiagnosticDescriptor RequiresDynamicCode = new("IL3050", "Requires dynamic code", "Requires dynamic code", "AOT", DiagnosticSeverity.Warning, isEnabledByDefault: true);

            public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [RequiresUnreferencedCode, RequiresDynamicCode];

            public override void Initialize(AnalysisContext context)
            {
                context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.Analyze | GeneratedCodeAnalysisFlags.ReportDiagnostics);
                context.EnableConcurrentExecution();
                context.RegisterOperationAction(Analyze, OperationKind.Invocation);
            }

            static void Analyze(OperationAnalysisContext context)
            {
                var invocation = (IInvocationOperation)context.Operation;
                var syntax = (InvocationExpressionSyntax)invocation.Syntax;
                var location = syntax.Expression is MemberAccessExpressionSyntax memberAccess ? memberAccess.GetLocation() : syntax.GetLocation();

                foreach (var attribute in invocation.TargetMethod.GetAttributes())
                {
                    var descriptor = attribute.AttributeClass?.Name switch
                    {
                        "RequiresUnreferencedCodeAttribute" => RequiresUnreferencedCode,
                        "RequiresDynamicCodeAttribute" => RequiresDynamicCode,
                        _ => null,
                    };

                    if (descriptor is not null)
                    {
                        context.ReportDiagnostic(Diagnostic.Create(descriptor, location));
                    }
                }
            }
        }
    }
}
