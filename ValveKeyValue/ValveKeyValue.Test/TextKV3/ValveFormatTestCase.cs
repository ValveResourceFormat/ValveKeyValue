using System.Linq;

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
            var expected = TestDataHelper.ReadTextResource("TextKV3.empty_array_serialized.kv3");

            var kv = KVSerializer.Create(KVSerializationFormat.KeyValues3Text);
            var data = kv.Deserialize(stream);

            Assert.That(SerializeToString(kv, data), Is.EqualTo(expected));
        }

        [Test]
        public void SerializesShortArray()
        {
            using var stream = TestDataHelper.OpenResource("TextKV3.short_array.kv3");
            var expected = TestDataHelper.ReadTextResource("TextKV3.short_array_serialized.kv3");

            var kv = KVSerializer.Create(KVSerializationFormat.KeyValues3Text);
            var data = kv.Deserialize(stream);

            Assert.That(SerializeToString(kv, data), Is.EqualTo(expected));
        }

        [Test]
        public void SerializesArrayFormatting()
        {
            using var stream = TestDataHelper.OpenResource("TextKV3.array_formatting.kv3");
            var expected = TestDataHelper.ReadTextResource("TextKV3.array_formatting_serialized.kv3");

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

        [Test]
        public void SerializesSpecialFloatsInArrays()
        {
            var kv = KVSerializer.Create(KVSerializationFormat.KeyValues3Text);

            var shortArray = new KVArrayValue();
            shortArray.Add((KVValue)1.0);
            shortArray.Add((KVValue)double.PositiveInfinity);
            shortArray.Add((KVValue)double.NegativeInfinity);
            shortArray.Add((KVValue)double.NaN);

            var collection = new KVCollectionValue();
            collection.Add(new KVObject("special", (KVValue)shortArray));
            var doc = new KVDocument(null, null, collection);

            var result = SerializeToString(kv, doc);
            Assert.That(result, Does.Contain("special = [ 1.0, inf, -inf, nan ]"));
        }

        [Test]
        public void SerializesBlobInArrayIsNotShort()
        {
            var kv = KVSerializer.Create(KVSerializationFormat.KeyValues3Text);

            var array = new KVArrayValue();
            array.Add(new KVBinaryBlob([0xAA]));
            array.Add(new KVBinaryBlob([0xBB]));
            array.Add(new KVBinaryBlob([0xCC]));

            var collection = new KVCollectionValue();
            collection.Add(new KVObject("blobs", (KVValue)array));
            var doc = new KVDocument(null, null, collection);

            var result = SerializeToString(kv, doc);
            // Blobs are NOT simple, so array must be multiline even with <=4 elements
            Assert.That(result, Does.Contain("\t#[ AA ],"));
            Assert.That(result, Does.Not.Contain("[ #["));
        }

        [Test]
        public void SerializesKeyQuoting()
        {
            var kv = KVSerializer.Create(KVSerializationFormat.KeyValues3Text);

            var collection = new KVCollectionValue();
            collection.Add(new KVObject("simple", "a"));
            collection.Add(new KVObject("with.dot", "b"));
            collection.Add(new KVObject("under_score", "c"));
            collection.Add(new KVObject("has space", "d"));
            collection.Add(new KVObject("has-dash", "e"));
            collection.Add(new KVObject("0startsWithDigit", "f"));
            collection.Add(new KVObject("", "g"));
            collection.Add(new KVObject("has\"quote", "h"));
            collection.Add(new KVObject("has\\backslash", "i"));
            collection.Add(new KVObject("has\nnewline", "j"));
            collection.Add(new KVObject("has\ttab", "k"));
            var doc = new KVDocument(null, null, collection);

            var result = SerializeToString(kv, doc);
            Assert.Multiple(() =>
            {
                Assert.That(result, Does.Contain("\tsimple = \"a\""));
                Assert.That(result, Does.Contain("\twith.dot = \"b\""));
                Assert.That(result, Does.Contain("\tunder_score = \"c\""));
                Assert.That(result, Does.Contain("\t\"has space\" = \"d\""));
                Assert.That(result, Does.Contain("\t\"has-dash\" = \"e\""));
                Assert.That(result, Does.Contain("\t\"0startsWithDigit\" = \"f\""));
                Assert.That(result, Does.Contain("\t\"\" = \"g\""));
                Assert.That(result, Does.Contain("\t\"has\\\"quote\" = \"h\""));
                Assert.That(result, Does.Contain("\t\"has\\\\backslash\" = \"i\""));
                Assert.That(result, Does.Contain("\t\"has\\nnewline\" = \"j\""));
                Assert.That(result, Does.Contain("\t\"has\\ttab\" = \"k\""));
            });
        }

        [Test]
        public void SerializesStringEscaping()
        {
            var kv = KVSerializer.Create(KVSerializationFormat.KeyValues3Text);

            var collection = new KVCollectionValue();
            collection.Add(new KVObject("newline", "hello\nworld"));
            collection.Add(new KVObject("tab", "a\tb"));
            collection.Add(new KVObject("backslash", "a\\b"));
            collection.Add(new KVObject("quote", "a\"b"));
            collection.Add(new KVObject("all", "\n\t\\\""));
            var doc = new KVDocument(null, null, collection);

            var result = SerializeToString(kv, doc);
            Assert.Multiple(() =>
            {
                // Single-line strings with \n get the \n escaped
                // But "hello\nworld" is multiline, so uses """ syntax
                Assert.That(result, Does.Contain("tab = \"a\\tb\""));
                Assert.That(result, Does.Contain("backslash = \"a\\\\b\""));
                Assert.That(result, Does.Contain("quote = \"a\\\"b\""));
            });
        }

        [Test]
        public void SerializesNullArray()
        {
            var kv = KVSerializer.Create(KVSerializationFormat.KeyValues3Text);

            var array = new KVArrayValue();
            array.Add(new KVNullValue());
            array.Add(new KVNullValue());
            array.Add(new KVNullValue());
            array.Add(new KVNullValue());

            var collection = new KVCollectionValue();
            collection.Add(new KVObject("nulls", (KVValue)array));
            var doc = new KVDocument(null, null, collection);

            var result = SerializeToString(kv, doc);
            // Nulls are simple, 4 elements -> short
            Assert.That(result, Does.Contain("nulls = [ null, null, null, null ]"));
        }

        [Test]
        public void ArrayFormattingRoundTrip()
        {
            using var stream = TestDataHelper.OpenResource("TextKV3.array_formatting.kv3");
            var kv = KVSerializer.Create(KVSerializationFormat.KeyValues3Text);
            var data = kv.Deserialize(stream);

            var data2 = RoundTrip(kv, data);

            Assert.Multiple(() =>
            {
                Assert.That(((KVArrayValue)data2["empty"]).Count, Is.EqualTo(0));
                Assert.That((int)((KVArrayValue)data2["one_int"])[0], Is.EqualTo(1));
                Assert.That(((KVArrayValue)data2["four_ints"]).Count, Is.EqualTo(4));
                Assert.That(((KVArrayValue)data2["nine_ints"]).Count, Is.EqualTo(9));
                Assert.That(((KVArrayValue)data2["matrix4x4"]).Count, Is.EqualTo(16));
                Assert.That(((KVArrayValue)data2["vector3"]).Count, Is.EqualTo(3));
                Assert.That((double)((KVArrayValue)data2["vector3"])[2], Is.EqualTo(3.14159).Within(0.00001));
                Assert.That(((KVArrayValue)data2["matrix_as_vectors"]).Count, Is.EqualTo(4));
                Assert.That(((KVArrayValue)((KVArrayValue)data2["matrix_as_vectors"])[0]).Count, Is.EqualTo(4));
                Assert.That(((KVArrayValue)data2["empty_arrays"]).Count, Is.EqualTo(3));
                Assert.That(((KVArrayValue)((KVArrayValue)data2["empty_arrays"])[0]).Count, Is.EqualTo(0));
            });
        }

        [Test]
        public void BinaryBlobInlineRoundTrip()
        {
            // Verify the parser can re-read the new inline blob format
            var kv = KVSerializer.Create(KVSerializationFormat.KeyValues3Text);

            var collection = new KVCollectionValue();
            collection.Add(new KVObject("empty", new KVBinaryBlob([])));
            collection.Add(new KVObject("small", new KVBinaryBlob([0x11, 0xFF])));
            collection.Add(new KVObject("exact32", new KVBinaryBlob(Enumerable.Range(0, 32).Select(i => (byte)i).ToArray())));
            var doc = new KVDocument(null, null, collection);

            // Serialize (produces inline format) -> deserialize -> verify
            var data2 = RoundTrip(kv, doc);

            Assert.Multiple(() =>
            {
                Assert.That(((KVBinaryBlob)data2["empty"]).Bytes.Length, Is.EqualTo(0));
                Assert.That(((KVBinaryBlob)data2["small"]).Bytes.ToArray(), Is.EqualTo(new byte[] { 0x11, 0xFF }));
                var exact32 = (KVBinaryBlob)data2["exact32"];
                Assert.That(exact32.Bytes.Length, Is.EqualTo(32));
                Assert.That(exact32.Bytes.Span[0], Is.EqualTo(0x00));
                Assert.That(exact32.Bytes.Span[31], Is.EqualTo(0x1F));
            });
        }

        [Test]
        public void ComprehensiveRoundTrip()
        {
            // Master round-trip: deserialize a complex file, serialize, deserialize again, verify all values
            using var stream = TestDataHelper.OpenResource("TextKV3.types.kv3");
            var kv = KVSerializer.Create(KVSerializationFormat.KeyValues3Text);
            var data = kv.Deserialize(stream);

            var data2 = RoundTrip(kv, data);

            Assert.Multiple(() =>
            {
                Assert.That((bool)data2["boolFalseValue"], Is.False);
                Assert.That((bool)data2["boolTrueValue"], Is.True);
                Assert.That(data2["nullValue"].ValueType, Is.EqualTo(KVValueType.Null));
                Assert.That((long)data2["intValue"], Is.EqualTo(128));
                Assert.That((double)data2["doubleValue"], Is.EqualTo(64.123).Within(0.001));
                Assert.That((long)data2["negativeIntValue"], Is.EqualTo(-1337));
                Assert.That((double)data2["negativeDoubleValue"], Is.EqualTo(-0.1337).Within(0.0001));
                Assert.That((string)data2["stringValue"], Is.EqualTo("hello world"));
                Assert.That((string)data2["empty.string"], Is.EqualTo(""));
                Assert.That((string)data2["singleQuotes"], Is.EqualTo("string"));
                Assert.That((string)data2["singleQuotesWithQuotesInside"], Is.EqualTo("string is \"pretty\" cool"));
            });
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
