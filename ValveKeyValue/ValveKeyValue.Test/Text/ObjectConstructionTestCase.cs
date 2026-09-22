using System.Diagnostics.CodeAnalysis;

namespace ValveKeyValue.Test
{
    class ObjectConstructionTestCase
    {
        static readonly KVSerializer KV1 = KVSerializer.Create(KVSerializationFormat.KeyValues1Text);

        [Test]
        public void PublicParameterlessConstructorIsPreferredAndInitializersApply()
        {
            var back = KV1.Deserialize<WithBothConstructors>("\"root\"\n{\n\t\"Name\"\t\"n\"\n}");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(back.UsedParameterless, Is.True);
                Assert.That(back.Name, Is.EqualTo("n"));
                Assert.That(back.Initialized, Is.EqualTo("init"));
            }
        }

        [Test]
        public void SingleParameterizedConstructorBindsByNameCaseInsensitively()
        {
            var back = KV1.Deserialize<Widget>("\"root\"\n{\n\t\"NAME\"\t\"n\"\n\t\"size\"\t\"7\"\n\t\"Color\"\t\"red\"\n}");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(back.Name, Is.EqualTo("n"));
                Assert.That(back.Size, Is.EqualTo(7));
                Assert.That(back.Color, Is.EqualTo("red"), "settable properties are assigned after construction");
            }
        }

        [Test]
        public void MissingConstructorParameterReceivesDefaultValue()
        {
            var back = KV1.Deserialize<Widget>("\"root\"\n{\n\t\"Color\"\t\"red\"\n}");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(back.Name, Is.Null);
                Assert.That(back.Size, Is.Zero);
                Assert.That(back.Color, Is.EqualTo("red"));
            }
        }

        [Test]
        public void MissingConstructorParameterUsesDeclaredDefault()
        {
            var back = KV1.Deserialize<WithDefaults>("\"root\"\n{\n\t\"Name\"\t\"n\"\n}");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(back.Name, Is.EqualTo("n"));
                Assert.That(back.Size, Is.EqualTo(5));
                Assert.That(back.Label, Is.EqualTo("unset"));
            }
        }

        [Test]
        public void StructSingleParameterizedConstructorBindsByName()
        {
            var back = KV1.Deserialize<Interval>("\"root\"\n{\n\t\"start\"\t\"1\"\n\t\"END\"\t\"5\"\n}");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(back.Start, Is.EqualTo(1));
                Assert.That(back.End, Is.EqualTo(5));
            }
        }

        [Test]
        public void StructWithoutPublicConstructorUsesDefaultValue()
        {
            var back = KV1.Deserialize<PlainStruct>("\"root\"\n{\n\t\"Value\"\t\"3\"\n}");

            Assert.That(back.Value, Is.EqualTo(3));
        }

        [Test]
        public void PropertyConsumedByConstructorParameterIsNotSetTwice()
        {
            var back = KV1.Deserialize<Counted>("\"root\"\n{\n\t\"Name\"\t\"n\"\n\t\"Other\"\t\"o\"\n}");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(back.Name, Is.EqualTo("n"));
                Assert.That(back.Other, Is.EqualTo("o"));
                Assert.That(back.NameSetCount, Is.EqualTo(1), "the constructor sets the property once and it is not assigned again");
            }
        }

        [Test]
        public void MarkedConstructorIsUsedEvenWhenNotPublic()
        {
            var back = KV1.Deserialize<WithMarkedConstructor>("\"root\"\n{\n\t\"Name\"\t\"n\"\n}");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(back.UsedMarked, Is.True);
                Assert.That(back.Name, Is.EqualTo("n"));
            }
        }

        [Test]
        public void MultipleMarkedConstructorsThrow()
        {
            Assert.That(
                () => KV1.Deserialize<WithTwoMarkedConstructors>("\"root\"\n{\n}"),
                Throws.InstanceOf<KeyValueException>().With.Message.EqualTo("Type 'WithTwoMarkedConstructors' has more than one constructor marked with [KVConstructor]."));
        }

        [Test]
        public void NoUsableConstructorThrows()
        {
            Assert.That(
                () => KV1.Deserialize<WithAmbiguousConstructors>("\"root\"\n{\n}"),
                Throws.InstanceOf<KeyValueException>().With.Message.EqualTo("Type 'WithAmbiguousConstructors' has no usable constructor; add a public parameterless constructor, a single public constructor, or mark one with [KVConstructor]."));
        }

        [Test]
        public void RenamedPropertyNameIsUsedToMatchConstructorParameter()
        {
            var back = KV1.Deserialize<Renamed>("\"root\"\n{\n\t\"display_name\"\t\"d\"\n}");

            Assert.That(back.DisplayName, Is.EqualTo("d"));
        }

        [Test]
        public void MissingRequiredMemberThrows()
        {
            Assert.That(
                () => KV1.Deserialize<WithRequired>("\"root\"\n{\n\t\"Other\"\t\"o\"\n}"),
                Throws.InstanceOf<KeyValueException>().With.Message.EqualTo("Required property 'Name' on type 'WithRequired' was not found in the KeyValues data."));
        }

        [Test]
        public void PresentRequiredMemberIsAssigned()
        {
            var back = KV1.Deserialize<WithRequired>("\"root\"\n{\n\t\"Name\"\t\"n\"\n}");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(back.Name, Is.EqualTo("n"));
                Assert.That(back.Other, Is.Null);
            }
        }

        [Test]
        public void RequiredMemberIsSatisfiedByConstructorParameter()
        {
            var back = KV1.Deserialize<WithRequiredConstructorParameter>("\"root\"\n{\n\t\"Name\"\t\"n\"\n}");

            Assert.That(back.Name, Is.EqualTo("n"));
        }

        [Test]
        public void RequiredMemberBoundToMissingConstructorParameterThrows()
        {
            Assert.That(
                () => KV1.Deserialize<WithRequiredConstructorParameter>("\"root\"\n{\n}"),
                Throws.InstanceOf<KeyValueException>().With.Message.EqualTo("Required property 'Name' on type 'WithRequiredConstructorParameter' was not found in the KeyValues data."));
        }

        [Test]
        public void SetsRequiredMembersConstructorDisablesEnforcement()
        {
            var back = KV1.Deserialize<WithRequiredSetByConstructor>("\"root\"\n{\n}");

            Assert.That(back.Name, Is.EqualTo("default"));
        }

        class WithBothConstructors
        {
            public WithBothConstructors()
            {
                UsedParameterless = true;
            }

            public WithBothConstructors(string name)
            {
                Name = name;
            }

            public string? Name { get; set; }

            public string? Initialized { get; set; } = "init";

            public bool UsedParameterless { get; set; }
        }

        class Widget
        {
            public Widget(string name, int size)
            {
                Name = name;
                Size = size;
            }

            public string? Name { get; }

            public int Size { get; }

            public string? Color { get; set; }
        }

        class WithDefaults
        {
            public WithDefaults(string name, int size = 5, string label = "unset")
            {
                Name = name;
                Size = size;
                Label = label;
            }

            public string Name { get; }

            public int Size { get; }

            public string Label { get; }
        }

        readonly struct Interval
        {
            public Interval(int start, int end)
            {
                Start = start;
                End = end;
            }

            public int Start { get; }

            public int End { get; }
        }

        struct PlainStruct
        {
            public int Value { get; set; }
        }

        class Counted
        {
            string? name;

            public Counted(string name)
            {
                Name = name;
            }

            public string? Name
            {
                get => name;
                set
                {
                    name = value;
                    NameSetCount++;
                }
            }

            public string? Other { get; set; }

            [KVIgnore]
            public int NameSetCount { get; private set; }
        }

        class WithMarkedConstructor
        {
            public WithMarkedConstructor()
            {
            }

            [KVConstructor]
            WithMarkedConstructor(string name)
            {
                Name = name;
                UsedMarked = true;
            }

            public string? Name { get; set; }

            public bool UsedMarked { get; set; }
        }

        class WithTwoMarkedConstructors
        {
            [KVConstructor]
            public WithTwoMarkedConstructors()
            {
            }

            [KVConstructor]
            public WithTwoMarkedConstructors(string name)
            {
                Name = name;
            }

            public string? Name { get; set; }
        }

        class WithAmbiguousConstructors
        {
            public WithAmbiguousConstructors(string name)
            {
                Name = name;
            }

            public WithAmbiguousConstructors(int size)
            {
                Size = size;
            }

            public string? Name { get; set; }

            public int Size { get; set; }
        }

        class Renamed
        {
            public Renamed(string displayName)
            {
                DisplayName = displayName;
            }

            [KVProperty("display_name")]
            public string? DisplayName { get; }
        }

        class WithRequired
        {
            public required string Name { get; set; }

            public string? Other { get; set; }
        }

        class WithRequiredConstructorParameter
        {
            public WithRequiredConstructorParameter(string name)
            {
                Name = name;
            }

            public required string Name { get; init; }
        }

        class WithRequiredSetByConstructor
        {
            [SetsRequiredMembers]
            public WithRequiredSetByConstructor()
            {
                Name = "default";
            }

            public required string Name { get; set; }
        }
    }
}
