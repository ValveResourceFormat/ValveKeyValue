namespace ValveKeyValue.Test
{
    [TestFixtureSource(typeof(TestFixtureSources), nameof(TestFixtureSources.SupportedEnumerableTypesForDeserialization))]
    class ArrayWhenSkippingKeysTestCase<TEnumerable>
        where TEnumerable : IEnumerable<string>
    {
        [Test]
        public void ThrowsInvalidOperationException()
        {
            using var stream = TestDataHelper.OpenResource("Text.list_of_values_skipping_keys.vdf");
            Assert.That(
                 () => KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize<SerializedType>(stream),
                 Throws.Exception.InstanceOf<InvalidOperationException>()
                 .With.Message.EqualTo($"Cannot deserialize a non-array value to type \"{typeof(TEnumerable).Namespace}.{typeof(TEnumerable).Name}\"."));
        }

        class SerializedType
        {
            public TEnumerable Numbers { get; set; }
        }
    }
}
