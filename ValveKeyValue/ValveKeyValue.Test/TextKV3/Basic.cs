using NUnit.Framework;

namespace ValveKeyValue.Test.TextKV3
{
    class BasicTest
    {
        [Test]
        public void DeserializesHeaderAndValue()
        {
            using var stream = TestDataHelper.OpenResource("TextKV3.basic.kv3");
            var data = KVSerializer.Create(KVSerializationFormat.KeyValues3Text).Deserialize(stream);

            Assert.That((string)data["foo"], Is.EqualTo("bar"));
        }

        [Test]
        public void DeserializesFlaggedValues()
        {
            using var stream = TestDataHelper.OpenResource("TextKV3.flagged_value.kv3");
            var data = KVSerializer.Create(KVSerializationFormat.KeyValues3Text).Deserialize(stream);

            Assert.That((string)data["foo"], Is.EqualTo("bar"));
        }

        [Test]
        public void DeserializesMultilineStrings()
        {
            using var stream = TestDataHelper.OpenResource("TextKV3.multiline.kv3");
            var data = KVSerializer.Create(KVSerializationFormat.KeyValues3Text).Deserialize(stream);

            Assert.That((string)data["multiLineStringValue"], Is.EqualTo("First line of a multi-line string literal.\nSecond line of a multi-line string literal."));
        }

        [Test]
        public void DeserializesMultilineStringsCRLF()
        {
            using var stream = TestDataHelper.OpenResource("TextKV3.multiline_crlf.kv3");
            var data = KVSerializer.Create(KVSerializationFormat.KeyValues3Text).Deserialize(stream);

            Assert.That((string)data["multiLineStringValue"], Is.EqualTo("First line of a multi-line string literal.\r\nSecond line of a multi-line string literal."));
        }
    }
}
