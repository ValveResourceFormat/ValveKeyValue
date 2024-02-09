namespace ValveKeyValue.Test
{
    class NumbersTestCase
    {
        [Test]
        public void CorrectlyDeserializesNumbers()
        {
            using var stream = TestDataHelper.OpenResource("Text.numbers.vdf");
            var data = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(stream);

            Assert.Multiple(() =>
            {
                Assert.That((string)data["0"], Is.EqualTo("12,34"));
                Assert.That((string)data["1"], Is.EqualTo("12, 34"));
                Assert.That((string)data["2"], Is.EqualTo("12 ,34"));
                Assert.That((string)data["3"], Is.EqualTo("12.34"));
                Assert.That((string)data["4"], Is.EqualTo("12. 34"));
                Assert.That((string)data["5"], Is.EqualTo("12 .34"));
                Assert.That((string)data["6"], Is.EqualTo("ab,34"));
                Assert.That((string)data["7"], Is.EqualTo("12,cd"));
                Assert.That((string)data["8"], Is.EqualTo("12.34"));
                Assert.That((string)data["9"], Is.EqualTo("-12.34"));
                Assert.That((string)data["10"], Is.EqualTo("-12.34+"));
                Assert.That((string)data["11"], Is.EqualTo("+12.34+"));
                Assert.That((string)data["12"], Is.EqualTo("+12.34-"));
                Assert.That((string)data["13"], Is.EqualTo("-12.34-"));
                Assert.That((string)data["14"], Is.EqualTo("-12.34"));
                Assert.That((string)data["15"], Is.EqualTo("-12.34   	"));
                Assert.That((string)data["16"], Is.EqualTo("0"));
                Assert.That((string)data["17"], Is.EqualTo("2147483647"));
                Assert.That((string)data["18"], Is.EqualTo("2147483648"));
                Assert.That((string)data["19"], Is.EqualTo("123456789"));
                Assert.That((string)data["20"], Is.EqualTo("6404082971767543753"));
                Assert.That((string)data["21"], Is.EqualTo("10000000"));
                Assert.That((string)data["22"], Is.EqualTo("-9223372036854775808"));
                Assert.That((string)data["23"], Is.EqualTo("9223372036854775807"));
                Assert.That((string)data["24"], Is.EqualTo("18446744073709551615"));
                Assert.That((string)data["25"], Is.EqualTo("4294967295"));
                Assert.That((string)data["26"], Is.EqualTo("-2147483648"));
                Assert.That((string)data["27"], Is.EqualTo("  -123456789012345"));
                Assert.That((string)data["28"], Is.EqualTo("  +123456789012345"));
                Assert.That((string)data["29"], Is.EqualTo("-1.2345679E+14"));
                Assert.That((string)data["30"], Is.EqualTo("1.234568E+15"));

                Assert.That(data["0"].ValueType, Is.EqualTo(KVValueType.String));
                Assert.That(data["1"].ValueType, Is.EqualTo(KVValueType.String));
                Assert.That(data["2"].ValueType, Is.EqualTo(KVValueType.String));
                Assert.That(data["3"].ValueType, Is.EqualTo(KVValueType.FloatingPoint));
                Assert.That(data["4"].ValueType, Is.EqualTo(KVValueType.String));
                Assert.That(data["5"].ValueType, Is.EqualTo(KVValueType.String));
                Assert.That(data["6"].ValueType, Is.EqualTo(KVValueType.String));
                Assert.That(data["7"].ValueType, Is.EqualTo(KVValueType.String));
                Assert.That(data["8"].ValueType, Is.EqualTo(KVValueType.FloatingPoint));
                Assert.That(data["9"].ValueType, Is.EqualTo(KVValueType.FloatingPoint));
                Assert.That(data["10"].ValueType, Is.EqualTo(KVValueType.String));
                Assert.That(data["11"].ValueType, Is.EqualTo(KVValueType.String));
                Assert.That(data["12"].ValueType, Is.EqualTo(KVValueType.String));
                Assert.That(data["13"].ValueType, Is.EqualTo(KVValueType.String));
                Assert.That(data["14"].ValueType, Is.EqualTo(KVValueType.FloatingPoint));
                Assert.That(data["15"].ValueType, Is.EqualTo(KVValueType.String));
                Assert.That(data["16"].ValueType, Is.EqualTo(KVValueType.Int32));
                Assert.That(data["17"].ValueType, Is.EqualTo(KVValueType.Int32));
                Assert.That(data["18"].ValueType, Is.EqualTo(KVValueType.String));
                Assert.That(data["19"].ValueType, Is.EqualTo(KVValueType.Int32));
                Assert.That(data["20"].ValueType, Is.EqualTo(KVValueType.String));
                Assert.That(data["21"].ValueType, Is.EqualTo(KVValueType.FloatingPoint));
                Assert.That(data["22"].ValueType, Is.EqualTo(KVValueType.String));
                Assert.That(data["23"].ValueType, Is.EqualTo(KVValueType.String));
                Assert.That(data["24"].ValueType, Is.EqualTo(KVValueType.String));
                Assert.That(data["25"].ValueType, Is.EqualTo(KVValueType.String));
                Assert.That(data["26"].ValueType, Is.EqualTo(KVValueType.Int32));
                Assert.That(data["27"].ValueType, Is.EqualTo(KVValueType.String));
                Assert.That(data["28"].ValueType, Is.EqualTo(KVValueType.String));
                Assert.That(data["29"].ValueType, Is.EqualTo(KVValueType.FloatingPoint));
                Assert.That(data["30"].ValueType, Is.EqualTo(KVValueType.FloatingPoint));
            });
        }
    }
}
