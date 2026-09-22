using System.Linq;
using System.Runtime.CompilerServices;
using ValveKeyValue.Metadata;

namespace ValveKeyValue.Test
{
    class ManualTypeInfoTestCase
    {
        static readonly KVSerializer KV1 = KVSerializer.Create(KVSerializationFormat.KeyValues1Text);

        [Test]
        public void ClassRoundTripsLikeReflection()
        {
            var value = new Widget
            {
                Id = "w1",
                Name = "Gadget",
                Count = 42,
                Tint = Tint.Blue,
                Optional = 5,
                Tags = ["a", "b", "c"],
                Scores = new Dictionary<string, int> { ["x"] = 1, ["y"] = 2 },
                Nested = new Child { Label = "inner", Weight = 1.5f },
            };

            var text = KV1.Serialize(value, typeInfo: TypeInfos.Widget);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(text, Is.EqualTo(KV1.Serialize(value)));
                Assert.That(text, Does.Contain("\"display_name\"\t\"Gadget\""));
                Assert.That(text, Does.Contain("\"Evens\""));
            }

            var manual = KV1.Deserialize(text, TypeInfos.Widget);
            var reflection = KV1.Deserialize<Widget>(text);

            AssertEqual(manual, value);
            AssertEqual(reflection, value);
        }

        [Test]
        public void NullMembersAreOmittedLikeReflection()
        {
            var value = new Widget { Id = "w2" };

            var text = KV1.Serialize(value, typeInfo: TypeInfos.Widget);

            Assert.That(text, Is.EqualTo(KV1.Serialize(value)));
            AssertEqual(KV1.Deserialize(text, TypeInfos.Widget), KV1.Deserialize<Widget>(text));
        }

        [Test]
        public void MissingRequiredMemberThrowsLikeReflection()
        {
            var text = "\"root\"\n{\n\t\"display_name\"\t\"n\"\n}";

            var expected = "Required property 'Id' on type 'Widget' was not found in the KeyValues data.";

            using (Assert.EnterMultipleScope())
            {
                Assert.That(() => KV1.Deserialize(text, TypeInfos.Widget), Throws.InstanceOf<KeyValueException>().With.Message.EqualTo(expected));
                Assert.That(() => KV1.Deserialize<Widget>(text), Throws.InstanceOf<KeyValueException>().With.Message.EqualTo(expected));
            }
        }

        [Test]
        public void PositionalRecordRoundTripsLikeReflection()
        {
            var value = new Point3(1, -2, 3);

            var text = KV1.Serialize(value, typeInfo: TypeInfos.Point3);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(text, Is.EqualTo(KV1.Serialize(value)));
                Assert.That(KV1.Deserialize(text, TypeInfos.Point3), Is.EqualTo(value));
                Assert.That(KV1.Deserialize<Point3>(text), Is.EqualTo(value));
            }
        }

        [Test]
        public void MissingParameterUsesDeclaredDefaultLikeReflection()
        {
            var text = "\"root\"\n{\n\t\"x\"\t\"4\"\n}";

            using (Assert.EnterMultipleScope())
            {
                Assert.That(KV1.Deserialize(text, TypeInfos.Point3), Is.EqualTo(new Point3(4, 0, 7)));
                Assert.That(KV1.Deserialize<Point3>(text), Is.EqualTo(new Point3(4, 0, 7)));
            }
        }

        [Test]
        public void StructRoundTripsLikeReflection()
        {
            var value = new Size { Width = 640, Height = 480 };

            var text = KV1.Serialize(value, typeInfo: TypeInfos.Size);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(text, Is.EqualTo(KV1.Serialize(value)));
                Assert.That(KV1.Deserialize(text, TypeInfos.Size), Is.EqualTo(value));
                Assert.That(KV1.Deserialize<Size>(text), Is.EqualTo(value));
            }
        }

        [Test]
        public void RecursiveTypeRoundTripsLikeReflection()
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

            var text = KV1.Serialize(value, typeInfo: TypeInfos.TreeNode);

            Assert.That(text, Is.EqualTo(KV1.Serialize(value)));

            var manual = KV1.Deserialize(text, TypeInfos.TreeNode);
            var reflection = KV1.Deserialize<TreeNode>(text);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(KV1.Serialize(manual, typeInfo: TypeInfos.TreeNode), Is.EqualTo(text));
                Assert.That(KV1.Serialize(reflection, typeInfo: TypeInfos.TreeNode), Is.EqualTo(text));
                Assert.That(manual.Children![0].Children![0].Name, Is.EqualTo("a1"));
                Assert.That(manual.Children[1].Children, Is.Null);
            }
        }

        [Test]
        public void SourceMapSerializationMatchesReflection()
        {
            var value = new Size { Width = 1, Height = 2 };

            var manual = KV1.SerializeWithSourceMap(value, "root", TypeInfos.Size);
            var reflection = KV1.SerializeWithSourceMap(value, "root");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(manual.Text, Is.EqualTo(reflection.Text));
                Assert.That(manual.Spans, Is.EqualTo(reflection.Spans));
            }
        }

        [Test]
        public void WriteOnlyEnumerableCannotBeDeserialized()
        {
            var typeInfo = KVMetadata.Collection<HashSet<string>, string>(KVMetadata.String);
            var text = "\"root\"\n{\n\t\"0\"\t\"x\"\n}";

            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    () => KV1.Deserialize(text, typeInfo),
                    Throws.InstanceOf<NotSupportedException>().With.Message.EqualTo("Cannot deserialize to enumerable type HashSet`1."));
                Assert.That(
                    () => KV1.Deserialize<HashSet<string>>(text),
                    Throws.InstanceOf<NotSupportedException>().With.Message.EqualTo("Cannot deserialize to enumerable type HashSet`1."));
            }
        }

        [Test]
        public void MembersAndParametersAreResolvedOnceOnFirstUse()
        {
            var memberCalls = 0;
            var parameterCalls = 0;

            var typeInfo = KVMetadata.Object<Point3>(
                create: static args => new Point3((int)args[0]!, (int)args[1]!, (int)args[2]!),
                parameters: () =>
                {
                    parameterCalls++;
                    return TypeInfos.Point3Parameters;
                },
                members: () =>
                {
                    memberCalls++;
                    return TypeInfos.Point3Members;
                },
                constructorSetsRequiredMembers: false);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(memberCalls, Is.Zero);
                Assert.That(parameterCalls, Is.Zero);
            }

            KV1.Deserialize("\"root\"\n{\n\t\"X\"\t\"1\"\n}", typeInfo);
            KV1.Deserialize("\"root\"\n{\n\t\"Y\"\t\"2\"\n}", typeInfo);
            KV1.Serialize(new Point3(1, 2, 3), typeInfo: typeInfo);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(memberCalls, Is.EqualTo(1));
                Assert.That(parameterCalls, Is.EqualTo(1));
            }
        }

        [Test]
        public void ParameterlessConstructorReceivesSharedEmptyArguments()
        {
            var arguments = new List<object?[]>();

            var typeInfo = KVMetadata.Object<Child>(
                create: args =>
                {
                    arguments.Add(args);
                    return new Child();
                },
                parameters: null,
                members: static () => [],
                constructorSetsRequiredMembers: false);

            KV1.Deserialize("\"root\"\n{\n}", typeInfo);
            KV1.Deserialize("\"root\"\n{\n}", typeInfo);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(arguments, Has.Count.EqualTo(2));
                Assert.That(arguments[0], Is.Empty);
                Assert.That(arguments[1], Is.SameAs(arguments[0]));
            }
        }

        [Test]
        public void MemberFactoryReturningNullThrows()
        {
            var typeInfo = KVMetadata.Object<Child>(static _ => new Child(), null, static () => null!, constructorSetsRequiredMembers: false);

            Assert.That(
                () => KV1.Serialize(new Child(), typeInfo: typeInfo),
                Throws.InvalidOperationException.With.Message.EqualTo("The member factory for 'Child' returned null."));
        }

        [Test]
        public void DuplicateMemberNamesThrow()
        {
            var typeInfo = KVMetadata.Object<Child>(
                static _ => new Child(),
                null,
                static () =>
                [
                    KVMetadata.Member<Child, string>("Label", "Label", KVMetadata.String, static c => c.Label, static (ref c, v) => c.Label = v, isRequired: false),
                    KVMetadata.Member<Child, float>("label", "Weight", KVMetadata.Single, static c => c.Weight, static (ref c, v) => c.Weight = v, isRequired: false),
                ],
                constructorSetsRequiredMembers: false);

            Assert.That(
                () => KV1.Deserialize("\"root\"\n{\n}", typeInfo),
                Throws.InstanceOf<KeyValueException>().With.Message.EqualTo("Type 'Child' has more than one member named 'label'; member names are compared case-insensitively."));
        }

        [Test]
        public void ObjectWithoutConstructorThrows()
        {
            var typeInfo = KVMetadata.Object<Child>(null, null, static () => [], constructorSetsRequiredMembers: false);

            Assert.That(
                () => KV1.Deserialize("\"root\"\n{\n}", typeInfo),
                Throws.InstanceOf<KeyValueException>().With.Message.EqualTo("Type 'Child' has no usable constructor; add a public parameterless constructor, a single public constructor, or mark one with [KVConstructor]."));
        }

        [Test]
        public void ValueTypeWithoutCreateThrows()
        {
            var typeInfo = KVMetadata.Object<Size>(null, null, static () => [], constructorSetsRequiredMembers: false);

            Assert.That(
                () => KV1.Deserialize("\"root\"\n{\n}", typeInfo),
                Throws.InstanceOf<KeyValueException>().With.Message.EqualTo("Type 'Size' has no usable constructor; add a public parameterless constructor, a single public constructor, or mark one with [KVConstructor]."));
        }

        [Test]
        public void DuplicateKeysAssignTheMemberForEachOccurrence()
        {
            var values = new List<string>();

            var typeInfo = KVMetadata.Object<Child>(
                static _ => new Child(),
                null,
                () => [KVMetadata.Member<Child, string>("Label", "Label", KVMetadata.String, static c => c.Label, (ref c, v) => values.Add(c.Label = v), isRequired: false)],
                constructorSetsRequiredMembers: false);

            var value = KV1.Deserialize("\"root\"\n{\n\t\"Label\"\t\"first\"\n\t\"label\"\t\"second\"\n}", typeInfo);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(value.Label, Is.EqualTo("second"));
                Assert.That(values, Is.EqualTo(["first", "second"]));
            }
        }

        [Test]
        public void DuplicateKeysOfConstructorParameterUseLastOccurrence()
        {
            var value = KV1.Deserialize("\"root\"\n{\n\t\"X\"\t\"1\"\n\t\"x\"\t\"2\"\n}", TypeInfos.Point3);

            Assert.That(value, Is.EqualTo(new Point3(2, 0, 7)));
        }

        [Test]
        public void ObjectRequiresMemberFactory()
        {
            Assert.That(() => KVMetadata.Object<Child>(null, null, null!, constructorSetsRequiredMembers: false), Throws.ArgumentNullException);
        }

        [Test]
        public void MemberAndParameterDescriptionsAreReported()
        {
            var members = TypeInfos.WidgetMembers;
            var parameters = TypeInfos.Point3Parameters;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(members.Select(m => m.Name), Is.EqualTo(["Id", "display_name", "Count", "Tint", "Optional", "Tags", "Scores", "Nested", "Evens"]));
                Assert.That(members[1].DeclaredName, Is.EqualTo("Name"));
                Assert.That(members[0].IsRequired, Is.True);
                Assert.That(members[^1].CanRead, Is.True);
                Assert.That(members[^1].CanWrite, Is.False);
                Assert.That(parameters.Select(p => p.Name), Is.EqualTo(["X", "Y", "Z"]));
                Assert.That(parameters[2].Type.Type, Is.EqualTo(typeof(int)));
            }
        }

        [Test]
        public void EnumRequiresMatchingUnderlyingSize()
        {
            Assert.That(() => KVMetadata.Enum<Tint, int>(KVMetadata.Int32), Throws.ArgumentException);
        }

        static void AssertEqual(Widget actual, Widget expected)
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(actual.Id, Is.EqualTo(expected.Id));
                Assert.That(actual.Name, Is.EqualTo(expected.Name));
                Assert.That(actual.Count, Is.EqualTo(expected.Count));
                Assert.That(actual.Tint, Is.EqualTo(expected.Tint));
                Assert.That(actual.Optional, Is.EqualTo(expected.Optional));
                Assert.That(actual.Tags, Is.EqualTo(expected.Tags));
                Assert.That(actual.Scores, Is.EqualTo(expected.Scores));
                Assert.That(actual.Nested?.Label, Is.EqualTo(expected.Nested?.Label));
                Assert.That(actual.Nested?.Weight, Is.EqualTo(expected.Nested?.Weight));
            }
        }

        // Type information written the way generated code declares it: one lazily created instance per type,
        // with members supplied through a factory so that recursive types can refer to their own instance.
        static class TypeInfos
        {
            static KVTypeInfo<Widget>? widget;
            static KVTypeInfo<Child>? child;
            static KVTypeInfo<Point3>? point3;
            static KVTypeInfo<Size>? size;
            static KVTypeInfo<TreeNode>? treeNode;

            public static KVTypeInfo<Tint> Tint { get; } = KVMetadata.Enum<Tint, byte>(KVMetadata.Byte);

            public static KVTypeInfo<Widget> Widget => widget ??= KVMetadata.Object<Widget>(
                create: static _ => CreateWidget(),
                parameters: null,
                members: static () => WidgetMembers,
                constructorSetsRequiredMembers: false);

            public static KVMemberInfo<Widget>[] WidgetMembers =>
            [
                KVMetadata.Member<Widget, string>("Id", "Id", KVMetadata.String, static w => w.Id, static (ref w, v) => SetId(w, v), isRequired: true),
                KVMetadata.Member<Widget, string>("display_name", "Name", KVMetadata.String, static w => w.Name, static (ref w, v) => w.Name = v, isRequired: false),
                KVMetadata.Member<Widget, int>("Count", "Count", KVMetadata.Int32, static w => w.Count, static (ref w, v) => w.Count = v, isRequired: false),
                KVMetadata.Member<Widget, Tint>("Tint", "Tint", Tint, static w => w.Tint, static (ref w, v) => w.Tint = v, isRequired: false),
                KVMetadata.Member<Widget, int?>("Optional", "Optional", KVMetadata.Nullable(KVMetadata.Int32), static w => w.Optional, static (ref w, v) => w.Optional = v, isRequired: false),
                KVMetadata.Member<Widget, List<string>>("Tags", "Tags", KVMetadata.Collection<List<string>, string>(KVMetadata.String), static w => w.Tags, static (ref w, v) => w.Tags = v, isRequired: false),
                KVMetadata.Member<Widget, Dictionary<string, int>>("Scores", "Scores", KVMetadata.Dictionary<Dictionary<string, int>, string, int>(KVMetadata.String, KVMetadata.Int32), static w => w.Scores, static (ref w, v) => w.Scores = v, isRequired: false),
                KVMetadata.Member<Widget, Child>("Nested", "Nested", Child, static w => w.Nested, static (ref w, v) => w.Nested = v, isRequired: false),
                KVMetadata.Member<Widget, IEnumerable<int>>("Evens", "Evens", KVMetadata.Collection<IEnumerable<int>, int>(KVMetadata.Int32), static w => w.Evens, setter: null, isRequired: false),
            ];

            public static KVTypeInfo<Child> Child => child ??= KVMetadata.Object<Child>(
                create: static _ => new Child(),
                parameters: null,
                members: static () =>
                [
                    KVMetadata.Member<Child, string>("Label", "Label", KVMetadata.String, static c => c.Label, static (ref c, v) => c.Label = v, isRequired: false),
                    KVMetadata.Member<Child, float>("Weight", "Weight", KVMetadata.Single, static c => c.Weight, static (ref c, v) => c.Weight = v, isRequired: false),
                ],
                constructorSetsRequiredMembers: false);

            public static KVTypeInfo<Point3> Point3 => point3 ??= KVMetadata.Object<Point3>(
                create: static args => new Point3((int)args[0]!, (int)args[1]!, (int)args[2]!),
                parameters: static () => Point3Parameters,
                members: static () => Point3Members,
                constructorSetsRequiredMembers: false);

            public static KVParameterInfo[] Point3Parameters =>
            [
                KVMetadata.Parameter<int>("X", KVMetadata.Int32, defaultValue: default),
                KVMetadata.Parameter<int>("Y", KVMetadata.Int32, defaultValue: default),
                KVMetadata.Parameter<int>("Z", KVMetadata.Int32, defaultValue: 7),
            ];

            public static KVMemberInfo<Point3>[] Point3Members =>
            [
                KVMetadata.Member<Point3, int>("X", "X", KVMetadata.Int32, static p => p.X, static (ref p, v) => SetX(p, v), isRequired: false),
                KVMetadata.Member<Point3, int>("Y", "Y", KVMetadata.Int32, static p => p.Y, static (ref p, v) => SetY(p, v), isRequired: false),
                KVMetadata.Member<Point3, int>("Z", "Z", KVMetadata.Int32, static p => p.Z, static (ref p, v) => SetZ(p, v), isRequired: false),
            ];

            public static KVTypeInfo<Size> Size => size ??= KVMetadata.Object<Size>(
                create: static _ => default,
                parameters: null,
                members: static () =>
                [
                    KVMetadata.Member<Size, int>("Width", "Width", KVMetadata.Int32, static s => s.Width, static (ref s, v) => s.Width = v, isRequired: false),
                    KVMetadata.Member<Size, int>("Height", "Height", KVMetadata.Int32, static s => s.Height, static (ref s, v) => s.Height = v, isRequired: false),
                ],
                constructorSetsRequiredMembers: false);

            public static KVTypeInfo<TreeNode> TreeNode => treeNode ??= KVMetadata.Object<TreeNode>(
                create: static _ => new TreeNode(),
                parameters: null,
                members: static () =>
                [
                    KVMetadata.Member<TreeNode, string>("Name", "Name", KVMetadata.String, static n => n.Name, static (ref n, v) => n.Name = v, isRequired: false),
                    KVMetadata.Member<TreeNode, List<TreeNode>>("Children", "Children", KVMetadata.Collection<List<TreeNode>, TreeNode>(TreeNode), static n => n.Children, static (ref n, v) => n.Children = v, isRequired: false),
                ],
                constructorSetsRequiredMembers: false);

            [UnsafeAccessor(UnsafeAccessorKind.Constructor)]
            static extern Widget CreateWidget();

            [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_Id")]
            static extern void SetId(Widget target, string value);

            [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_X")]
            static extern void SetX(Point3 target, int value);

            [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_Y")]
            static extern void SetY(Point3 target, int value);

            [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "set_Z")]
            static extern void SetZ(Point3 target, int value);
        }

        enum Tint : byte
        {
            None = 0,
            Red = 5,
            Blue = 200,
        }

        class Widget
        {
            public required string Id { get; init; }

            [KVProperty("display_name")]
            public string? Name { get; set; }

            public int Count { get; set; }

            public Tint Tint { get; set; }

            public int? Optional { get; set; }

            public List<string>? Tags { get; set; }

            public Dictionary<string, int>? Scores { get; set; }

            public Child? Nested { get; set; }

            public IEnumerable<int> Evens => Enumerable.Range(0, 3).Select(i => i * 2 + Count);
        }

        class Child
        {
            public string? Label { get; set; }

            public float Weight { get; set; }
        }

        record Point3(int X, int Y, int Z = 7);

        struct Size
        {
            public int Width { get; set; }

            public int Height { get; set; }
        }

        class TreeNode
        {
            public string? Name { get; set; }

            public List<TreeNode>? Children { get; set; }
        }
    }
}
