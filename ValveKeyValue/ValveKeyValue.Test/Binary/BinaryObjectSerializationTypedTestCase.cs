﻿using System;
using System.IO;
using NUnit.Framework;

namespace ValveKeyValue.Test
{
    class BinaryObjectSerializationTypedTestCase
    {
        class TestObject
        {
            public string key { get; set; } = "value";
            public string key_utf8 { get; set; } = "邪恶的战";
            [KVProperty("int")]
            public int int_value { get; set; } = 0x10203040;
            public float flt { get; set; } = 1234.5678f;
            public IntPtr ptr { get; set; } = new IntPtr(0x12345678);
            public ulong lng { get; set; } = 0x8877665544332211u;
            public long i64 { get; set; } = 0x0102030405060708;
        }

        [Test]
        public void SerializesToBinaryStructure()
        {
            var expectedData = new byte[]
            {
                0x00, // object: TestObject
                    0x54, 0x65, 0x73, 0x74, 0x4F, 0x62, 0x6A, 0x65, 0x63, 0x74, 0x00,
                    0x03, // float32: flt = 1234.5678f
                        0x66, 0x6C, 0x74, 0x00,
                        0x2B, 0x52, 0x9A, 0x44,
                    0x0A, // int64, i64 = 0x0102030405070809
                        0x69, 0x36, 0x34, 0x00,
                        0x08, 0x07, 0x06, 0x05, 0x04, 0x03, 0x02, 0x01,
                    0x02, // int32: int = 0x10203040
                        0x69, 0x6E, 0x74, 0x00,
                        0x40, 0x30, 0x20, 0x10,
                    0x01, // string: key = value
                        0x6B, 0x65, 0x79, 0x00,
                        0x76, 0x61, 0x6C, 0x75, 0x65, 0x00,
                    0x01, // string_utf8: key_utf8 = 邪恶的战
                        0x6B, 0x65, 0x79, 0x5F, 0x75, 0x74, 0x66, 0x38, 0x00,
                        0xE9, 0x82, 0xAA, 0xE6, 0x81, 0xB6, 0xE7, 0x9A, 0x84, 0xE6, 0x88, 0x98, 0x00,
                    0x07, // uint64: lng = 0x8877665544332211
                        0x6C, 0x6E, 0x67, 0x00,
                        0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88,
                    0x04, // pointer: ptr = 0x12345678
                        0x70, 0x74, 0x72, 0x00,
                        0x78, 0x56, 0x34, 0x12,
                    0x08, // end object
                0x08, // end document
            };

            var kvo = new TestObject();

            using var ms = new MemoryStream();
            KVSerializer.Create(KVSerializationFormat.KeyValues1Binary).Serialize(ms, kvo, "TestObject");

            Assert.That(ms.ToArray(), Is.EqualTo(expectedData));
        }
    }
}
