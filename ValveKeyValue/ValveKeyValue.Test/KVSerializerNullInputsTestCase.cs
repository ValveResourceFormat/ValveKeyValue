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
                () => KVSerializer.Deserialize(null),
                Throws.Exception.TypeOf<ArgumentNullException>()
                .With.Property(nameof(ArgumentNullException.ParamName)).EqualTo("stream"));
        }
    }
}
