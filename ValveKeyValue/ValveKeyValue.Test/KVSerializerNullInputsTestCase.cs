using System;
using NUnit.Framework;

namespace ValveKeyValue.Test
{
    class KVSerializerNullInputsTestCase
    {
        [Test]
        public void DeserializeWithNullStream()
        {
            Assert.That(
                () => KVSerializer.Deserialize(stream: null),
                Throws.Exception.TypeOf<ArgumentNullException>()
                .With.Property(nameof(ArgumentNullException.ParamName)).EqualTo("stream"));
        }

        [Test]
        public void DeserializeWithNullString()
        {
            Assert.That(
                () => KVSerializer.Deserialize(text: null),
                Throws.Exception.TypeOf<ArgumentNullException>()
                .With.Property(nameof(ArgumentNullException.ParamName)).EqualTo("text"));
        }
    }
}
