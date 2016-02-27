using System;
using NUnit.Framework;

namespace ValveKeyValue.Test
{
    class KVSerialiserNullInputsTestCase
    {
        [Test]
        public void DeserializeWithNullStream()
        {
            Assert.That(
                () => KVSerialiser.Deserialize(null),
                Throws.Exception.TypeOf<ArgumentNullException>()
                .With.Property(nameof(ArgumentNullException.ParamName)).EqualTo("stream"));
        }
    }
}
