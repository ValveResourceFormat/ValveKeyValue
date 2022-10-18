﻿using System;
using System.IO;
using NUnit.Framework;

namespace ValveKeyValue.Test
{
    class ObjectSerializationTypesTestCase
    {
        [Test]
        public void CreatesTextDocument()
        {
            var dataObject = new[]
            {
                new DataObject
                {
                    VString = "Test String",
                    VInt = 0x10203040,
                    VFloat = 1234.5678f,
                    VLong = 0x0102030405060708,
                    VULong = 0x8877665544332211u,
                },
            };

            string text;
            using (var ms = new MemoryStream())
            {
                KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Serialize(ms, dataObject, "test data");

                ms.Seek(0, SeekOrigin.Begin);
                using var reader = new StreamReader(ms);
                text = reader.ReadToEnd();
            }

            var expected = TestDataHelper.ReadTextResource("Text.serialization_types_expected.vdf");
            Assert.That(text, Is.EqualTo(expected));
        }

        class DataObject
        {
            public string VString { get; set; }
            public int VInt { get; set; }
            public long VLong { get; set; }
            public ulong VULong { get; set; }
            public float VFloat { get; set; }
            public SomeEnum VEnum { get; set; }
        }

        enum SomeEnum
        {
            One = 1,
            Two = 2,
            Leet = 1337,
        }
    }
}
