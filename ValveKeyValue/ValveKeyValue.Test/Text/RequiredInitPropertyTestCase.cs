namespace ValveKeyValue.Test
{
    class RequiredInitPropertyTestCase
    {
        [Test]
        public void RequiredInitPropertiesAreDeserializedCorrectly()
        {
            using var stream = TestDataHelper.OpenResource("Text.required_init_person.vdf");
            var person = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize<PersonWithRequiredInit>(stream);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(person.FirstName, Is.EqualTo("Alice"));
                Assert.That(person.LastName, Is.EqualTo("Smith"));
                Assert.That(person.Age, Is.EqualTo(30));
            }
        }

        [Test]
        public void GetOnlyPropertiesAreSkipped()
        {
            using var stream = TestDataHelper.OpenResource("Text.required_init_person.vdf");
            var person = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize<PersonWithGetOnly>(stream);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(person.FirstName, Is.EqualTo(string.Empty), "the property initializer runs and the data is not assigned");
                Assert.That(person.LastName, Is.EqualTo(string.Empty));
                Assert.That(person.Age, Is.Zero);
            }
        }

        [Test]
        public void MissingRequiredInitStringPropertyThrows()
        {
            // VDF has FirstName/LastName/Age but not City
            using var stream = TestDataHelper.OpenResource("Text.required_init_person.vdf");

            Assert.That(
                () => KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize<PersonWithExtraRequiredInit>(stream),
                Throws.InstanceOf<KeyValueException>().With.Message.EqualTo("Required property 'City' on type 'PersonWithExtraRequiredInit' was not found in the KeyValues data."));
        }

        [Test]
        public void MissingRequiredInitIntPropertyThrows()
        {
            // City is present but ZipCode is not
            var text = "\"object\"\n{\n\t\"FirstName\"\t\"Alice\"\n\t\"LastName\"\t\"Smith\"\n\t\"Age\"\t\"30\"\n\t\"City\"\t\"Springfield\"\n}";

            Assert.That(
                () => KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize<PersonWithExtraRequiredInit>(text),
                Throws.InstanceOf<KeyValueException>().With.Message.EqualTo("Required property 'ZipCode' on type 'PersonWithExtraRequiredInit' was not found in the KeyValues data."));
        }

        internal class PersonWithRequiredInit
        {
            public required string FirstName { get; init; }

            public required string LastName { get; init; }

            public required int Age { get; init; }
        }

        internal class PersonWithGetOnly
        {
            public string FirstName { get; } = string.Empty;

            public string LastName { get; } = string.Empty;

            public int Age { get; }
        }

        internal class PersonWithExtraRequiredInit
        {
            public required string FirstName { get; init; }

            public required string LastName { get; init; }

            public required int Age { get; init; }

            public required string City { get; init; }

            public required int ZipCode { get; init; }
        }
    }
}
