using System.Linq;
using System.Text;

namespace ValveKeyValue.Test
{
    class ObjectMappingBehaviorTestCase
    {
        static readonly KVSerializer KV1 = KVSerializer.Create(KVSerializationFormat.KeyValues1Text);

        static string SerializeToText<T>(T value)
        {
            using var ms = new MemoryStream();
            KV1.Serialize(ms, value, "root");
            return Encoding.UTF8.GetString(ms.ToArray());
        }

        static KVObject SerializeToTree<T>(T value)
            => KV1.Deserialize(SerializeToText(value)).Root;

        [Test]
        public void InheritedPropertiesSerializeDeclaredTypeFirstThenBase()
        {
            var value = new Derived { DerivedValue = "d", BaseValue = "b", ProtectedValue = "p" };
            value.SetBasePrivate("secret");

            var tree = SerializeToTree(value);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(tree.Keys, Is.EqualTo(["DerivedValue", "BaseValue", "ProtectedValue"]));
                Assert.That(tree.ContainsKey("BasePrivate"), Is.False, "private members of base types are not visible");
            }
        }

        [Test]
        public void InheritedPropertiesDeserialize()
        {
            var text = "\"root\"\n{\n\t\"DerivedValue\"\t\"d\"\n\t\"BaseValue\"\t\"b\"\n\t\"ProtectedValue\"\t\"p\"\n\t\"BasePrivate\"\t\"ignored\"\n}";
            var value = KV1.Deserialize<Derived>(text);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(value.DerivedValue, Is.EqualTo("d"));
                Assert.That(value.BaseValue, Is.EqualTo("b"));
                Assert.That(value.ProtectedValue, Is.EqualTo("p"));
                Assert.That(value.GetBasePrivate(), Is.Null);
            }
        }

        [Test]
        public void NonPublicPropertiesOfDeclaredTypeAreMapped()
        {
            var value = new WithNonPublic("private", "protected", "internal", "privateset");

            var tree = SerializeToTree(value);
            var back = KV1.Deserialize<WithNonPublic>(SerializeToText(value));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(tree.Keys, Is.EqualTo(["PrivateValue", "ProtectedValue", "InternalValue", "PrivateSetValue"]));
                Assert.That(back.Snapshot(), Is.EqualTo(value.Snapshot()));
            }
        }

        [Test]
        public void PropertyValueSerializesUsingRuntimeType()
        {
            var value = new Zoo { Pet = new Dog { Name = "Rex", Breed = "Collie" } };

            var tree = SerializeToTree(value);

            using (Assert.EnterMultipleScope())
            {
                Assert.That((string)tree["Pet"]["Name"], Is.EqualTo("Rex"));
                Assert.That((string)tree["Pet"]["Breed"], Is.EqualTo("Collie"));
            }
        }

        [Test]
        public void ReadOnlyDictionaryPropertySerializesAsObject()
        {
            var value = new WithReadOnlyDictionary
            {
                Values = new Dictionary<string, int> { ["a"] = 1, ["b"] = 2 },
            };

            var tree = SerializeToTree(value);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(tree["Values"].Keys, Is.EqualTo(["a", "b"]));
                Assert.That((int)tree["Values"]["b"], Is.EqualTo(2));
            }
        }

        [Test]
        public void EnumerableOnlyPropertiesSerializeAsArrays()
        {
            var value = new WithEnumerables
            {
                Set = ["x", "y"],
                Lazy = Enumerable.Range(1, 3).Select(i => i * 10),
            };

            var tree = SerializeToTree(value);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(tree["Set"].Keys, Is.EqualTo(["0", "1"]));
                Assert.That((string)tree["Set"]["1"], Is.EqualTo("y"));
                Assert.That(tree["Lazy"].Keys, Is.EqualTo(["0", "1", "2"]));
                Assert.That((int)tree["Lazy"]["2"], Is.EqualTo(30));
            }
        }

        [Test]
        public void EnumerableOnlyPropertyCannotBeDeserialized()
        {
            var text = "\"root\"\n{\n\t\"Set\"\n\t{\n\t\t\"0\"\t\"x\"\n\t}\n}";

            Assert.That(
                () => KV1.Deserialize<WithEnumerables>(text),
                Throws.InstanceOf<NotSupportedException>().With.Message.EqualTo("Cannot deserialize to enumerable type HashSet`1."));
        }

        [Test]
        public void RecordRoundTrips()
        {
            var value = new Person("Alice", 30);

            var tree = SerializeToTree(value);
            var back = KV1.Deserialize<Person>(SerializeToText(value));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(tree.Keys, Is.EqualTo(["Name", "Age"]));
                Assert.That(back, Is.EqualTo(value));
            }
        }

        [Test]
        public void RecursiveTypeRoundTrips()
        {
            var value = new Node
            {
                Name = "root",
                Children =
                [
                    new Node { Name = "a", Children = [new Node { Name = "a1" }] },
                    new Node { Name = "b" },
                ],
            };

            var back = KV1.Deserialize<Node>(SerializeToText(value));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(back.Name, Is.EqualTo("root"));
                Assert.That(back.Children, Has.Count.EqualTo(2));
                Assert.That(back.Children![0].Children![0].Name, Is.EqualTo("a1"));
                Assert.That(back.Children[1].Children, Is.Null);
            }
        }

        [Test]
        public void StructWithPropertiesRoundTrips()
        {
            var value = new Point { X = 3, Y = -4 };

            var back = KV1.Deserialize<Point>(SerializeToText(value));

            Assert.That(back, Is.EqualTo(value));
        }

        [Test]
        public void NullableEnumRoundTrips()
        {
            var value = new WithNullableEnum { Present = Color.Green };

            var tree = SerializeToTree(value);
            var back = KV1.Deserialize<WithNullableEnum>(SerializeToText(value));

            using (Assert.EnterMultipleScope())
            {
                Assert.That((int)tree["Present"], Is.EqualTo(2));
                Assert.That(tree.ContainsKey("Missing"), Is.False);
                Assert.That(back.Present, Is.EqualTo(Color.Green));
                Assert.That(back.Missing, Is.Null);
            }
        }

        [Test]
        public void ListOfNullableScalarsDeserializes()
        {
            var text = "\"root\"\n{\n\t\"0\"\t\"1\"\n\t\"1\"\t\"2\"\n}";

            var value = KV1.Deserialize<List<int?>>(text);

            Assert.That(value, Is.EqualTo([1, 2]));
        }

        [Test]
        public void DictionaryOfBlobsDeserializesFromKV3()
        {
            var text = "<!-- kv3 encoding:text:version{e21c7f3c-8a33-41c5-9977-a76d3a32aa0d} format:generic:version{7412167c-06e9-4698-aff2-e63eb59037e7} -->\n{\n\tfirst = #[01 02]\n\tsecond = #[FF]\n}";
            using var ms = new MemoryStream(Encoding.UTF8.GetBytes(text));

            var value = KVSerializer.Create(KVSerializationFormat.KeyValues3Text).Deserialize<Dictionary<string, byte[]>>(ms);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(value["first"], Is.EqualTo(new byte[] { 1, 2 }));
                Assert.That(value["second"], Is.EqualTo(new byte[] { 0xFF }));
            }
        }

        [Test]
        public void KV3ArrayIntoNonCollectionTypeThrows()
        {
            var text = "<!-- kv3 encoding:text:version{e21c7f3c-8a33-41c5-9977-a76d3a32aa0d} format:generic:version{7412167c-06e9-4698-aff2-e63eb59037e7} -->\n{\n\tName = [1, 2]\n}";
            using var ms = new MemoryStream(Encoding.UTF8.GetBytes(text));

            Assert.That(
                () => KVSerializer.Create(KVSerializationFormat.KeyValues3Text).Deserialize<Node>(ms),
                Throws.InstanceOf<NotSupportedException>().With.Message.Contains("Cannot convert Array to String"));
        }

        [Test]
        public void InvalidScalarConversionThrowsNotSupported()
        {
            var text = "\"root\"\n{\n\t\"X\"\t\"not a number\"\n}";

            Assert.That(
                () => KV1.Deserialize<Point>(text),
                Throws.InstanceOf<NotSupportedException>()
                    .With.Message.EqualTo("Conversion to System.Int32 failed. (type = String)")
                    .And.InnerException.InstanceOf<FormatException>());
        }

        [Test]
        public void EqualButDistinctObjectsAreNotCircularReferences()
        {
            var value = new WithRecords
            {
                Items = [new Person("Bob", 1), new Person("Bob", 1)],
            };

            var tree = SerializeToTree(value);

            Assert.That(tree["Items"].Keys, Is.EqualTo(["0", "1"]));
        }

        [Test]
        public void GetOnlyPropertyIsSerializedButSkippedOnDeserialize()
        {
            var value = new WithGetOnly("computed") { Writable = "w" };
            var text = SerializeToText(value);

            var tree = KV1.Deserialize(text).Root;
            var back = KV1.Deserialize<WithGetOnly>(text);

            using (Assert.EnterMultipleScope())
            {
                Assert.That((string)tree["ReadOnly"], Is.EqualTo("computed"));
                Assert.That(back.Writable, Is.EqualTo("w"));
                Assert.That(back.ReadOnly, Is.Null, "object is created uninitialized and the get-only member is left alone");
            }
        }

        [Test]
        public void ObjectIsCreatedWithoutRunningConstructorOrInitializers()
        {
            var back = KV1.Deserialize<WithInitializers>("\"root\"\n{\n\t\"Name\"\t\"n\"\n}");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(back.Name, Is.EqualTo("n"));
                Assert.That(back.Initialized, Is.Null);
                Assert.That(back.ConstructorRan, Is.False);
            }
        }

        class Base
        {
            public string? BaseValue { get; set; }

            protected internal string? ProtectedValue { get; set; }

            string? BasePrivate { get; set; }

            public void SetBasePrivate(string value) => BasePrivate = value;

            public string? GetBasePrivate() => BasePrivate;
        }

        class Derived : Base
        {
            public string? DerivedValue { get; set; }
        }

        class WithNonPublic
        {
            public WithNonPublic()
            {
            }

            public WithNonPublic(string privateValue, string protectedValue, string internalValue, string privateSetValue)
            {
                PrivateValue = privateValue;
                ProtectedValue = protectedValue;
                InternalValue = internalValue;
                PrivateSetValue = privateSetValue;
            }

            string? PrivateValue { get; set; }

            protected string? ProtectedValue { get; set; }

            internal string? InternalValue { get; set; }

            public string? PrivateSetValue { get; private set; }

            public (string?, string?, string?, string?) Snapshot() => (PrivateValue, ProtectedValue, InternalValue, PrivateSetValue);
        }

        class Animal
        {
            public string? Name { get; set; }
        }

        class Dog : Animal
        {
            public string? Breed { get; set; }
        }

        class Zoo
        {
            public Animal? Pet { get; set; }
        }

        class WithReadOnlyDictionary
        {
            public IReadOnlyDictionary<string, int>? Values { get; set; }
        }

        class WithEnumerables
        {
            public HashSet<string>? Set { get; set; }

            public IEnumerable<int>? Lazy { get; set; }
        }

        class Node
        {
            public string? Name { get; set; }

            public List<Node>? Children { get; set; }
        }

        struct Point
        {
            public int X { get; set; }

            public int Y { get; set; }
        }

        enum Color
        {
            Red = 1,
            Green = 2,
        }

        class WithNullableEnum
        {
            public Color? Present { get; set; }

            public Color? Missing { get; set; }
        }

        record Person(string Name, int Age);

        class WithRecords
        {
            public List<Person>? Items { get; set; }
        }

        class WithGetOnly
        {
            public WithGetOnly(string readOnly)
            {
                ReadOnly = readOnly;
            }

            public string? ReadOnly { get; }

            public string? Writable { get; set; }
        }

        class WithInitializers
        {
            public WithInitializers()
            {
                ConstructorRan = true;
            }

            public string? Name { get; set; }

            public string? Initialized { get; set; } = "init";

            public bool ConstructorRan { get; set; }
        }
    }
}
