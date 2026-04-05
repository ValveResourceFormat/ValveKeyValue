using System.Globalization;
using System.Text;

namespace ValveKeyValue.Test.TextKV3
{
    class BasicKV3TestCases
    {
        private static readonly string[] ExpectedNames = ["a", "b", "c"];
        private static readonly int[] ExpectedIntValues = [10, 20, 30];
        private static readonly byte[] ExpectedBlobData =
        [
            0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77,
            0x88, 0x99, 0xAA, 0xBB, 0xCC, 0xDD, 0xFF
        ];

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

            using (Assert.EnterMultipleScope())
            {
                Assert.That(data["foo"].Flag, Is.EqualTo(KVFlag.Resource));
                Assert.That((string)data["foo"], Is.EqualTo("bar"));

                Assert.That(data["bar"].Flag, Is.EqualTo(KVFlag.Resource));
                Assert.That((string)data["bar"], Is.EqualTo("foo"));

                Assert.That(data["multipleFlags"].Flag, Is.EqualTo(KVFlag.SubClass));
                Assert.That((string)data["multipleFlags"], Is.EqualTo("cool value"));

                Assert.That(data["flaggedNumber"].Flag, Is.EqualTo(KVFlag.Panorama));
                Assert.That((long)data["flaggedNumber"], Is.EqualTo(-1234));

                Assert.That(data["soundEvent"].Flag, Is.EqualTo(KVFlag.SoundEvent));
                Assert.That((string)data["soundEvent"], Is.EqualTo("event sound"));

                Assert.That(data["noFlags"].Flag, Is.EqualTo(KVFlag.None));
                Assert.That((long)data["noFlags"], Is.EqualTo(5));

                Assert.That(data["flaggedObject"].Flag, Is.EqualTo(KVFlag.Panorama));
                Assert.That(data["flaggedObject"]["1"].Flag, Is.EqualTo(KVFlag.SoundEvent));
                Assert.That(data["flaggedObject"]["2"].Flag, Is.EqualTo(KVFlag.None));
                Assert.That(data["flaggedObject"]["3"].Flag, Is.EqualTo(KVFlag.SubClass));
                Assert.That(data["flaggedObject"]["4"].Flag, Is.EqualTo(KVFlag.ResourceName));
            }
        }

        [Test]
        public void DeserializesMultilineStrings()
        {
            using var stream = TestDataHelper.OpenResource("TextKV3.multiline.kv3");
            var data = KVSerializer.Create(KVSerializationFormat.KeyValues3Text).Deserialize(stream);

            using (Assert.EnterMultipleScope())
            {
                Assert.That((string)data["multiLineStringValue"], Is.EqualTo("First line of a multi-line string literal.\nSecond line of a multi-line string literal."));
                Assert.That((string)data["multiLineWithQuotesInside"], Is.EqualTo("hmm this \\\"\"\"is awkward\n\\\"\"\" yes"));
                Assert.That((string)data["singleQuotesButWithNewLineAnyway"], Is.EqualTo("hello\nvalve"));
            }
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

            using (Assert.EnterMultipleScope())
            {
                Assert.That((string)data["foo"], Is.EqualTo("bar"));
                Assert.That((string)data["one"], Is.EqualTo("1"));
                Assert.That((string)data["two"], Is.EqualTo("2"));
            }
        }

        [Test]
        public void DeserializesArray()
        {
            using var stream = TestDataHelper.OpenResource("TextKV3.array.kv3");
            var data = KVSerializer.Create(KVSerializationFormat.KeyValues3Text).Deserialize(stream);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(data["arrayValue"].ValueType, Is.EqualTo(KVValueType.Array));
                Assert.That(data["arrayOnSingleLine"].ValueType, Is.EqualTo(KVValueType.Array));
                Assert.That(data["arrayNoSpace"].ValueType, Is.EqualTo(KVValueType.Array));
                Assert.That(data["arrayMixedTypes"].ValueType, Is.EqualTo(KVValueType.Array));
            }

            var arrayValue = data["arrayValue"];

            Assert.That(arrayValue, Has.Count.EqualTo(2));
            using (Assert.EnterMultipleScope())
            {
                Assert.That(arrayValue[0].ToString(CultureInfo.InvariantCulture), Is.EqualTo("a"));
                Assert.That(arrayValue[1].ToString(CultureInfo.InvariantCulture), Is.EqualTo("b"));
            }

            // TODO: Test all the children values
        }

        [Test]
        public void DeserializesBinaryBlob()
        {
            using var stream = TestDataHelper.OpenResource("TextKV3.binary_blob.kv3");
            var data = KVSerializer.Create(KVSerializationFormat.KeyValues3Text).Deserialize(stream);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(data["array"].ValueType, Is.EqualTo(KVValueType.BinaryBlob));
                Assert.That(data["array"].AsBlob(), Is.EqualTo(ExpectedBlobData));
            }
        }

        [Test]
        public void DeserializesBinaryBlobToTypedByteArray()
        {
            using var stream = TestDataHelper.OpenResource("TextKV3.binary_blob.kv3");
            var data = KVSerializer.Create(KVSerializationFormat.KeyValues3Text).Deserialize<TypedBlobData>(stream);

            Assert.That(data.Array, Is.Not.Null);
            Assert.That(data.Array, Is.EqualTo(ExpectedBlobData));
        }

        [Test]
        public void DeserializesNestedObject()
        {
            using var stream = TestDataHelper.OpenResource("TextKV3.object.kv3");
            var data = KVSerializer.Create(KVSerializationFormat.KeyValues3Text).Deserialize(stream);

            Assert.That((string)data["a"]["b"]["c"], Is.EqualTo("d"));
        }

        [Test]
        public void DeserializesEntityNameFlag()
        {
            using var stream = TestDataHelper.OpenResource("TextKV3.entity_name.kv3");
            var data = KVSerializer.Create(KVSerializationFormat.KeyValues3Text).Deserialize(stream);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(data["name"].Flag, Is.EqualTo(KVFlag.EntityName));
                Assert.That((string)data["name"], Is.EqualTo("some_entity"));
            }
        }

        [Test]
        public void DeserializesEscapeSequences()
        {
            using var stream = TestDataHelper.OpenResource("TextKV3.escape_sequences.kv3");
            var data = KVSerializer.Create(KVSerializationFormat.KeyValues3Text).Deserialize(stream);

            using (Assert.EnterMultipleScope())
            {
                Assert.That((string)data["newline"], Is.EqualTo("hello\nworld"));
                Assert.That((string)data["tab"], Is.EqualTo("hello\tworld"));
                Assert.That((string)data["backslash"], Is.EqualTo("hello\\world"));
                Assert.That((string)data["quote"], Is.EqualTo("hello\"world"));
                Assert.That((string)data["combined"], Is.EqualTo("line1\nline2\ttab\\slash\"quote"));
            }
        }

        [Test]
        public void DeserializesBasicTypes()
        {
            using var stream = TestDataHelper.OpenResource("TextKV3.types.kv3");
            var data = KVSerializer.Create(KVSerializationFormat.KeyValues3Text).Deserialize(stream).Root;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(data["boolFalseValue"].ValueType, Is.EqualTo(KVValueType.Boolean));
                Assert.That((bool)data["boolFalseValue"], Is.False);

                Assert.That(data["boolTrueValue"].ValueType, Is.EqualTo(KVValueType.Boolean));
                Assert.That((bool)data["boolTrueValue"], Is.True);

                Assert.That(data["nullValue"].ValueType, Is.EqualTo(KVValueType.Null));

                Assert.That(data["intValue"].ValueType, Is.EqualTo(KVValueType.UInt64));
                Assert.That((int)data["intValue"], Is.EqualTo(128));

                Assert.That(data["doubleValue"].ValueType, Is.EqualTo(KVValueType.FloatingPoint64));
                Assert.That((double)data["doubleValue"], Is.EqualTo(64.123));

                Assert.That(data["negativeIntValue"].ValueType, Is.EqualTo(KVValueType.Int64));
                Assert.That((long)data["negativeIntValue"], Is.EqualTo(-1337));

                Assert.That(data["negativeDoubleValue"].ValueType, Is.EqualTo(KVValueType.FloatingPoint64));
                Assert.That((double)data["negativeDoubleValue"], Is.EqualTo(-0.1337));

                Assert.That(data["plusIntValue"].ValueType, Is.EqualTo(KVValueType.UInt64));
                Assert.That((ulong)data["plusIntValue"], Is.EqualTo(+1337));

                Assert.That(data["plusDoubleValue"].ValueType, Is.EqualTo(KVValueType.FloatingPoint64));
                Assert.That((double)data["plusDoubleValue"], Is.EqualTo(+0.1337));

                Assert.That(data["stringValue"].ValueType, Is.EqualTo(KVValueType.String));
                Assert.That((string)data["stringValue"], Is.EqualTo("hello world"));

                Assert.That(data["negativeMaxInt"].ValueType, Is.EqualTo(KVValueType.Int64));
                Assert.That((long)data["negativeMaxInt"], Is.EqualTo(-9223372036854775807));

                Assert.That(data["positiveMaxInt"].ValueType, Is.EqualTo(KVValueType.UInt64));
                Assert.That((ulong)data["positiveMaxInt"], Is.EqualTo(18446744073709551615));

                Assert.That(data["doubleMaxValue"].ValueType, Is.EqualTo(KVValueType.FloatingPoint64));
                Assert.That((double)data["doubleMaxValue"], Is.EqualTo(62147483647.1337));

                Assert.That(data["doubleNegativeMaxValue"].ValueType, Is.EqualTo(KVValueType.FloatingPoint64));
                Assert.That((double)data["doubleNegativeMaxValue"], Is.EqualTo(-62147483647.1337));

                Assert.That(data["doubleExponent"].ValueType, Is.EqualTo(KVValueType.FloatingPoint64));
                Assert.That((double)data["doubleExponent"], Is.EqualTo(123.456));

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

                Assert.That(data["empty.string"].ValueType, Is.EqualTo(KVValueType.String));
                Assert.That((string)data["empty.string"], Is.EqualTo(string.Empty));
            }
        }

        [Test]
        public void DeserializesArrayToTypedIntList()
        {
            var kv3Text = "<!-- kv3 encoding:text:version{e21c7f3c-8a33-41c5-9977-a76d3a32aa0d} format:generic:version{7412167c-06e9-4698-aff2-e63eb59037e7} -->\n{\n\tnumbers = [1, 2, 3]\n}";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(kv3Text));
            var data = KVSerializer.Create(KVSerializationFormat.KeyValues3Text).Deserialize<TypedArrayData>(stream);

            Assert.That(data.Numbers, Is.Not.Null);
            Assert.That(data.Numbers, Has.Count.EqualTo(3));
            using (Assert.EnterMultipleScope())
            {
                Assert.That(data.Numbers[0], Is.EqualTo(1));
                Assert.That(data.Numbers[1], Is.EqualTo(2));
                Assert.That(data.Numbers[2], Is.EqualTo(3));
            }
        }

        [Test]
        public void DeserializesArrayToTypedStringArray()
        {
            var kv3Text = "<!-- kv3 encoding:text:version{e21c7f3c-8a33-41c5-9977-a76d3a32aa0d} format:generic:version{7412167c-06e9-4698-aff2-e63eb59037e7} -->\n{\n\tnames = [\"a\", \"b\", \"c\"]\n}";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(kv3Text));
            var data = KVSerializer.Create(KVSerializationFormat.KeyValues3Text).Deserialize<TypedStringArrayData>(stream);

            Assert.That(data.Names, Is.Not.Null);
            Assert.That(data.Names, Has.Length.EqualTo(3));
            Assert.That(data.Names, Is.EqualTo(ExpectedNames));
        }

        [Test]
        public void DeserializesArrayToTypedIntArray()
        {
            var kv3Text = "<!-- kv3 encoding:text:version{e21c7f3c-8a33-41c5-9977-a76d3a32aa0d} format:generic:version{7412167c-06e9-4698-aff2-e63eb59037e7} -->\n{\n\tvalues = [10, 20, 30]\n}";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(kv3Text));
            var data = KVSerializer.Create(KVSerializationFormat.KeyValues3Text).Deserialize<TypedIntArrayData>(stream);

            Assert.That(data.Values, Is.Not.Null);
            Assert.That(data.Values, Has.Length.EqualTo(3));
            Assert.That(data.Values, Is.EqualTo(ExpectedIntValues));
        }

        [Test]
        public void OddLengthHexBlobThrows()
        {
            var kv3Text = "<!-- kv3 encoding:text:version{e21c7f3c-8a33-41c5-9977-a76d3a32aa0d} format:generic:version{7412167c-06e9-4698-aff2-e63eb59037e7} -->\n#[ AB C ]";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(kv3Text));

            Assert.That(
                () => KVSerializer.Create(KVSerializationFormat.KeyValues3Text).Deserialize(stream),
                Throws.Exception.TypeOf<InvalidDataException>());
        }

        [Test]
        public void UnterminatedBlockCommentThrows()
        {
            var kv3Text = "<!-- kv3 encoding:text:version{e21c7f3c-8a33-41c5-9977-a76d3a32aa0d} format:generic:version{7412167c-06e9-4698-aff2-e63eb59037e7} -->\n{ /* unterminated comment\n}";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(kv3Text));

            Assert.That(
                () => KVSerializer.Create(KVSerializationFormat.KeyValues3Text).Deserialize(stream),
                Throws.Exception);
        }

#pragma warning disable CA1812 // Avoid uninstantiated internal classes - used by deserializer
        class TypedArrayData
        {
            public required List<int> Numbers { get; set; }
        }

        class TypedStringArrayData
        {
            public required string[] Names { get; set; }
        }

        class TypedIntArrayData
        {
            public required int[] Values { get; set; }
        }

        class TypedBlobData
        {
            public required byte[] Array { get; set; }
        }
#pragma warning restore CA1812
    }
}
