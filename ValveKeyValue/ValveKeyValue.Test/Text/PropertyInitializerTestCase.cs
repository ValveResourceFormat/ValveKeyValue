namespace ValveKeyValue.Test
{
    class PropertyInitializerTestCase
    {
        static readonly KVSerializer KV1 = KVSerializer.Create(KVSerializationFormat.KeyValues1Text);

        [Test]
        public void MissingKeysKeepInitializerValues()
        {
            var obj = KV1.Deserialize<WithInitializers>("\"root\"\n{\n}");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(obj.Count, Is.EqualTo(7));
                Assert.That(obj.Scale, Is.EqualTo(0.5f));
                Assert.That(obj.Name, Is.EqualTo("default"));
                Assert.That(obj.Empty, Is.Not.Null.And.Empty);
                Assert.That(obj.Items, Is.EqualTo(["a", "b"]));
                Assert.That(obj.Map, Has.Count.EqualTo(1).And.ContainKey("key"));
                Assert.That(obj.Child, Is.Not.Null);
                Assert.That(obj.Child.Level, Is.EqualTo(3));
            }
        }

        [Test]
        public void PresentKeysOverrideInitializerValuesAndUnknownKeysAreIgnored()
        {
            var text = "\"root\"\n{\n\t\"Unknown\"\t\"value\"\n\t\"Count\"\t\"2\"\n\t\"Name\"\t\"other\"\n\t\"Items\"\n\t{\n\t\t\"0\"\t\"c\"\n\t}\n\t\"Map\"\n\t{\n\t\t\"x\"\t\"y\"\n\t}\n}";
            var obj = KV1.Deserialize<WithInitializers>(text);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(obj.Count, Is.EqualTo(2));
                Assert.That(obj.Name, Is.EqualTo("other"));
                Assert.That(obj.Items, Is.EqualTo(["c"]));
                Assert.That(obj.Map, Has.Count.EqualTo(1).And.ContainKey("x"));
            }
        }

        [Test]
        public void EmptyAndZeroValuesOverrideInitializerValues()
        {
            var text = "\"root\"\n{\n\t\"Count\"\t\"0\"\n\t\"Scale\"\t\"0\"\n\t\"Name\"\t\"\"\n\t\"Items\"\n\t{\n\t}\n\t\"Map\"\n\t{\n\t}\n}";
            var obj = KV1.Deserialize<WithInitializers>(text);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(obj.Count, Is.Zero);
                Assert.That(obj.Scale, Is.Zero);
                Assert.That(obj.Name, Is.Empty);
                Assert.That(obj.Items, Is.Empty);
                Assert.That(obj.Map, Is.Empty);
            }
        }

        [Test]
        public void NestedObjectsKeepTheirInitializerValues()
        {
            var text = "\"root\"\n{\n\t\"Child\"\n\t{\n\t\t\"Label\"\t\"set\"\n\t}\n\t\"Children\"\n\t{\n\t\t\"0\"\n\t\t{\n\t\t\t\"Label\"\t\"first\"\n\t\t}\n\t}\n\t\"ChildMap\"\n\t{\n\t\t\"one\"\n\t\t{\n\t\t}\n\t}\n}";
            var obj = KV1.Deserialize<WithInitializers>(text);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(obj.Child.Label, Is.EqualTo("set"));
                Assert.That(obj.Child.Level, Is.EqualTo(3));
                Assert.That(obj.Children, Has.Count.EqualTo(1));
                Assert.That(obj.Children[0].Label, Is.EqualTo("first"));
                Assert.That(obj.Children[0].Level, Is.EqualTo(3));
                Assert.That(obj.ChildMap["one"].Label, Is.EqualTo("child"));
                Assert.That(obj.ChildMap["one"].Level, Is.EqualTo(3));
            }
        }

        [Test]
        public void TypeWithoutParameterlessConstructorIsDeserialized()
        {
            var text = "\"root\"\n{\n\t\"Name\"\t\"value\"\n}";
            var obj = KV1.Deserialize<WithoutParameterlessConstructor>(text);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(obj.Name, Is.EqualTo("value"));
                Assert.That(obj.Count, Is.Zero, "initializers do not run without a parameterless constructor");
            }
        }

        [Test]
        public void StructParameterlessConstructorRuns()
        {
            var obj = KV1.Deserialize<StructWithConstructor>("\"root\"\n{\n\t\"Width\"\t\"2\"\n}");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(obj.Width, Is.EqualTo(2));
                Assert.That(obj.Height, Is.EqualTo(9));
                Assert.That(obj.Depth, Is.EqualTo(4));
            }
        }

        [Test]
        public void StructWithoutConstructorIsDefaultForMissingKeys()
        {
            var obj = KV1.Deserialize<PlainStruct>("\"root\"\n{\n\t\"Width\"\t\"2\"\n}");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(obj.Width, Is.EqualTo(2));
                Assert.That(obj.Height, Is.Zero);
            }
        }

        [Test]
        public void RecordKeepsInitializerValues()
        {
            var obj = KV1.Deserialize<RecordWithInitializers>("\"root\"\n{\n\t\"Name\"\t\"set\"\n}");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(obj.Name, Is.EqualTo("set"));
                Assert.That(obj.Level, Is.EqualTo(3));
            }
        }

        [Test]
        public void PositionalRecordMissingKeyIsDefault()
        {
            var obj = KV1.Deserialize<PositionalRecord>("\"root\"\n{\n\t\"Name\"\t\"set\"\n}");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(obj.Name, Is.EqualTo("set"));
                Assert.That(obj.Level, Is.Zero);
            }
        }

        [Test]
        public void NonPublicParameterlessConstructorIsNotUsed()
        {
            var obj = KV1.Deserialize<WithPrivateConstructor>("\"root\"\n{\n\t\"Name\"\t\"set\"\n}");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(obj.Name, Is.EqualTo("set"));
                Assert.That(obj.Count, Is.Zero);
            }
        }

        [Test]
        public void NestedObjectFromDataReplacesInitializerInstance()
        {
            var obj = KV1.Deserialize<WithCustomizedChild>("\"root\"\n{\n\t\"Child\"\n\t{\n\t\t\"Label\"\t\"set\"\n\t}\n}");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(obj.Child.Label, Is.EqualTo("set"));
                Assert.That(obj.Child.Level, Is.EqualTo(3));
            }
        }

        [Test]
        public void ConstructorExceptionIsNotWrapped()
        {
            using (Assert.EnterMultipleScope())
            {
                Assert.That(
                    () => KV1.Deserialize<WithThrowingConstructor>("\"root\"\n{\n}"),
                    Throws.InstanceOf<InvalidOperationException>());
                Assert.That(
                    () => KV1.Deserialize<WithThrowingChild>("\"root\"\n{\n\t\"Child\"\n\t{\n\t}\n}"),
                    Throws.InstanceOf<InvalidOperationException>());
            }
        }

        class WithThrowingChild
        {
            public WithThrowingConstructor? Child { get; set; }
        }

        class WithCustomizedChild
        {
            public Child Child { get; set; } = new() { Level = 10 };
        }

        class WithThrowingConstructor
        {
            public WithThrowingConstructor()
            {
                throw new InvalidOperationException("constructor failed");
            }
        }

        struct StructWithConstructor
        {
            public StructWithConstructor()
            {
                Height = 9;
            }

            public int Width { get; set; }
            public int Height { get; set; }
            public int Depth { get; set; } = 4;
        }

        struct PlainStruct
        {
            public int Width { get; set; }
            public int Height { get; set; }
        }

        record RecordWithInitializers
        {
            public string Name { get; init; } = "default";
            public int Level { get; init; } = 3;
        }

        record PositionalRecord(string Name, int Level);

        class WithPrivateConstructor
        {
            WithPrivateConstructor()
            {
            }

            public string? Name { get; set; }
            public int Count { get; set; } = 5;
        }

        class WithInitializers
        {
            public int Count { get; set; } = 7;
            public float Scale { get; set; } = 0.5f;
            public string Name { get; set; } = "default";
            public string Empty { get; set; } = string.Empty;
            public List<string> Items { get; set; } = ["a", "b"];
            public Dictionary<string, string> Map { get; set; } = new() { ["key"] = "value" };
            public Child Child { get; set; } = new();
            public List<Child> Children { get; set; } = [];
            public Dictionary<string, Child> ChildMap { get; set; } = [];
        }

        class Child
        {
            public int Level { get; set; } = 3;
            public string Label { get; set; } = "child";
        }

        class WithoutParameterlessConstructor
        {
            public WithoutParameterlessConstructor(string name)
            {
                Name = name;
            }

            public string Name { get; set; }
            public int Count { get; set; } = 5;
        }
    }
}
