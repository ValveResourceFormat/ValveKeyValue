using System.Collections;

namespace ValveKeyValue.Test
{
    class KVSerializerNullInputsTestCase
    {
        [TestCaseSource(nameof(Formats))]
        public void DeserializeWithNullStream(KVSerializationFormat format)
        {
            Assert.That(
                () => KVSerializer.Create(format).Deserialize(stream: null!),
                Throws.Exception.TypeOf<ArgumentNullException>()
                .With.Property(nameof(ArgumentNullException.ParamName)).EqualTo("stream"));
        }

        [TestCaseSource(nameof(Formats))]
        public void SerializeWithNullStream(KVSerializationFormat format)
        {
            Assert.That(
                () => KVSerializer.Create(format).Serialize(stream: null!, data: new KVObject(), "test"),
                Throws.Exception.TypeOf<ArgumentNullException>()
                .With.Property(nameof(ArgumentNullException.ParamName)).EqualTo("stream"));
        }

        [TestCaseSource(nameof(Formats))]
        public void SerializeDocumentWithNullStream(KVSerializationFormat format)
        {
            var doc = new KVDocument(new KVHeader(), "test", new KVObject());
            Assert.That(
                () => KVSerializer.Create(format).Serialize(stream: null!, data: doc),
                Throws.Exception.TypeOf<ArgumentNullException>()
                .With.Property(nameof(ArgumentNullException.ParamName)).EqualTo("stream"));
        }

        public static IEnumerable Formats => Enum.GetValues<KVSerializationFormat>();
    }
}
