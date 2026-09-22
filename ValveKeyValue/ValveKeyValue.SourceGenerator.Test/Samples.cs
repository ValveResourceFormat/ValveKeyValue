namespace ValveKeyValue.SourceGenerator.Test
{
    static class Samples
    {
        public static string Get(string name) => name switch
        {
            nameof(Objects) => Objects,
            nameof(Construction) => Construction,
            _ => throw new ArgumentOutOfRangeException(nameof(name), name, null),
        };

        public const string Objects = """
            using System.Collections.Generic;
            using System.IO;
            using ValveKeyValue;

            namespace Sample
            {
                enum Color : byte
                {
                    Red = 1,
                    Green = 2,
                }

                class Settings
                {
                    public string? Name { get; set; }

                    [KVProperty("max_count")]
                    public int MaxCount { get; set; }

                    public Color Color { get; set; }

                    public int? Optional { get; set; }

                    public List<string>? Tags { get; set; }

                    public Dictionary<int, Child>? Children { get; set; }

                    [KVIgnore]
                    public string? Ignored { get; set; }

                    public IEnumerable<int> Computed => new[] { MaxCount };
                }

                class Child
                {
                    public double Weight { get; set; }

                    public Child[]? Nested { get; set; }
                }

                static class Program
                {
                    static readonly KVSerializer Serializer = KVSerializer.Create(KVSerializationFormat.KeyValues1Text);

                    public static Settings Load(Stream stream) => Serializer.Deserialize<Settings>(stream);

                    public static Settings LoadAgain(Stream stream) => Serializer.Deserialize<Settings>(stream, null);

                    public static void Save(Stream stream, Settings settings) => Serializer.Serialize(stream, settings, "settings");

                    public static Dictionary<string, Settings> LoadAll(Stream stream) => Serializer.Deserialize<Dictionary<string, Settings>>(stream);

                    public static Settings? LoadNullable(Stream stream) => Serializer.Deserialize<Settings?>(stream);

                    public static void SaveNullable(Stream stream, Settings? settings) => Serializer.Serialize(stream, settings, "settings");

                    public static int? LoadNumber(Stream stream) => Serializer?.Deserialize<int?>(stream);
                }
            }
            """;

        public const string Construction = """
            using System.Collections.Generic;
            using System.Diagnostics.CodeAnalysis;
            using System.IO;
            using ValveKeyValue;
            using ValveKeyValue.Metadata;

            namespace Sample
            {
                enum Mode
                {
                    Fast = -1,
                    Slow = 2,
                }

                record Point(int X, int Y, int Z = 7);

                record Pair<T>(T First, T Second) where T : class;

                readonly struct Size
                {
                    public Size(int width, int height)
                    {
                        Width = width;
                        Height = height;
                    }

                    public int Width { get; }

                    public int Height { get; }
                }

                struct Counter
                {
                    [KVInclude]
                    public int Value { get; private set; }
                }

                class Account
                {
                    public required string Id { get; init; }

                    [KVInclude]
                    public string? Secret { private get; set; }

                    [KVInclude]
                    string? Hidden { get; set; }
                }

                class Configured
                {
                    [SetsRequiredMembers]
                    public Configured()
                    {
                        Name = "default";
                    }

                    public required string Name { get; set; }
                }

                class Factory
                {
                    public Factory()
                    {
                    }

                    [KVConstructor]
                    Factory(string name, Mode mode = Mode.Fast, float scale = 0.5f, string? label = null, in long limit = 3, short offset = -2, Size size = default, int? count = 4)
                    {
                        Name = name;
                    }

                    public string Name { get; } = "";
                }

                class Outer<T>
                    where T : struct
                {
                    public class Inner
                    {
                        [KVInclude]
                        public T Value { get; private set; }
                    }
                }

                class ByReference
                {
                    public ByReference(ref int value, out long limit)
                    {
                        Value = value;
                        limit = 1;
                    }

                    public int Value { get; }
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

                    public int A { get; set; }
                }

                class Ambiguous
                {
                    public Ambiguous(int a)
                    {
                    }

                    public Ambiguous(string b)
                    {
                    }

                    public int A { get; set; }
                }

                class SpanConstructor
                {
                    public SpanConstructor(System.ReadOnlySpan<char> name)
                    {
                    }

                    public string? Name { get; set; }
                }

                static class Program
                {
                    static readonly KVSerializer Serializer = KVSerializer.Create(KVSerializationFormat.KeyValues3Text);

                    public static Point LoadPoint(Stream stream) => Serializer.Deserialize<Point>(stream);

                    public static Pair<string> LoadPair(Stream stream) => Serializer.Deserialize<Pair<string>>(stream);

                    public static (Size, Counter) LoadTuple(Stream stream) => Serializer.Deserialize<(Size Size, Counter Counter)>(stream);

                    public static Account LoadAccount(Stream stream) => Serializer.Deserialize<Account>(stream);

                    public static Configured LoadConfigured(Stream stream) => Serializer.Deserialize<Configured>(stream);

                    public static Factory LoadFactory(Stream stream) => Serializer.Deserialize<Factory>(stream);

                    public static Outer<int>.Inner LoadInner(Stream stream) => Serializer.Deserialize<Outer<int>.Inner>(stream);

                    public static ByReference LoadByReference(Stream stream) => Serializer.Deserialize<ByReference>(stream);

                    public static TwoMarked LoadTwoMarked(Stream stream) => Serializer.Deserialize<TwoMarked>(stream);

                    public static void SaveAmbiguous(Stream stream, Ambiguous value) => Serializer.Serialize(stream, value, "value");

                    public static SpanConstructor LoadSpanConstructor(Stream stream) => Serializer.Deserialize<SpanConstructor>(stream);

                    public static Dictionary<Point, int> LoadByPoint(Stream stream) => Serializer.Deserialize<Dictionary<Point, int>>(stream);

                    public static string Show(Point point) => Serializer.SerializeWithSourceMap(point, "point").Text;

                    public static KVTypeInfo<Point> PointInfo() => KVSerializer.GetTypeInfo<Point>();

                    public static KVTypeInfo<int> IntInfo() => KVSerializer.GetTypeInfo<int>();
                }
            }
            """;
    }
}
