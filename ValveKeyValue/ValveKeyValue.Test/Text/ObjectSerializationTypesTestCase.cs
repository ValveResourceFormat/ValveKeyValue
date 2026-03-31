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

        [Test]
        public void AllScalarTypesRoundTripThroughKV1Text()
        {
            var dataObject = new AllScalarsObject
            {
                VBool = true,
                VByte = 255,
                VSByte = -128,
                VShort = short.MinValue,
                VUShort = ushort.MaxValue,
                VInt = int.MinValue,
                VUInt = uint.MaxValue,
                VLong = long.MinValue,
                VULong = ulong.MaxValue,
                VFloat = -1234.5678f,
                VDouble = 3.14159265358979,
                VString = "hello world",
            };

            using var ms = new MemoryStream();
            KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Serialize(ms, dataObject, "test data");

            ms.Seek(0, SeekOrigin.Begin);
            var d = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize<AllScalarsObject>(ms);

            Assert.Multiple(() =>
            {
                Assert.That(d.VBool, Is.EqualTo(true));
                Assert.That(d.VByte, Is.EqualTo((byte)255));
                Assert.That(d.VSByte, Is.EqualTo((sbyte)-128));
                Assert.That(d.VShort, Is.EqualTo(short.MinValue));
                Assert.That(d.VUShort, Is.EqualTo(ushort.MaxValue));
                Assert.That(d.VInt, Is.EqualTo(int.MinValue));
                Assert.That(d.VUInt, Is.EqualTo(uint.MaxValue));
                Assert.That(d.VLong, Is.EqualTo(long.MinValue));
                Assert.That(d.VULong, Is.EqualTo(ulong.MaxValue));
                Assert.That(d.VFloat, Is.EqualTo(-1234.5678f));
                Assert.That(d.VDouble, Is.EqualTo(3.14159265358979).Within(0.0000001));
                Assert.That(d.VString, Is.EqualTo("hello world"));
            });
        }

        [Test]
        public void ByteArraySerializesAsHexBlob()
        {
            var dataObject = new ByteArrayObject { VBlob = [0xAB, 0xCD, 0xEF] };

            using var ms = new MemoryStream();
            KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Serialize(ms, dataObject, "test data");

            ms.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(ms);
            var text = reader.ReadToEnd();

            Assert.That(text, Does.Not.Contain("\"0\""));
            Assert.That(text, Does.Contain("AB CD EF"));
        }

        class AllScalarsObject
        {
            public bool VBool { get; set; }
            public byte VByte { get; set; }
            public sbyte VSByte { get; set; }
            public short VShort { get; set; }
            public ushort VUShort { get; set; }
            public int VInt { get; set; }
            public uint VUInt { get; set; }
            public long VLong { get; set; }
            public ulong VULong { get; set; }
            public float VFloat { get; set; }
            public double VDouble { get; set; }
            public string VString { get; set; }
        }

        class ByteArrayObject
        {
            public byte[] VBlob { get; set; }
        }
    }
}
