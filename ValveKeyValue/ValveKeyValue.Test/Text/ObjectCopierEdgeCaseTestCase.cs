using System.Globalization;

namespace ValveKeyValue.Test
{
    class ObjectCopierEdgeCaseTestCase
    {
        static readonly KVSerializer Serializer = KVSerializer.Create(KVSerializationFormat.KeyValues1Text);

        #region Nullable value types

        [Test]
        public void NullableIntProperty()
        {
            var result = Serializer.Deserialize<NullableProps>("\"obj\" { \"IntVal\" \"42\" }");
            Assert.That(result.IntVal, Is.EqualTo(42));
        }

        [Test]
        public void NullableBoolProperty()
        {
            var result = Serializer.Deserialize<NullableProps>("\"obj\" { \"BoolVal\" \"1\" }");
            Assert.That(result.BoolVal, Is.True);
        }

        [Test]
        public void NullableEnumProperty()
        {
            var result = Serializer.Deserialize<NullableProps>("\"obj\" { \"EnumVal\" \"2\" }");
            Assert.That(result.EnumVal, Is.EqualTo(TestEnum.Two));
        }

        [Test]
        public void NullablePropertyMissingStaysNull()
        {
            var result = Serializer.Deserialize<NullableProps>("\"obj\" { }");
            Assert.That(result.IntVal, Is.Null);
            Assert.That(result.BoolVal, Is.Null);
            Assert.That(result.EnumVal, Is.Null);
        }

        class NullableProps
        {
            public int? IntVal { get; set; }
            public bool? BoolVal { get; set; }
            public TestEnum? EnumVal { get; set; }
        }

        enum TestEnum { One = 1, Two = 2 }

        #endregion

        #region Nested POCO

        [Test]
        public void NestedObjectDeserialization()
        {
            var result = Serializer.Deserialize<Outer>(
                "\"obj\" { \"Name\" \"parent\" \"Inner\" { \"Value\" \"42\" } }");
            Assert.That(result.Name, Is.EqualTo("parent"));
            Assert.That(result.Inner, Is.Not.Null);
            Assert.That(result.Inner.Value, Is.EqualTo(42));
        }

        [Test]
        public void DeeplyNestedObjects()
        {
            var result = Serializer.Deserialize<Outer>(
                "\"obj\" { \"Name\" \"top\" \"Inner\" { \"Value\" \"1\" \"Deep\" { \"Flag\" \"1\" } } }");
            Assert.That(result.Inner.Deep, Is.Not.Null);
            Assert.That(result.Inner.Deep.Flag, Is.True);
        }

        class Outer
        {
            public string Name { get; set; }
            public Middle Inner { get; set; }
        }

        class Middle
        {
            public int Value { get; set; }
            public Leaf Deep { get; set; }
        }

        class Leaf
        {
            public bool Flag { get; set; }
        }

        #endregion

        #region Enum collections

        [Test]
        public void ListOfEnums()
        {
            var result = Serializer.Deserialize<EnumListContainer>(
                "\"obj\" { \"Items\" { \"0\" \"1\" \"1\" \"2\" } }");
            Assert.That(result.Items, Is.EqualTo(new[] { TestEnum.One, TestEnum.Two }));
        }

        [Test]
        public void ArrayOfEnums()
        {
            var result = Serializer.Deserialize<EnumArrayContainer>(
                "\"obj\" { \"Items\" { \"0\" \"1\" \"1\" \"2\" } }");
            Assert.That(result.Items, Is.EqualTo(new[] { TestEnum.One, TestEnum.Two }));
        }

        class EnumListContainer
        {
            public List<TestEnum> Items { get; set; }
        }

        class EnumArrayContainer
        {
            public TestEnum[] Items { get; set; }
        }

        #endregion

        #region Scalar type coercion (KV1 text infers int, target expects different type)

        [Test]
        public void IntInferredValueToStringProperty()
        {
            // KV1 parser infers "42" as int, but target is string
            var result = Serializer.Deserialize<Dictionary<string, string>>("\"obj\" { \"key\" \"42\" }");
            Assert.That(result["key"], Is.EqualTo("42"));
        }

        [Test]
        public void FloatInferredValueToStringProperty()
        {
            var result = Serializer.Deserialize<Dictionary<string, string>>("\"obj\" { \"key\" \"3.14\" }");
            Assert.That(result["key"], Is.EqualTo("3.14"));
        }

        [Test]
        public void IntInferredValueToBoolProperty()
        {
            // KV1 parser may infer "1" as int, target is bool
            var result = Serializer.Deserialize<BoolContainer>("\"obj\" { \"Flag\" \"1\" }");
            Assert.That(result.Flag, Is.True);
        }

        [Test]
        public void ZeroInferredValueToBoolProperty()
        {
            var result = Serializer.Deserialize<BoolContainer>("\"obj\" { \"Flag\" \"0\" }");
            Assert.That(result.Flag, Is.False);
        }

        class BoolContainer
        {
            public bool Flag { get; set; }
        }

        #endregion

        #region Unknown properties silently ignored

        [Test]
        public void UnknownPropertiesIgnored()
        {
            var result = Serializer.Deserialize<SimpleObject>(
                "\"obj\" { \"Known\" \"hello\" \"Unknown\" \"ignored\" \"AlsoUnknown\" \"42\" }");
            Assert.That(result.Known, Is.EqualTo("hello"));
        }

        class SimpleObject
        {
            public string Known { get; set; }
        }

        #endregion

        #region Struct deserialization

        [Test]
        public void StructDeserialization()
        {
            var result = Serializer.Deserialize<StructContainer>(
                "\"obj\" { \"Data\" { \"X\" \"10\" \"Y\" \"20\" } }");
            Assert.That(result.Data.X, Is.EqualTo(10));
            Assert.That(result.Data.Y, Is.EqualTo(20));
        }

        class StructContainer
        {
            public PointStruct Data { get; set; }
        }

        struct PointStruct
        {
            public int X { get; set; }
            public int Y { get; set; }
        }

        #endregion

        #region Empty containers

        [Test]
        public void EmptyDictionary()
        {
            var result = Serializer.Deserialize<DictContainer>("\"obj\" { \"Dict\" { } }");
            Assert.That(result.Dict, Is.Not.Null);
            Assert.That(result.Dict, Is.Empty);
        }

        [Test]
        public void EmptyList()
        {
            var result = Serializer.Deserialize<ListContainer>("\"obj\" { \"Items\" { } }");
            Assert.That(result.Items, Is.Not.Null);
            Assert.That(result.Items, Is.Empty);
        }

        class DictContainer
        {
            public Dictionary<string, string> Dict { get; set; }
        }

        class ListContainer
        {
            public List<string> Items { get; set; }
        }

        #endregion

        #region Serialization round-trip

        [Test]
        public void RoundTripNestedObject()
        {
            var original = new Outer
            {
                Name = "test",
                Inner = new Middle { Value = 42, Deep = new Leaf { Flag = true } }
            };

            string text;
            using (var ms = new MemoryStream())
            {
                Serializer.Serialize(ms, original, "root");
                ms.Seek(0, SeekOrigin.Begin);
                using var reader = new StreamReader(ms);
                text = reader.ReadToEnd();
            }

            var deserialized = Serializer.Deserialize<Outer>(text);
            Assert.That(deserialized.Name, Is.EqualTo("test"));
            Assert.That(deserialized.Inner.Value, Is.EqualTo(42));
            Assert.That(deserialized.Inner.Deep.Flag, Is.True);
        }

        [Test]
        public void RoundTripWithEnumAndCollections()
        {
            var original = new MixedContainer
            {
                Name = "mixed",
                Count = 5,
                Active = true,
                Items = ["a", "b", "c"],
            };

            string text;
            using (var ms = new MemoryStream())
            {
                Serializer.Serialize(ms, original, "root");
                ms.Seek(0, SeekOrigin.Begin);
                using var reader = new StreamReader(ms);
                text = reader.ReadToEnd();
            }

            var deserialized = Serializer.Deserialize<MixedContainer>(text);
            Assert.That(deserialized.Name, Is.EqualTo("mixed"));
            Assert.That(deserialized.Count, Is.EqualTo(5));
            Assert.That(deserialized.Active, Is.True);
            Assert.That(deserialized.Items, Has.Count.EqualTo(3));
            Assert.That(deserialized.Items[0], Is.EqualTo("a"));
            Assert.That(deserialized.Items[1], Is.EqualTo("b"));
            Assert.That(deserialized.Items[2], Is.EqualTo("c"));
        }

        class MixedContainer
        {
            public string Name { get; set; }
            public int Count { get; set; }
            public bool Active { get; set; }
            public List<string> Items { get; set; }
        }

        #endregion

        #region Dictionary with non-string value types

        [Test]
        public void DictionaryWithIntValues()
        {
            var result = Serializer.Deserialize<Dictionary<string, int>>(
                "\"obj\" { \"a\" \"1\" \"b\" \"2\" \"c\" \"3\" }");
            Assert.That(result["a"], Is.EqualTo(1));
            Assert.That(result["b"], Is.EqualTo(2));
            Assert.That(result["c"], Is.EqualTo(3));
        }

        [Test]
        public void DictionaryWithBoolValues()
        {
            var result = Serializer.Deserialize<Dictionary<string, bool>>(
                "\"obj\" { \"enabled\" \"1\" \"disabled\" \"0\" }");
            Assert.That(result["enabled"], Is.True);
            Assert.That(result["disabled"], Is.False);
        }

        #endregion

        #region Char and decimal properties

        [Test]
        public void CharProperty()
        {
            var result = Serializer.Deserialize<CharDecContainer>("\"obj\" { \"Ch\" \"A\" }");
            Assert.That(result.Ch, Is.EqualTo('A'));
        }

        [Test]
        public void DecimalProperty()
        {
            var result = Serializer.Deserialize<CharDecContainer>("\"obj\" { \"Dec\" \"123.456\" }");
            Assert.That(result.Dec, Is.EqualTo(123.456m));
        }

        class CharDecContainer
        {
            public char Ch { get; set; }
            public decimal Dec { get; set; }
        }

        #endregion

        #region KVProperty on properties in nested objects

        [Test]
        public void KVPropertyInNestedObject()
        {
            var result = Serializer.Deserialize<RenamedOuter>(
                "\"obj\" { \"child_obj\" { \"display name\" \"Hello\" } }");
            Assert.That(result.Child, Is.Not.Null);
            Assert.That(result.Child.DisplayName, Is.EqualTo("Hello"));
        }

        class RenamedOuter
        {
            [KVProperty("child_obj")]
            public RenamedInner Child { get; set; }
        }

        class RenamedInner
        {
            [KVProperty("display name")]
            public string DisplayName { get; set; }
        }

        #endregion

        #region Negative and boundary numeric values

        [Test]
        public void NegativeIntDeserialization()
        {
            var result = Serializer.Deserialize<NumericEdges>("\"obj\" { \"SignedVal\" \"-2147483648\" }");
            Assert.That(result.SignedVal, Is.EqualTo(int.MinValue));
        }

        [Test]
        public void MaxULongSerialization()
        {
            var obj = new NumericEdges { UnsignedVal = ulong.MaxValue };
            string text;
            using (var ms = new MemoryStream())
            {
                Serializer.Serialize(ms, obj, "root");
                ms.Seek(0, SeekOrigin.Begin);
                using var reader = new StreamReader(ms);
                text = reader.ReadToEnd();
            }

            Assert.That(text, Does.Contain(ulong.MaxValue.ToString(CultureInfo.InvariantCulture)));
        }

        class NumericEdges
        {
            public int SignedVal { get; set; }
            public ulong UnsignedVal { get; set; }
        }

        #endregion

        #region Array inside dictionary value

        [Test]
        public void DictionaryWithArrayValues()
        {
            var result = Serializer.Deserialize<DictWithArrayContainer>(
                "\"obj\" { \"Data\" { \"nums\" { \"0\" \"10\" \"1\" \"20\" } } }");
            Assert.That(result.Data["nums"], Has.Length.EqualTo(2));
            Assert.That(result.Data["nums"][0], Is.EqualTo(10));
            Assert.That(result.Data["nums"][1], Is.EqualTo(20));
        }

        class DictWithArrayContainer
        {
            public Dictionary<string, int[]> Data { get; set; }
        }

        #endregion

        #region Collection with non-zero-based or non-consecutive keys fails for list target

        [Test]
        public void NonZeroBasedKeysThrowsForListTarget()
        {
            Assert.That(
                () => Serializer.Deserialize<ListContainer>("\"obj\" { \"Items\" { \"1\" \"a\" \"2\" \"b\" } }"),
                Throws.InstanceOf<InvalidOperationException>());
        }

        [Test]
        public void NonConsecutiveKeysThrowsForArrayTarget()
        {
            Assert.That(
                () => Serializer.Deserialize<StringArrayContainer>("\"obj\" { \"Items\" { \"0\" \"a\" \"2\" \"b\" } }"),
                Throws.InstanceOf<InvalidOperationException>());
        }

        [Test]
        public void NonNumericKeysAreDictionaryNotArray()
        {
            // Same data succeeds as dictionary
            var result = Serializer.Deserialize<DictContainer>(
                "\"obj\" { \"Dict\" { \"alpha\" \"one\" \"beta\" \"two\" } }");
            Assert.That(result.Dict["alpha"], Is.EqualTo("one"));
            Assert.That(result.Dict["beta"], Is.EqualTo("two"));
        }

        class StringArrayContainer
        {
            public string[] Items { get; set; }
        }

        #endregion
    }
}
