using System;
using NUnit.Framework;

namespace ValveKeyValue.Test;

public class NoTypeGuessingTestCase
{
    private KVObject _data;

    [OneTimeSetUp]
    public void SetUp()
    {
        using var stream = TestDataHelper.OpenResource("Text.type_guessing.vdf");
        _data = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(stream, new KVSerializerOptions
        {
            DisableTypeGuessing = true,
        });
    }

    [Test]
    public void IsNotNull()
    {
        Assert.That(_data, Is.Not.Null);
    }

    [TestCase("string", "123foo")]
    [TestCase("bigint", "0x0000000000000001")]
    [TestCase("gaben_steamid", "0x01100001000056bA")]
    [TestCase("float", "123.456")]
    [TestCase("float_exp", "1.23456E+2")]
    [TestCase("int", "1234")]
    [TestCase("negint", "-1234")]
    public void HasValueOfType<TExpected>(string key, TExpected expectedValue)
    {
        var actualValue = _data[key];

        Assert.That(actualValue, Is.Not.Null);
        Assert.That(actualValue.ValueType, Is.EqualTo(KVValueType.String), nameof(KVValueType));
        Assert.That(actualValue.GetTypeCode(), Is.EqualTo(TypeCode.String), nameof(TypeCode));

        var typedActualValue = Convert.ChangeType(actualValue, typeof(TExpected));
        Assert.That(typedActualValue, Is.EqualTo(expectedValue));
    }
}
