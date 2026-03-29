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
                    VEnum = SomeEnum.Leet,
                    VFlags = SomeFlags.Foo | SomeFlags.Bar,
                    VByteEnum = ByteEnum.Max,
                    VShortEnum = ShortEnum.Negative,
                    VLongEnum = LongEnum.Big,
                    VULongEnum = ULongEnum.Big,
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

            var deserialized = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize<DataObject[]>(text);
            Assert.That(deserialized, Has.Length.EqualTo(1));
            Assert.That(deserialized[0].VString, Is.EqualTo(dataObject[0].VString));
            Assert.That(deserialized[0].VInt, Is.EqualTo(dataObject[0].VInt));
            Assert.That(deserialized[0].VFloat, Is.EqualTo(dataObject[0].VFloat));
            Assert.That(deserialized[0].VLong, Is.EqualTo(dataObject[0].VLong));
            Assert.That(deserialized[0].VULong, Is.EqualTo(dataObject[0].VULong));
            Assert.That(deserialized[0].VEnum, Is.EqualTo(dataObject[0].VEnum));
            Assert.That(deserialized[0].VFlags, Is.EqualTo(dataObject[0].VFlags));
            Assert.That(deserialized[0].VByteEnum, Is.EqualTo(dataObject[0].VByteEnum));
            Assert.That(deserialized[0].VShortEnum, Is.EqualTo(dataObject[0].VShortEnum));
            Assert.That(deserialized[0].VLongEnum, Is.EqualTo(dataObject[0].VLongEnum));
            Assert.That(deserialized[0].VULongEnum, Is.EqualTo(dataObject[0].VULongEnum));
        }

        class DataObject
        {
            public string VString { get; set; }
            public int VInt { get; set; }
            public long VLong { get; set; }
            public ulong VULong { get; set; }
            public float VFloat { get; set; }
            public SomeEnum VEnum { get; set; }
            public SomeFlags VFlags { get; set; }
            public ByteEnum VByteEnum { get; set; }
            public ShortEnum VShortEnum { get; set; }
            public LongEnum VLongEnum { get; set; }
            public ULongEnum VULongEnum { get; set; }
        }

        enum SomeEnum
        {
            One = 1,
            Two = 2,
            Leet = 1337,
        }

        [Flags]
        enum SomeFlags
        {
            Foo = 1 << 1,
            Bar = 1 << 3,
        }

        enum ByteEnum : byte
        {
            Zero = 0,
            Max = 255,
        }

        enum ShortEnum : short
        {
            Negative = -1,
            Positive = 100,
        }

        enum LongEnum : long
        {
            Big = 0x0102030405060708,
        }

        enum ULongEnum : ulong
        {
            Big = 0x8877665544332211,
        }
    }
}
