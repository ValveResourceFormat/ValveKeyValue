namespace ValveKeyValue.Test
{
    class TypeGuessingTestCase
    {
        [Test]
        public void IsNotNull()
        {
            Assert.That(data, Is.Not.Null);
        }

        [TestCase(KVValueType.String, TypeCode.String, "string", "123foo")]
        [TestCase(KVValueType.UInt64, TypeCode.UInt64, "bigint", 1UL)]
        [TestCase(KVValueType.UInt64, TypeCode.UInt64, "gaben_steamid", 76561197960287930UL)]
        [TestCase(KVValueType.FloatingPoint, TypeCode.Single, "float", 123.456f)]
        [TestCase(KVValueType.FloatingPoint, TypeCode.Single, "float_exp", 123.456f)]
        [TestCase(KVValueType.Int32, TypeCode.Int32, "int", 1234)]
        [TestCase(KVValueType.Int32, TypeCode.Int32, "negint", -1234)]
        public void HasValueOfType<TExpected>(KVValueType expectedType, TypeCode expectedTypeCode, string key, TExpected expectedValue)
        {
            var actualValue = data[key];

            Assert.That(actualValue, Is.Not.Null);
            Assert.That(actualValue.ValueType, Is.EqualTo(expectedType), nameof(KVValueType));
            Assert.That(actualValue.GetTypeCode(), Is.EqualTo(expectedTypeCode), nameof(TypeCode));

            var typedActualValue = Convert.ChangeType(actualValue, typeof(TExpected));
            Assert.That(typedActualValue, Is.EqualTo(expectedValue));
        }

        KVObject data;

        [OneTimeSetUp]
        public void SetUp()
        {
            using var stream = TestDataHelper.OpenResource("Text.type_guessing.vdf");
            data = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(stream);
        }
    }
}
