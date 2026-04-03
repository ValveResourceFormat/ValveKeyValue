namespace ValveKeyValue.Test
{
    class RequiredInitPropertyTestCase
    {
        [Test]
        public void RequiredInitPropertiesAreDeserializedCorrectly()
        {
            using var stream = TestDataHelper.OpenResource("Text.required_init_person.vdf");
            var person = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize<PersonWithRequiredInit>(stream);

            Assert.That(person.FirstName, Is.EqualTo("Alice"));
            Assert.That(person.LastName, Is.EqualTo("Smith"));
            Assert.That(person.Age, Is.EqualTo(30));
        }

        [Test]
        public void GetOnlyPropertyThrows()
        {
            using var stream = TestDataHelper.OpenResource("Text.required_init_person.vdf");
            var serializer = KVSerializer.Create(KVSerializationFormat.KeyValues1Text);

            Assert.That(
                () => serializer.Deserialize<PersonWithGetOnly>(stream),
                Throws.ArgumentException.With.Message.EqualTo("Property set method not found."));
        }

        class PersonWithRequiredInit
        {
            public required string FirstName { get; init; }

            public required string LastName { get; init; }

            public required int Age { get; init; }
        }

        class PersonWithGetOnly
        {
            public string FirstName { get; } = string.Empty;

            public string LastName { get; } = string.Empty;

            public int Age { get; }
        }
    }
}
