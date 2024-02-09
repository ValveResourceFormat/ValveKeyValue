namespace ValveKeyValue.Test
{
    class EscapedBackslashNoEscapeTestCase
    {
        [Test]
        public void ParsesStringWithBackwardsSlashAtTheEnd()
        {
            Assert.That((string)data["BuildOutput"], Is.EqualTo(@"..\output\"));
        }

        KVObject data;

        [OneTimeSetUp]
        public void SetUp()
        {
            var options = new KVSerializerOptions { HasEscapeSequences = false };
            using var stream = TestDataHelper.OpenResource("Text.escaped_backslash_single_slash.vdf");
            data = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(stream, options);
        }
    }
}
