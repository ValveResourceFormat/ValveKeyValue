namespace ValveKeyValue.Test.TextKV3
{
    class SerializationTestCase
    {
        [Test]
        public void CreatesTextDocument()
        {
            using var stream = TestDataHelper.OpenResource("TextKV3.types.kv3");
            var expected = TestDataHelper.ReadTextResource("TextKV3.types_serialized.kv3");

            var kv = KVSerializer.Create(KVSerializationFormat.KeyValues3Text);
            var data = kv.Deserialize(stream);

            data.Add(new KVObject("multiLineString", "hello\nworld"));

            string text;
            using (var ms = new MemoryStream())
            {
                kv.Serialize(ms, data);

                ms.Seek(0, SeekOrigin.Begin);
                using var reader = new StreamReader(ms);
                text = reader.ReadToEnd();
            }

            Assert.That(text, Is.EqualTo(expected));
        }

        [Test]
        public void SerializesArray()
        {
            using var stream = TestDataHelper.OpenResource("TextKV3.array.kv3");
            var expected = TestDataHelper.ReadTextResource("TextKV3.array_serialized.kv3");

            var kv = KVSerializer.Create(KVSerializationFormat.KeyValues3Text);
            var data = kv.Deserialize(stream);

            data.Add(new KVObject("test", "success"));

            string text;
            using (var ms = new MemoryStream())
            {
                kv.Serialize(ms, data);

                ms.Seek(0, SeekOrigin.Begin);
                using var reader = new StreamReader(ms);
                text = reader.ReadToEnd();
            }

            Assert.That(text, Is.EqualTo(expected));
        }

        [Test]
        public void SerializesNestedArray()
        {
            var expected = TestDataHelper.ReadTextResource("TextKV3.array_nested.kv3");

            var kv = KVSerializer.Create(KVSerializationFormat.KeyValues3Text);
            var data = kv.Deserialize(expected);

            string text;
            using (var ms = new MemoryStream())
            {
                kv.Serialize(ms, data);

                ms.Seek(0, SeekOrigin.Begin);
                using var reader = new StreamReader(ms);
                text = reader.ReadToEnd();
            }

            Assert.That(text, Is.EqualTo(expected));
        }

        [Test]
        public void SerializesEscapeSequencesRoundTrip()
        {
            using var stream = TestDataHelper.OpenResource("TextKV3.escape_sequences.kv3");
            var kv = KVSerializer.Create(KVSerializationFormat.KeyValues3Text);
            var data = kv.Deserialize(stream);

            var data2 = RoundTrip(kv, data);

            Assert.Multiple(() =>
            {
                Assert.That((string)data2["newline"], Is.EqualTo("hello\nworld"));
                Assert.That((string)data2["tab"], Is.EqualTo("hello\tworld"));
                Assert.That((string)data2["backslash"], Is.EqualTo("hello\\world"));
                Assert.That((string)data2["quote"], Is.EqualTo("hello\"world"));
                Assert.That((string)data2["combined"], Is.EqualTo("line1\nline2\ttab\\slash\"quote"));
            });
        }

        [Test]
        public void SerializesEntityNameFlag()
        {
            using var stream = TestDataHelper.OpenResource("TextKV3.entity_name.kv3");
            var kv = KVSerializer.Create(KVSerializationFormat.KeyValues3Text);
            var data = kv.Deserialize(stream);

            var data2 = RoundTrip(kv, data);

            Assert.Multiple(() =>
            {
                Assert.That(data2["name"].Value.Flag, Is.EqualTo(KVFlag.EntityName));
                Assert.That((string)data2["name"], Is.EqualTo("some_entity"));
            });
        }

        [Test]
        public void SerializesRootValues()
        {
            var kv = KVSerializer.Create(KVSerializationFormat.KeyValues3Text);

            using var nullStream = TestDataHelper.OpenResource("TextKV3.root_null.kv3");
            var nullData = RoundTrip(kv, kv.Deserialize(nullStream));
            Assert.That(nullData.ValueType, Is.EqualTo(KVValueType.Null));

            using var stringStream = TestDataHelper.OpenResource("TextKV3.root_string.kv3");
            var stringData = RoundTrip(kv, kv.Deserialize(stringStream));
            Assert.That((string)stringData, Is.EqualTo("cool 123 string"));

            using var numberStream = TestDataHelper.OpenResource("TextKV3.root_number.kv3");
            var numberData = RoundTrip(kv, kv.Deserialize(numberStream));
            Assert.That((int)numberData, Is.EqualTo(1234567890));

            using var floatStream = TestDataHelper.OpenResource("TextKV3.root_float.kv3");
            var floatData = RoundTrip(kv, kv.Deserialize(floatStream));
            Assert.That((float)floatData, Is.EqualTo(-1337.401f));

            using var arrayStream = TestDataHelper.OpenResource("TextKV3.root_array.kv3");
            var arrayData = RoundTrip(kv, kv.Deserialize(arrayStream));
            Assert.That(arrayData.ValueType, Is.EqualTo(KVValueType.Array));
            Assert.That(arrayData[0].Value.ToString(), Is.EqualTo("a"));
        }

        [Test]
        public void SerializesFlags()
        {
            using var stream = TestDataHelper.OpenResource("TextKV3.flagged_value.kv3");
            var expected = TestDataHelper.ReadTextResource("TextKV3.flagged_value_serialized.kv3");

            var kv = KVSerializer.Create(KVSerializationFormat.KeyValues3Text);
            var data = kv.Deserialize(stream);

            data.Add(new KVObject("test", "success"));

            string text;
            using (var ms = new MemoryStream())
            {
                kv.Serialize(ms, data);

                ms.Seek(0, SeekOrigin.Begin);
                using var reader = new StreamReader(ms);
                text = reader.ReadToEnd();
            }

            Assert.That(text, Is.EqualTo(expected));
        }

        static KVDocument RoundTrip(KVSerializer kv, KVDocument data)
        {
            using var ms = new MemoryStream();
            kv.Serialize(ms, data);
            ms.Seek(0, SeekOrigin.Begin);
            return kv.Deserialize(ms);
        }
    }
}
