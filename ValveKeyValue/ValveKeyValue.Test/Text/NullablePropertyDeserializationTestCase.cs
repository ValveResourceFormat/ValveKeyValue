namespace ValveKeyValue.Test
{
    class NullablePropertyDeserializationTestCase
    {
        #region Deserialization - present values

        [Test]
        public void NullableStringPropertiesAreDeserialized()
        {
            using var stream = TestDataHelper.OpenResource("Text.nullable_types.vdf");
            var obj = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize<NullableObject>(stream);

            Assert.That(obj.Name, Is.EqualTo("TestName"));
            Assert.That(obj.Description, Is.EqualTo("TestDescription"));
        }

        [Test]
        public void NullableIntPropertyIsDeserializedWhenPresent()
        {
            using var stream = TestDataHelper.OpenResource("Text.nullable_types.vdf");
            var obj = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize<NullableObject>(stream);

            Assert.That(obj.Age, Is.EqualTo(42));
        }

        [Test]
        public void NullableListPropertyIsDeserializedWhenPresent()
        {
            using var stream = TestDataHelper.OpenResource("Text.nullable_types.vdf");
            var obj = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize<NullableObject>(stream);

            Assert.That(obj.Numbers, Is.Not.Null);
            Assert.That(obj.Numbers, Has.Count.EqualTo(3));
            Assert.That(obj.Numbers![0], Is.EqualTo(1));
            Assert.That(obj.Numbers[1], Is.EqualTo(2));
            Assert.That(obj.Numbers[2], Is.EqualTo(3));
        }

        #endregion

        #region Deserialization - missing values remain null

        [Test]
        public void NullableStringPropertyRemainsNullWhenMissing()
        {
            using var stream = TestDataHelper.OpenResource("Text.nullable_types.vdf");
            var obj = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize<NullableObject>(stream);

            Assert.That(obj.MissingProperty, Is.Null);
        }

        [Test]
        public void NullableIntPropertyRemainsNullWhenMissing()
        {
            using var stream = TestDataHelper.OpenResource("Text.nullable_types.vdf");
            var obj = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize<NullableObject>(stream);

            Assert.That(obj.MissingInt, Is.Null);
        }

        [Test]
        public void NullableListPropertyRemainsNullWhenMissing()
        {
            using var stream = TestDataHelper.OpenResource("Text.nullable_types.vdf");
            var obj = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize<NullableObject>(stream);

            Assert.That(obj.MissingList, Is.Null);
        }

        #endregion

        #region Deserialization - required properties missing from data

        [Test]
        public void RequiredStringPropertyIsDefaultWhenMissingFromData()
        {
            // VDF only has Name/Description/Age/Numbers, but RequiredObject expects RequiredName
            var vdf = "\"object\"\n{\n\t\"Name\"\t\"hello\"\n}";
            var obj = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize<RequiredObject>(vdf);

            // required is bypassed by GetUninitializedObject - property stays at default
            Assert.That(obj.RequiredName, Is.Null);
            Assert.That(obj.Name, Is.EqualTo("hello"));
        }

        #endregion

        #region Serialization - nullable properties

        [Test]
        public void SerializesNullableStringProperty()
        {
            var obj = new NullableObject
            {
                Name = "TestName",
                Description = null,
            };

            string text;
            using (var ms = new MemoryStream())
            {
                KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Serialize(ms, obj, "object");
                ms.Seek(0, SeekOrigin.Begin);
                using var reader = new StreamReader(ms);
                text = reader.ReadToEnd();
            }

            // Present nullable string should be serialized
            Assert.That(text, Does.Contain("\"Name\""));
            Assert.That(text, Does.Contain("\"TestName\""));

            // Null string should be omitted
            Assert.That(text, Does.Not.Contain("\"Description\""));
        }

        [Test]
        public void SerializesNullableIntProperty()
        {
            var obj = new NullableObject
            {
                Age = 42,
                MissingInt = null,
            };

            string text;
            using (var ms = new MemoryStream())
            {
                KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Serialize(ms, obj, "object");
                ms.Seek(0, SeekOrigin.Begin);
                using var reader = new StreamReader(ms);
                text = reader.ReadToEnd();
            }

            // Present nullable int should be serialized
            Assert.That(text, Does.Contain("\"Age\""));
            Assert.That(text, Does.Contain("\"42\""));

            // Null int? should be omitted
            Assert.That(text, Does.Not.Contain("\"MissingInt\""));
        }

        [Test]
        public void SerializesNullableListProperty()
        {
            var obj = new NullableObject
            {
                Numbers = [10, 20],
                MissingList = null,
            };

            string text;
            using (var ms = new MemoryStream())
            {
                KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Serialize(ms, obj, "object");
                ms.Seek(0, SeekOrigin.Begin);
                using var reader = new StreamReader(ms);
                text = reader.ReadToEnd();
            }

            Assert.That(text, Does.Contain("\"Numbers\""));
            Assert.That(text, Does.Contain("\"10\""));
            Assert.That(text, Does.Contain("\"20\""));
            Assert.That(text, Does.Not.Contain("\"MissingList\""));
        }

        #endregion

        #region Round-trip

        [Test]
        public void NullablePropertiesRoundTrip()
        {
            var original = new NullableObject
            {
                Name = "RoundTrip",
                Age = 99,
                Numbers = [1, 2, 3],
            };

            var serializer = KVSerializer.Create(KVSerializationFormat.KeyValues1Text);
            using var ms = new MemoryStream();
            serializer.Serialize(ms, original, "object");
            ms.Seek(0, SeekOrigin.Begin);
            var deserialized = serializer.Deserialize<NullableObject>(ms);

            Assert.That(deserialized.Name, Is.EqualTo("RoundTrip"));
            Assert.That(deserialized.Age, Is.EqualTo(99));
            Assert.That(deserialized.Numbers, Has.Count.EqualTo(3));
            Assert.That(deserialized.Description, Is.Null);
            Assert.That(deserialized.MissingInt, Is.Null);
            Assert.That(deserialized.MissingList, Is.Null);
        }

        #endregion

        #region Test classes

        class NullableObject
        {
            public string? Name { get; set; }
            public string? Description { get; set; }
            public int? Age { get; set; }
            public List<int>? Numbers { get; set; }
            public string? MissingProperty { get; set; }
            public int? MissingInt { get; set; }
            public List<string>? MissingList { get; set; }
        }

        class RequiredObject
        {
            public required string RequiredName { get; set; }
            public string? Name { get; set; }
        }

        #endregion
    }
}
