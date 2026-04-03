namespace ValveKeyValue.Test
{
    class KVDateTimeTestCase
    {
        [Test]
        public void InvalidCast()
        {
            var obj = new KVObject("some value that could be a date");
            Assert.That(
                () => ((IConvertible)obj).ToDateTime(default),
                Throws.InstanceOf<NotSupportedException>());
        }

        [Test]
        public void DeserializeDateTimeNotSupported()
        {
            var obj = KVObject.ListCollection();
            obj.Add("Value", "some value that could be a date");
            using var ms = new MemoryStream();
            var serializer = KVSerializer.Create(KVSerializationFormat.KeyValues1Text);

            serializer.Serialize(ms, new KVDocument(null!, "test", obj));
            ms.Seek(0, SeekOrigin.Begin);

            Assert.That(
                () => serializer.Deserialize<SerializedType>(ms),
                Throws.InstanceOf<NotSupportedException>()
                .With.Message.EqualTo("Converting to DateTime is not supported. (type = String)"));
        }

        class SerializedType
        {
            public DateTime Value { get; set; }
        }
    }
}
