using System.Collections;

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
            Assert.That((bool)kv.Value, Is.EqualTo(expected));
        }

        [TestCaseSource(nameof(CommonFailures))]
        [TestCase("true")]
        [TestCase("True")]
        [TestCase("false")]
        [TestCase("False")]
        public void BooleanFailure(string value)
        {
            var kv = new KVObject("aaa", value);
            Assert.That(() => (bool)kv.Value, Throws.Exception.TypeOf<FormatException>());
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
            Assert.That((byte)kv.Value, Is.EqualTo(expected));
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
            Assert.That(() => (byte)kv.Value, Throws.Exception.TypeOf<FormatException>().Or.TypeOf<OverflowException>());
        }

        [TestCase("a")]
        [TestCase("B")]
        [TestCase("C")]
        public void CharSuccess(string value)
        {
            var kv = new KVObject("aaa", value);
            Assert.That((char)kv.Value, Is.EqualTo(value[0]));
        }

        [TestCaseSource(nameof(CommonFailures))]
        [TestCase("..")]
        public void CharFailure(string value)
        {
            var kv = new KVObject("aaa", value);
            Assert.That(() => (char)kv.Value, Throws.Exception.TypeOf<FormatException>());
        }

        [TestCase("123", (short)123)]
        [TestCase("0", (short)0)]
        [TestCase("1", (short)1)]
        [TestCase("32767", (short)32767)]
        [TestCase("-123", -123)]
        public void Int16Success(string value, short expected)
        {
            var kv = new KVObject("aaa", value);
            Assert.That((short)kv.Value, Is.EqualTo(expected));
        }

        [TestCaseSource(nameof(CommonFailures))]
        [TestCase("65535")]
        [TestCase("123456789123456789")]
        [TestCase("4294967295")]
        [TestCase("76561197960265729")]
        public void Int16Failure(string value)
        {
            var kv = new KVObject("aaa", value);
            Assert.That(() => (short)kv.Value, Throws.Exception.TypeOf<FormatException>().Or.TypeOf<OverflowException>());
        }

        [TestCase("123", 123)]
        [TestCase("-123", -123)]
        [TestCase("0", 0)]
        [TestCase("1", 1)]
        public void Int32Success(string value, int expected)
        {
            var kv = new KVObject("aaa", value);
            Assert.That((int)kv.Value, Is.EqualTo(expected));
        }

        [TestCaseSource(nameof(CommonFailures))]
        [TestCase("4294967295")]
        public void Int32Failure(string value)
        {
            var kv = new KVObject("aaa", value);
            Assert.That(() => (int)kv.Value, Throws.Exception.TypeOf<FormatException>().Or.TypeOf<OverflowException>());
        }

        [TestCase("123", 123L)]
        [TestCase("0", 0L)]
        [TestCase("1", 1L)]
        [TestCase("4294967295", 4294967295L)]
        [TestCase("76561197960265729", 76561197960265729L)]
        [TestCase("-123", -123)]
        public void Int64Success(string value, long expected)
        {
            var kv = new KVObject("aaa", value);
            Assert.That((long)kv.Value, Is.EqualTo(expected));
        }

        [TestCaseSource(nameof(CommonFailures))]
        [TestCase("123456789123456789012345678")]
        public void Int64Failure(string value)
        {
            var kv = new KVObject("aaa", value);
            Assert.That(() => (long)kv.Value, Throws.Exception.TypeOf<FormatException>().Or.TypeOf<OverflowException>());
        }

        [TestCase("0", 0)]
        [TestCase("-0", 0)]
        [TestCase("00", 0)]
        [TestCase("-128", -128)]
        [TestCase("127", 127)]
        public void SByteSuccess(string value, sbyte expected)
        {
            var kv = new KVObject("aaa", value);
            Assert.That((sbyte)kv.Value, Is.EqualTo(expected));
        }

        [TestCaseSource(nameof(CommonFailures))]
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
            Assert.That(() => (sbyte)kv.Value, Throws.Exception.TypeOf<FormatException>().Or.TypeOf<OverflowException>());
        }

        [TestCase("123", (ushort)123)]
        [TestCase("0", (ushort)0)]
        [TestCase("1", (ushort)1)]
        [TestCase("65535", (ushort)65535)]
        public void UInt16Success(string value, ushort expected)
        {
            var kv = new KVObject("aaa", value);
            Assert.That((ushort)kv.Value, Is.EqualTo(expected));
        }

        [TestCaseSource(nameof(CommonFailures))]
        [TestCase("123456789123456789")]
        [TestCase("-123")]
        [TestCase("4294967295")]
        [TestCase("76561197960265729")]
        public void UInt16Failure(string value)
        {
            var kv = new KVObject("aaa", value);
            Assert.That(() => (ushort)kv.Value, Throws.Exception.TypeOf<FormatException>().Or.TypeOf<OverflowException>());
        }

        [TestCase("123", 123U)]
        [TestCase("0", 0U)]
        [TestCase("1", 1U)]
        [TestCase("4294967295", 4294967295U)]
        public void UInt32Success(string value, uint expected)
        {
            var kv = new KVObject("aaa", value);
            Assert.That((uint)kv.Value, Is.EqualTo(expected));
        }

        [TestCaseSource(nameof(CommonFailures))]
        [TestCase("123456789123456789")]
        [TestCase("-123")]
        [TestCase("76561197960265729")]
        public void UInt32Failure(string value)
        {
            var kv = new KVObject("aaa", value);
            Assert.That(() => (uint)kv.Value, Throws.Exception.TypeOf<FormatException>().Or.TypeOf<OverflowException>());
        }

        [TestCase("123", 123UL)]
        [TestCase("0", 0UL)]
        [TestCase("1", 1UL)]
        [TestCase("4294967295", 4294967295UL)]
        [TestCase("76561197960265729", 76561197960265729UL)]
        public void UInt64Success(string value, ulong expected)
        {
            var kv = new KVObject("aaa", value);
            Assert.That((ulong)kv.Value, Is.EqualTo(expected));
        }

        [TestCaseSource(nameof(CommonFailures))]
        [TestCase("123456789123456789012345678")]
        [TestCase("-123")]
        public void UInt64Failure(string value)
        {
            var kv = new KVObject("aaa", value);
            Assert.That(() => (ulong)kv.Value, Throws.Exception.TypeOf<FormatException>().Or.TypeOf<OverflowException>());
        }

        [TestCase("3.5", 3.5f)]
        [TestCase("0", 0f)]
        [TestCase("1", 1f)]
        [TestCase("1.7976931348623157", 1.7976931348623157f)]
        [TestCase("-98765432.109", -98765432.109f)]
        public void FloatSuccess(string value, float expected)
        {
            var kv = new KVObject("aaa", value);
            Assert.That((float)kv.Value, Is.EqualTo(expected));
        }

        [TestCase("3.5", 3.5D)]
        [TestCase("0", 0D)]
        [TestCase("1", 1D)]
        [TestCase("1.7976931348623157", 1.7976931348623157D)]
        [TestCase("-98765432.109", -98765432.109D)]
        public void DoubleSuccess(string value, double expected)
        {
            var kv = new KVObject("aaa", value);
            Assert.That((double)kv.Value, Is.EqualTo(expected));
        }

        [TestCase]
        public void DecimalSuccess()
        {
            var kv = new KVObject("aaa", "79228162514264337593543950335");
            Assert.That((decimal)kv.Value, Is.EqualTo(79228162514264337593543950335m));

            kv = new KVObject("aaa", "1500000");
            Assert.That((decimal)kv.Value, Is.EqualTo(1.5E6m));

            kv = new KVObject("aaa", "-1500000");
            Assert.That((decimal)kv.Value, Is.EqualTo(-1.5E6m));
        }

        static IEnumerable CommonFailures
        {
            get
            {
                yield return string.Empty;
                yield return "abc";
                yield return "zero";
                yield return "one";
                yield return "ZER0";
                yield return "2wo";
            }
        }

        [TestCase(typeof(bool))]
        [TestCase(typeof(byte))]
        [TestCase(typeof(char))]
        [TestCase(typeof(DateTime))]
        [TestCase(typeof(decimal))]
        [TestCase(typeof(double))]
        [TestCase(typeof(float))]
        [TestCase(typeof(int))]
        [TestCase(typeof(long))]
        [TestCase(typeof(uint))]
        [TestCase(typeof(ulong))]
        [TestCase(typeof(ushort))]
        [TestCase(typeof(sbyte))]
        [TestCase(typeof(short))]
        public void ConvertingObjectWithChildrenIsNotSupported(Type type)
        {
            var kv = new KVObject(
                "aaa",
                [new KVObject("bbb", "ccc")]);

            Assert.That(
                () => Convert.ChangeType(kv.Value, type),
                Throws.Exception.TypeOf<NotSupportedException>());
        }

        [TestCase(typeof(string), ExpectedResult = "[Collection]")]
        public object ConvertingObjectWithChildren(Type type)
        {
            var kv = new KVObject(
                "aaa",
                [new KVObject("bbb", "ccc")]);

            return Convert.ChangeType(kv.Value, type);
        }
    }
}
