namespace ValveKeyValue.Test.TextKV3
{
    class RootTypesTestCase
    {
        [Test]
        public void DeserializesRootNull()
        {
            using var stream = TestDataHelper.OpenResource("TextKV3.root_null.kv3");
            var data = KVSerializer.Create(KVSerializationFormat.KeyValues3Text).Deserialize(stream);

            Assert.Multiple(() =>
            {
                Assert.That(data.Name, Is.EqualTo("root"));
                Assert.That(data.Value.ValueType, Is.EqualTo(KVValueType.Null));
                Assert.That((string)data.Value, Is.EqualTo("")); // TODO: This should be a null value
            });
        }

        [Test]
        public void DeserializesRootString()
        {
            using var stream = TestDataHelper.OpenResource("TextKV3.root_string.kv3");
            var data = KVSerializer.Create(KVSerializationFormat.KeyValues3Text).Deserialize(stream);

            Assert.Multiple(() =>
            {
                Assert.That(data.Name, Is.EqualTo("root"));
                Assert.That(data.Value.ValueType, Is.EqualTo(KVValueType.String));
                Assert.That((string)data.Value, Is.EqualTo("cool 123 string"));
            });
        }

        [Test]
        public void DeserializesRootMultilineString()
        {
            using var stream = TestDataHelper.OpenResource("TextKV3.root_multiline.kv3");
            var data = KVSerializer.Create(KVSerializationFormat.KeyValues3Text).Deserialize(stream);

            Assert.Multiple(() =>
            {
                Assert.That(data.Name, Is.EqualTo("root"));
                Assert.That((string)data.Value, Is.EqualTo("First line of a multi-line string literal.\nSecond line of a multi-line string literal."));
            });
        }

        [Test]
        public void DeserializesRootFlaggedString()
        {
            using var stream = TestDataHelper.OpenResource("TextKV3.root_flagged_string.kv3");
            var data = KVSerializer.Create(KVSerializationFormat.KeyValues3Text).Deserialize(stream);

            Assert.Multiple(() =>
            {
                Assert.That(data.Name, Is.EqualTo("root"));
                Assert.That(data.Value.ValueType, Is.EqualTo(KVValueType.String));
                Assert.That(data.Value.Flag, Is.EqualTo(KVFlag.Resource));
                Assert.That((string)data.Value, Is.EqualTo("cool_resource.txt"));
            });
        }

        [Test]
        public void DeserializesRootFlaggedObject()
        {
            using var stream = TestDataHelper.OpenResource("TextKV3.root_flagged_object.kv3");
            var data = KVSerializer.Create(KVSerializationFormat.KeyValues3Text).Deserialize(stream);

            Assert.Multiple(() =>
            {
                Assert.That(data.Name, Is.EqualTo("root"));
                Assert.That(data.Value.ValueType, Is.EqualTo(KVValueType.Collection));
                Assert.That(data.Value.Flag, Is.EqualTo(KVFlag.Panorama));
                Assert.That(data["foo"].Flag, Is.EqualTo(KVFlag.Resource));
                Assert.That((string)data["foo"], Is.EqualTo("bar"));
            });
        }

        [Test]
        public void DeserializesRootBinaryBlob()
        {
            using var stream = TestDataHelper.OpenResource("TextKV3.root_binary_blob.kv3");
            var data = KVSerializer.Create(KVSerializationFormat.KeyValues3Text).Deserialize(stream);

            Assert.Multiple(() =>
            {
                Assert.That(data.Name, Is.EqualTo("root"));
                Assert.That(data.Value.ValueType, Is.EqualTo(KVValueType.BinaryBlob));
                Assert.That(((KVBinaryBlob)data.Value).Bytes.ToArray(), Is.EqualTo(new byte[]
                {
                    0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77,
                    0x88, 0x99, 0xAA, 0xBB, 0xCC, 0xDD, 0xFF
                }));
            });
        }

        [Test]
        public void DeserializesRootNumber()
        {
            using var stream = TestDataHelper.OpenResource("TextKV3.root_number.kv3");
            var data = KVSerializer.Create(KVSerializationFormat.KeyValues3Text).Deserialize(stream);

            Assert.Multiple(() =>
            {
                Assert.That(data.Name, Is.EqualTo("root"));
                Assert.That(data.Value.ValueType, Is.EqualTo(KVValueType.UInt64));
                Assert.That((int)data.Value, Is.EqualTo(1234567890));
            });
        }

        [Test]
        public void DeserializesRootNumberNegative()
        {
            using var stream = TestDataHelper.OpenResource("TextKV3.root_number_negative.kv3");
            var data = KVSerializer.Create(KVSerializationFormat.KeyValues3Text).Deserialize(stream);

            Assert.Multiple(() =>
            {
                Assert.That(data.Name, Is.EqualTo("root"));
                Assert.That(data.Value.ValueType, Is.EqualTo(KVValueType.Int64));
                Assert.That((int)data.Value, Is.EqualTo(-1234567890));
            });
        }

        [Test]
        public void DeserializesRootFloat()
        {
            using var stream = TestDataHelper.OpenResource("TextKV3.root_float.kv3");
            var data = KVSerializer.Create(KVSerializationFormat.KeyValues3Text).Deserialize(stream);

            Assert.Multiple(() =>
            {
                Assert.That(data.Name, Is.EqualTo("root"));
                Assert.That(data.Value.ValueType, Is.EqualTo(KVValueType.FloatingPoint));
                Assert.That((float)data.Value, Is.EqualTo(-1337.401f));
            });
        }

        [Test]
        public void DeserializesRootArray()
        {
            using var stream = TestDataHelper.OpenResource("TextKV3.root_array.kv3");
            var data = KVSerializer.Create(KVSerializationFormat.KeyValues3Text).Deserialize(stream);

            Assert.That(data.Value.ValueType, Is.EqualTo(KVValueType.Array));
        }
    }
}
