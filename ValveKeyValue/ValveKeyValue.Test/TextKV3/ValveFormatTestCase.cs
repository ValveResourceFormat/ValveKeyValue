namespace ValveKeyValue.Test.TextKV3
{
    /// <summary>
    /// Tests for matching Valve's KV3 text serializer output format.
    /// Tests that document unimplemented Valve behavior are marked with TODO comments
    /// next to the expected file name or assertion value.
    /// </summary>
    class ValveFormatTestCase
    {
        [Test]
        public void SerializesEmptyArray()
        {
            using var stream = TestDataHelper.OpenResource("TextKV3.empty_array.kv3");
            // TODO: Valve outputs empty arrays inline as "[  ]"
            var expected = TestDataHelper.ReadTextResource("TextKV3.empty_array_serialized.kv3");

            var kv = KVSerializer.Create(KVSerializationFormat.KeyValues3Text);
            var data = kv.Deserialize(stream);

            Assert.That(SerializeToString(kv, data), Is.EqualTo(expected));
        }

        [Test]
        public void SerializesShortArray()
        {
            using var stream = TestDataHelper.OpenResource("TextKV3.short_array.kv3");
            // TODO: Valve outputs short arrays (<=4 simple elements) inline as "[ 1, 2, 3 ]"
            var expected = TestDataHelper.ReadTextResource("TextKV3.short_array_serialized.kv3");

            var kv = KVSerializer.Create(KVSerializationFormat.KeyValues3Text);
            var data = kv.Deserialize(stream);

            Assert.That(SerializeToString(kv, data), Is.EqualTo(expected));
        }

        [Test]
        public void SerializesFloats()
        {
            using var stream = TestDataHelper.OpenResource("TextKV3.floats.kv3");
            var expected = TestDataHelper.ReadTextResource("TextKV3.floats_serialized.kv3");

            var kv = KVSerializer.Create(KVSerializationFormat.KeyValues3Text);
            var data = kv.Deserialize(stream);

            Assert.That(SerializeToString(kv, data), Is.EqualTo(expected));
        }

        [Test]
        public void SerializesFloatInfAndNan()
        {
            using var stream = TestDataHelper.OpenResource("TextKV3.float_inf_nan.kv3");
            var expected = TestDataHelper.ReadTextResource("TextKV3.float_inf_nan_serialized.kv3");

            var kv = KVSerializer.Create(KVSerializationFormat.KeyValues3Text);
            var data = kv.Deserialize(stream);

            Assert.That((double)data["positiveInf"], Is.EqualTo(double.PositiveInfinity));
            Assert.That((double)data["positiveInf2"], Is.EqualTo(double.PositiveInfinity));
            Assert.That((double)data["negativeInf"], Is.EqualTo(double.NegativeInfinity));
            Assert.That((double)data["nan"], Is.NaN);

            Assert.That(SerializeToString(kv, data), Is.EqualTo(expected));
        }

        [Test]
        public void SerializesFloatInfAndNanFromFloat()
        {
            var expected = TestDataHelper.ReadTextResource("TextKV3.inf_nan_serialized.kv3");
            var kv = KVSerializer.Create(KVSerializationFormat.KeyValues3Text);

            var collection = new KVCollectionValue();
            collection.Add(new KVObject("positiveInf", (KVValue)float.PositiveInfinity));
            collection.Add(new KVObject("negativeInf", (KVValue)float.NegativeInfinity));
            collection.Add(new KVObject("nan", (KVValue)float.NaN));
            var doc = new KVDocument(null, null, collection);

            Assert.That(SerializeToString(kv, doc), Is.EqualTo(expected));
        }

        [Test]
        public void SerializesFloatInfAndNanFromDouble()
        {
            var expected = TestDataHelper.ReadTextResource("TextKV3.inf_nan_serialized.kv3");
            var kv = KVSerializer.Create(KVSerializationFormat.KeyValues3Text);

            var collection = new KVCollectionValue();
            collection.Add(new KVObject("positiveInf", (KVValue)double.PositiveInfinity));
            collection.Add(new KVObject("negativeInf", (KVValue)double.NegativeInfinity));
            collection.Add(new KVObject("nan", (KVValue)double.NaN));
            var doc = new KVDocument(null, null, collection);

            Assert.That(SerializeToString(kv, doc), Is.EqualTo(expected));
        }

        [Test]
        public void SerializesBinaryBlobs()
        {
            using var stream = TestDataHelper.OpenResource("TextKV3.binary_blobs.kv3");
            // TODO: Valve outputs small blobs (<=32 bytes) inline as "#[ XX XX ]"
            var expected = TestDataHelper.ReadTextResource("TextKV3.binary_blobs_serialized.kv3");

            var kv = KVSerializer.Create(KVSerializationFormat.KeyValues3Text);
            var data = kv.Deserialize(stream);

            Assert.That(SerializeToString(kv, data), Is.EqualTo(expected));
        }

        [Test]
        public void SerializesBinaryBlobsRoundTrip()
        {
            using var stream = TestDataHelper.OpenResource("TextKV3.binary_blobs.kv3");
            var kv = KVSerializer.Create(KVSerializationFormat.KeyValues3Text);
            var data = kv.Deserialize(stream);

            var data2 = RoundTrip(kv, data);

            Assert.That(((KVBinaryBlob)data2["emptyBlob"]).Bytes.Length, Is.EqualTo(0));
            Assert.That(((KVBinaryBlob)data2["smallBlob"]).Bytes.ToArray(), Is.EqualTo(new byte[] { 0x11, 0xFF }));
            Assert.That(((KVBinaryBlob)data2["flaggedBlob"]).Bytes.ToArray(), Is.EqualTo(new byte[] { 0xAA, 0xBB, 0xCC }));
            Assert.That(((KVBinaryBlob)data2["blob15"]).Bytes.ToArray(), Is.EqualTo(new byte[] { 0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, 0x99, 0xAA, 0xBB, 0xCC, 0xDD, 0xFF }));

            var blob32 = (KVBinaryBlob)data2["blob32"];
            Assert.That(blob32.Bytes.Length, Is.EqualTo(32));
            Assert.That(blob32.Bytes.Span[0], Is.EqualTo(0x00));
            Assert.That(blob32.Bytes.Span[31], Is.EqualTo(0x1F));

            var blob100 = (KVBinaryBlob)data2["blob100"];
            Assert.That(blob100.Bytes.Length, Is.EqualTo(100));
            Assert.That(blob100.Bytes.Span[0], Is.EqualTo(0x00));
            Assert.That(blob100.Bytes.Span[99], Is.EqualTo(0x63));
        }

        [Test]
        public void SerializesEmptyObject()
        {
            using var stream = TestDataHelper.OpenResource("TextKV3.empty_object.kv3");
            var expected = TestDataHelper.ReadTextResource("TextKV3.empty_object_serialized.kv3");

            var kv = KVSerializer.Create(KVSerializationFormat.KeyValues3Text);
            var data = kv.Deserialize(stream);

            Assert.That(SerializeToString(kv, data), Is.EqualTo(expected));
        }

        [Test]
        public void SerializesNestedObjects()
        {
            using var stream = TestDataHelper.OpenResource("TextKV3.nested_objects.kv3");
            var expected = TestDataHelper.ReadTextResource("TextKV3.nested_objects.kv3");

            var kv = KVSerializer.Create(KVSerializationFormat.KeyValues3Text);
            var data = kv.Deserialize(stream);

            Assert.That(SerializeToString(kv, data), Is.EqualTo(expected));
        }

        [Test]
        public void SerializesMultilineStrings()
        {
            using var stream = TestDataHelper.OpenResource("TextKV3.multiline_serialized.kv3");
            var expected = TestDataHelper.ReadTextResource("TextKV3.multiline_serialized.kv3");

            var kv = KVSerializer.Create(KVSerializationFormat.KeyValues3Text);
            var data = kv.Deserialize(stream);

            Assert.That(SerializeToString(kv, data), Is.EqualTo(expected));
        }

        static string SerializeToString(KVSerializer kv, KVDocument data)
        {
            using var ms = new MemoryStream();
            kv.Serialize(ms, data);
            ms.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(ms);
            return reader.ReadToEnd();
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
