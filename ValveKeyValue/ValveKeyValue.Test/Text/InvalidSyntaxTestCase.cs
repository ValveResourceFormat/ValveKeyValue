namespace ValveKeyValue.Test
{
    class InvalidSyntaxTestCase
    {
        [TestCase("empty")]
        [TestCase("quoteonly")]
        [TestCase("partialname")]
        [TestCase("nameonly")]
        [TestCase("partial_nodata")]
        [TestCase("partial_opening_key")]
        [TestCase("partial_partialkey")]
        [TestCase("partial_novalue")]
        [TestCase("partial_opening_value")]
        [TestCase("partial_partialvalue")]
        [TestCase("partial_noclose")]
        [TestCase("invalid_zerobracerepeated")]
        public void InvalidTextSyntaxThrowsKeyValueException(string resourceName)
        {
            using var stream = TestDataHelper.OpenResource("Text." + resourceName + ".vdf");
            Assert.That(
                () => KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(stream),
                Throws.Exception.TypeOf<KeyValueException>());
        }
    }
}
