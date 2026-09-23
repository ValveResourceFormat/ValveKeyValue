using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;

namespace ValveKeyValue.Test
{
    class EnumMappingTestCase
    {
        static readonly KVSerializationFormat[] Formats = [KVSerializationFormat.KeyValues1Text, KVSerializationFormat.KeyValues3Text];

        static readonly KVSerializer KV1 = KVSerializer.Create(KVSerializationFormat.KeyValues1Text);

        static string Serialize<T>(KVSerializationFormat format, T value)
        {
            using var ms = new MemoryStream();
            KVSerializer.Create(format).Serialize(ms, value, "root");
            return Encoding.UTF8.GetString(ms.ToArray());
        }

        static T RoundTrip<T>(KVSerializationFormat format, T value)
            => KVSerializer.Create(format).Deserialize<T>(Serialize(format, value));

        [TestCaseSource(nameof(Formats))]
        public void ListRoundTrips(KVSerializationFormat format)
        {
            var value = new List<Color> { Color.Red, Color.Blue, Color.Green, (Color)99 };

            Assert.That(RoundTrip(format, value), Is.EqualTo(value));
        }

        [TestCaseSource(nameof(Formats))]
        public void ArrayRoundTrips(KVSerializationFormat format)
        {
            var value = new[] { Color.Blue, Color.Red };

            Assert.That(RoundTrip(format, value), Is.EqualTo(value));
        }

        [TestCaseSource(nameof(Formats))]
        public void ReadOnlyListRoundTrips(KVSerializationFormat format)
        {
            IReadOnlyList<Color> value = [Color.Green, Color.Red];

            Assert.That(RoundTrip(format, value), Is.EqualTo(value));
        }

        [TestCaseSource(nameof(Formats))]
        public void CollectionRoundTrips(KVSerializationFormat format)
        {
            var value = new Collection<Color> { Color.Green, Color.Blue };

            Assert.That(RoundTrip(format, value), Is.EqualTo(value));
        }

        [TestCaseSource(nameof(Formats))]
        public void NullableListRoundTrips(KVSerializationFormat format)
        {
            var value = new List<Color?> { Color.Red, (Color)99, Color.Blue };

            Assert.That(RoundTrip(format, value), Is.EqualTo(value));
        }

        [TestCaseSource(nameof(Formats))]
        public void FlagsListRoundTrips(KVSerializationFormat format)
        {
            var value = new List<Permissions> { Permissions.None, Permissions.Read | Permissions.Write, Permissions.All, (Permissions)64 };

            Assert.That(RoundTrip(format, value), Is.EqualTo(value));
        }

        [TestCaseSource(nameof(Formats))]
        public void DictionaryValuesRoundTrip(KVSerializationFormat format)
        {
            var value = new Dictionary<string, Color> { ["first"] = Color.Red, ["second"] = Color.Blue, ["undefined"] = (Color)99 };

            Assert.That(RoundTrip(format, value), Is.EqualTo(value));
        }

        [TestCaseSource(nameof(Formats))]
        public void DictionaryKeysRoundTrip(KVSerializationFormat format)
        {
            var value = new Dictionary<Color, int> { [Color.Red] = 1, [Color.Blue] = 3, [(Color)99] = 99 };

            Assert.That(RoundTrip(format, value), Is.EqualTo(value));
        }

        [TestCaseSource(nameof(Formats))]
        public void FlagsDictionaryKeysRoundTrip(KVSerializationFormat format)
        {
            var value = new Dictionary<Permissions, string>
            {
                [Permissions.None] = "none",
                [Permissions.Read | Permissions.Write] = "read write",
                [Permissions.All] = "all",
                [(Permissions)64] = "undefined",
                [Permissions.Read | (Permissions)64] = "partially defined",
            };

            Assert.That(RoundTrip(format, value), Is.EqualTo(value));
        }

        [TestCaseSource(nameof(Formats))]
        public void ReadOnlyDictionaryKeysAndValuesRoundTrip(KVSerializationFormat format)
        {
            var value = new Dictionary<Color, Permissions>
            {
                [Color.Red] = Permissions.Read,
                [Color.Green] = Permissions.Read | Permissions.Execute,
                [(Color)99] = (Permissions)64,
            };

            var back = KVSerializer.Create(format).Deserialize<IReadOnlyDictionary<Color, Permissions>>(Serialize(format, value));

            Assert.That(back, Is.EqualTo(value));
        }

        [TestCaseSource(nameof(Formats))]
        public void ObjectWithEnumShapesRoundTrips(KVSerializationFormat format)
        {
            var value = new Shapes
            {
                Single = Color.Green,
                NullableSingle = (Color)99,
                List = [Color.Red, Color.Blue],
                Array = [Color.Blue, (Color)99],
                ReadOnlyList = [Color.Green],
                InterfaceList = [Color.Red, Color.Green],
                Enumerable = [Color.Blue],
                Collection = [Color.Red],
                Observable = [Color.Green, Color.Red],
                NullableList = [Color.Blue, Color.Red],
                Flags = [Permissions.Read | Permissions.Write, Permissions.Execute],
                Values = new Dictionary<string, Color> { ["a"] = Color.Blue, ["b"] = (Color)99 },
                NullableValues = new Dictionary<string, Color?> { ["a"] = Color.Green },
                Keys = new Dictionary<Color, int> { [Color.Green] = 2, [(Color)99] = 99 },
                FlagKeys = new Dictionary<Permissions, string> { [Permissions.Read | Permissions.Execute] = "rx" },
                Nested = new Dictionary<Color, List<Permissions>> { [Color.Red] = [Permissions.Write, Permissions.All] },
            };

            var back = RoundTrip(format, value);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(back.Single, Is.EqualTo(value.Single));
                Assert.That(back.NullableSingle, Is.EqualTo(value.NullableSingle));
                Assert.That(back.List, Is.EqualTo(value.List));
                Assert.That(back.Array, Is.EqualTo(value.Array));
                Assert.That(back.ReadOnlyList, Is.EqualTo(value.ReadOnlyList));
                Assert.That(back.InterfaceList, Is.EqualTo(value.InterfaceList));
                Assert.That(back.Enumerable, Is.EqualTo(value.Enumerable));
                Assert.That(back.Collection, Is.EqualTo(value.Collection));
                Assert.That(back.Observable, Is.EqualTo(value.Observable));
                Assert.That(back.NullableList, Is.EqualTo(value.NullableList));
                Assert.That(back.Flags, Is.EqualTo(value.Flags));
                Assert.That(back.Values, Is.EqualTo(value.Values));
                Assert.That(back.NullableValues, Is.EqualTo(value.NullableValues));
                Assert.That(back.Keys, Is.EqualTo(value.Keys));
                Assert.That(back.FlagKeys, Is.EqualTo(value.FlagKeys));
                Assert.That(back.Nested, Is.EqualTo(value.Nested));
            }
        }

        [Test]
        public void ListSerializesAsUnderlyingNumbers()
        {
            var text = Serialize(KVSerializationFormat.KeyValues1Text, new List<Color> { Color.Red, Color.Blue, (Color)99 });

            Assert.That(text, Is.EqualTo("\"root\"\n{\n\t\"0\"\t\"1\"\n\t\"1\"\t\"3\"\n\t\"2\"\t\"99\"\n}\n"));
        }

        [Test]
        public void DictionaryKeysSerializeAsNames()
        {
            var value = new Dictionary<Permissions, Color>
            {
                [Permissions.Read] = Color.Red,
                [Permissions.Read | Permissions.Write] = Color.Blue,
                [(Permissions)64] = (Color)99,
            };

            var text = Serialize(KVSerializationFormat.KeyValues1Text, value);

            Assert.That(text, Is.EqualTo("\"root\"\n{\n\t\"Read\"\t\"1\"\n\t\"Read, Write\"\t\"3\"\n\t\"64\"\t\"99\"\n}\n"));
        }

        [Test]
        public void DictionaryKeysDeserializeFromNumbers()
        {
            var text = "\"root\"\n{\n\t\"1\"\t\"10\"\n\t\"3\"\t\"30\"\n\t\"99\"\t\"990\"\n}";

            var value = KV1.Deserialize<Dictionary<Color, int>>(text);

            Assert.That(value, Is.EqualTo(new Dictionary<Color, int> { [Color.Red] = 10, [Color.Blue] = 30, [(Color)99] = 990 }));
        }

        [Test]
        public void DictionaryKeysDeserializeFromNames()
        {
            var text = "\"root\"\n{\n\t\"Red\"\t\"10\"\n\t\"blue\"\t\"30\"\n\t\"GREEN\"\t\"20\"\n}";

            var value = KV1.Deserialize<Dictionary<Color, int>>(text);

            Assert.That(value, Is.EqualTo(new Dictionary<Color, int> { [Color.Red] = 10, [Color.Blue] = 30, [Color.Green] = 20 }));
        }

        [Test]
        public void FlagsDictionaryKeysDeserializeFromNamesAndNumbers()
        {
            var text = "\"root\"\n{\n\t\"Read, Write\"\t\"rw\"\n\t\"execute,read\"\t\"rx\"\n\t\"7\"\t\"all\"\n\t\"None\"\t\"none\"\n\t\"64\"\t\"undefined\"\n}";

            var value = KV1.Deserialize<Dictionary<Permissions, string>>(text);

            Assert.That(value, Is.EqualTo(new Dictionary<Permissions, string>
            {
                [Permissions.Read | Permissions.Write] = "rw",
                [Permissions.Read | Permissions.Execute] = "rx",
                [Permissions.All] = "all",
                [Permissions.None] = "none",
                [(Permissions)64] = "undefined",
            }));
        }

        [Test]
        public void ListValuesDeserializeFromNamesAndNumbers()
        {
            var text = "\"root\"\n{\n\t\"0\"\t\"Red\"\n\t\"1\"\t\"blue\"\n\t\"2\"\t\"2\"\n\t\"3\"\t\"99\"\n}";

            var value = KV1.Deserialize<List<Color>>(text);

            Assert.That(value, Is.EqualTo(new[] { Color.Red, Color.Blue, Color.Green, (Color)99 }));
        }

        [Test]
        public void NullableListDeserializes()
        {
            var text = "\"root\"\n{\n\t\"0\"\t\"3\"\n\t\"1\"\t\"Green\"\n}";

            var value = KV1.Deserialize<List<Color?>>(text);

            Assert.That(value, Is.EqualTo(new Color?[] { Color.Blue, Color.Green }));
        }

        [Test]
        public void FlagsValueDeserializesFromNames()
        {
            var text = "\"root\"\n{\n\t\"Flags\"\n\t{\n\t\t\"0\"\t\"write\"\n\t\t\"1\"\t\"3\"\n\t\t\"2\"\t\"Read, Execute\"\n\t}\n}";

            var value = KV1.Deserialize<Shapes>(text);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(value.Flags, Is.EqualTo(new[] { Permissions.Write, Permissions.Read | Permissions.Write, Permissions.Read | Permissions.Execute }));
                Assert.That(KV1.Deserialize<Permissions>("\"root\"\t\"Read, Execute\""), Is.EqualTo(Permissions.Read | Permissions.Execute));
            }
        }

        [Test]
        public void LookupValuesDeserialize()
        {
            var text = "\"root\"\n{\n\t\"a\"\t\"Red\"\n\t\"a\"\t\"2\"\n\t\"b\"\t\"blue\"\n\t\"c\"\t\"99\"\n}";

            var value = KV1.Deserialize<ILookup<string, Color>>(text);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(value["a"], Is.EqualTo(new[] { Color.Red, Color.Green }));
                Assert.That(value["b"], Is.EqualTo(new[] { Color.Blue }));
                Assert.That(value["c"], Is.EqualTo(new[] { (Color)99 }));
            }
        }

        [Test]
        public void LookupPropertyDeserializes()
        {
            var text = "\"root\"\n{\n\t\"Lookup\"\n\t{\n\t\t\"x\"\t\"1\"\n\t\t\"x\"\t\"Blue\"\n\t}\n}";

            var value = KV1.Deserialize<WithLookup>(text);

            Assert.That(value.Lookup!["x"], Is.EqualTo(new[] { Color.Red, Color.Blue }));
        }

        [Test]
        public void TopLevelEnumDeserializesFromName()
        {
            Assert.That(KV1.Deserialize<Color>("\"root\"\t\"green\""), Is.EqualTo(Color.Green));
        }

        [Test]
        public void InvalidEnumNameThrows()
        {
            Assert.That(() => KV1.Deserialize<List<Color>>("\"root\"\n{\n\t\"0\"\t\"Purple\"\n}"), Throws.TypeOf<NotSupportedException>());
        }

        [TestCaseSource(nameof(Formats))]
        public void ByteEnumRoundTrips(KVSerializationFormat format)
            => AssertUnderlyingRoundTrips(format, ByteEnum.Min, ByteEnum.Max, (ByteEnum)99);

        [TestCaseSource(nameof(Formats))]
        public void SByteEnumRoundTrips(KVSerializationFormat format)
            => AssertUnderlyingRoundTrips(format, SByteEnum.Min, SByteEnum.Max, (SByteEnum)(-5), (SByteEnum)99);

        [TestCaseSource(nameof(Formats))]
        public void Int16EnumRoundTrips(KVSerializationFormat format)
            => AssertUnderlyingRoundTrips(format, Int16Enum.Min, Int16Enum.Max, (Int16Enum)(-5), (Int16Enum)99);

        [TestCaseSource(nameof(Formats))]
        public void UInt16EnumRoundTrips(KVSerializationFormat format)
            => AssertUnderlyingRoundTrips(format, UInt16Enum.Min, UInt16Enum.Max, (UInt16Enum)99);

        [TestCaseSource(nameof(Formats))]
        public void Int32EnumRoundTrips(KVSerializationFormat format)
            => AssertUnderlyingRoundTrips(format, Int32Enum.Min, Int32Enum.Max, (Int32Enum)(-5), (Int32Enum)99);

        [TestCaseSource(nameof(Formats))]
        public void UInt32EnumRoundTrips(KVSerializationFormat format)
            => AssertUnderlyingRoundTrips(format, UInt32Enum.Min, UInt32Enum.Max, (UInt32Enum)99);

        [TestCaseSource(nameof(Formats))]
        public void Int64EnumRoundTrips(KVSerializationFormat format)
            => AssertUnderlyingRoundTrips(format, Int64Enum.Min, Int64Enum.Max, (Int64Enum)(-5), (Int64Enum)99);

        [TestCaseSource(nameof(Formats))]
        public void UInt64EnumRoundTrips(KVSerializationFormat format)
            => AssertUnderlyingRoundTrips(format, UInt64Enum.Min, UInt64Enum.AboveInt64, UInt64Enum.Max, (UInt64Enum)99, (UInt64Enum)((ulong)long.MaxValue + 5));

        [Test]
        public void UInt64EnumDeserializesFromText()
        {
            var text = "\"root\"\n{\n\t\"Single\"\t\"18446744073709551615\"\n\t\"List\"\n\t{\n\t\t\"0\"\t\"9223372036854775808\"\n\t\t\"1\"\t\"AboveInt64\"\n\t}\n\t\"Keys\"\n\t{\n\t\t\"9223372036854775813\"\t\"a\"\n\t\t\"max\"\t\"b\"\n\t}\n}";

            var value = KV1.Deserialize<Holder<UInt64Enum>>(text);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(value.Single, Is.EqualTo(UInt64Enum.Max));
                Assert.That(value.List, Is.EqualTo(new[] { UInt64Enum.AboveInt64, UInt64Enum.AboveInt64 }));
                Assert.That(value.Keys, Is.EqualTo(new Dictionary<UInt64Enum, string>
                {
                    [(UInt64Enum)((ulong)long.MaxValue + 6)] = "a",
                    [UInt64Enum.Max] = "b",
                }));
            }
        }

        [Test]
        public void NegativeEnumDeserializesFromText()
        {
            var text = "\"root\"\n{\n\t\"Single\"\t\"-9223372036854775808\"\n\t\"List\"\n\t{\n\t\t\"0\"\t\"-5\"\n\t\t\"1\"\t\"min\"\n\t}\n\t\"Keys\"\n\t{\n\t\t\"-5\"\t\"a\"\n\t\t\"Min\"\t\"b\"\n\t}\n}";

            var value = KV1.Deserialize<Holder<Int64Enum>>(text);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(value.Single, Is.EqualTo(Int64Enum.Min));
                Assert.That(value.List, Is.EqualTo(new[] { (Int64Enum)(-5), Int64Enum.Min }));
                Assert.That(value.Keys, Is.EqualTo(new Dictionary<Int64Enum, string>
                {
                    [(Int64Enum)(-5)] = "a",
                    [Int64Enum.Min] = "b",
                }));
            }
        }

        static void AssertUnderlyingRoundTrips<TEnum>(KVSerializationFormat format, params TEnum[] values)
            where TEnum : struct, Enum
        {
            var value = new Holder<TEnum>
            {
                Single = values[^1],
                Nullable = values[0],
                List = [.. values],
                Array = values,
                Values = values.Select((v, i) => (v, i)).ToDictionary(x => "v" + x.i.ToString(CultureInfo.InvariantCulture), x => x.v),
                Keys = values.Select((v, i) => (v, i)).ToDictionary(x => x.v, x => "k" + x.i.ToString(CultureInfo.InvariantCulture)),
            };

            var back = RoundTrip(format, value);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(back.Single, Is.EqualTo(value.Single));
                Assert.That(back.Nullable, Is.EqualTo(value.Nullable));
                Assert.That(back.List, Is.EqualTo(value.List));
                Assert.That(back.Array, Is.EqualTo(value.Array));
                Assert.That(back.Values, Is.EqualTo(value.Values));
                Assert.That(back.Keys, Is.EqualTo(value.Keys));
            }
        }

        enum Color
        {
            Red = 1,
            Green = 2,
            Blue = 3,
        }

        [Flags]
        enum Permissions
        {
            None = 0,
            Read = 1,
            Write = 2,
            Execute = 4,
            All = Read | Write | Execute,
        }

        enum ByteEnum : byte
        {
            Min = byte.MinValue,
            Max = byte.MaxValue,
        }

        enum SByteEnum : sbyte
        {
            Min = sbyte.MinValue,
            Max = sbyte.MaxValue,
        }

        enum Int16Enum : short
        {
            Min = short.MinValue,
            Max = short.MaxValue,
        }

        enum UInt16Enum : ushort
        {
            Min = ushort.MinValue,
            Max = ushort.MaxValue,
        }

        enum Int32Enum
        {
            Min = int.MinValue,
            Max = int.MaxValue,
        }

        enum UInt32Enum : uint
        {
            Min = uint.MinValue,
            Max = uint.MaxValue,
        }

        enum Int64Enum : long
        {
            Min = long.MinValue,
            Max = long.MaxValue,
        }

        enum UInt64Enum : ulong
        {
            Min = ulong.MinValue,
            AboveInt64 = (ulong)long.MaxValue + 1,
            Max = ulong.MaxValue,
        }

        class Holder<TEnum>
            where TEnum : struct, Enum
        {
            public TEnum Single { get; set; }

            public TEnum? Nullable { get; set; }

            public List<TEnum>? List { get; set; }

            public TEnum[]? Array { get; set; }

            public Dictionary<string, TEnum>? Values { get; set; }

            public Dictionary<TEnum, string>? Keys { get; set; }
        }

        class Shapes
        {
            public Color Single { get; set; }

            public Color? NullableSingle { get; set; }

            public List<Color>? List { get; set; }

            public Color[]? Array { get; set; }

            public IReadOnlyList<Color>? ReadOnlyList { get; set; }

            public IList<Color>? InterfaceList { get; set; }

            public IEnumerable<Color>? Enumerable { get; set; }

            public Collection<Color>? Collection { get; set; }

            public ObservableCollection<Color>? Observable { get; set; }

            public List<Color?>? NullableList { get; set; }

            public List<Permissions>? Flags { get; set; }

            public Dictionary<string, Color>? Values { get; set; }

            public Dictionary<string, Color?>? NullableValues { get; set; }

            public Dictionary<Color, int>? Keys { get; set; }

            public Dictionary<Permissions, string>? FlagKeys { get; set; }

            public Dictionary<Color, List<Permissions>>? Nested { get; set; }
        }

        class WithLookup
        {
            public ILookup<string, Color>? Lookup { get; set; }
        }
    }
}
