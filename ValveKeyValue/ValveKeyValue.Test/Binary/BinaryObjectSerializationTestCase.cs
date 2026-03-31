namespace ValveKeyValue.Test
{
    class BinaryObjectSerializationTestCase
    {
        [Test]
        public void SerializesToBinaryStructure()
        {
            var kvo = KVObject.ListCollection();
            kvo.Add("key", "value");
            kvo.Add("key_utf8", "邪恶的战");
            kvo.Add("int", 0x10203040);
            kvo.Add("flt", 1234.5678f);
            kvo.Add("ptr", new IntPtr(0x12345678));
            kvo.Add("lng", 0x8877665544332211u);
            kvo.Add("i64", 0x0102030405060708);
            var doc = new KVDocument(null, "TestObject", kvo);

            var expectedData = new byte[]
            {
                0x00, // object: TestObject
                    0x54, 0x65, 0x73, 0x74, 0x4F, 0x62, 0x6A, 0x65, 0x63, 0x74, 0x00,
                    0x01, // string: key = value
                        0x6B, 0x65, 0x79, 0x00,
                        0x76, 0x61, 0x6C, 0x75, 0x65, 0x00,
                    0x01, // string_utf8: key_utf8 = 邪恶的战
                        0x6B, 0x65, 0x79, 0x5F, 0x75, 0x74, 0x66, 0x38, 0x00,
                        0xE9, 0x82, 0xAA, 0xE6, 0x81, 0xB6, 0xE7, 0x9A, 0x84, 0xE6, 0x88, 0x98, 0x00,
                    0x02, // int32: int = 0x10203040
                        0x69, 0x6E, 0x74, 0x00,
                        0x40, 0x30, 0x20, 0x10,
                    0x03, // float32: flt = 1234.5678f
                        0x66, 0x6C, 0x74, 0x00,
                        0x2B, 0x52, 0x9A, 0x44,
                    0x04, // pointer: ptr = 0x12345678
                        0x70, 0x74, 0x72, 0x00,
                        0x78, 0x56, 0x34, 0x12,
                    0x07, // uint64: lng = 0x8877665544332211
                        0x6C, 0x6E, 0x67, 0x00,
                        0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88,
                    0x0A, // int64, i64 = 0x0102030405070809
                        0x69, 0x36, 0x34, 0x00,
                        0x08, 0x07, 0x06, 0x05, 0x04, 0x03, 0x02, 0x01,
                    0x08, // end object
                0x08, // end document
            };

            using var ms = new MemoryStream();
            KVSerializer.Create(KVSerializationFormat.KeyValues1Binary).Serialize(ms, doc);
            Assert.That(ms.ToArray(), Is.EqualTo(expectedData));

            ms.Seek(0, SeekOrigin.Begin);
            var deserialized = KVSerializer.Create(KVSerializationFormat.KeyValues1Binary).Deserialize(ms);
            Assert.That(deserialized.Name, Is.EqualTo("TestObject"));
            Assert.That((string)deserialized["key"], Is.EqualTo("value"));
            Assert.That((string)deserialized["key_utf8"], Is.EqualTo("邪恶的战"));
            Assert.That((int)deserialized["int"], Is.EqualTo(0x10203040));
            Assert.That((float)deserialized["flt"], Is.EqualTo(1234.5678f));
            Assert.That((IntPtr)deserialized["ptr"], Is.EqualTo(new IntPtr(0x12345678)));
            Assert.That((ulong)deserialized["lng"], Is.EqualTo(0x8877665544332211u));
            Assert.That((long)deserialized["i64"], Is.EqualTo(0x0102030405060708));
        }

        [Test]
        public void NewValueTypesAreWidenedInBinarySerialization()
        {
            var kvo = KVObject.ListCollection();
            kvo.Add("bool", new KVObject(true));
            kvo.Add("i16", (short)42);
            kvo.Add("u16", (ushort)42);
            kvo.Add("u32", (uint)42);
            kvo.Add("f64", 3.14);
            kvo.Add("blob", KVObject.Blob([0xAB, 0xCD]));
            kvo.Add("null", KVObject.Null());
            var doc = new KVDocument(null, "Test", kvo);

            using var ms = new MemoryStream();
            KVSerializer.Create(KVSerializationFormat.KeyValues1Binary).Serialize(ms, doc);

            ms.Seek(0, SeekOrigin.Begin);
            var deserialized = KVSerializer.Create(KVSerializationFormat.KeyValues1Binary).Deserialize(ms);

            Assert.Multiple(() =>
            {
                Assert.That((int)deserialized["bool"], Is.EqualTo(1));
                Assert.That((int)deserialized["i16"], Is.EqualTo(42));
                Assert.That((int)deserialized["u16"], Is.EqualTo(42));
                Assert.That((ulong)deserialized["u32"], Is.EqualTo(42UL));
                Assert.That((float)deserialized["f64"], Is.EqualTo(3.14f).Within(0.01));
                Assert.That((string)deserialized["blob"], Is.EqualTo("AB CD"));
                Assert.That((string)deserialized["null"], Is.EqualTo(string.Empty));
            });
        }
    }
}
