using NUnit.Framework;

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
                Assert.That((string)data["8"], Is.EqualTo("+12.34"));
                Assert.That((string)data["9"], Is.EqualTo("-12.34"));
                Assert.That((string)data["10"], Is.EqualTo("-12.34+"));
                Assert.That((string)data["11"], Is.EqualTo("+12.34+"));
                Assert.That((string)data["12"], Is.EqualTo("+12.34-"));
                Assert.That((string)data["13"], Is.EqualTo("-12.34-"));
                Assert.That((string)data["14"], Is.EqualTo("        \t-12.34"));
                Assert.That((string)data["15"], Is.EqualTo("-12.34   	"));
                Assert.That((string)data["16"], Is.EqualTo("0"));
                Assert.That((string)data["17"], Is.EqualTo("2147483647"));
                //Assert.That((string)data["18"], Is.EqualTo("2147483648"));
                Assert.That((string)data["19"], Is.EqualTo("   \t  123456789"));

                Assert.That(data["0"].ValueType, Is.EqualTo(KVValueType.String));
                Assert.That(data["1"].ValueType, Is.EqualTo(KVValueType.String));
                Assert.That(data["2"].ValueType, Is.EqualTo(KVValueType.String));
                Assert.That(data["3"].ValueType, Is.EqualTo(KVValueType.String));
                Assert.That(data["4"].ValueType, Is.EqualTo(KVValueType.String));
                Assert.That(data["5"].ValueType, Is.EqualTo(KVValueType.String));
                Assert.That(data["6"].ValueType, Is.EqualTo(KVValueType.String));
                Assert.That(data["7"].ValueType, Is.EqualTo(KVValueType.String));
                Assert.That(data["8"].ValueType, Is.EqualTo(KVValueType.String));
                Assert.That(data["9"].ValueType, Is.EqualTo(KVValueType.String));
                Assert.That(data["10"].ValueType, Is.EqualTo(KVValueType.String));
                Assert.That(data["11"].ValueType, Is.EqualTo(KVValueType.String));
                Assert.That(data["12"].ValueType, Is.EqualTo(KVValueType.String));
                Assert.That(data["13"].ValueType, Is.EqualTo(KVValueType.String));
                Assert.That(data["14"].ValueType, Is.EqualTo(KVValueType.String));
                Assert.That(data["15"].ValueType, Is.EqualTo(KVValueType.String));
                Assert.That(data["16"].ValueType, Is.EqualTo(KVValueType.String));
                Assert.That(data["17"].ValueType, Is.EqualTo(KVValueType.String));
                Assert.That(data["18"].ValueType, Is.EqualTo(KVValueType.String));
                Assert.That(data["19"].ValueType, Is.EqualTo(KVValueType.String));
            });
        }
    }
}
