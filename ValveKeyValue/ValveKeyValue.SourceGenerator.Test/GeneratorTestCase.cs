using System.Linq;
using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace ValveKeyValue.SourceGenerator.Test
{
    class GeneratorTestCase
    {
        [TestCase(nameof(Samples.Objects))]
        [TestCase(nameof(Samples.Construction))]
        public void GeneratedCodeCompilesAndMatchesSnapshot(string name)
        {
            var result = GeneratorHarness.Run(Samples.Get(name));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.DriverDiagnostics, Is.Empty);
                Assert.That(result.RunResult.Diagnostics, Is.Empty);
                Assert.That(result.Input.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error), Is.Empty, "the sample compiles");
                Assert.That(result.OutputErrors, Is.Empty, "the generated code compiles");
                Assert.That(result.Output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Warning).Select(d => d.ToString()), Is.Empty, "the generated code and its interceptors produce no warnings");
                Assert.That(CountInterceptedCalls(result), Is.EqualTo(CountCalls(result.Input)), "every call is intercepted");
            }

            using (Assert.EnterMultipleScope())
            {
                AssertMatchesSnapshot(name + ".Interceptors", result.GeneratedSource!);
                AssertMatchesSnapshot(name + ".TypeInfo", result.TypeInfoSource!);
            }
        }

        [Test]
        public void GeneratedCodeCompilesWithLatestLanguageVersion()
        {
            var result = GeneratorHarness.Run(Samples.Construction, GeneratorHarness.CreateParseOptions(LanguageVersion.Latest));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.GeneratedSource, Is.Not.Null);
                Assert.That(result.OutputErrors, Is.Empty);
            }
        }

        [TestCase("VKV0001", "static T Load<T>(Stream stream) => Serializer.Deserialize<T>(stream);")]
        [TestCase("VKV0001", "static List<T> Load<T>(Stream stream) => Serializer.Deserialize<List<T>>(stream);")]
        [TestCase("VKV0002", "static Hidden Load(Stream stream) => Serializer.Deserialize<Hidden>(stream); class Hidden { public int X { get; set; } }")]
        [TestCase("VKV0002", "static void Save(Stream stream) => Serializer.Serialize(stream, new { X = 1 }, \"root\");")]
        [TestCase("VKV0003", "static WithObject Load(Stream stream) => Serializer.Deserialize<WithObject>(stream);")]
        [TestCase("VKV0003", "static System.Collections.ArrayList Load(Stream stream) => Serializer.Deserialize<System.Collections.ArrayList>(stream);")]
        [TestCase("VKV0003", "static IShape Load(Stream stream) => Serializer.Deserialize<IShape>(stream);")]
        [TestCase("VKV0003", "static int[,] Load(Stream stream) => Serializer.Deserialize<int[,]>(stream);")]
        [TestCase("VKV0003", "static SelfList Load(Stream stream) => Serializer.Deserialize<SelfList>(stream);")]
        [TestCase("VKV0003", "static WithPointerConstructor Load(Stream stream) => Serializer.Deserialize<WithPointerConstructor>(stream);")]
        public void UnsupportedCallIsReportedAndNotIntercepted(string id, string member)
        {
            var result = GeneratorHarness.Run(GetCallSample(member));
            var unsupported = GetCalls(result.Input).First();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Input.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error), Is.Empty, "the sample compiles");
                Assert.That(result.RunResult.Diagnostics.Select(d => d.Id), Is.EqualTo([id]));
                Assert.That(result.RunResult.Diagnostics.Single().Severity, Is.EqualTo(DiagnosticSeverity.Info));
                Assert.That(result.RunResult.Diagnostics.Single().Location.SourceSpan.IntersectsWith(unsupported.Span), Is.True);
                Assert.That(GetInterceptedData(result), Does.Not.Contain(GetLocationData(result.Input, unsupported)));
                Assert.That(CountInterceptedCalls(result), Is.EqualTo(1), "the supported call is still intercepted");
                Assert.That(result.OutputErrors, Is.Empty);
            }
        }

        // Deserializing these types throws at runtime like reflection-based object mapping does, so their calls are intercepted.
        [TestCase("static Dictionary<Point, int> Load(Stream stream) => Serializer.Deserialize<Dictionary<Point, int>>(stream);")]
        [TestCase("static Dictionary<byte[], int> Load(Stream stream) => Serializer.Deserialize<Dictionary<byte[], int>>(stream);")]
        [TestCase("static Duplicate Load(Stream stream) => Serializer.Deserialize<Duplicate>(stream);")]
        [TestCase("static TwoConstructors Load(Stream stream) => Serializer.Deserialize<TwoConstructors>(stream);")]
        [TestCase("static TwoMarked Load(Stream stream) => Serializer.Deserialize<TwoMarked>(stream);")]
        [TestCase("static WithSpanConstructor Load(Stream stream) => Serializer.Deserialize<WithSpanConstructor>(stream);")]
        public void CallThatFailsAtRuntimeIsIntercepted(string member)
        {
            var result = GeneratorHarness.Run(GetCallSample(member));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Input.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Error), Is.Empty, "the sample compiles");
                Assert.That(result.RunResult.Diagnostics, Is.Empty);
                Assert.That(CountInterceptedCalls(result), Is.EqualTo(2));
                Assert.That(result.OutputErrors, Is.Empty);
                Assert.That(result.Output.GetDiagnostics().Where(d => d.Severity == DiagnosticSeverity.Warning).Select(d => d.ToString()), Is.Empty);
            }
        }

        static string GetCallSample(string member)
            => $$"""
                using System.Collections.Generic;
                using System.IO;
                using ValveKeyValue;

                interface IShape
                {
                    int Sides { get; }
                }

                class Point
                {
                    public int X { get; set; }
                }

                class WithObject
                {
                    public object? Value { get; set; }
                }

                class SelfList : List<SelfList>
                {
                }

                class Duplicate
                {
                    public string? Name { get; set; }

                    [KVProperty("NAME")]
                    public string? Other { get; set; }
                }

                class TwoConstructors
                {
                    public TwoConstructors(int a)
                    {
                    }

                    public TwoConstructors(string b)
                    {
                    }
                }

                class TwoMarked
                {
                    [KVConstructor]
                    public TwoMarked(int a)
                    {
                    }

                    [KVConstructor]
                    public TwoMarked(string b)
                    {
                    }
                }

                class WithSpanConstructor
                {
                    public WithSpanConstructor(System.ReadOnlySpan<char> name)
                    {
                    }
                }

                unsafe class WithPointerConstructor
                {
                    public WithPointerConstructor(delegate*<int> callback)
                    {
                    }
                }

                class Program
                {
                    static readonly KVSerializer Serializer = KVSerializer.Create(KVSerializationFormat.KeyValues1Text);

                    {{member}}

                    static Point Supported(Stream stream) => Serializer.Deserialize<Point>(stream);
                }
                """;

        [Test]
        public void OldLanguageVersionIsReported()
        {
            var result = GeneratorHarness.Run(Samples.Objects, GeneratorHarness.CreateParseOptions(LanguageVersion.CSharp11));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.RunResult.Diagnostics.Select(d => d.Id).Distinct(), Is.EqualTo(["VKV0006"]));
                Assert.That(result.GeneratedSource, Is.Null);
            }
        }

        [Test]
        public void DisabledInterceptorsNamespaceIsReported()
        {
            var result = GeneratorHarness.Run(Samples.Objects, GeneratorHarness.CreateParseOptions(enableInterceptors: false));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.RunResult.Diagnostics.Select(d => d.Id).Distinct(), Is.EqualTo(["VKV0007"]));
                Assert.That(result.GeneratedSource, Is.Null);
                Assert.That(result.OutputErrors, Is.Empty);
            }
        }

        [Test]
        public void NothingIsGeneratedWithoutCalls()
        {
            var result = GeneratorHarness.Run("""
                using System.IO;
                using ValveKeyValue;
                using ValveKeyValue.Metadata;

                class Program
                {
                    static KVDocument Load(Stream stream) => KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(stream);

                    static int Load(Stream stream, KVTypeInfo<int> typeInfo) => KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(stream, typeInfo);

                    static void Deserialize<T>(Stream stream) { }

                    static void Call(Stream stream) => Deserialize<int>(stream);
                }
                """);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.RunResult.GeneratedSources, Is.Empty);
                Assert.That(result.RunResult.Diagnostics, Is.Empty);
            }
        }

        [Test]
        public void UnrelatedEditIsCached()
        {
            var parseOptions = GeneratorHarness.CreateParseOptions();
            var compilation = GeneratorHarness.CreateCompilation(Samples.Objects, parseOptions)
                .AddSyntaxTrees(CSharpSyntaxTree.ParseText("class Unrelated { }", parseOptions, path: "Unrelated.cs"));

            var driver = GeneratorHarness.CreateDriver(parseOptions).RunGenerators(compilation);

            var unrelated = compilation.SyntaxTrees.Single(t => t.FilePath == "Unrelated.cs");
            var edited = compilation.ReplaceSyntaxTree(unrelated, CSharpSyntaxTree.ParseText("class Unrelated { int field; }", parseOptions, path: "Unrelated.cs"));

            var result = driver.RunGenerators(edited).GetRunResult().Results.Single();

            var callSiteSteps = result.TrackedSteps[KVSerializerInterceptorGenerator.TrackingNames.CallSites];

            using (Assert.EnterMultipleScope())
            {
                Assert.That(callSiteSteps.SelectMany(s => s.Outputs).Count(), Is.EqualTo(CountCalls(edited)));
                Assert.That(result.TrackedOutputSteps.SelectMany(s => s.Value).SelectMany(s => s.Outputs), Is.Not.Empty);
                Assert.That(GetReasons(result, KVSerializerInterceptorGenerator.TrackingNames.CallSites), Is.All.AnyOf(IncrementalStepRunReason.Cached, IncrementalStepRunReason.Unchanged));
                Assert.That(GetReasons(result, KVSerializerInterceptorGenerator.TrackingNames.TypeInfoOutput), Is.Not.Empty.And.All.AnyOf(IncrementalStepRunReason.Cached, IncrementalStepRunReason.Unchanged));
                Assert.That(GetReasons(result, KVSerializerInterceptorGenerator.TrackingNames.InterceptorOutput), Is.Not.Empty.And.All.AnyOf(IncrementalStepRunReason.Cached, IncrementalStepRunReason.Unchanged));
                Assert.That(result.TrackedOutputSteps.SelectMany(s => s.Value).SelectMany(s => s.Outputs).Select(o => o.Reason), Is.All.EqualTo(IncrementalStepRunReason.Cached));
            }
        }

        [Test]
        public void CallSiteEditRegeneratesOnlyInterceptors()
        {
            var parseOptions = GeneratorHarness.CreateParseOptions();
            var compilation = GeneratorHarness.CreateCompilation(Samples.Objects, parseOptions);

            var driver = GeneratorHarness.CreateDriver(parseOptions).RunGenerators(compilation);
            var before = driver.GetRunResult().Results.Single();

            // Every intercepted location includes a checksum of its file, so any edit of the file moves them all.
            var program = compilation.SyntaxTrees.Single(t => t.FilePath == "Program.cs");
            var edited = compilation.ReplaceSyntaxTree(program, CSharpSyntaxTree.ParseText("// edited\n" + Samples.Objects.ReplaceLineEndings("\n"), parseOptions, path: "Program.cs"));

            driver = driver.RunGenerators(edited);
            var result = driver.GetRunResult().Results.Single();

            string? Source(GeneratorRunResult run, string hintName) => run.GeneratedSources.Single(s => s.HintName == hintName).SourceText.ToString();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(GetReasons(result, KVSerializerInterceptorGenerator.TrackingNames.CallSites), Is.All.EqualTo(IncrementalStepRunReason.Modified));
                Assert.That(GetReasons(result, KVSerializerInterceptorGenerator.TrackingNames.TypeGraphs), Is.Not.Empty.And.All.EqualTo(IncrementalStepRunReason.Unchanged));
                Assert.That(GetReasons(result, KVSerializerInterceptorGenerator.TrackingNames.TypeModels), Is.Not.Empty.And.All.AnyOf(IncrementalStepRunReason.Cached, IncrementalStepRunReason.Unchanged));
                Assert.That(GetReasons(result, KVSerializerInterceptorGenerator.TrackingNames.TypeInfoOutput), Is.Not.Empty.And.All.AnyOf(IncrementalStepRunReason.Cached, IncrementalStepRunReason.Unchanged));
                Assert.That(GetReasons(result, KVSerializerInterceptorGenerator.TrackingNames.InterceptorOutput), Is.EqualTo(new[] { IncrementalStepRunReason.Modified }));
                Assert.That(result.TrackedOutputSteps.SelectMany(s => s.Value).SelectMany(s => s.Outputs).Select(o => o.Reason), Is.EquivalentTo(new[] { IncrementalStepRunReason.Cached, IncrementalStepRunReason.Modified }));
                Assert.That(Source(result, KVSerializerInterceptorGenerator.TypeInfoFileName), Is.EqualTo(Source(before, KVSerializerInterceptorGenerator.TypeInfoFileName)));
                Assert.That(Source(result, KVSerializerInterceptorGenerator.FileName), Is.Not.EqualTo(Source(before, KVSerializerInterceptorGenerator.FileName)));
            }
        }

        [Test]
        public void AssembliesWithInternalsVisibleToDoNotConflict()
        {
            const string LibrarySource = """
                using System.IO;
                using System.Runtime.CompilerServices;
                using ValveKeyValue;

                [assembly: InternalsVisibleTo("Consumer")]

                namespace Library
                {
                    internal class Settings
                    {
                        public string? Name { get; set; }

                        public int[]? Values { get; set; }
                    }

                    static class Loader
                    {
                        public static Settings Load(Stream stream) => KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize<Settings>(stream);
                    }
                }
                """;

            const string ConsumerSource = """
                using System.IO;
                using ValveKeyValue;

                namespace Consumer
                {
                    internal class Settings
                    {
                        public int[]? Values { get; set; }
                    }

                    static class Loader
                    {
                        public static Settings Load(Stream stream) => KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize<Settings>(stream);

                        public static Library.Settings LoadLibrary(Stream stream) => KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize<Library.Settings>(stream);
                    }
                }
                """;

            var library = GeneratorHarness.Run(GeneratorHarness.CreateCompilation(LibrarySource, assemblyName: "Library"));
            var consumer = GeneratorHarness.Run(GeneratorHarness.CreateCompilation(ConsumerSource, assemblyName: "Consumer", references: [library.Output.ToMetadataReference()]));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(library.TypeInfoSource, Is.Not.Null);
                Assert.That(consumer.TypeInfoSource, Is.Not.Null);
                Assert.That(library.RunResult.Diagnostics, Is.Empty);
                Assert.That(consumer.RunResult.Diagnostics, Is.Empty);
                Assert.That(library.Output.GetDiagnostics().Where(d => d.Severity >= DiagnosticSeverity.Warning).Select(d => d.ToString()), Is.Empty);
                Assert.That(consumer.Output.GetDiagnostics().Where(d => d.Severity >= DiagnosticSeverity.Warning).Select(d => d.ToString()), Is.Empty);
                Assert.That(GetInterceptedData(consumer), Has.Count.EqualTo(2));
            }
        }

        static IEnumerable<IncrementalStepRunReason> GetReasons(GeneratorRunResult result, string trackingName)
            => result.TrackedSteps.TryGetValue(trackingName, out var steps) ? steps.SelectMany(s => s.Outputs).Select(o => o.Reason) : [];

        [Test]
        public void ModelsContainNoSymbols()
        {
            var result = GeneratorHarness.Run(Samples.Construction);
            var names = typeof(KVSerializerInterceptorGenerator.TrackingNames).GetFields().Select(f => (string)f.GetValue(null)!).ToList();

            var steps = result.RunResult.TrackedSteps.Where(s => names.Contains(s.Key)).ToList();

            Assert.That(steps.Select(s => s.Key), Does.Contain(KVSerializerInterceptorGenerator.TrackingNames.TypeModels).And.Contain(KVSerializerInterceptorGenerator.TrackingNames.Interceptors));

            // The steps of the generator only; the compilation that the generator reads its assembly name from is tracked too.
            foreach (var step in steps.SelectMany(s => s.Value))
            {
                foreach (var (value, _) in step.Outputs)
                {
                    AssertNoSymbols(value, []);
                }
            }
        }

        static void AssertNoSymbols(object? value, HashSet<object> visited)
        {
            if (value is null || value is string || value.GetType().IsPrimitive || value.GetType().IsEnum || !visited.Add(value))
            {
                return;
            }

            Assert.That(value, Is.Not.InstanceOf<ISymbol>().And.Not.InstanceOf<SemanticModel>().And.Not.InstanceOf<Compilation>().And.Not.InstanceOf<SyntaxNode>());

            if (value is System.Collections.IEnumerable enumerable)
            {
                foreach (var item in enumerable)
                {
                    AssertNoSymbols(item, visited);
                }

                return;
            }

            if (value.GetType().Namespace?.StartsWith("ValveKeyValue", StringComparison.Ordinal) != true)
            {
                return;
            }

            foreach (var property in value.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public))
            {
                if (property.GetIndexParameters().Length == 0)
                {
                    AssertNoSymbols(property.GetValue(value), visited);
                }
            }
        }

        internal static IEnumerable<InvocationExpressionSyntax> GetCalls(Compilation compilation)
        {
            var tree = compilation.SyntaxTrees.Single(t => t.FilePath == "Program.cs");
            var model = compilation.GetSemanticModel(tree);

            return tree.GetRoot().DescendantNodes().OfType<InvocationExpressionSyntax>()
                .Where(i => model.GetSymbolInfo(i).Symbol is IMethodSymbol { IsGenericMethod: true, ContainingType.Name: "KVSerializer" } method
                    && !method.Parameters.Any(p => p.Type.Name == "KVTypeInfo"));
        }

        static int CountCalls(Compilation compilation) => GetCalls(compilation).Count();

        internal static string GetLocationData(Compilation compilation, InvocationExpressionSyntax invocation)
            => compilation.GetSemanticModel(invocation.SyntaxTree).GetInterceptableLocation(invocation)!.Data;

        internal static IReadOnlyList<string> GetInterceptedData(GeneratorResult result)
        {
            var tree = result.Output.SyntaxTrees.SingleOrDefault(t => t.FilePath.EndsWith(KVSerializerInterceptorGenerator.FileName, StringComparison.Ordinal));

            if (tree is null)
            {
                return [];
            }

            return [.. tree.GetRoot().DescendantNodes().OfType<AttributeSyntax>()
                .Where(a => a.Name.ToString().EndsWith("InterceptsLocationAttribute", StringComparison.Ordinal))
                .Select(a => ((LiteralExpressionSyntax)a.ArgumentList!.Arguments[1].Expression).Token.ValueText)];
        }

        static int CountInterceptedCalls(GeneratorResult result) => GetInterceptedData(result).Count;

        static void AssertMatchesSnapshot(string name, string actual)
        {
            var version = typeof(KVSerializerInterceptorGenerator).Assembly.GetName().Version!.ToString();
            actual = actual.Replace($"\"{version}\"", "\"VERSION\"", StringComparison.Ordinal);

            var resourceName = $"ValveKeyValue.SourceGenerator.Test.Snapshots.{name}.g.cs";
            using var stream = typeof(GeneratorTestCase).Assembly.GetManifestResourceStream(resourceName);
            using var reader = stream is null ? null : new StreamReader(stream);
            var expected = reader?.ReadToEnd().ReplaceLineEndings("\n");

            if (expected != actual)
            {
                File.WriteAllText(Path.Combine(TestContext.CurrentContext.TestDirectory, $"{name}.received.g.cs"), actual);
            }

            Assert.That(actual, Is.EqualTo(expected), $"The generated code does not match Snapshots/{name}.g.cs; the received output was written next to the test assembly.");
        }
    }
}
