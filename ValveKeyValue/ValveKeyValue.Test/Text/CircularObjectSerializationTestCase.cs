using System.IO;
using NUnit.Framework;

namespace ValveKeyValue.Test
{
    class CircularObjectSerializationTestCase
    {
        [Test]
        public void ThrowsException()
        {
            var dataObject1 = new DataObject
            {
                Name = "First"
            };

            var dataObject2 = new DataObject
            {
                Name = "Second",
                Other = dataObject1
            };

            dataObject1.Other = dataObject2;

            Assert.That(dataObject1.Other, Is.SameAs(dataObject2), "Sanity check");
            Assert.That(dataObject2.Other, Is.SameAs(dataObject1), "Sanity check");

            using (var ms = new MemoryStream())
            {
                Assert.That(
                    () => KVSerializer.Serialize(ms, dataObject1, "test data"),
                    Throws.Exception.InstanceOf<KeyValueException>()
                    .With.Message.EqualTo("Serialization failed - circular object reference detected."));
            }
        }

        class DataObject
        {
            public string Name { get; set; }

            public DataObject Other { get; set; }
        }
    }
}