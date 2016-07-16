using NUnit.Framework;

namespace ValveKeyValue
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
                    0x07, // uint64: long = 0x1122334455667788
                        0x6C, 0x6E, 0x67, 0x00,
                        0x88, 0x77, 0x66, 0x55, 0x44, 0x33, 0x22, 0x11,
                    0x08, // end object
                0x08, // end document
            };

            obj = KVSerializer.Deserialize<TestObject>(data);
        }

        class TestObject
        {
            [KVProperty("key")]
            public string StringValue { get; set; }

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
        }
    }
}
