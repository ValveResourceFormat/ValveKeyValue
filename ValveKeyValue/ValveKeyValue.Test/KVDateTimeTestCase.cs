namespace ValveKeyValue.Test
{
    class KVDateTimeTestCase
    {
        [Test]
        public void InvalidCast()
        {
            var obj = new KVObject("test", "some value that could be a date");
            Assert.That(
                () => obj.Value.ToDateTime(default),
                Throws.InstanceOf<InvalidCastException>());
        }

        [Test]
        public void DeserializeDateTimeNotSupported()
        {
            var obj = new KVObject("test",
            [
                new KVObject("Value", "some value that could be a date")
            ]);
            using var ms = new MemoryStream();
            var serializer = KVSerializer.Create(KVSerializationFormat.KeyValues1Text);

            serializer.Serialize(ms, obj);
            ms.Seek(0, SeekOrigin.Begin);
            
            Assert.That(
                () => serializer.Deserialize<SerializedType>(ms),
                Throws.InstanceOf<NotSupportedException>()
                .With.Message.EqualTo("Converting to DateTime is not supported. (key = Value, type = String)"));
        }

        class SerializedType
        {
            public DateTime Value { get;set; }
        }
    }
}