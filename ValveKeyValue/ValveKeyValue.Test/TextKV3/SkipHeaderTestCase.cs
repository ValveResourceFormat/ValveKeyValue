using System.Text;

namespace ValveKeyValue.Test.TextKV3
{
    class SkipHeaderTestCase
    {
        [Test]
        public void DeserializesWithoutHeader()
        {
            var options = new KVSerializerOptions { SkipHeader = true };
            var kv = KVSerializer.Create(KVSerializationFormat.KeyValues3Text);
            var data = kv.Deserialize("{\n\tkey = \"value\"\n}\n", options);

            Assert.That((string)data["key"], Is.EqualTo("value"));
        }

        [Test]
        public void DeserializesWithoutHeaderReturnsEmptyHeader()
        {
            var options = new KVSerializerOptions { SkipHeader = true };
            var kv = KVSerializer.Create(KVSerializationFormat.KeyValues3Text);
            var data = kv.Deserialize("{\n\tkey = \"value\"\n}\n", options);

            Assert.Multiple(() =>
            {
                Assert.That(data.Header, Is.Not.Null);
                Assert.That(data.Header!.Encoding.Name, Is.Null);
                Assert.That(data.Header.Format.Name, Is.Null);
            });
        }

        [Test]
        public void SerializesWithoutHeader()
        {
            var options = new KVSerializerOptions { SkipHeader = true };
            var kv = KVSerializer.Create(KVSerializationFormat.KeyValues3Text);
            var root = KVObject.Collection();
            root.Add("key", "value");
            var doc = new KVDocument(null, null, root);

            string text;
            using (var ms = new MemoryStream())
            {
                kv.Serialize(ms, doc, options);
                ms.Seek(0, SeekOrigin.Begin);
                using var reader = new StreamReader(ms);
                text = reader.ReadToEnd();
            }

            Assert.That(text, Does.Not.StartWith("<!--"));
            Assert.That(text, Is.EqualTo("{\n\tkey = \"value\"\n}\n"));
        }

        [Test]
        public void RoundTripsWithoutHeader()
        {
            var options = new KVSerializerOptions { SkipHeader = true };
            var kv = KVSerializer.Create(KVSerializationFormat.KeyValues3Text);
            var root = KVObject.Collection();
            root.Add("key", "value");
            root.Add("number", 42);
            var doc = new KVDocument(null, null, root);

            using var ms = new MemoryStream();
            kv.Serialize(ms, doc, options);
            ms.Seek(0, SeekOrigin.Begin);
            var data = kv.Deserialize(ms, options);

            Assert.Multiple(() =>
            {
                Assert.That((string)data["key"], Is.EqualTo("value"));
                Assert.That((int)data["number"], Is.EqualTo(42));
            });
        }

        [Test]
        public void DeserializeWithoutHeaderFailsWhenHeaderPresent()
        {
            var options = new KVSerializerOptions { SkipHeader = true };
            var kv = KVSerializer.Create(KVSerializationFormat.KeyValues3Text);
            var input = "<!-- kv3 encoding:text:version{e21c7f3c-8a33-41c5-9977-a76d3a32aa0d} format:generic:version{7412167c-06e9-4698-aff2-e63eb59037e7} -->\n{\n\tkey = \"value\"\n}\n";

            Assert.That(() => kv.Deserialize(input, options), Throws.Exception);
        }

        [Test]
        public void DeserializeWithHeaderFailsWhenHeaderMissing()
        {
            var kv = KVSerializer.Create(KVSerializationFormat.KeyValues3Text);
            var input = "{\n\tkey = \"value\"\n}\n";

            Assert.That(() => kv.Deserialize(input), Throws.Exception.TypeOf<InvalidDataException>());
        }

        [Test]
        public void SerializesWithHeaderByDefault()
        {
            var kv = KVSerializer.Create(KVSerializationFormat.KeyValues3Text);
            var root = KVObject.Collection();
            root.Add("key", "value");
            var doc = new KVDocument(null, null, root);

            string text;
            using (var ms = new MemoryStream())
            {
                kv.Serialize(ms, doc);
                ms.Seek(0, SeekOrigin.Begin);
                using var reader = new StreamReader(ms);
                text = reader.ReadToEnd();
            }

            Assert.That(text, Does.StartWith("<!-- kv3 encoding:"));
        }
    }
}
