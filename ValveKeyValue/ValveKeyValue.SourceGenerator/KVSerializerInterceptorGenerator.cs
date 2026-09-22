using System.Collections.Immutable;
using Microsoft.CodeAnalysis;

namespace ValveKeyValue.SourceGenerator
{
    /// <summary>
    /// Intercepts calls to the reflection-based generic methods of <c>ValveKeyValue.KVSerializer</c> that have a concrete
    /// type argument, and replaces them with calls that use generated type information.
    /// </summary>
    [Generator(LanguageNames.CSharp)]
    public sealed class KVSerializerInterceptorGenerator : IIncrementalGenerator
    {
        /// <summary>The name of the generated source file that contains the interceptors.</summary>
        public const string FileName = "KVSerializerInterceptors.g.cs";

        /// <summary>The name of the generated source file that contains the type information.</summary>
        public const string TypeInfoFileName = "KVGeneratedTypeInfo.g.cs";

        /// <summary>The tracking names of the pipeline steps.</summary>
        public static class TrackingNames
        {
            /// <summary>The result of inspecting each call site.</summary>
            public const string CallSites = nameof(CallSites);

            /// <summary>The diagnostics of call sites that are not intercepted.</summary>
            public const string Diagnostics = nameof(Diagnostics);

            /// <summary>The types that each intercepted call site refers to.</summary>
            public const string TypeGraphs = nameof(TypeGraphs);

            /// <summary>The distinct types of all intercepted call sites.</summary>
            public const string TypeModels = nameof(TypeModels);

            /// <summary>The interceptor of each intercepted call site.</summary>
            public const string Interceptors = nameof(Interceptors);

            /// <summary>The input of the type information source.</summary>
            public const string TypeInfoOutput = nameof(TypeInfoOutput);

            /// <summary>The input of the interceptors source.</summary>
            public const string InterceptorOutput = nameof(InterceptorOutput);
        }

        /// <inheritdoc/>
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var results = context.SyntaxProvider
                .CreateSyntaxProvider(static (node, _) => KnownMethods.IsCandidateInvocation(node), Parser.Parse)
                .Where(static result => result is not null)
                .Select(static (result, _) => result!)
                .WithTrackingName(TrackingNames.CallSites);

            var diagnostics = results
                .Where(static result => result.Diagnostic is not null)
                .Select(static (result, _) => result.Diagnostic!)
                .WithTrackingName(TrackingNames.Diagnostics);

            context.RegisterSourceOutput(diagnostics, static (context, diagnostic) => context.ReportDiagnostic(diagnostic.ToDiagnostic()));

            var callSites = results
                .Where(static result => result.Model is not null)
                .Select(static (result, _) => result.Model!);

            var className = context.CompilationProvider
                .Select(static (compilation, _) => TypeNames.GetTypeInfoClassName(compilation.AssemblyName));

            // The type information depends only on the set of types, not on where the calls are, so that editing a
            // file with calls regenerates only the interceptors.
            var typeModels = callSites
                .Select(static (callSite, _) => callSite.Types)
                .WithTrackingName(TrackingNames.TypeGraphs)
                .Collect()
                .Select(static (graphs, _) => MergeTypes(graphs))
                .WithTrackingName(TrackingNames.TypeModels)
                .Combine(className)
                .WithTrackingName(TrackingNames.TypeInfoOutput);

            context.RegisterSourceOutput(typeModels, static (context, input) =>
            {
                var (types, className) = input;

                if (types.Count > 0)
                {
                    context.AddSource(TypeInfoFileName, TypeInfoEmitter.Emit(types, className));
                }
            });

            var interceptors = callSites
                .Select(static (callSite, _) => callSite.Interceptor)
                .WithTrackingName(TrackingNames.Interceptors)
                .Collect()
                .WithTrackingName(TrackingNames.InterceptorOutput);

            context.RegisterSourceOutput(interceptors, static (context, callSites) =>
            {
                if (!callSites.IsEmpty)
                {
                    context.AddSource(FileName, InterceptorEmitter.Emit(callSites));
                }
            });
        }

        // Returns every distinct type, ordered by name.
        static EquatableArray<TypeModel> MergeTypes(ImmutableArray<EquatableArray<TypeModel>> graphs)
        {
            var types = new SortedDictionary<string, TypeModel>(StringComparer.Ordinal);

            foreach (var graph in graphs)
            {
                foreach (var type in graph)
                {
                    if (!types.ContainsKey(type.Type))
                    {
                        types.Add(type.Type, type);
                    }
                }
            }

            return new EquatableArray<TypeModel>(types.Values);
        }
    }
}
