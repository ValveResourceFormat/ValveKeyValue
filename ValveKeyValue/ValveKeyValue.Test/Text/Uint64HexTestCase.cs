namespace ValveKeyValue.Test
{
    class Uint64HexTestCase
    {
        [TestCase("valid_lowercase", 0x1122334455667788UL)]
        [TestCase("valid_uppercase_digits", 0x1122AABBCCDDEEFFUL)]
        [TestCase("valid_mixed_case_digits", 0xAABBCCDDEEFF0011UL)]
        [TestCase("garbage_brackets", 0x1122334455667966UL)]
        [TestCase("garbage_above_f", 0x5555555555555553UL)]
        [TestCase("garbage_minus", 0xD122334455667788UL)]
        [TestCase("garbage_overflow", 0x0000000000000000UL)]
        [TestCase("garbage_below_zero", 0xEEEEEEEEEEEEEEEFUL)]
        [TestCase("garbage_second_x", 0x1000000000000000UL)]
        public void ParsesAsUnsignedLong(string key, ulong expectedValue)
        {
            var value = data[key];

            Assert.That(value, Is.Not.Null);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(value.ValueType, Is.EqualTo(KVValueType.UInt64));
                Assert.That((ulong)value, Is.EqualTo(expectedValue));
            }
        }

        [TestCase("uppercase_x", "0X1122334455667788")]
        [TestCase("too_short", "0x112233445566778")]
        [TestCase("too_long", "0x11223344556677889")]
        public void ParsesAsString(string key, string expectedValue)
        {
            var value = data[key];

            Assert.That(value, Is.Not.Null);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(value.ValueType, Is.EqualTo(KVValueType.String));
                Assert.That((string)value, Is.EqualTo(expectedValue));
            }
        }

        KVObject data;

        [OneTimeSetUp]
        public void SetUp()
        {
            using var stream = TestDataHelper.OpenResource("Text.uint64_hex.vdf");
            data = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(stream);
        }
    }
}
