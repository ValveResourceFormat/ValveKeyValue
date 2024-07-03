using System.Linq;

namespace ValveKeyValue.Test
{
    class StringTableTestCase
    {
        [Test]
        public void IsNotNull()
            => Assert.That(obj, Is.Not.Null);

        [Test]
        public void HasName()
            => Assert.That(obj.Name, Is.EqualTo("TestObject"));

        [Test]
        public void IsObjectWithChildren()
            => Assert.That(obj.Value.ValueType, Is.EqualTo(KVValueType.Collection));

        [TestCase(ExpectedResult = 5)]
        public int HasChildren()
            => obj.Children.Count();

        [TestCase("key", "value", typeof(string))]
        [TestCase("int", 0x01020304, typeof(int))]
        [TestCase("flt", 1234.5678f, typeof(float))]
        [TestCase("lng", 0x1122334455667788, typeof(ulong))]
        [TestCase("i64", 0x0102030405060708, typeof(long))]
        public void HasNamedChildWithValue(string name, object value, Type valueType)
        {
            Assert.That(Convert.ChangeType(obj[name], valueType), Is.EqualTo(value));
        }

        [Test]
        public void SymmetricStringTableSerialization()
        {
            var serializer = KVSerializer.Create(KVSerializationFormat.KeyValues1Binary);

            using var ms = new MemoryStream();
            serializer.Serialize(ms, obj, new KVSerializerOptions { StringTable = new(TestStringTable) });

            Assert.That(ms.ToArray(), Is.EqualTo(TestData.ToArray()));
        }

        KVObject obj;

        [OneTimeSetUp]
        public void SetUp()
        {
            obj = KVSerializer.Create(KVSerializationFormat.KeyValues1Binary)
                .Deserialize(
                    TestData.ToArray(),
                    new KVSerializerOptions { StringTable = new(TestStringTable) });
        }

        static string[] TestStringTable => [
            "flt",
            "i64",
            "int",
            "key",
            "lng",
            "TestObject"
        ];

        static ReadOnlySpan<byte> TestData =>
        [
            0x00, // object: TestObject
                0x05, 0x00, 0x00, 0x00, // stringTable[5] = "TestObject",
                0x01, // string: key = value
                    0x03, 0x00, 0x00, 0x00, // stringTable[3] = "key",
                    0x76, 0x61, 0x6C, 0x75, 0x65, 0x00,
                0x02, // int32: int = 0x01020304
                    0x02, 0x00, 0x00, 0x00, // stringTable[2] = "int"
                    0x04, 0x03, 0x02, 0x01,
                0x03, // float32: flt = 1234.5678f
                    0x00, 0x00, 0x00, 0x00, // stringTable[0] = "flt"
                    0x2B, 0x52, 0x9A, 0x44,
                0x07, // uint64: lng = 0x1122334455667788
                    0x04, 0x00, 0x00, 0x00, // stringTable[4] = "lng"
                    0x88, 0x77, 0x66, 0x55, 0x44, 0x33, 0x22, 0x11,
                0x0A, // int64, i64 = 0x0102030405070809
                    0x01, 0x00, 0x00, 0x00, // stringTable[1] = "i64"
                    0x08, 0x07, 0x06, 0x05, 0x04, 0x03, 0x02, 0x01,
                0x08, // end object
            0x08, // end document
        ];
    }
}
