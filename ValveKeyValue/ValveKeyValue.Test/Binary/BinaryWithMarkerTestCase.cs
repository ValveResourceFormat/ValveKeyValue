using System.Linq;

namespace ValveKeyValue.Test
{
    class BinaryWithMarkerTestCase
    {
        [Test]
        public void WithMagicHeader()
        {
            var data = new byte[]
            {
                0x56, 0x42, 0x4B, 0x56, // VBKV
                0x00, 0x00, 0x00, 0x00, // CRC32, not verified
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
                    0x05, // wide string: wstr = "hi", int16 length without terminator
                        0x77, 0x73, 0x74, 0x72, 0x00,
                        0x02, 0x00,
                        0x68, 0x00, 0x69, 0x00,
                    0x06, // color: col = 0x10203040
                        0x63, 0x6F, 0x6C, 0x00,
                        0x40, 0x30, 0x20, 0x10,
                    0x07, // uint64: lng = 0x1122334455667788
                        0x6C, 0x6E, 0x67, 0x00,
                        0x88, 0x77, 0x66, 0x55, 0x44, 0x33, 0x22, 0x11,
                    0x00, // object: empty
                        0x65, 0x6D, 0x70, 0x74, 0x79, 0x00,
                    0x0B, // end object: empty
                    0x0B, // end object: TestObject
                0x0B, // end document
                0xFF, 0xFF, // trailing data, not read
            };

            using var stream = new MemoryStream(data);
            var obj = KVSerializer.Create(KVSerializationFormat.KeyValues1Binary).Deserialize(stream);

            Assert.Multiple(() =>
            {
                Assert.That(stream.Position, Is.EqualTo(data.Length - 2));
                Assert.That(obj.Name, Is.EqualTo("TestObject"));
                Assert.That(obj.Root.Children.Count(), Is.EqualTo(7));
                Assert.That((string)obj["key"], Is.EqualTo("value"));
                Assert.That((int)obj["int"], Is.EqualTo(0x01020304));
                Assert.That((float)obj["flt"], Is.EqualTo(1234.5678f));
                Assert.That((string)obj["wstr"], Is.EqualTo("hi"));
                Assert.That((int)obj["col"], Is.EqualTo(0x10203040));
                Assert.That((ulong)obj["lng"], Is.EqualTo(0x1122334455667788));
                Assert.That(obj.Root["empty"].Children.Count(), Is.Zero);
            });
        }

        [Test]
        public void WithoutMagicHeader()
        {
            var data = new byte[]
            {
                0x00, // object: TestObject
                    0x54, 0x65, 0x73, 0x74, 0x4F, 0x62, 0x6A, 0x65, 0x63, 0x74, 0x00,
                    0x01, // string: key = value
                        0x6B, 0x65, 0x79, 0x00,
                        0x76, 0x61, 0x6C, 0x75, 0x65, 0x00,
                    0x00, // object: empty
                        0x65, 0x6D, 0x70, 0x74, 0x79, 0x00,
                    0x0B, // end object: empty
                    0x0B, // end object: TestObject
                0x0B, // end document
            };

            var obj = KVSerializer.Create(KVSerializationFormat.KeyValues1Binary).Deserialize(data);

            Assert.Multiple(() =>
            {
                Assert.That(obj.Name, Is.EqualTo("TestObject"));
                Assert.That((string)obj["key"], Is.EqualTo("value"));
                Assert.That(obj.Root["empty"].Children.Count(), Is.Zero);
            });
        }
    }
}
