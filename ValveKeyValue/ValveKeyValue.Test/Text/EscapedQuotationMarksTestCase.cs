namespace ValveKeyValue.Test
{
    class EscapedQuotationMarksTestCase
    {
        [Test]
        public void QuotedKeyReturnsQuotedValue()
        {
            Assert.That((string)data["name \"of\" key"], Is.EqualTo("value \"of\" key"));
        }

        KVObject data;

        [OneTimeSetUp]
        public void SetUp()
        {
            using var stream = TestDataHelper.OpenResource("Text.escaped_quotation_marks.vdf");
            var options = new KVSerializerOptions { HasEscapeSequences = true };
            data = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(stream, options);
        }
    }
}
