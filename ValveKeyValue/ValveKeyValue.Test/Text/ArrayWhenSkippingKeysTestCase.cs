using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace ValveKeyValue.Test
{
    // [TestFixtureSource(typeof(TestFixtureSources), nameof(TestFixtureSources.SupportedEnumerableTypesForDeserialization))]
    [TestFixture(typeof(List<string>))]
    [TestFixture(typeof(string[]))]
    [TestFixture(typeof(System.Collections.ObjectModel.Collection<string>))]
    [TestFixture(typeof(IList<string>))]
    [TestFixture(typeof(ICollection<string>))]
    [TestFixture(typeof(System.Collections.ObjectModel.ObservableCollection<string>))]
    class ArrayWhenSkippingKeysTestCase<TEnumerable>
        where TEnumerable : IEnumerable<string>
    {
        [Test]
        public void ThrowsInvalidOperationException()
        {
            using (var stream = TestDataHelper.OpenResource("Text.list_of_values_skipping_keys.vdf"))
            {
                Assert.That(
                     () => KVSerializer.Deserialize<SerializedType>(stream),
                     Throws.Exception.InstanceOf<InvalidOperationException>()
                     .With.Message.EqualTo($"Cannot deserialize a non-array value to type \"{typeof(TEnumerable).Namespace}.{typeof(TEnumerable).Name}\"."));
            }
        }

        class SerializedType
        {
            public TEnumerable Numbers { get; set; }
        }
    }
}
