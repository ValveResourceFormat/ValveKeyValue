using System.Diagnostics.CodeAnalysis;

namespace ValveKeyValue.Test
{
    // de-DE and fa-IR are chosen because together they differ from the invariant culture in every
    // way that matters: comma/Arabic decimal separators, a period group separator, and a non-ASCII
    // negative sign (U+2212) on negative numbers. Every scalar KVValueType is covered, including
    // already-safe ones, so the guarantee is locked down for all types and both directions.
    class CultureInvariantFormattingTestCase
    {
        static IEnumerable<TestCaseData> ScalarCases()
        {
            yield return Named(new KVObject("plain string"), "plain string", "string");
            yield return Named(new KVObject(true), "1", "bool true");
            yield return Named(new KVObject(false), "0", "bool false");
            yield return Named(new KVObject((short)-12345), "-12345", "short negative");
            yield return Named(new KVObject((ushort)54321), "54321", "ushort");
            yield return Named(new KVObject(-1234567), "-1234567", "int negative");
            yield return Named(new KVObject(4000000000u), "4000000000", "uint");
            yield return Named(new KVObject(-9876543210L), "-9876543210", "long negative");
            yield return Named(new KVObject(18000000000000000000UL), "18000000000000000000", "ulong");
            yield return Named(new KVObject(new IntPtr(-1234)), "-1234", "pointer negative");
            yield return Named(new KVObject(0.8f), "0.8", "float positive fractional");
            yield return Named(new KVObject(-2.75f), "-2.75", "float negative fractional");
            yield return Named(new KVObject(1234.5d), "1234.5", "double positive fractional");
            yield return Named(new KVObject(-0.25d), "-0.25", "double negative fractional");
        }

        // bool/string have format quirks (true/false, quoting) unrelated to culture.
        static IEnumerable<TestCaseData> NumericCases()
        {
            foreach (var data in ScalarCases())
            {
                var value = (KVObject)data.Arguments[0]!;
                if (value.ValueType is not (KVValueType.String or KVValueType.Boolean))
                {
                    yield return data;
                }
            }
        }

        static TestCaseData Named(KVObject value, string expected, string description)
            => new TestCaseData(value, expected).SetName($"{{m}} - {description}");

        static string Serialize(KVSerializationFormat format, KVObject value)
        {
            var root = KVObject.Collection();
            root.Add("v", value);

            using var ms = new MemoryStream();
            KVSerializer.Create(format).Serialize(ms, root, "root");
            ms.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(ms);
            return reader.ReadToEnd();
        }

        [TestCaseSource(nameof(ScalarCases))]
        [SetCulture("de-DE")]
        public void ToStringNullProviderIsInvariant_DE(KVObject value, string expected)
            => Assert.That(value.ToString(null), Is.EqualTo(expected));

        [TestCaseSource(nameof(ScalarCases))]
        [SetCulture("fa-IR")]
        public void ToStringNullProviderIsInvariant_FA(KVObject value, string expected)
            => Assert.That(value.ToString(null), Is.EqualTo(expected));

        [TestCaseSource(nameof(ScalarCases))]
        [SetCulture("de-DE")]
        [SuppressMessage("Globalization", "CA1305:Specify IFormatProvider", Justification = "Deliberately exercising the parameterless overload to assert it is invariant.")]
        public void ParameterlessToStringIsInvariant_DE(KVObject value, string expected)
            => Assert.That(value.ToString(), Is.EqualTo(expected));

        [TestCaseSource(nameof(ScalarCases))]
        [SetCulture("fa-IR")]
        [SuppressMessage("Globalization", "CA1305:Specify IFormatProvider", Justification = "Deliberately exercising the parameterless overload to assert it is invariant.")]
        public void ParameterlessToStringIsInvariant_FA(KVObject value, string expected)
            => Assert.That(value.ToString(), Is.EqualTo(expected));

        [TestCaseSource(nameof(ScalarCases))]
        [SetCulture("de-DE")]
        public void ExplicitStringCastIsInvariant_DE(KVObject value, string expected)
            => Assert.That((string)value, Is.EqualTo(expected));

        [TestCaseSource(nameof(ScalarCases))]
        [SetCulture("fa-IR")]
        public void ExplicitStringCastIsInvariant_FA(KVObject value, string expected)
            => Assert.That((string)value, Is.EqualTo(expected));

        [TestCaseSource(nameof(NumericCases))]
        [SetCulture("de-DE")]
        public void ToTypeStringIsInvariant_DE(KVObject value, string expected)
            => Assert.That(value.ToType(typeof(string), null), Is.EqualTo(expected));

        [TestCaseSource(nameof(NumericCases))]
        [SetCulture("fa-IR")]
        public void ToTypeStringIsInvariant_FA(KVObject value, string expected)
            => Assert.That(value.ToType(typeof(string), null), Is.EqualTo(expected));

        [TestCaseSource(nameof(NumericCases))]
        [SetCulture("de-DE")]
        public void KeyValues1TextSerializationIsInvariant_DE(KVObject value, string expected)
            => Assert.That(Serialize(KVSerializationFormat.KeyValues1Text, value), Does.Contain(expected));

        [TestCaseSource(nameof(NumericCases))]
        [SetCulture("fa-IR")]
        public void KeyValues1TextSerializationIsInvariant_FA(KVObject value, string expected)
            => Assert.That(Serialize(KVSerializationFormat.KeyValues1Text, value), Does.Contain(expected));

        [TestCaseSource(nameof(NumericCases))]
        [SetCulture("de-DE")]
        public void KeyValues3TextSerializationIsInvariant_DE(KVObject value, string expected)
            => Assert.That(Serialize(KVSerializationFormat.KeyValues3Text, value), Does.Contain(expected));

        [TestCaseSource(nameof(NumericCases))]
        [SetCulture("fa-IR")]
        public void KeyValues3TextSerializationIsInvariant_FA(KVObject value, string expected)
            => Assert.That(Serialize(KVSerializationFormat.KeyValues3Text, value), Does.Contain(expected));

        // The array-index key path already formats invariantly; the dictionary key path did not.
        [Test]
        [SetCulture("de-DE")]
        public void DictionaryFloatKeysSerializeInvariant_DE()
        {
            var data = new Dictionary<float, string> { [0.8f] = "value" };

            using var ms = new MemoryStream();
            KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Serialize(ms, data, "root");
            ms.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(ms);
            var text = reader.ReadToEnd();

            Assert.That(text, Does.Contain("\"0.8\""));
        }

        [Test]
        [SetCulture("fa-IR")]
        public void DictionaryNegativeIntKeysSerializeInvariant_FA()
        {
            var data = new Dictionary<int, string> { [-1234] = "value" };

            using var ms = new MemoryStream();
            KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Serialize(ms, data, "root");
            ms.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(ms);
            var text = reader.ReadToEnd();

            Assert.That(text, Does.Contain("\"-1234\""));
        }

        static IEnumerable<TestCaseData> StringToFractionalCases()
        {
            yield return new TestCaseData(new KVObject("1.5"), 1.5d).SetName("{m} - 1.5");
            yield return new TestCaseData(new KVObject("-2.75"), -2.75d).SetName("{m} - -2.75");
            yield return new TestCaseData(new KVObject("1234.5"), 1234.5d).SetName("{m} - 1234.5");
            yield return new TestCaseData(new KVObject("-0.25"), -0.25d).SetName("{m} - -0.25");
        }

        [TestCaseSource(nameof(StringToFractionalCases))]
        [SetCulture("de-DE")]
        public void StringToDoubleIsInvariant_DE(KVObject value, double expected)
            => Assert.That(value.ToDouble(null), Is.EqualTo(expected));

        [TestCaseSource(nameof(StringToFractionalCases))]
        [SetCulture("fa-IR")]
        public void StringToDoubleIsInvariant_FA(KVObject value, double expected)
            => Assert.That(value.ToDouble(null), Is.EqualTo(expected));

        [TestCaseSource(nameof(StringToFractionalCases))]
        [SetCulture("fa-IR")]
        public void StringToSingleIsInvariant_FA(KVObject value, double expected)
            => Assert.That(value.ToSingle(null), Is.EqualTo((float)expected));

        [TestCaseSource(nameof(StringToFractionalCases))]
        [SetCulture("fa-IR")]
        public void StringToDecimalIsInvariant_FA(KVObject value, double expected)
            => Assert.That(value.ToDecimal(null), Is.EqualTo((decimal)expected));

        [Test]
        [SetCulture("fa-IR")]
        public void StringToInt64IsInvariant_FA()
            => Assert.That(new KVObject("-9876543210").ToInt64(null), Is.EqualTo(-9876543210L));

        [Test]
        [SetCulture("fa-IR")]
        public void FloatCastFromStringIsInvariant_FA()
            => Assert.That((float)new KVObject("-2.75"), Is.EqualTo(-2.75f));

        [Test]
        [SetCulture("fa-IR")]
        public void IntCastFromStringIsInvariant_FA()
            => Assert.That((int)new KVObject("-1234567"), Is.EqualTo(-1234567));

        [TestCase(KVSerializationFormat.KeyValues1Text)]
        [TestCase(KVSerializationFormat.KeyValues3Text)]
        [SetCulture("de-DE")]
        public void TextRoundTripPreservesNumbers_DE(KVSerializationFormat format) => AssertNumberRoundTrip(format);

        [TestCase(KVSerializationFormat.KeyValues1Text)]
        [TestCase(KVSerializationFormat.KeyValues3Text)]
        [SetCulture("fa-IR")]
        public void TextRoundTripPreservesNumbers_FA(KVSerializationFormat format) => AssertNumberRoundTrip(format);

        static void AssertNumberRoundTrip(KVSerializationFormat format)
        {
            var root = KVObject.Collection();
            root.Add("floatValue", new KVObject(0.8f));
            root.Add("doubleValue", new KVObject(-2.75d));
            root.Add("intValue", new KVObject(-1234567));

            var kv = KVSerializer.Create(format);

            using var ms = new MemoryStream();
            kv.Serialize(ms, root, "root");
            ms.Seek(0, SeekOrigin.Begin);
            var result = kv.Deserialize(ms);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Root["floatValue"].ToSingle(null), Is.EqualTo(0.8f));
                Assert.That(result.Root["doubleValue"].ToDouble(null), Is.EqualTo(-2.75d));
                Assert.That(result.Root["intValue"].ToInt32(null), Is.EqualTo(-1234567));
            }
        }
    }
}
