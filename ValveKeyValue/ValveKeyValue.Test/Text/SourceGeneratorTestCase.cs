using System.Text.Json.Serialization;

namespace ValveKeyValue.Test
{
    partial class SourceGeneratorTestCase
    {
        static readonly KVSerializer Serializer = KVSerializer.Create(KVSerializationFormat.KeyValues1Text);

        #region Test types

        class PersonData
        {
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public bool CanFixIt { get; set; }
        }

        class PersonWithAttributes
        {
            [KVProperty("first_name")]
            public string FirstName { get; set; }

            [KVProperty("last_name")]
            public string LastName { get; set; }

            [KVIgnore]
            public string Secret { get; set; }
        }

        class NestedData
        {
            public string Name { get; set; }
            public InnerData Inner { get; set; }
        }

        class InnerData
        {
            public int Value { get; set; }
        }

        // A type NOT registered in the source-gen context, to test fallback
        class UnregisteredType
        {
            public string Foo { get; set; }
        }

        #endregion

        #region Source-gen context

        [JsonSerializable(typeof(PersonData))]
        [JsonSerializable(typeof(PersonWithAttributes))]
        [JsonSerializable(typeof(NestedData))]
        [JsonSerializable(typeof(InnerData))]
        [JsonSerializable(typeof(Dictionary<string, string>))]
        [JsonSerializable(typeof(Dictionary<string, int>))]
        [JsonSerializable(typeof(List<string>))]
        [JsonSerializable(typeof(string[]))]
        partial class TestJsonContext : JsonSerializerContext;

        #endregion

        #region Deserialization

        [Test]
        public void BasicDeserialization()
        {
            var result = Serializer.Deserialize<PersonData>(
                "\"obj\" { \"FirstName\" \"Bob\" \"LastName\" \"Builder\" \"CanFixIt\" \"1\" }",
                TestJsonContext.Default);

            Assert.That(result.FirstName, Is.EqualTo("Bob"));
            Assert.That(result.LastName, Is.EqualTo("Builder"));
            Assert.That(result.CanFixIt, Is.True);
        }

        [Test]
        public void NestedObjectDeserialization()
        {
            var result = Serializer.Deserialize<NestedData>(
                "\"obj\" { \"Name\" \"parent\" \"Inner\" { \"Value\" \"42\" } }",
                TestJsonContext.Default);

            Assert.That(result.Name, Is.EqualTo("parent"));
            Assert.That(result.Inner, Is.Not.Null);
            Assert.That(result.Inner.Value, Is.EqualTo(42));
        }

        [Test]
        public void DictionaryDeserialization()
        {
            var result = Serializer.Deserialize<Dictionary<string, string>>(
                "\"obj\" { \"key1\" \"value1\" \"key2\" \"value2\" }",
                TestJsonContext.Default);

            Assert.That(result["key1"], Is.EqualTo("value1"));
            Assert.That(result["key2"], Is.EqualTo("value2"));
        }

        [Test]
        public void DictionaryWithIntValues()
        {
            var result = Serializer.Deserialize<Dictionary<string, int>>(
                "\"obj\" { \"a\" \"1\" \"b\" \"2\" }",
                TestJsonContext.Default);

            Assert.That(result["a"], Is.EqualTo(1));
            Assert.That(result["b"], Is.EqualTo(2));
        }

        [Test]
        public void ListDeserialization()
        {
            var result = Serializer.Deserialize<List<string>>(
                "\"obj\" { \"0\" \"alpha\" \"1\" \"beta\" \"2\" \"gamma\" }",
                TestJsonContext.Default);

            Assert.That(result, Has.Count.EqualTo(3));
            Assert.That(result[0], Is.EqualTo("alpha"));
            Assert.That(result[1], Is.EqualTo("beta"));
            Assert.That(result[2], Is.EqualTo("gamma"));
        }

        [Test]
        public void ArrayDeserialization()
        {
            var result = Serializer.Deserialize<string[]>(
                "\"obj\" { \"0\" \"x\" \"1\" \"y\" }",
                TestJsonContext.Default);

            Assert.That(result, Has.Length.EqualTo(2));
            Assert.That(result[0], Is.EqualTo("x"));
            Assert.That(result[1], Is.EqualTo("y"));
        }

        #endregion

        #region KVProperty and KVIgnore with source-gen

        [Test]
        public void KVPropertyAttributeHonoredWithSourceGen()
        {
            var result = Serializer.Deserialize<PersonWithAttributes>(
                "\"obj\" { \"first_name\" \"Jane\" \"last_name\" \"Doe\" }",
                TestJsonContext.Default);

            Assert.That(result.FirstName, Is.EqualTo("Jane"));
            Assert.That(result.LastName, Is.EqualTo("Doe"));
        }

        [Test]
        public void KVIgnoreAttributeHonoredWithSourceGen()
        {
            var result = Serializer.Deserialize<PersonWithAttributes>(
                "\"obj\" { \"first_name\" \"Jane\" \"last_name\" \"Doe\" \"Secret\" \"hidden\" }",
                TestJsonContext.Default);

            Assert.That(result.FirstName, Is.EqualTo("Jane"));
            Assert.That(result.Secret, Is.Null);
        }

        #endregion

        #region Serialization

        [Test]
        public void BasicSerialization()
        {
            var data = new PersonData
            {
                FirstName = "Bob",
                LastName = "Builder",
                CanFixIt = true,
            };

            string text;
            using (var ms = new MemoryStream())
            {
                Serializer.Serialize(ms, data, "root", TestJsonContext.Default);
                ms.Seek(0, SeekOrigin.Begin);
                using var reader = new StreamReader(ms);
                text = reader.ReadToEnd();
            }

            var deserialized = Serializer.Deserialize<PersonData>(text, TestJsonContext.Default);
            Assert.That(deserialized.FirstName, Is.EqualTo("Bob"));
            Assert.That(deserialized.LastName, Is.EqualTo("Builder"));
            Assert.That(deserialized.CanFixIt, Is.True);
        }

        [Test]
        public void SerializationWithKVPropertyAttribute()
        {
            var data = new PersonWithAttributes
            {
                FirstName = "Jane",
                LastName = "Doe",
                Secret = "should not appear",
            };

            string text;
            using (var ms = new MemoryStream())
            {
                Serializer.Serialize(ms, data, "root", TestJsonContext.Default);
                ms.Seek(0, SeekOrigin.Begin);
                using var reader = new StreamReader(ms);
                text = reader.ReadToEnd();
            }

            Assert.That(text, Does.Contain("first_name"));
            Assert.That(text, Does.Contain("last_name"));
            Assert.That(text, Does.Not.Contain("Secret"));

            var deserialized = Serializer.Deserialize<PersonWithAttributes>(text, TestJsonContext.Default);
            Assert.That(deserialized.FirstName, Is.EqualTo("Jane"));
            Assert.That(deserialized.LastName, Is.EqualTo("Doe"));
        }

        #endregion

        #region Fallback to reflection for unregistered types

        [Test]
        public void FallbackToReflectionForUnregisteredType()
        {
            var result = Serializer.Deserialize<UnregisteredType>(
                "\"obj\" { \"Foo\" \"bar\" }",
                TestJsonContext.Default);

            Assert.That(result.Foo, Is.EqualTo("bar"));
        }

        #endregion
    }
}
