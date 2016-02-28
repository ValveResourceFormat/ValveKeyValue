using System;

using NUnit.Framework;

namespace ValveKeyValue.Test
{
    class KVObjectCastTestCase
    {
        [TestCase("1", true)]
        [TestCase("01", true)]
        [TestCase("001", true)]
        [TestCase("0", false)]
        [TestCase("00", false)]
        public void BooleanSuccess(string value, bool expected)
        {
            var kv = new KVObject("aaa", value);
            Assert.That((bool)kv, Is.EqualTo(expected));
        }

        [TestCase("")]
        [TestCase("abc")]
        [TestCase("true")]
        [TestCase("True")]
        [TestCase("false")]
        [TestCase("False")]
        public void BooleanFailure(string value)
        {
            var kv = new KVObject("aaa", value);
            Assert.That(() => (bool)kv, Throws.Exception.TypeOf<FormatException>());
        }

        [TestCase("123", 123)]
        [TestCase("-123", -123)]
        [TestCase("0", 0)]
        [TestCase("1", 1)]
        public void Int32Success(string value, int expected)
        {
            var kv = new KVObject("aaa", value);
            Assert.That((int)kv, Is.EqualTo(expected));
        }

        [TestCase("")]
        [TestCase("abc")]
        [TestCase("zero")]
        [TestCase("one")]
        [TestCase("ZER0")]
        [TestCase("2wo")]
        public void Int32Failure(string value)
        {
            var kv = new KVObject("aaa", value);
            Assert.That(() => (int)kv, Throws.Exception.TypeOf<FormatException>());
        }
    }
}
