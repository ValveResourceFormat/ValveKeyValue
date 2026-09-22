using System.Collections.ObjectModel;
using System.Linq;

namespace ValveKeyValue.Test.Generated
{
    enum Rank : byte
    {
        None = 0,
        Low = 5,
        High = 200,
    }

    [Flags]
    enum Permissions : long
    {
        None = 0,
        Read = 1,
        Write = 2,
        Huge = 1L << 40,
    }

    class EnumShapes
    {
        public List<Rank>? List { get; set; }

        public Rank[]? Array { get; set; }

        public List<Rank?>? Nullable { get; set; }

        public Dictionary<string, Rank>? Values { get; set; }

        public Dictionary<Permissions, int>? FlagKeys { get; set; }

        public IReadOnlyDictionary<Rank, Permissions>? ReadOnly { get; set; }
    }

    class Scalars
    {
        public bool Boolean { get; set; }

        public byte Byte { get; set; }

        public sbyte SByte { get; set; }

        public char Char { get; set; }

        public short Int16 { get; set; }

        public ushort UInt16 { get; set; }

        public int Int32 { get; set; }

        public uint UInt32 { get; set; }

        public long Int64 { get; set; }

        public ulong UInt64 { get; set; }

        public float Single { get; set; }

        public double Double { get; set; }

        public decimal Decimal { get; set; }

        public string? String { get; set; }

        public Rank Rank { get; set; }

        public Permissions Permissions { get; set; }

        public int? MaybeInt { get; set; }

        public Rank? MaybeRank { get; set; }

        public double? MaybeDouble { get; set; }
    }

    class Catalog
    {
        public string? Name { get; set; }

        public Dictionary<int, Section>? Sections { get; set; }
    }

    class Section
    {
        public string? Title { get; set; }

        public List<Entry>? Entries { get; set; }
    }

    class Entry
    {
        public int Id { get; set; }

        public Dictionary<string, Depot>? Depots { get; set; }
    }

    class Depot
    {
        public string? Manifest { get; set; }

        public ulong Size { get; set; }
    }

    class CollectionHolder
    {
        public List<Depot>? List { get; set; }

        public int[]? Array { get; set; }

        public ObservableCollection<string>? Observable { get; set; }

        public IReadOnlyList<string>? ReadOnlyList { get; set; }

        public ICollection<int>? Collection { get; set; }

        public IEnumerable<int>? Enumerable { get; set; }

        public IDictionary<string, int>? Dictionary { get; set; }

        public List<List<int>>? Nested { get; set; }
    }

    class SerializeOnly
    {
        public HashSet<string>? Set { get; set; }

        public SortedDictionary<string, int>? Sorted { get; set; }
    }

    class LookupHolder
    {
        public ILookup<string, int>? Values { get; set; }
    }

    record Point3(int X, int Y, int Z = 7);

    record Pair<T>(T First, T Second);

    readonly struct Dimensions
    {
        public Dimensions(int width, int height)
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

        public int Other { get; set; }

        public static Counter Create(int value, int other) => new() { Value = value, Other = other };
    }

    class WithDefaults
    {
        public WithDefaults(int x, string text = "text", Rank rank = Rank.High, double scale = -1.5, long? limit = 9, string? missing = null, char letter = 'c', decimal amount = 2.5m)
        {
            X = x;
            Text = text;
            Rank = rank;
            Scale = scale;
            Limit = limit;
            Missing = missing;
            Letter = letter;
            Amount = amount;
        }

        public int X { get; }

        public string Text { get; }

        public Rank Rank { get; }

        public double Scale { get; }

        public long? Limit { get; }

        public string? Missing { get; }

        public char Letter { get; }

        public decimal Amount { get; }

        public (int, string, Rank, double, long?, string?, char, decimal) Snapshot() => (X, Text, Rank, Scale, Limit, Missing, Letter, Amount);
    }

    class Account
    {
        public required string Id { get; init; }

        public string? Owner { get; init; }

        public int Balance { get; set; }
    }

    class Attributed
    {
        [KVProperty("display_name")]
        public string? Name { get; set; }

        [KVIgnore]
        public string? Ignored { get; set; }

        [KVInclude]
        public string? PrivateSet { get; private set; }

        [KVInclude]
        string? Secret { get; set; }

        public void Assign(string privateSet, string secret)
        {
            PrivateSet = privateSet;
            Secret = secret;
        }

        public string? GetSecret() => Secret;
    }

    class BaseItem
    {
        public string? Label { get; set; }

        [KVInclude]
        protected string? Protected { get; set; }

        public void AssignBase(string label, string value)
        {
            Label = label;
            Protected = value;
        }

        public string? GetProtected() => Protected;
    }

    class DerivedItem : BaseItem
    {
        public new string? Label { get; set; }

        public string? Tag { get; set; }
    }

    class Box<T>
    {
        [KVInclude]
        public T? Value { get; private set; }

        public string? Label { get; init; }

        public static Box<T> Create(T value, string label) => new() { Value = value, Label = label };
    }

    class Factory
    {
        public Factory()
        {
        }

        [KVConstructor]
        Factory(string name, int count = 3)
        {
            Name = name;
            Count = count;
            UsedMarkedConstructor = true;
        }

        public string? Name { get; }

        public int Count { get; }

        public bool UsedMarkedConstructor { get; }

        public static Factory Make(string name, int count) => new(name, count);
    }

    class ByReference
    {
        [KVConstructor]
        ByReference(in int value, ref readonly long limit)
        {
            Value = value;
            Limit = limit;
        }

        public int Value { get; }

        public long Limit { get; }

        public static ByReference Make(int value, long limit) => new(in value, in limit);
    }

    class Outer<T>
    {
        internal class Inner
        {
            [KVInclude]
            public T? Value { get; private set; }

            public string? Label { get; set; }

            public static Inner Create(T value, string label) => new() { Value = value, Label = label };
        }

        internal sealed class Created
        {
            [KVConstructor]
            Created(T value)
            {
                Value = value;
            }

            public T Value { get; }

            public static Created Make(T value) => new(value);
        }
    }

    class ByReferenceParameters
    {
        public ByReferenceParameters(ref int value, out long limit)
        {
            Value = value;
            limit = 5;
        }

        public int Value { get; }
    }

    class TwoMarkedConstructors
    {
        [KVConstructor]
        public TwoMarkedConstructors(int a)
        {
            A = a;
        }

        [KVConstructor]
        public TwoMarkedConstructors(string b)
        {
            A = b.Length;
        }

        public int A { get; set; }
    }

    class AmbiguousConstructors
    {
        public AmbiguousConstructors(int a)
        {
            A = a;
        }

        public AmbiguousConstructors(string b)
        {
            A = b.Length;
        }

        public int A { get; set; }
    }

    class SpanConstructor
    {
        public SpanConstructor(ReadOnlySpan<char> a)
        {
            A = a.Length;
        }

        public int A { get; set; }
    }

    class DuplicateNames
    {
        public int A { get; set; }

        [KVProperty("a")]
        public int Other { get; set; }
    }

    class TreeNode
    {
        public string? Name { get; set; }

        public List<TreeNode>? Children { get; set; }
    }

    class Clamped
    {
        int percent;

        public int Percent
        {
            get => percent;
            set => percent = Math.Clamp(value, 0, 100);
        }
    }
}
