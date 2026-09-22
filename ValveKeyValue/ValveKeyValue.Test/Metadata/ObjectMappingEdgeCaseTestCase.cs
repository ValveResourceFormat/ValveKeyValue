using System.Collections;
using System.Linq;
using System.Text;
using ValveKeyValue.Metadata;

namespace ValveKeyValue.Test
{
    class ObjectMappingEdgeCaseTestCase
    {
        static readonly KVSerializer KV1 = KVSerializer.Create(KVSerializationFormat.KeyValues1Text);
        static readonly KVSerializer KV3 = KVSerializer.Create(KVSerializationFormat.KeyValues3Text);

        const string KV3Header = "<!-- kv3 encoding:text:version{e21c7f3c-8a33-41c5-9977-a76d3a32aa0d} format:generic:version{7412167c-06e9-4698-aff2-e63eb59037e7} -->\n";

        static KVObject SerializeToTree<T>(T value) => KV1.Deserialize(KV1.Serialize(value)).Root;

        [Test]
        public void ByteArrayDeserializesFromIndexedCollection()
        {
            var value = KV1.Deserialize<byte[]>("\"root\"\n{\n\t\"1\"\t\"2\"\n\t\"0\"\t\"1\"\n}");

            Assert.That(value, Is.EqualTo(new byte[] { 1, 2 }));
        }

        [Test]
        public void ByteArrayDeserializesFromKV3Array()
        {
            var value = KV3.Deserialize<WithBytes>(KV3Header + "{\n\tData = [1, 2, 3]\n}");

            Assert.That(value.Data, Is.EqualTo(new byte[] { 1, 2, 3 }));
        }

        [Test]
        public void ByteArrayFromStringThrows()
        {
            Assert.That(
                () => KV1.Deserialize<WithBytes>("\"root\"\n{\n\t\"Data\"\t\"abc\"\n}"),
                Throws.InstanceOf<NotSupportedException>().With.Message.EqualTo("Converting to Byte[] is not supported. (type = String)"));
        }

        [Test]
        public void KV3ArrayIntoWriteOnlyCollectionThrows()
        {
            Assert.That(
                () => KV3.Deserialize<WithSet>(KV3Header + "{\n\tSet = [1, 2]\n}"),
                Throws.InstanceOf<NotSupportedException>().With.Message.EqualTo("Cannot convert Array to HashSet`1."));
        }

        [Test]
        public void ScalarIntoCollectionThrows()
        {
            Assert.That(
                () => KV1.Deserialize<WithList>("\"root\"\n{\n\t\"Items\"\t\"x\"\n}"),
                Throws.InstanceOf<NotSupportedException>().With.Message.EqualTo("Converting to List`1 is not supported. (type = String)"));
        }

        [Test]
        public void KV3ArrayIntoListDeserializes()
        {
            var value = KV3.Deserialize<WithList>(KV3Header + "{\n\tItems = [3, 1, 2]\n}");

            Assert.That(value.Items, Is.EqualTo([3, 1, 2]));
        }

        [Test]
        public void GenericCollectionWithoutKnownConstructorCannotBeDeserialized()
        {
            Assert.That(
                () => KV1.Deserialize<IntList>("\"root\"\n{\n\t\"0\"\t\"1\"\n}"),
                Throws.InstanceOf<NotSupportedException>().With.Message.EqualTo("Cannot deserialize to enumerable type IntList."));
        }

        [Test]
        public void ObservableCollectionAndCollectionDeserialize()
        {
            var value = KV1.Deserialize<WithCollections>("\"root\"\n{\n\t\"Observable\"\n\t{\n\t\t\"0\"\t\"a\"\n\t}\n\t\"Plain\"\n\t{\n\t\t\"0\"\t\"b\"\n\t}\n}");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(value.Observable, Is.EqualTo(["a"]));
                Assert.That(value.Plain, Is.EqualTo(["b"]));
            }
        }

        [Test]
        public void SortedDictionarySerializesButCannotBeDeserialized()
        {
            var tree = SerializeToTree(new SortedDictionary<string, int> { ["b"] = 2, ["a"] = 1 });

            using (Assert.EnterMultipleScope())
            {
                Assert.That(tree.Keys, Is.EqualTo(["a", "b"]));
                Assert.That(
                    () => KV1.Deserialize<SortedDictionary<string, int>>("\"root\"\n{\n\t\"a\"\t\"1\"\n}"),
                    Throws.InstanceOf<NotSupportedException>().With.Message.EqualTo("Cannot deserialize to enumerable type SortedDictionary`2."));
            }
        }

        [Test]
        public void DictionaryFromKV3ArrayThrows()
        {
            Assert.That(
                () => KV3.Deserialize<WithDictionary>(KV3Header + "{\n\tMap = [1]\n}"),
                Throws.InstanceOf<NotSupportedException>().With.Message.EqualTo("Cannot convert Array to Dictionary`2."));
        }

        [Test]
        public void DictionaryFromScalarThrows()
        {
            Assert.That(
                () => KV1.Deserialize<WithDictionary>("\"root\"\n{\n\t\"Map\"\t\"x\"\n}"),
                Throws.InstanceOf<NotSupportedException>().With.Message.EqualTo("Converting to Dictionary`2 is not supported. (type = String)"));
        }

        [Test]
        public void DictionaryKeepsFirstDuplicateKey()
        {
            var value = KV1.Deserialize<Dictionary<string, string>>("\"root\"\n{\n\t\"a\"\t\"first\"\n\t\"a\"\t\"second\"\n}");

            Assert.That(value["a"], Is.EqualTo("first"));
        }

        [Test]
        public void EnumElementsAndKeysDeserializeThroughUnderlyingValue()
        {
            var list = KV1.Deserialize<List<Color>>("\"root\"\n{\n\t\"0\"\t\"2\"\n\t\"1\"\t\"1\"\n}");
            var dictionary = KV1.Deserialize<Dictionary<Color, string>>("\"root\"\n{\n\t\"2\"\t\"green\"\n}");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(list, Is.EqualTo([Color.Green, Color.Red]));
                Assert.That(dictionary[Color.Green], Is.EqualTo("green"));
            }
        }

        [Test]
        public void EnumFromCollectionThrows()
        {
            Assert.That(
                () => KV1.Deserialize<WithEnum>("\"root\"\n{\n\t\"Value\"\n\t{\n\t}\n}"),
                Throws.InstanceOf<NotSupportedException>().With.Message.EqualTo("Converting to Color is not supported. (type = Collection)"));
        }

        [Test]
        public void EnumFromKV3ArrayThrows()
        {
            Assert.That(
                () => KV3.Deserialize<WithEnum>(KV3Header + "{\n\tValue = [1]\n}"),
                Throws.InstanceOf<NotSupportedException>().With.Message.EqualTo("Cannot convert Array to Color."));
        }

        [Test]
        public void EnumElementFromCollectionThrows()
        {
            Assert.That(
                () => KV1.Deserialize<List<Color>>("\"root\"\n{\n\t\"0\"\n\t{\n\t}\n}"),
                Throws.InstanceOf<NotSupportedException>().With.Message.EqualTo("Converting to Color is not supported. (type = Collection)"));
        }

        [Test]
        public void ScalarFromCollectionThrows()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    () => KV1.Deserialize<WithScalars>("\"root\"\n{\n\t\"Number\"\n\t{\n\t}\n}"),
                    Throws.InstanceOf<NotSupportedException>().With.Message.EqualTo("Converting to Int32 is not supported. (type = Collection)"));
                Assert.That(
                    () => KV1.Deserialize<WithScalars>("\"root\"\n{\n\t\"Text\"\n\t{\n\t}\n}"),
                    Throws.InstanceOf<NotSupportedException>().With.Message.EqualTo("Converting to String is not supported. (type = Collection)"));
                Assert.That(
                    () => KV1.Deserialize<List<int>>("\"root\"\n{\n\t\"0\"\n\t{\n\t}\n}"),
                    Throws.InstanceOf<NotSupportedException>().With.Message.EqualTo("Converting to Int32 is not supported. (type = Collection)"));
                Assert.That(
                    () => KV1.Deserialize<Dictionary<string, string>>("\"root\"\n{\n\t\"a\"\n\t{\n\t}\n}"),
                    Throws.InstanceOf<NotSupportedException>().With.Message.EqualTo("Converting to String is not supported. (type = Collection)"));
            }
        }

        [Test]
        public void ScalarsUseKVObjectConversions()
        {
            var value = KV1.Deserialize<WithConversions>("\"root\"\n{\n\t\"Truncated\"\t\"1.9\"\n\t\"Negative\"\t\"-2.5\"\n\t\"Flag\"\t\"2\"\n\t\"Text\"\t\"1.5\"\n}");
            var kv3 = KV3.Deserialize<WithConversions>(KV3Header + "{\n\tText = true\n\tTruncated = 7.99\n}");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(value.Truncated, Is.EqualTo(1));
                Assert.That(value.Negative, Is.EqualTo(-2));
                Assert.That(value.Flag, Is.True);
                Assert.That(value.Text, Is.EqualTo("1.5"));
                Assert.That(kv3.Text, Is.EqualTo("1"));
                Assert.That(kv3.Truncated, Is.EqualTo(7));
            }
        }

        [TestCase("true", true)]
        [TestCase("False", false)]
        [TestCase("1", true)]
        [TestCase("0", false)]
        [TestCase("2", true)]
        public void BooleanFromText(string text, bool expected)
        {
            var value = KV1.Deserialize<WithConversions>($"\"root\"\n{{\n\t\"Flag\"\t\"{text}\"\n}}");

            Assert.That(value.Flag, Is.EqualTo(expected));
        }

        [Test]
        public void BooleanFromInvalidTextThrows()
        {
            Assert.That(
                () => KV1.Deserialize<WithConversions>("\"root\"\n{\n\t\"Flag\"\t\"yes\"\n}"),
                Throws.InstanceOf<NotSupportedException>()
                    .With.Message.EqualTo("Conversion to System.Boolean failed. (type = String)")
                    .And.InnerException.InstanceOf<FormatException>());
        }

        [Test]
        public void ScalarOverflowThrows()
        {
            Assert.That(
                () => KV1.Deserialize<List<byte>>("\"root\"\n{\n\t\"0\"\t\"256\"\n}"),
                Throws.InstanceOf<NotSupportedException>()
                    .With.Message.EqualTo("Conversion to System.Byte failed. (type = Int32)")
                    .And.InnerException.InstanceOf<OverflowException>());
        }

        [Test]
        public void KV3NullIntoNullableElementsIsNull()
        {
            var reflection = KV3.Deserialize<List<int?>>(KV3Header + "[1, null, 3]");

            using var ms = new MemoryStream(Encoding.UTF8.GetBytes(KV3Header + "[1, null, 3]"));
            var generated = KV3.Deserialize<List<int?>>(ms);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(reflection, Is.EqualTo(new int?[] { 1, null, 3 }));
                Assert.That(generated, Is.EqualTo(new int?[] { 1, null, 3 }));
            }
        }

        [Test]
        public void KV3NullIntoReferenceTypesIsNull()
        {
            var value = KV3.Deserialize<WithNulls>(KV3Header + "{\n\tText = null\n\tChild = null\n\tItems = null\n\tNames = [\"a\", null]\n\tMap = { a = null }\n\tMaybe = null\n}");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(value.Text, Is.Null);
                Assert.That(value.Child, Is.Null);
                Assert.That(value.Items, Is.Null);
                Assert.That(value.Names, Is.EqualTo(new[] { "a", null }));
                Assert.That(value.Map, Is.EqualTo(new Dictionary<string, Child?> { ["a"] = null }));
                Assert.That(value.Maybe, Is.Null);
            }
        }

        [Test]
        public void KV3NullIntoNonNullableValueTypeThrows()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    () => KV3.Deserialize<List<int>>(KV3Header + "[1, null]"),
                    Throws.InstanceOf<NotSupportedException>().With.Message.EqualTo("Converting to Int32 is not supported. (type = Null)"));
                Assert.That(
                    () => KV3.Deserialize<WithEnum>(KV3Header + "{\n\tValue = null\n}"),
                    Throws.InstanceOf<NotSupportedException>().With.Message.EqualTo("Converting to Color is not supported. (type = Null)"));
                Assert.That(
                    () => KV3.Deserialize<int>(KV3Header + "null"),
                    Throws.InstanceOf<NotSupportedException>().With.Message.EqualTo("Converting to Int32 is not supported. (type = Null)"));
            }
        }

        [Test]
        public void KV3NullRootIsNull()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(KV3.Deserialize<WithNulls>(KV3Header + "null"), Is.Null);
                Assert.That(KV3.Deserialize<int?>(KV3Header + "null"), Is.Null);
            }
        }

        [Test]
        public void ScalarDictionaryKeysRoundTrip()
        {
            AssertKeysRoundTrip(new Dictionary<char, int> { ['a'] = 1, ['Z'] = 2 });
            AssertKeysRoundTrip(new Dictionary<decimal, int> { [1.5m] = 1, [-2m] = 2 });
            AssertKeysRoundTrip(new Dictionary<double, int> { [0.1] = 1, [-1e300] = 2 });
            AssertKeysRoundTrip(new Dictionary<sbyte, int> { [-128] = 1, [127] = 2 });
            AssertKeysRoundTrip(new Dictionary<ulong, int> { [ulong.MaxValue] = 1, [0] = 2 });
            AssertKeysRoundTrip(new Dictionary<long, int> { [long.MinValue] = 1 });
            AssertKeysRoundTrip(new Dictionary<string, int> { ["key"] = 1, [""] = 2 });
            AssertKeysRoundTrip(new Dictionary<Color, int> { [Color.Green] = 1, [(Color)7] = 2 });
        }

        [Test]
        public void BooleanDictionaryKeysRoundTrip()
        {
            var tree = SerializeToTree(new Dictionary<bool, int> { [true] = 1 });

            using (Assert.EnterMultipleScope())
            {
                Assert.That(tree.Keys, Is.EqualTo(["True"]));
                AssertKeysRoundTrip(new Dictionary<bool, int> { [true] = 1, [false] = 2 });
                Assert.That(KV1.Deserialize<Dictionary<bool, int>>("\"root\"\n{\n\t\"1\"\t\"1\"\n\t\"0\"\t\"2\"\n}"), Is.EqualTo(new Dictionary<bool, int> { [true] = 1, [false] = 2 }));
            }
        }

        static void AssertKeysRoundTrip<TKey>(Dictionary<TKey, int> value)
            where TKey : notnull
        {
            using var ms = new MemoryStream();
            KV1.Serialize(ms, value, "root");
            ms.Seek(0, SeekOrigin.Begin);

            Assert.That(KV1.Deserialize<Dictionary<TKey, int>>(ms), Is.EqualTo(value), typeof(TKey).Name);
        }

        [Test]
        public void ScalarFromKV3ArrayThrows()
        {
            Assert.That(
                () => KV3.Deserialize<WithScalars>(KV3Header + "{\n\tNumber = [1]\n}"),
                Throws.InstanceOf<NotSupportedException>().With.Message.EqualTo("Cannot convert Array to Int32."));
        }

        [Test]
        public void NullableScalarConversionFailureNamesUnderlyingType()
        {
            Assert.That(
                () => KV1.Deserialize<WithNullable>("\"root\"\n{\n\t\"Value\"\t\"abc\"\n}"),
                Throws.InstanceOf<NotSupportedException>()
                    .With.Message.EqualTo("Conversion to System.Int32 failed. (type = String)")
                    .And.InnerException.InstanceOf<FormatException>());
        }

        [Test]
        public void NullableFromKV3ArrayThrows()
        {
            Assert.That(
                () => KV3.Deserialize<WithNullable>(KV3Header + "{\n\tValue = [1]\n}"),
                Throws.InstanceOf<NotSupportedException>().With.Message.EqualTo("Cannot convert Array to Int32."));
        }

        [Test]
        public void ObjectFromKV3ArrayThrows()
        {
            Assert.That(
                () => KV3.Deserialize<WithChild>(KV3Header + "{\n\tChild = [1]\n}"),
                Throws.InstanceOf<NotSupportedException>().With.Message.EqualTo("Cannot convert Array to Child."));
        }

        [Test]
        public void ObjectFromScalarThrows()
        {
            Assert.That(
                () => KV1.Deserialize<WithChild>("\"root\"\n{\n\t\"Child\"\t\"x\"\n}"),
                Throws.InstanceOf<NotSupportedException>().With.Message.EqualTo("Converting to Child is not supported. (type = String)"));
        }

        [Test]
        public void LookupFromKV3ArrayThrows()
        {
            Assert.That(
                () => KV3.Deserialize<WithLookup>(KV3Header + "{\n\tLookup = [1]\n}"),
                Throws.InstanceOf<NotSupportedException>().With.Message.EqualTo("Cannot convert Array to ILookup`2."));
        }

        [Test]
        public void LookupFromScalarThrows()
        {
            Assert.That(
                () => KV1.Deserialize<WithLookup>("\"root\"\n{\n\t\"Lookup\"\t\"x\"\n}"),
                Throws.InstanceOf<NotSupportedException>().With.Message.EqualTo("Converting to ILookup`2 is not supported. (type = String)"));
        }

        [Test]
        public void LookupSerializesGroupingsAsArrays()
        {
            var lookup = new[] { ("a", "1"), ("b", "2"), ("a", "3") }.ToLookup(p => p.Item1, p => p.Item2);

            var declared = KV1.Deserialize(KV1.Serialize(lookup, typeInfo: KVMetadata.Lookup(KVMetadata.String))).Root;
            var member = SerializeToTree(new WithLookup { Lookup = lookup })["Lookup"];

            using (Assert.EnterMultipleScope())
            {
                Assert.That(declared.Keys, Is.EqualTo(["0", "1"]));
                Assert.That(declared["0"].Values.Select(v => (string)v), Is.EqualTo(["1", "3"]));
                Assert.That(declared["1"].Values.Select(v => (string)v), Is.EqualTo(["2"]));
                Assert.That(member.Keys, Is.EqualTo(declared.Keys));
                Assert.That(member["0"].Values.Select(v => (string)v), Is.EqualTo(["1", "3"]));
            }
        }

        [Test]
        public void NonGenericEnumerablesSerializeUsingRuntimeTypes()
        {
            var value = new WithUntyped
            {
                List = new ArrayList { 1, "two" },
                Table = new Hashtable { ["key"] = 3 },
            };

            var tree = SerializeToTree(value);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(tree["List"].Keys, Is.EqualTo(["0", "1"]));
                Assert.That((string)tree["List"]["1"], Is.EqualTo("two"));
                Assert.That((int)tree["Table"]["key"], Is.EqualTo(3));
            }
        }

        [Test]
        public void NonGenericEnumerablesCannotBeDeserialized()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    () => KV1.Deserialize<WithUntyped>("\"root\"\n{\n\t\"List\"\n\t{\n\t\t\"0\"\t\"1\"\n\t}\n}"),
                    Throws.InstanceOf<NotSupportedException>().With.Message.EqualTo("Cannot deserialize to enumerable type ArrayList."));
                Assert.That(
                    () => KV1.Deserialize<WithUntyped>("\"root\"\n{\n\t\"List\"\t\"x\"\n}"),
                    Throws.InstanceOf<NotSupportedException>().With.Message.EqualTo("Converting to ArrayList is not supported. (type = String)"));
                Assert.That(
                    () => KV3.Deserialize<WithUntyped>(KV3Header + "{\n\tTable = [1]\n}"),
                    Throws.InstanceOf<NotSupportedException>().With.Message.EqualTo("Cannot convert Array to Hashtable."));
            }
        }

        [Test]
        public void SharedReferenceInsideNonGenericEnumerableIsDetected()
        {
            var child = new Child();
            var value = new WithUntyped { List = new ArrayList { child, child } };

            Assert.That(
                () => SerializeToTree(value),
                Throws.InstanceOf<KeyValueException>().With.Message.EqualTo("Serialization failed - circular object reference detected."));
        }

        [Test]
        public void UnsupportedMembersAreSkipped()
        {
            var tree = SerializeToTree(new WithUnsupportedMembers { Name = "n" });

            Assert.That(tree.Keys, Is.EqualTo(["Name"]));
        }

        [Test]
        public void UnsupportedConstructorParameterThrows()
        {
            Assert.That(
                () => KV1.Deserialize<WithSpanConstructor>("\"root\"\n{\n}"),
                Throws.InstanceOf<NotSupportedException>().With.Message.EqualTo("Constructor parameter 'name' of type 'ReadOnlySpan`1' on type 'WithSpanConstructor' is not supported."));
        }

        [Test]
        public void InParameterConstructorBinds()
        {
            var value = KV1.Deserialize<WithInConstructor>("\"root\"\n{\n\t\"Size\"\t\"4\"\n}");

            Assert.That(value.Size, Is.EqualTo(4));
        }

        enum Color
        {
            Red = 1,
            Green = 2,
        }

        class WithBytes
        {
            public byte[]? Data { get; set; }
        }

        class WithSet
        {
            public HashSet<int>? Set { get; set; }
        }

        class WithList
        {
            public List<int>? Items { get; set; }
        }

        sealed class IntList : List<int>
        {
        }

        class WithCollections
        {
            public System.Collections.ObjectModel.ObservableCollection<string>? Observable { get; set; }

            public ICollection<string>? Plain { get; set; }
        }

        class WithDictionary
        {
            public Dictionary<string, int>? Map { get; set; }
        }

        class WithEnum
        {
            public Color Value { get; set; }
        }

        class WithScalars
        {
            public int Number { get; set; }

            public string? Text { get; set; }
        }

        class WithNullable
        {
            public int? Value { get; set; }
        }

        class WithConversions
        {
            public int Truncated { get; set; }

            public long Negative { get; set; }

            public bool Flag { get; set; }

            public string? Text { get; set; }
        }

        class WithNulls
        {
            public string? Text { get; set; }

            public Child? Child { get; set; }

            public List<int>? Items { get; set; }

            public List<string?>? Names { get; set; }

            public Dictionary<string, Child?>? Map { get; set; }

            public Color? Maybe { get; set; }
        }

        class Child
        {
            public string? Name { get; set; }
        }

        class WithChild
        {
            public Child? Child { get; set; }
        }

        class WithLookup
        {
            public ILookup<string, string>? Lookup { get; set; }
        }

        class WithUntyped
        {
            public ArrayList? List { get; set; }

            public Hashtable? Table { get; set; }
        }

        class WithUnsupportedMembers
        {
            readonly int[] values = [1, 2];

            public string? Name { get; set; }

            public ReadOnlySpan<int> Span => values;

            public int this[int index] => values[index];
        }

        class WithSpanConstructor
        {
            public WithSpanConstructor(ReadOnlySpan<char> name)
            {
                Name = name.ToString();
            }

            public string Name { get; }
        }

        class WithInConstructor
        {
            public WithInConstructor(in int size)
            {
                Size = size;
            }

            public int Size { get; }
        }
    }
}
