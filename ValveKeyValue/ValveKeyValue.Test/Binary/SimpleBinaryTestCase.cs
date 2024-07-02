using System.Linq;

namespace ValveKeyValue.Test
{
    class SimpleBinaryTestCase
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

        [TestCase(ExpectedResult = 7)]
        public int HasChildren()
            => obj.Children.Count();

        [TestCase("key", "value", typeof(string))]
        [TestCase("int", 0x01020304, typeof(int))]
        [TestCase("flt", 1234.5678f, typeof(float))]
        [TestCase("col", 0x10203040, typeof(int))]
        [TestCase("ptr", 0x11223344, typeof(int))]
        [TestCase("lng", 0x1122334455667788, typeof(ulong))]
        [TestCase("i64", 0x0102030405060708, typeof(long))]
        public void HasNamedChildWithValue(string name, object value, Type valueType)
        {
            Assert.That(Convert.ChangeType(obj[name], valueType), Is.EqualTo(value));
        }

        KVObject obj;

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
                    0x02, // int32: int = 0x01020304
                        0x69, 0x6E, 0x74, 0x00,
                        0x04, 0x03, 0x02, 0x01,
                    0x03, // float32: flt = 1234.5678f
                        0x66, 0x6C, 0x74, 0x00,
                        0x2B, 0x52, 0x9A, 0x44,
                    0x04, // color: col = 0x10203040
                        0x63, 0x6F, 0x6C, 0x00,
                        0x40, 0x30, 0x20, 0x10,
                    0x06, // pointer: ptr = 0x11223344
                        0x70, 0x74, 0x72, 0x00,
                        0x44, 0x33, 0x22, 0x11,
                    0x07, // uint64: lng = 0x1122334455667788
                        0x6C, 0x6E, 0x67, 0x00,
                        0x88, 0x77, 0x66, 0x55, 0x44, 0x33, 0x22, 0x11,
                    0x0A, // int64, i64 = 0x0102030405070809
                        0x69, 0x36, 0x34, 0x00,
                        0x08, 0x07, 0x06, 0x05, 0x04, 0x03, 0x02, 0x01,
                    0x08, // end object
                0x08, // end document
            };
            obj = KVSerializer.Create(KVSerializationFormat.KeyValues1Binary).Deserialize(data);
        }
    }
}
