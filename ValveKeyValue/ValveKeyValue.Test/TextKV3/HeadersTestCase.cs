using System;
using System.IO;
using System.Text;
using NUnit.Framework;

namespace ValveKeyValue.Test.TextKV3
{
    class HeadersTestCase
    {
        [TestCase("<!--")]
        [TestCase("<!-- -->")]
        [TestCase("<!-- kv3 -->")]
        [TestCase("<!-- kv3 encoding:text:")]
        [TestCase("<!-- kv3 encoding:text:version{e21c7f3c-8a33-41c5-9977-a76d3a32aa0d} FORMAT:generic:version{7412167c-06e9-4698-aff2-e63eb59037e7} -->\n{}")]
        [TestCase("<!-- kv3 ENCODING:text:version{e21c7f3c-8a33-41c5-9977-a76d3a32aa0d} format:generic:version{7412167c-06e9-4698-aff2-e63eb59037e7} -->\n{}")]
        [TestCase("<!-- kv3 format:generic:version{7412167c-06e9-4698-aff2-e63eb59037e7} encoding:text:version{e21c7f3c-8a33-41c5-9977-a76d3a32aa0d} -->\n{}")]
        [TestCase("<!-- kv4 encoding:text:version{e21c7f3c-8a33-41c5-9977-a76d3a32aa0d} format:generic:version{7412167c-06e9-4698-aff2-e63eb59037e7} -->\n{}")]
        [TestCase("<!-- kv3 encoding~text:version{e21c7f3c-8a33-41c5-9977-a76d3a32aa0d} format:generic:version{7412167c-06e9-4698-aff2-e63eb59037e7} -->\n{}")]
        [TestCase("<!-- kv3 encoding:text~version{e21c7f3c-8a33-41c5-9977-a76d3a32aa0d} format:generic:version{7412167c-06e9-4698-aff2-e63eb59037e7} -->\n{}")]
        [TestCase("<!-- kv3 encoding:text:version~e21c7f3c-8a33-41c5-9977-a76d3a32aa0d} format:generic:version{7412167c-06e9-4698-aff2-e63eb59037e7} -->\n{}")]
        [TestCase("<!-- kv3 encoding:text:version{e21c7f3c-8a33-41c5-9977-a76d3a32aa0d} format~generic:version{7412167c-06e9-4698-aff2-e63eb59037e7} -->\n{}")]
        [TestCase("<!-- kv3 encoding:text:version{e21c7f3c-8a33-41c5-9977-a76d3a32aa0d} format:generic~version{7412167c-06e9-4698-aff2-e63eb59037e7} -->\n{}")]
        [TestCase("<!-- kv3 encoding:text:version{e21c7f3c-8a33-41c5-9977-a76d3a32aa0d} format:generic:version~7412167c-06e9-4698-aff2-e63eb59037e7} -->\n{}")]
        [TestCase("<!-- kv3 encoding:text:version{e21c7f3c-8a33-41c5-9977-a76d3a32aa0d} format:generic:VERSION{7412167c-06e9-4698-aff2-e63eb59037e7} -->\n{}")]
        [TestCase("<!-- kv3 encoding:text:VERSION{e21c7f3c-8a33-41c5-9977-a76d3a32aa0d} format:generic:version{7412167c-06e9-4698-aff2-e63eb59037e7} -->\n{}")]
        [TestCase("<!-- kv3 encoding:text:version{e21c7f3c-8a33-41c5-9977-a76d3a32aa0d} format:generic:version{7412167c-06e9-4698-aff2-e63eb59037e7} extra-data -->\n{}")]
        [TestCase("<!-- kv3 encoding:text:version{e21c7f3c-8a33-41c5-9977-a76d3a32aa0d} format:generic:version{7412167c-06e9-4698-aff2-e63eb59037e7} ->-\n{}")]
        public void InvalidHeadersThrow(string value)
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(value));
            var kv = KVSerializer.Create(KVSerializationFormat.KeyValues3Text);

            Assert.That(() => kv.Deserialize(stream), Throws.Exception.TypeOf<InvalidDataException>().Or.TypeOf<EndOfStreamException>());
        }

        [Test]
        public void IncorrectEncodingTextGuidThrows()
        {
            var value = "<!-- kv3 encoding:text:version{e21c7f3c-8a33-4111-9977-a76d3a32aa0d} format:generic:version{7412167c-06e9-4698-aff2-e63eb59037e7} -->";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(value));
            var kv = KVSerializer.Create(KVSerializationFormat.KeyValues3Text);

            Assert.That(() => kv.Deserialize(stream), Throws.Exception.TypeOf<InvalidDataException>());
        }

        [Test]
        public void IncorrectFormatGenericGuidThrows()
        {
            var value = "<!-- kv3 encoding:text:version{e21c7f3c-8a33-41c5-9977-a76d3a32aa0d} format:generic:version{7412167c-06e9-1337-aff2-e63eb59037e7} -->";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(value));
            var kv = KVSerializer.Create(KVSerializationFormat.KeyValues3Text);

            Assert.That(() => kv.Deserialize(stream), Throws.Exception.TypeOf<InvalidDataException>());
        }

        [TestCase("<!-- kv3 encoding:text:version{abc} format:generic:version{7412167c-06e9-4698-aff2-e63eb59037e7} -->")]
        [TestCase("<!-- kv3 encoding:text:version{e21c7f3c-8a33-41c5-9977-a76d3a32aa0d} format:generic:version{abc} -->")]
        [TestCase("<!-- kv3 encoding:text:version{e21c7f3c-8a33-41c5-9977-a76d3a32aa0d~ format:generic:version{7412167c-06e9-4698-aff2-e63eb59037e7} -->")]
        [TestCase("<!-- kv3 encoding:text:version{e21c7f3c-8a33-41c5-9977-a76d3a32aa0d} format:generic:version{7412167c-06e9-4698-aff2-e63eb59037e7~ -->}")]
        public void InvalidGuidThrows(string value)
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(value));
            var kv = KVSerializer.Create(KVSerializationFormat.KeyValues3Text);

            Assert.That(() => kv.Deserialize(stream), Throws.Exception.TypeOf<FormatException>());
        }

        [TestCase("<!-- kv3 encoding:TEXT:version{e21c7f3c-8a33-41c5-9977-a76d3a32aa0d} format:generic:version{7412167c-06e9-4698-aff2-e63eb59037e7} -->\n{}")]
        [TestCase("<!-- kv3 encoding:text:version{e21c7f3c-8a33-41c5-9977-a76d3a32aa0d} format:GENERIC:version{7412167c-06e9-4698-aff2-e63eb59037e7} -->\n{}")]
        [TestCase("<!--    kv3     encoding:text:version{e21c7f3c-8a33-41c5-9977-a76d3a32aa0d}      format:GENERIC:version{7412167c-06e9-4698-aff2-e63eb59037e7}     -->\n{}")]
        [TestCase("<!--kv3 encoding:text:version{e21c7f3c-8a33-41c5-9977-a76d3a32aa0d} format:GENERIC:version{7412167c-06e9-4698-aff2-e63eb59037e7}-->\n{}")]
        [TestCase("<!--\tkv3\tencoding:text:version{e21c7f3c-8a33-41c5-9977-a76d3a32aa0d}\tformat:GENERIC:version{7412167c-06e9-4698-aff2-e63eb59037e7}\t-->\n{}")]
        [TestCase("<!--\nkv3\nencoding:text:version{e21c7f3c-8a33-41c5-9977-a76d3a32aa0d}\nformat:GENERIC:version{7412167c-06e9-4698-aff2-e63eb59037e7}\n-->\n{}")]
        public void ValidHeadersAreParsed(string value)
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(value));
            var kv = KVSerializer.Create(KVSerializationFormat.KeyValues3Text);

            Assert.Pass();
        }
    }
}
