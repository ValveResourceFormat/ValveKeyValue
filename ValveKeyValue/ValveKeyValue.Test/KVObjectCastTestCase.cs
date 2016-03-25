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

        [TestCase("0", 0)]
        [TestCase("-0", 0)]
        [TestCase("00", 0)]
        [TestCase("128", 128)]
        [TestCase("012", 12)]
        [TestCase("255", 255)]
        public void ByteSuccess(string value, byte expected)
        {
            var kv = new KVObject("aaa", value);
            Assert.That((byte)kv, Is.EqualTo(expected));
        }

        [TestCase("")]
        [TestCase("abc")]
        [TestCase("-129")]
        [TestCase("-1")]
        [TestCase("256")]
        [TestCase("0256")]
        [TestCase("abyte")]
        [TestCase("0x123")]
        public void ByteFailure(string value)
        {
            var kv = new KVObject("aaa", value);
            Assert.That(() => (byte)kv, Throws.Exception.TypeOf<FormatException>().Or.TypeOf<OverflowException>());
        }

        [TestCase("a")]
        [TestCase("B")]
        [TestCase("C")]
        public void CharSuccess(string value)
        {
            var kv = new KVObject("aaa", value);
            Assert.That((char)kv, Is.EqualTo(value[0]));
        }

        [TestCase("")]
        [TestCase("abc")]
        [TestCase("..")]
        public void CharFailure(string value)
        {
            var kv = new KVObject("aaa", value);
            Assert.That(() => (char)kv, Throws.Exception.TypeOf<FormatException>());
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
        [TestCase("4294967295")]
        public void Int32Failure(string value)
        {
            var kv = new KVObject("aaa", value);
            Assert.That(() => (int)kv, Throws.Exception.TypeOf<FormatException>().Or.TypeOf<OverflowException>());
        }

        [TestCase("0", 0)]
        [TestCase("-0", 0)]
        [TestCase("00", 0)]
        [TestCase("-128", -128)]
        [TestCase("127", 127)]
        public void SByteSuccess(string value, sbyte expected)
        {
            var kv = new KVObject("aaa", value);
            Assert.That((sbyte)kv, Is.EqualTo(expected));
        }

        [TestCase("")]
        [TestCase("abc")]
        [TestCase("-129")]
        [TestCase("128")]
        [TestCase("256")]
        [TestCase("0256")]
        [TestCase("abyte")]
        [TestCase("0x123")]
        [TestCase("255")]
        public void SByteFailure(string value)
        {
            var kv = new KVObject("aaa", value);
            Assert.That(() => (sbyte)kv, Throws.Exception.TypeOf<FormatException>().Or.TypeOf<OverflowException>());
        }

        [TestCase("123", 123U)]
        [TestCase("0", 0U)]
        [TestCase("1", 1U)]
        [TestCase("4294967295", 4294967295U)]
        public void UInt32Success(string value, uint expected)
        {
            var kv = new KVObject("aaa", value);
            Assert.That((uint)kv, Is.EqualTo(expected));
        }

        [TestCase("")]
        [TestCase("abc")]
        [TestCase("zero")]
        [TestCase("one")]
        [TestCase("ZER0")]
        [TestCase("2wo")]
        [TestCase("123456789123456789")]
        [TestCase("-123")]
        [TestCase("76561197960265729")]
        public void UInt32Failure(string value)
        {
            var kv = new KVObject("aaa", value);
            Assert.That(() => (uint)kv, Throws.Exception.TypeOf<FormatException>().Or.TypeOf<OverflowException>());
        }

        [TestCase("123", 123UL)]
        [TestCase("0", 0UL)]
        [TestCase("1", 1UL)]
        [TestCase("4294967295", 4294967295UL)]
        [TestCase("76561197960265729", 76561197960265729UL)]
        public void UInt64Success(string value, ulong expected)
        {
            var kv = new KVObject("aaa", value);
            Assert.That((ulong)kv, Is.EqualTo(expected));
        }

        [TestCase("")]
        [TestCase("abc")]
        [TestCase("zero")]
        [TestCase("one")]
        [TestCase("ZER0")]
        [TestCase("2wo")]
        [TestCase("123456789123456789012345678")]
        [TestCase("-123")]
        public void UInt64Failure(string value)
        {
            var kv = new KVObject("aaa", value);
            Assert.That(() => (ulong)kv, Throws.Exception.TypeOf<FormatException>().Or.TypeOf<OverflowException>());
        }
    }
}
