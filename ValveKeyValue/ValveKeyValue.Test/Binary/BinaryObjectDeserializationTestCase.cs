namespace ValveKeyValue.Test
{
    class BinaryObjectDeserializationTestCase
    {
        [Test]
        public void IsNotNull()
            => Assert.That(obj, Is.Not.Null);

        [Test]
        public void StringValue()
            => Assert.That(obj.StringValue, Is.EqualTo("value"));

        [Test]
        public void Utf8StringValue()
            => Assert.That(obj.StringUtf8Value, Is.EqualTo("邪恶的战"));

        [Test]
        public void TheIntegerValue()
            => Assert.That(obj.TheIntegerValue, Is.EqualTo(0x01020304));

        [Test]
        public void TheFloatingMember()
            => Assert.That(obj.TheFloatingMember, Is.EqualTo(1234.5678f));

        [Test]
        public void Colour()
            => Assert.That(obj.Colour, Is.EqualTo(0x10203040));

        [Test]
        public void Pointer()
            => Assert.That(obj.Pointer, Is.EqualTo(0x11223344));

        [Test]
        public void ULongValue()
            => Assert.That(obj.ULongValue, Is.EqualTo(0x1122334455667788u));

        [Test]
        public void LongValue()
            => Assert.That(obj.LongValue, Is.EqualTo(0x0102030405060708));

        TestObject obj;

        [OneTimeSetUp]
        public void SetUp()
        {
            var data = new byte[]
            {
                0x00, // object: TestObject
                    0x54, 0x65, 0x73, 0x74, 0x4F, 0x62, 0x6A, 0x65, 0x63, 0x74, 0x00,
                    0x01, // string: key = value
                        0x6B, 0x65, 0x79, 0x00,
                        0x76, 0x61, 0x6C, 0x75, 0x65, 0x00,
                    0x01, // string_utf8: key_utf8 = 邪恶的战
                        0x6B, 0x65, 0x79, 0x5F, 0x75, 0x74, 0x66, 0x38, 0x00,
                        0xE9, 0x82, 0xAA, 0xE6, 0x81, 0xB6, 0xE7, 0x9A, 0x84, 0xE6, 0x88, 0x98, 0x00,
                    0x02, // int32: int = 0x01020304
                        0x69, 0x6E, 0x74, 0x00,
                        0x04, 0x03, 0x02, 0x01,
                    0x03, // float32: flt = 1234.5678f
                        0x66, 0x6C, 0x74, 0x00,
                        0x2B, 0x52, 0x9A, 0x44,
                    0x04, // pointer: ptr = 0x11223344
                        0x70, 0x74, 0x72, 0x00,
                        0x44, 0x33, 0x22, 0x11,
                    0x06, // color: col = 0x10203040
                        0x63, 0x6F, 0x6C, 0x00,
                        0x40, 0x30, 0x20, 0x10,
                    0x07, // uint64: lng = 0x1122334455667788
                        0x6C, 0x6E, 0x67, 0x00,
                        0x88, 0x77, 0x66, 0x55, 0x44, 0x33, 0x22, 0x11,
                    0x0A, // int64, i64 = 0x0102030405070809
                        0x69, 0x36, 0x34, 0x00,
                        0x08, 0x07, 0x06, 0x05, 0x04, 0x03, 0x02, 0x01,
                    0x08, // end object
                0x08, // end document
            };

            obj = KVSerializer.Create(KVSerializationFormat.KeyValues1Binary).Deserialize<TestObject>(data);
        }

        class TestObject
        {
            [KVProperty("key")]
            public string StringValue { get; set; }

            [KVProperty("key_utf8")]
            public string StringUtf8Value { get; set; }

            [KVProperty("int")]
            public int TheIntegerValue { get; set; }

            [KVProperty("flt")]
            public float TheFloatingMember { get; set; }

            [KVProperty("col")]
            public int Colour { get; set; }

            [KVProperty("ptr")]
            public int Pointer { get; set; }

            [KVProperty("lng")]
            public ulong ULongValue { get; set; }

            [KVProperty("i64")]
            public long LongValue { get; set; }
        }
    }
}
