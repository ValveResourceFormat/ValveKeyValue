namespace ValveKeyValue.Test
{
    [TestFixture(typeof(StreamKVTextReader))]
    [TestFixture(typeof(StringKVTextReader))]
    class ObjectDeserializationRecordTestCase<TReader>
        where TReader : IKVTextReader, new()
    {
        [Test]
        public void IsNotNull()
        {
            Assert.That(person, Is.Not.Null);
        }

        [Test]
        public void FirstName()
        {
            Assert.That(person.FirstName, Is.EqualTo("Bob"));
        }

        [Test]
        public void LastName()
        {
            Assert.That(person.LastName, Is.EqualTo("Builder"));
        }

        [Test]
        public void CanFixIt()
        {
            Assert.That(person.CanFixIt, Is.True);
        }

        Person person;

        [OneTimeSetUp]
        public void SetUp()
        {
            person = new TReader().Read<Person>("Text.object_person.vdf");
        }

        record Person (string FirstName, string LastName, bool CanFixIt);
    }
}
