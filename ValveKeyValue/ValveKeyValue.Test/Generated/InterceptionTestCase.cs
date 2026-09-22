using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Text;
using ValveKeyValue.Metadata;

namespace ValveKeyValue.Test.Generated
{
    // Calls with a concrete type argument are intercepted by the source generator. Each test compares a concrete call
    // (generated type information) with the same call made through an open generic helper (reflection).
    [TestFixture(KVSerializationFormat.KeyValues1Text)]
    [TestFixture(KVSerializationFormat.KeyValues3Text)]
    class InterceptionTestCase(KVSerializationFormat format)
    {
        readonly KVSerializer serializer = KVSerializer.Create(format);

        [Test]
        public void TestAssemblyContainsGeneratedInterceptors()
        {
            var generatedTypes = typeof(InterceptionTestCase).Assembly.GetTypes().Where(t => t.Namespace == "ValveKeyValue.Generated");

            Assert.That(generatedTypes, Is.Not.Empty);
        }

        [Test]
        public void ConcreteGetTypeInfoReturnsGeneratedTypeInformation()
        {
            var generated = KVSerializer.GetTypeInfo<Scalars>();
            var reflection = Reflection.GetTypeInfo<Scalars>();

            using (Assert.EnterMultipleScope())
            {
                Assert.That(KVSerializer.GetTypeInfo<Scalars>(), Is.SameAs(generated));
                Assert.That(generated, Is.Not.SameAs(reflection));
                Assert.That(GetOrigin(generated), Is.EqualTo(typeof(InterceptionTestCase).Assembly));
                Assert.That(GetOrigin(reflection), Is.EqualTo(typeof(KVSerializer).Assembly));
                Assert.That(GetOrigin(KVSerializer.GetTypeInfo<List<Scalars>>()), Is.EqualTo(typeof(InterceptionTestCase).Assembly));
                Assert.That(GetOrigin(KVSerializer.GetTypeInfo<Dictionary<int, Scalars>>()), Is.EqualTo(typeof(InterceptionTestCase).Assembly));
            }
        }

        [Test]
        public void GeneratedTypeInformationCanBePassedToGenericCode()
        {
            var value = new Scalars { Int32 = 5, String = "s" };

            var text = serializer.Serialize(value, typeInfo: KVSerializer.GetTypeInfo<Scalars>());

            Assert.That(text, Is.EqualTo(Reflection.Serialize(serializer, value)));
        }

        [Test]
        public void ScalarsEnumsAndNullables()
        {
            var value = new Scalars
            {
                Boolean = true,
                Byte = 200,
                SByte = -5,
                Char = 'q',
                Int16 = -300,
                UInt16 = 60000,
                Int32 = -70000,
                UInt32 = 4000000000,
                Int64 = -5000000000,
                UInt64 = 18000000000000000000,
                Single = 1.5f,
                Double = -2.25,
                Decimal = 3.5m,
                String = "text",
                Rank = Rank.High,
                Permissions = Permissions.Read | Permissions.Huge,
                MaybeInt = 7,
                MaybeRank = Rank.Low,
                MaybeDouble = null,
            };

            AssertSameAsReflection(value, (s, v) => serializer.Serialize(s, v, "root"), s => serializer.Deserialize<Scalars>(s));
        }

        [Test]
        public void EnumsInCollectionsAndDictionaries()
        {
            var value = new EnumShapes
            {
                List = [Rank.Low, Rank.High, (Rank)99],
                Array = [Rank.None, Rank.High],
                Nullable = [Rank.Low, Rank.High],
                Values = new Dictionary<string, Rank> { ["a"] = Rank.Low, ["b"] = (Rank)99 },
                FlagKeys = new Dictionary<Permissions, int> { [Permissions.None] = 0, [Permissions.Read | Permissions.Write] = 3, [Permissions.Huge] = 4 },
                ReadOnly = new Dictionary<Rank, Permissions> { [Rank.Low] = Permissions.Read, [(Rank)99] = Permissions.Huge },
            };

            AssertSameAsReflection(value, (s, v) => serializer.Serialize(s, v, "root"), s => serializer.Deserialize<EnumShapes>(s));
        }

        [Test]
        public void EnumsReadFromNamesAndNumbers()
        {
            var text = format == KVSerializationFormat.KeyValues1Text
                ? "\"root\"\n{\n\t\"List\"\n\t{\n\t\t\"0\"\t\"low\"\n\t\t\"1\"\t\"200\"\n\t}\n\t\"FlagKeys\"\n\t{\n\t\t\"Read, write\"\t\"3\"\n\t\t\"1099511627776\"\t\"4\"\n\t}\n}"
                : "<!-- kv3 encoding:text:version{e21c7f3c-8a33-41c5-9977-a76d3a32aa0d} format:generic:version{7412167c-06e9-4698-aff2-e63eb59037e7} -->\n{\n\tList = [ \"low\", 200 ]\n\tFlagKeys = { \"Read, write\" = 3 \"1099511627776\" = 4 }\n}";

            var generated = Read(text, s => serializer.Deserialize<EnumShapes>(s));
            var reflection = Reflection.Deserialize<EnumShapes>(serializer, text);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(generated.List, Is.EqualTo(new[] { Rank.Low, Rank.High }));
                Assert.That(generated.FlagKeys, Is.EquivalentTo(new Dictionary<Permissions, int> { [Permissions.Read | Permissions.Write] = 3, [Permissions.Huge] = 4 }));
                Assert.That(Reflection.Serialize(serializer, generated), Is.EqualTo(Reflection.Serialize(serializer, reflection)));
            }
        }

        [Test]
        public void NestedObjectGraph()
        {
            var value = new Catalog
            {
                Name = "catalog",
                Sections = new Dictionary<int, Section>
                {
                    [10] = new Section
                    {
                        Title = "games",
                        Entries =
                        [
                            new Entry
                            {
                                Id = 440,
                                Depots = new Dictionary<string, Depot>
                                {
                                    ["441"] = new Depot { Manifest = "123", Size = 1000 },
                                    ["442"] = new Depot { Manifest = "456", Size = 2000 },
                                },
                            },
                        ],
                    },
                    [20] = new Section { Title = "tools", Entries = [] },
                },
            };

            AssertSameAsReflection(value, (s, v) => serializer.Serialize(s, v, "root"), s => serializer.Deserialize<Catalog>(s));
        }

        [Test]
        public void DictionaryRoots()
        {
            var byId = new Dictionary<int, Depot> { [1] = new Depot { Manifest = "a", Size = 1 }, [2] = new Depot { Manifest = "b", Size = 2 } };
            var byName = new Dictionary<string, Depot> { ["x"] = new Depot { Manifest = "c", Size = 3 } };
            IReadOnlyDictionary<long, Rank> readOnly = new Dictionary<long, Rank> { [-1] = Rank.Low, [5000000000] = Rank.High };
            var byRank = new Dictionary<Rank, long> { [Rank.Low] = -1, [Rank.High] = 5000000000 };

            AssertSameAsReflection(byId, (s, v) => serializer.Serialize(s, v, "root"), s => serializer.Deserialize<Dictionary<int, Depot>>(s));
            AssertSameAsReflection(byName, (s, v) => serializer.Serialize(s, v, "root"), s => serializer.Deserialize<Dictionary<string, Depot>>(s));
            AssertSameAsReflection(readOnly, (s, v) => serializer.Serialize(s, v, "root"), s => serializer.Deserialize<IReadOnlyDictionary<long, Rank>>(s));
            AssertSameAsReflection(byRank, (s, v) => serializer.Serialize(s, v, "root"), s => serializer.Deserialize<Dictionary<Rank, long>>(s));
        }

        [Test]
        public void Collections()
        {
            var value = new CollectionHolder
            {
                List = [new Depot { Manifest = "a", Size = 1 }],
                Array = [1, 2, 3],
                Observable = ["x", "y"],
                ReadOnlyList = ["r"],
                Collection = [4, 5],
                Enumerable = [6],
                Dictionary = new Dictionary<string, int> { ["k"] = 1 },
                Nested = [[1, 2], [3]],
            };

            AssertSameAsReflection(value, (s, v) => serializer.Serialize(s, v, "root"), s => serializer.Deserialize<CollectionHolder>(s));

            var roundTripped = Read(Write(s => serializer.Serialize(s, value, "root")), s => serializer.Deserialize<CollectionHolder>(s));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(roundTripped.Observable, Is.InstanceOf<ObservableCollection<string>>());
                Assert.That(roundTripped.Collection, Is.InstanceOf<Collection<int>>());
                Assert.That(roundTripped.Nested, Is.EqualTo(value.Nested));
            }
        }

        [Test]
        public void CollectionRoots()
        {
            List<Depot> list = [new Depot { Manifest = "a", Size = 1 }, new Depot { Manifest = "b", Size = 2 }];
            int[] array = [3, 1, 2];
            ObservableCollection<string> observable = ["o"];

            AssertSameAsReflection(list, (s, v) => serializer.Serialize(s, v, "root"), s => serializer.Deserialize<List<Depot>>(s));
            AssertSameAsReflection(array, (s, v) => serializer.Serialize(s, v, "root"), s => serializer.Deserialize<int[]>(s));
            AssertSameAsReflection(observable, (s, v) => serializer.Serialize(s, v, "root"), s => serializer.Deserialize<ObservableCollection<string>>(s));
        }

        [Test]
        public void SerializeOnlyCollections()
        {
            var value = new SerializeOnly
            {
                Set = ["a"],
                Sorted = new SortedDictionary<string, int> { ["b"] = 2, ["a"] = 1 },
            };

            var generated = Write(s => serializer.Serialize(s, value, "root"));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(generated, Is.EqualTo(Reflection.Serialize(serializer, value)));
                AssertSameException(() => Read(generated, s => serializer.Deserialize<SerializeOnly>(s)), () => Reflection.Deserialize<SerializeOnly>(serializer, generated));
            }
        }

        [Test]
        public void Lookup()
        {
            var text = format == KVSerializationFormat.KeyValues1Text
                ? "\"root\"\n{\n\t\"Values\"\n\t{\n\t\t\"a\"\t\"1\"\n\t\t\"b\"\t\"2\"\n\t\t\"a\"\t\"3\"\n\t}\n}"
                : "<!-- kv3 encoding:text:version{e21c7f3c-8a33-41c5-9977-a76d3a32aa0d} format:generic:version{7412167c-06e9-4698-aff2-e63eb59037e7} -->\n{\n\tValues = { a = 1 b = 2 }\n}";

            var generated = Read(text, s => serializer.Deserialize<LookupHolder>(s));
            var reflection = Reflection.Deserialize<LookupHolder>(serializer, text);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(generated.Values!.Select(g => (g.Key, g.ToArray())), Is.EqualTo(reflection.Values!.Select(g => (g.Key, g.ToArray()))));
                Assert.That(Write(s => serializer.Serialize(s, generated, "root")), Is.EqualTo(Reflection.Serialize(serializer, reflection)));
            }
        }

        [Test]
        public void ValueTuples()
        {
            var pair = (1, "two");
            var long9 = (1, "b", 3.5, 4, 5, 6, 7, "h", true);

            AssertSameAsReflection(pair, (s, v) => serializer.Serialize(s, v, "root"), s => serializer.Deserialize<(int, string)>(s));
            AssertSameAsReflection(long9, (s, v) => serializer.Serialize(s, v, "root"), s => serializer.Deserialize<(int, string, double, int, int, int, int, string, bool)>(s));

            Assert.That(Read(Write(s => serializer.Serialize(s, long9, "root")), s => serializer.Deserialize<(int, string, double, int, int, int, int, string, bool)>(s)), Is.EqualTo(long9));
        }

        [Test]
        public void RecordsAndStructs()
        {
            var point = new Point3(1, -2, 3);
            var dimensions = new Dimensions(640, 480);
            var counter = Counter.Create(5, 6);
            var pair = new Pair<string>("a", "b");

            AssertSameAsReflection(point, (s, v) => serializer.Serialize(s, v, "root"), s => serializer.Deserialize<Point3>(s));
            AssertSameAsReflection(dimensions, (s, v) => serializer.Serialize(s, v, "root"), s => serializer.Deserialize<Dimensions>(s));
            AssertSameAsReflection(counter, (s, v) => serializer.Serialize(s, v, "root"), s => serializer.Deserialize<Counter>(s));
            AssertSameAsReflection(pair, (s, v) => serializer.Serialize(s, v, "root"), s => serializer.Deserialize<Pair<string>>(s));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(Read(Write(s => serializer.Serialize(s, point, "root")), s => serializer.Deserialize<Point3>(s)), Is.EqualTo(point));
                Assert.That(Read(Write(s => serializer.Serialize(s, dimensions, "root")), s => serializer.Deserialize<Dimensions>(s)), Is.EqualTo(dimensions));
                Assert.That(Read(Write(s => serializer.Serialize(s, counter, "root")), s => serializer.Deserialize<Counter>(s)), Is.EqualTo(counter));
                Assert.That(Read(Write(s => serializer.Serialize(s, pair, "root")), s => serializer.Deserialize<Pair<string>>(s)), Is.EqualTo(pair));
            }
        }

        [Test]
        public void MissingParametersUseDeclaredDefaults()
        {
            var text = Write(s => serializer.Serialize(s, new Dictionary<string, int> { ["x"] = 4 }, "root"));

            var generated = Read(text, s => serializer.Deserialize<Point3>(s));
            var reflection = Reflection.Deserialize<Point3>(serializer, text);
            var defaults = Read(text, s => serializer.Deserialize<WithDefaults>(s));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(generated, Is.EqualTo(new Point3(4, 0, 7)));
                Assert.That(generated, Is.EqualTo(reflection));
                Assert.That(defaults.Snapshot(), Is.EqualTo(Reflection.Deserialize<WithDefaults>(serializer, text).Snapshot()));
                Assert.That(defaults.Snapshot(), Is.EqualTo((4, "text", Rank.High, -1.5, (long?)9, (string?)null, 'c', 2.5m)));
            }
        }

        [Test]
        public void RequiredAndInitMembers()
        {
            var value = new Account { Id = "a1", Owner = "o", Balance = 10 };

            AssertSameAsReflection(value, (s, v) => serializer.Serialize(s, v, "root"), s => serializer.Deserialize<Account>(s));

            var missing = Write(s => serializer.Serialize(s, new Dictionary<string, string> { ["Owner"] = "o" }, "root"));

            AssertSameException(() => Read(missing, s => serializer.Deserialize<Account>(s)), () => Reflection.Deserialize<Account>(serializer, missing));
        }

        [Test]
        public void AttributesAndNonPublicMembers()
        {
            var value = new Attributed { Name = "n", Ignored = "i" };
            value.Assign("private set", "secret");

            AssertSameAsReflection(value, (s, v) => serializer.Serialize(s, v, "root"), s => serializer.Deserialize<Attributed>(s));

            var back = Read(Write(s => serializer.Serialize(s, value, "root")), s => serializer.Deserialize<Attributed>(s));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(back.Name, Is.EqualTo("n"));
                Assert.That(back.Ignored, Is.Null);
                Assert.That(back.PrivateSet, Is.EqualTo("private set"));
                Assert.That(back.GetSecret(), Is.EqualTo("secret"));
            }
        }

        [Test]
        public void InheritedMembers()
        {
            var value = new DerivedItem { Label = "derived", Tag = "t" };
            value.AssignBase("base label", "protected");

            AssertSameAsReflection(value, (s, v) => serializer.Serialize(s, v, "root"), s => serializer.Deserialize<DerivedItem>(s));

            var back = Read(Write(s => serializer.Serialize(s, value, "root")), s => serializer.Deserialize<DerivedItem>(s));

            using (Assert.EnterMultipleScope())
            {
                Assert.That(back.Tag, Is.EqualTo("t"));
                Assert.That(back.Label, Is.EqualTo("derived"));
                Assert.That(back.GetProtected(), Is.EqualTo("protected"));
            }
        }

        [Test]
        public void GenericTypeWithNonPublicSetter()
        {
            var value = Box<int>.Create(42, "label");

            AssertSameAsReflection(value, (s, v) => serializer.Serialize(s, v, "root"), s => serializer.Deserialize<Box<int>>(s));

            var back = Read(Write(s => serializer.Serialize(s, value, "root")), s => serializer.Deserialize<Box<int>>(s));

            Assert.That((back.Value, back.Label), Is.EqualTo((42, "label")));
        }

        [Test]
        public void MarkedPrivateConstructor()
        {
            var value = Factory.Make("f", 12);

            AssertSameAsReflection(value, (s, v) => serializer.Serialize(s, v, "root"), s => serializer.Deserialize<Factory>(s));

            var back = Read(Write(s => serializer.Serialize(s, value, "root")), s => serializer.Deserialize<Factory>(s));

            Assert.That((back.Name, back.Count, back.UsedMarkedConstructor), Is.EqualTo(("f", 12, true)));
        }

        [Test]
        public void ConstructorParametersPassedByReadOnlyReference()
        {
            var value = ByReference.Make(5, 6);

            AssertSameAsReflection(value, (s, v) => serializer.Serialize(s, v, "root"), s => serializer.Deserialize<ByReference>(s));

            var back = Read(Write(s => serializer.Serialize(s, value, "root")), s => serializer.Deserialize<ByReference>(s));

            Assert.That((back.Value, back.Limit), Is.EqualTo((5, 6L)));
        }

        [Test]
        public void TypesNestedInGenericTypeWithNonPublicMembers()
        {
            var inner = Outer<int>.Inner.Create(7, "nested");
            var created = Outer<string>.Created.Make("made");

            AssertSameAsReflection(inner, (s, v) => serializer.Serialize(s, v, "root"), s => serializer.Deserialize<Outer<int>.Inner>(s));
            AssertSameAsReflection(created, (s, v) => serializer.Serialize(s, v, "root"), s => serializer.Deserialize<Outer<string>.Created>(s));

            var innerBack = Read(Write(s => serializer.Serialize(s, inner, "root")), s => serializer.Deserialize<Outer<int>.Inner>(s));
            var createdBack = Read(Write(s => serializer.Serialize(s, created, "root")), s => serializer.Deserialize<Outer<string>.Created>(s));

            using (Assert.EnterMultipleScope())
            {
                Assert.That((innerBack.Value, innerBack.Label), Is.EqualTo((7, "nested")));
                Assert.That(createdBack.Value, Is.EqualTo("made"));
                Assert.That(GetOrigin(KVSerializer.GetTypeInfo<Outer<int>.Inner>()), Is.EqualTo(typeof(InterceptionTestCase).Assembly));
                Assert.That(GetOrigin(KVSerializer.GetTypeInfo<Outer<string>.Created>()), Is.EqualTo(typeof(InterceptionTestCase).Assembly));
            }
        }

        [Test]
        public void ConstructorParametersPassedByReference()
        {
            var text = Write(s => serializer.Serialize(s, new Dictionary<string, int> { ["value"] = 4, ["limit"] = 9 }, "root"));

            var generated = Read(text, s => serializer.Deserialize<ByReferenceParameters>(s));
            var reflection = Reflection.Deserialize<ByReferenceParameters>(serializer, text);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(generated.Value, Is.EqualTo(4));
                Assert.That(reflection.Value, Is.EqualTo(4));
            }
        }

        [Test]
        public void TypesThatCannotBeDeserializedThrowLikeReflection()
        {
            var text = Write(s => serializer.Serialize(s, new Dictionary<string, int> { ["a"] = 1 }, "root"));

            using (Assert.EnterMultipleScope())
            {
                AssertSameException(() => Read(text, s => serializer.Deserialize<TwoMarkedConstructors>(s)), () => Reflection.Deserialize<TwoMarkedConstructors>(serializer, text));
                AssertSameException(() => Read(text, s => serializer.Deserialize<AmbiguousConstructors>(s)), () => Reflection.Deserialize<AmbiguousConstructors>(serializer, text));
                AssertSameException(() => Read(text, s => serializer.Deserialize<SpanConstructor>(s)), () => Reflection.Deserialize<SpanConstructor>(serializer, text));
                AssertSameException(() => Read(text, s => serializer.Deserialize<DuplicateNames>(s)), () => Reflection.Deserialize<DuplicateNames>(serializer, text));
                AssertSameException(() => Read(text, s => serializer.Deserialize<Dictionary<Depot, int>>(s)), () => Reflection.Deserialize<Dictionary<Depot, int>>(serializer, text));

                Assert.That(Write(s => serializer.Serialize(s, new TwoMarkedConstructors(1), "root")), Is.EqualTo(Reflection.Serialize(serializer, new TwoMarkedConstructors(1))));
                Assert.That(Write(s => serializer.Serialize(s, new AmbiguousConstructors(1), "root")), Is.EqualTo(Reflection.Serialize(serializer, new AmbiguousConstructors(1))));
                Assert.That(Write(s => serializer.Serialize(s, new SpanConstructor("ab"), "root")), Is.EqualTo(Reflection.Serialize(serializer, new SpanConstructor("ab"))));
                Assert.That(Write(s => serializer.Serialize(s, new DuplicateNames { A = 1, Other = 2 }, "root")), Is.EqualTo(Reflection.Serialize(serializer, new DuplicateNames { A = 1, Other = 2 })));
                Assert.That(GetOrigin(KVSerializer.GetTypeInfo<Dictionary<Depot, int>>()), Is.EqualTo(typeof(InterceptionTestCase).Assembly));
            }
        }

        [Test]
        public void RecursiveType()
        {
            var value = new TreeNode
            {
                Name = "root",
                Children =
                [
                    new TreeNode { Name = "a", Children = [new TreeNode { Name = "a1" }] },
                    new TreeNode { Name = "b" },
                ],
            };

            AssertSameAsReflection(value, (s, v) => serializer.Serialize(s, v, "root"), s => serializer.Deserialize<TreeNode>(s));
        }

        [Test]
        public void SetterLogicRuns()
        {
            var text = Write(s => serializer.Serialize(s, new Dictionary<string, int> { ["Percent"] = 250 }, "root"));

            var generated = Read(text, s => serializer.Deserialize<Clamped>(s));

            Assert.That(generated.Percent, Is.EqualTo(100));
            Assert.That(generated.Percent, Is.EqualTo(Reflection.Deserialize<Clamped>(serializer, text).Percent));
        }

        [Test]
        public void SourceMapMatchesReflection()
        {
            var value = new Point3(1, 2, 3);

            var generated = serializer.SerializeWithSourceMap(value, "root");
            var reflection = Reflection.SerializeWithSourceMap(serializer, value);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(generated.Text, Is.EqualTo(reflection.Text));
                Assert.That(generated.Spans, Is.EqualTo(reflection.Spans));
            }
        }

        void AssertSameAsReflection<T>(T value, Action<Stream, T> serialize, Func<Stream, T> deserialize)
        {
            var generatedText = Write(s => serialize(s, value));
            var reflectionText = Reflection.Serialize(serializer, value);

            Assert.That(generatedText, Is.EqualTo(reflectionText), "serialized text");

            var generated = Read(generatedText, deserialize);
            var reflection = Reflection.Deserialize<T>(serializer, reflectionText);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(Reflection.Serialize(serializer, generated), Is.EqualTo(Reflection.Serialize(serializer, reflection)), "deserialized value");
                Assert.That(Reflection.Serialize(serializer, generated), Is.EqualTo(generatedText), "round trip");
            }
        }

        static void AssertSameException(TestDelegate generated, TestDelegate reflection)
        {
            var generatedException = Assert.Catch(generated);
            var reflectionException = Assert.Catch(reflection);

            Assert.That((generatedException?.GetType(), generatedException?.Message), Is.EqualTo((reflectionException?.GetType(), reflectionException?.Message)));
        }

        static string Write(Action<Stream> write)
        {
            using var ms = new MemoryStream();
            write(ms);
            return Encoding.UTF8.GetString(ms.ToArray());
        }

        static T Read<T>(string text, Func<Stream, T> read)
        {
            using var ms = new MemoryStream(Encoding.UTF8.GetBytes(text));
            return read(ms);
        }

        // The assembly that declares the first delegate stored by the type information, which is generated code for
        // generated type information and the library for reflection-based type information.
        // Returns the assembly that declares the delegates of the type information or of the type information it refers
        // to, preferring an assembly other than the library, which declares the delegates of scalars.
        static Assembly? GetOrigin(KVTypeInfo typeInfo)
        {
            var assemblies = new List<Assembly?>();
            CollectDelegateAssemblies(typeInfo, assemblies, depth: 2);
            return assemblies.FirstOrDefault(a => a != typeof(KVSerializer).Assembly) ?? assemblies.FirstOrDefault();
        }

        static void CollectDelegateAssemblies(KVTypeInfo typeInfo, List<Assembly?> assemblies, int depth)
        {
            foreach (var field in typeInfo.GetType().GetFields(BindingFlags.Instance | BindingFlags.NonPublic))
            {
                switch (field.GetValue(typeInfo))
                {
                    case Delegate value:
                        assemblies.Add(value.Method.DeclaringType?.Assembly);
                        break;
                    case KVTypeInfo nested when depth > 0:
                        CollectDelegateAssemblies(nested, assemblies, depth - 1);
                        break;
                }
            }
        }

        // The type argument of these calls is a type parameter, so they are not intercepted and use reflection.
        static class Reflection
        {
            public static KVTypeInfo<T> GetTypeInfo<T>() => KVSerializer.GetTypeInfo<T>();

            public static string Serialize<T>(KVSerializer serializer, T value) => serializer.Serialize(value);

            public static T Deserialize<T>(KVSerializer serializer, string text) => serializer.Deserialize<T>(text);

            public static (string Text, IReadOnlyList<KvSourceSpan> Spans) SerializeWithSourceMap<T>(KVSerializer serializer, T value)
                => serializer.SerializeWithSourceMap(value, "root");
        }
    }
}
