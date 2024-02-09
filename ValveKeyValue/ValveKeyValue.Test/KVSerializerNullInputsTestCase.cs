using System.Collections;

namespace ValveKeyValue.Test
{
    class KVSerializerNullInputsTestCase
    {
        [TestCaseSource(nameof(Formats))]
        public void DeserializeWithNullStream(KVSerializationFormat format)
        {
            Assert.That(
                () => KVSerializer.Create(format).Deserialize(stream: null),
                Throws.Exception.TypeOf<ArgumentNullException>()
                .With.Property(nameof(ArgumentNullException.ParamName)).EqualTo("stream"));
        }

        public static IEnumerable Formats => Enum.GetValues(typeof(KVSerializationFormat));
    }
}
