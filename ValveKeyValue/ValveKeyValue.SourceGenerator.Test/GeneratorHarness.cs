using System.Collections.Immutable;
using System.Linq;
using Basic.Reference.Assemblies;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace ValveKeyValue.SourceGenerator.Test
{
    static class GeneratorHarness
    {
        public static CSharpParseOptions CreateParseOptions(LanguageVersion languageVersion = LanguageVersion.CSharp12, bool enableInterceptors = true)
        {
            var options = new CSharpParseOptions(languageVersion);

            return enableInterceptors
                ? options.WithFeatures([new KeyValuePair<string, string>("InterceptorsNamespaces", "ValveKeyValue.Generated")])
                : options;
        }

        public static CSharpCompilation CreateCompilation(string source, CSharpParseOptions? parseOptions = null, string assemblyName = "Sample", IEnumerable<MetadataReference>? references = null)
        {
            parseOptions ??= CreateParseOptions();

            // Intercepted locations include a checksum of the file, so line endings must not depend on the checkout.
            source = source.ReplaceLineEndings("\n");

            return CSharpCompilation.Create(
                assemblyName,
                [CSharpSyntaxTree.ParseText(source, parseOptions, path: "Program.cs")],
                [.. Net100.References.All, MetadataReference.CreateFromFile(typeof(KVSerializer).Assembly.Location), .. references ?? []],
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary, nullableContextOptions: NullableContextOptions.Enable, allowUnsafe: true));
        }

        public static GeneratorDriver CreateDriver(CSharpParseOptions? parseOptions = null)
            => CSharpGeneratorDriver.Create(
                [new KVSerializerInterceptorGenerator().AsSourceGenerator()],
                parseOptions: parseOptions ?? CreateParseOptions(),
                driverOptions: new GeneratorDriverOptions(IncrementalGeneratorOutputKind.None, trackIncrementalGeneratorSteps: true));

        public static GeneratorResult Run(string source, CSharpParseOptions? parseOptions = null)
        {
            parseOptions ??= CreateParseOptions();

            return Run(CreateCompilation(source, parseOptions), parseOptions);
        }

        public static GeneratorResult Run(Compilation compilation, CSharpParseOptions? parseOptions = null)
        {
            var driver = CreateDriver(parseOptions).RunGeneratorsAndUpdateCompilation(compilation, out var outputCompilation, out var driverDiagnostics);
            var runResult = driver.GetRunResult().Results.Single();

            return new GeneratorResult(compilation, outputCompilation, runResult, driverDiagnostics);
        }
    }

    sealed record GeneratorResult(Compilation Input, Compilation Output, GeneratorRunResult RunResult, ImmutableArray<Diagnostic> DriverDiagnostics)
    {
        public string? GeneratedSource => GetSource(KVSerializerInterceptorGenerator.FileName);

        public string? TypeInfoSource => GetSource(KVSerializerInterceptorGenerator.TypeInfoFileName);

        public IEnumerable<Diagnostic> OutputErrors => Output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error);

        string? GetSource(string hintName) => RunResult.GeneratedSources.SingleOrDefault(s => s.HintName == hintName).SourceText?.ToString();
    }
}
