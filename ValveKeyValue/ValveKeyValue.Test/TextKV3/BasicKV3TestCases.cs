using NUnit.Framework;

namespace ValveKeyValue.Test.TextKV3
{
    class BasicKV3TestCases
    {
        [Test]
        public void DeserializesHeaderAndValue()
        {
            using var stream = TestDataHelper.OpenResource("TextKV3.basic.kv3");
            var data = KVSerializer.Create(KVSerializationFormat.KeyValues3Text).Deserialize(stream);

            Assert.That((string)data["foo"], Is.EqualTo("bar"));
        }

        [Test]
        public void DeserializesFlaggedValues()
        {
            using var stream = TestDataHelper.OpenResource("TextKV3.flagged_value.kv3");
            var data = KVSerializer.Create(KVSerializationFormat.KeyValues3Text).Deserialize(stream);

            Assert.Multiple(() =>
            {
                Assert.That((string)data["foo"], Is.EqualTo("bar"));
                Assert.That((string)data["bar"], Is.EqualTo("foo"));
                Assert.That((string)data["multipleFlags"], Is.EqualTo("cool value"));
                Assert.That((long)data["flaggedNumber"], Is.EqualTo(-1234));
            });
        }

        [Test]
        public void DeserializesMultilineStrings()
        {
            using var stream = TestDataHelper.OpenResource("TextKV3.multiline.kv3");
            var data = KVSerializer.Create(KVSerializationFormat.KeyValues3Text).Deserialize(stream);

            Assert.That((string)data["multiLineStringValue"], Is.EqualTo("First line of a multi-line string literal.\nSecond line of a multi-line string literal."));
        }

        [Test]
        public void DeserializesMultilineStringsCRLF()
        {
            using var stream = TestDataHelper.OpenResource("TextKV3.multiline_crlf.kv3");
            var data = KVSerializer.Create(KVSerializationFormat.KeyValues3Text).Deserialize(stream);

            Assert.That((string)data["multiLineStringValue"], Is.EqualTo("First line of a multi-line string literal.\r\nSecond line of a multi-line string literal."));
        }

        [Test]
        public void DeserializesComments()
        {
            using var stream = TestDataHelper.OpenResource("TextKV3.comments.kv3");
            var data = KVSerializer.Create(KVSerializationFormat.KeyValues3Text).Deserialize(stream);

            Assert.Multiple(() =>
            {
                Assert.That((string)data["foo"], Is.EqualTo("bar"));
                Assert.That((string)data["one"], Is.EqualTo("1"));
                Assert.That((string)data["two"], Is.EqualTo("2"));
            });
        }

        [Test]
        public void DeserializesArray()
        {
            using var stream = TestDataHelper.OpenResource("TextKV3.array.kv3");
            var data = KVSerializer.Create(KVSerializationFormat.KeyValues3Text).Deserialize(stream);

            Assert.True(false);
        }

        [Test]
        public void DeserializesBasicTypes()
        {
            using var stream = TestDataHelper.OpenResource("TextKV3.types.kv3");
            var data = KVSerializer.Create(KVSerializationFormat.KeyValues3Text).Deserialize(stream);
            Assert.Multiple(() =>
            {
                Assert.That(data["boolFalseValue"].ValueType, Is.EqualTo(KVValueType.Boolean));
                Assert.That((bool)data["boolFalseValue"], Is.EqualTo(false));

                Assert.That(data["boolTrueValue"].ValueType, Is.EqualTo(KVValueType.Boolean));
                Assert.That((bool)data["boolTrueValue"], Is.EqualTo(true));

                Assert.That(data["nullValue"].ValueType, Is.EqualTo(KVValueType.Null));
                //Assert.That(data["nullValue"], Is.EqualTo(null));

                Assert.That(data["intValue"].ValueType, Is.EqualTo(KVValueType.UInt64));
                Assert.That((int)data["intValue"], Is.EqualTo(128));

                Assert.That(data["doubleValue"].ValueType, Is.EqualTo(KVValueType.FloatingPoint));
                Assert.That((double)data["doubleValue"], Is.EqualTo(64.123));

                Assert.That(data["negativeIntValue"].ValueType, Is.EqualTo(KVValueType.Int64));
                Assert.That((long)data["negativeIntValue"], Is.EqualTo(-1337));

                Assert.That(data["negativeDoubleValue"].ValueType, Is.EqualTo(KVValueType.FloatingPoint));
                Assert.That((double)data["negativeDoubleValue"], Is.EqualTo(-0.1337));

                Assert.That(data["plusIntValue"].ValueType, Is.EqualTo(KVValueType.UInt64));
                Assert.That((ulong)data["plusIntValue"], Is.EqualTo(+1337));

                Assert.That(data["plusDoubleValue"].ValueType, Is.EqualTo(KVValueType.FloatingPoint));
                Assert.That((double)data["plusDoubleValue"], Is.EqualTo(+0.1337));

                Assert.That(data["stringValue"].ValueType, Is.EqualTo(KVValueType.String));
                Assert.That((string)data["stringValue"], Is.EqualTo("hello world"));

                Assert.That(data["negativeMaxInt"].ValueType, Is.EqualTo(KVValueType.Int64));
                Assert.That((long)data["negativeMaxInt"], Is.EqualTo(-9223372036854775807));

                Assert.That(data["positiveMaxInt"].ValueType, Is.EqualTo(KVValueType.UInt64));
                Assert.That((ulong)data["positiveMaxInt"], Is.EqualTo(18446744073709551615));

                Assert.That(data["doubleMaxValue"].ValueType, Is.EqualTo(KVValueType.FloatingPoint));
                Assert.That((double)data["doubleMaxValue"], Is.EqualTo(62147483647.1337));

                Assert.That(data["doubleNegativeMaxValue"].ValueType, Is.EqualTo(KVValueType.FloatingPoint));
                Assert.That((double)data["doubleNegativeMaxValue"], Is.EqualTo(-62147483647.1337));

                Assert.That(data["doubleExponent"].ValueType, Is.EqualTo(KVValueType.FloatingPoint));
                Assert.That((double)data["doubleExponent"], Is.EqualTo(123.456));

                // TODO: Should this throw instead because strings need to be quoted? Or should it parse until it hits a non number like 123?
                Assert.That(data["intWithStringSuffix"].ValueType, Is.EqualTo(KVValueType.String));
                Assert.That((string)data["intWithStringSuffix"], Is.EqualTo("123foobar"));

                Assert.That(data["singleQuotes"].ValueType, Is.EqualTo(KVValueType.String));
                Assert.That((string)data["singleQuotes"], Is.EqualTo("string"));

                Assert.That(data["singleQuotesWithQuotesInside"].ValueType, Is.EqualTo(KVValueType.String));
                Assert.That((string)data["singleQuotesWithQuotesInside"], Is.EqualTo("string is \"pretty\" cool"));

                Assert.That(data["key_with._various.separators"].ValueType, Is.EqualTo(KVValueType.String));
                Assert.That((string)data["key_with._various.separators"], Is.EqualTo("test"));

                Assert.That(data["quoted key with : {} terminators"].ValueType, Is.EqualTo(KVValueType.String));
                Assert.That((string)data["quoted key with : {} terminators"], Is.EqualTo("test quoted key"));

                Assert.That(data["this is a multi\nline\nkey"].ValueType, Is.EqualTo(KVValueType.String));
                Assert.That((string)data["this is a multi\nline\nkey"], Is.EqualTo("multi line key parsed"));
            });
        }
    }
}
