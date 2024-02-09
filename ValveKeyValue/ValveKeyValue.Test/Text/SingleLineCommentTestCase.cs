namespace ValveKeyValue.Test
{
    class SingleLineCommentTestCase
    {
        [TestCase("comment_singleline")]
        [TestCase("comment_singleline_wholeline")]
        [TestCase("comment_singleline_singleslash")]
        [TestCase("comment_singleline_singleslash_wholeline")]
        public void SingleLineComment(string resourceName)
        {
            using var stream = TestDataHelper.OpenResource("Text." + resourceName + ".vdf");
            Assert.That(() => KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(stream), Throws.Nothing);
        }
    }
}
