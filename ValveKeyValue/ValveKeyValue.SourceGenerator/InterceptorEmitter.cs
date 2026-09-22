using System.Collections.Immutable;
using System.Linq;

namespace ValveKeyValue.SourceGenerator
{
    // Writes one interceptor for each intercepted method and type argument, which passes the type information of the
    // type argument to the overload that accepts it.
    static class InterceptorEmitter
    {
        public static string Emit(ImmutableArray<InterceptorModel> callSites)
        {
            var writer = new CodeWriter();

            writer.WriteLines(CodeWriter.Header);
            writer.WriteLine("namespace System.Runtime.CompilerServices");
            writer.OpenBlock();
            writer.WriteLine(CodeWriter.GeneratedCodeAttribute);
            writer.WriteLine("[global::System.AttributeUsage(global::System.AttributeTargets.Method, AllowMultiple = true)]");
            writer.WriteLine("file sealed class InterceptsLocationAttribute : global::System.Attribute");
            writer.OpenBlock();
            writer.WriteLine("public InterceptsLocationAttribute(int version, string data)");
            writer.OpenBlock();
            writer.CloseBlock();
            writer.CloseBlock();
            writer.CloseBlock();
            writer.WriteLine();
            writer.WriteLine($"namespace {Parser.GeneratedNamespace}");
            writer.OpenBlock();
            writer.WriteLine(CodeWriter.GeneratedCodeAttribute);
            writer.WriteLine("file static class KVSerializerInterceptors");
            writer.OpenBlock();

            var groups = callSites
                .GroupBy(static callSite => (callSite.Method, callSite.RootType))
                .OrderBy(static group => group.Key.Method)
                .ThenBy(static group => group.Key.RootType, StringComparer.Ordinal);

            var first = true;

            foreach (var group in groups)
            {
                if (!first)
                {
                    writer.WriteLine();
                }

                first = false;

                var (method, rootType) = group.Key;
                var typeInfo = group.First().TypeInfo;
                var name = method + "_" + TypeNames.Mangle(rootType).TrimStart('@');

                // Call sites keep the order in which the compiler reports them, which follows the source.
                foreach (var callSite in group)
                {
                    writer.WriteLine($"{callSite.InterceptsLocationAttribute} // {callSite.DisplayLocation}");
                }

                switch (method)
                {
                    case InterceptedMethod.Deserialize:
                        writer.WriteLine($"public static {rootType} {name}(this global::ValveKeyValue.KVSerializer serializer, global::System.IO.Stream stream, global::ValveKeyValue.KVSerializerOptions? options = null)");
                        writer.WriteLine($"    => serializer.Deserialize<{rootType}>(stream, {typeInfo}, options);");
                        break;

                    case InterceptedMethod.Serialize:
                        writer.WriteLine($"public static void {name}(this global::ValveKeyValue.KVSerializer serializer, global::System.IO.Stream stream, {rootType} data, string name, global::ValveKeyValue.KVSerializerOptions? options = null)");
                        writer.WriteLine($"    => serializer.Serialize<{rootType}>(stream, data, name, {typeInfo}, options);");
                        break;

                    case InterceptedMethod.SerializeWithSourceMap:
                        writer.WriteLine($"public static (string Text, global::System.Collections.Generic.IReadOnlyList<global::ValveKeyValue.KvSourceSpan> Spans) {name}(this global::ValveKeyValue.KVSerializer serializer, {rootType} data, string name, global::ValveKeyValue.KVSerializerOptions? options = null)");
                        writer.WriteLine($"    => serializer.SerializeWithSourceMap<{rootType}>(data, name, {typeInfo}, options);");
                        break;

                    case InterceptedMethod.GetTypeInfo:
                        writer.WriteLine($"public static {TypeNames.KVTypeInfo}<{rootType}> {name}()");
                        writer.WriteLine($"    => {typeInfo};");
                        break;
                }
            }

            writer.CloseBlock();
            writer.CloseBlock();

            return writer.ToString();
        }
    }
}
