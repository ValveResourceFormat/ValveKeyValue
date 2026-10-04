using System.Globalization;
using System.Linq;
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
        private static readonly string[] ExpectedQuotedLiterals = ["42", "true", "null", "1.5", "nan"];
        private static readonly int[] ExpectedCommentedArray = [1, 2];
        private static readonly string[] ExpectedMultilineArray = ["x", "y"];
        private static readonly KVValueType[] ExpectedMixedArrayTypes =
        [
            KVValueType.UInt64, KVValueType.String, KVValueType.Array, KVValueType.Collection, KVValueType.BinaryBlob,
            KVValueType.Null, KVValueType.Boolean, KVValueType.FloatingPoint64, KVValueType.Int64,
        ];

        [Test]
        public void DeserializesHeaderAndValue()
        {
            using var stream = TestDataHelper.OpenResource("TextKV3.basic.kv3");
            var data = KVSerializer.Create(KVSerializationFormat.KeyValues3Text).Deserialize(stream);

            Assert.That((string)data["foo"], Is.EqualTo("bar"));
        }

        [Test]
        public void QuotedFlagPrefixThrows()
        {
            const string text = "<!-- kv3 encoding:text:version{e21c7f3c-8a33-41c5-9977-a76d3a32aa0d} format:generic:version{7412167c-06e9-4698-aff2-e63eb59037e7} -->\n{\n\tfoo = \"resource\":\"path/to/file.vmdl\"\n}\n";

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));
            Assert.Throws<KeyValueException>(() => KVSerializer.Create(KVSerializationFormat.KeyValues3Text).Deserialize(stream));
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
                Assert.That((string)data["multiLineWithQuotesInside"], Is.EqualTo("hmm this \"\"\"is awkward\n\"\"\" yes"));
                Assert.That((string)data["singleQuotesButWithNewLineAnyway"], Is.EqualTo("hello\nvalve"));
            }
        }

        [Test]
        public void DeserializesMultilineStringsCRLF()
        {
            using var stream = TestDataHelper.OpenResource("TextKV3.multiline_crlf.kv3");
            var data = KVSerializer.Create(KVSerializationFormat.KeyValues3Text).Deserialize(stream);

            Assert.That((string)data["multiLineStringValue"], Is.EqualTo("First line of a multi-line string literal.\nSecond line of a multi-line string literal."));
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
        public void DeserializesLocalizeFlag()
        {
            using var stream = TestDataHelper.OpenResource("TextKV3.localize.kv3");
            var data = KVSerializer.Create(KVSerializationFormat.KeyValues3Text).Deserialize(stream);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(data["title"].Flag, Is.EqualTo(KVFlag.Localize));
                Assert.That((string)data["title"], Is.EqualTo("#SFUI_Title"));
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
                Throws.Exception.TypeOf<KeyValueException>());
        }

        [Test]
        public void InvalidHexBlobThrows()
        {
            var kv3Text = "<!-- kv3 encoding:text:version{e21c7f3c-8a33-41c5-9977-a76d3a32aa0d} format:generic:version{7412167c-06e9-4698-aff2-e63eb59037e7} -->\n#[ AB ZZ ]";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(kv3Text));

            Assert.That(
                () => KVSerializer.Create(KVSerializationFormat.KeyValues3Text).Deserialize(stream),
                Throws.Exception.TypeOf<KeyValueException>());
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

        [Test]
        public void QuotedLiteralsAreStrings()
        {
            var data = TestDataHelper.ParseKV3Text("{ a = \"42\" b = \"true\" c = \"null\" d = '1.5' e = \"nan\" }");

            Assert.That(data.Root.Values.Select(x => x.ValueType), Is.All.EqualTo(KVValueType.String));
            Assert.That(data.Root.Values.Select(x => (string)x), Is.EqualTo(ExpectedQuotedLiterals));
        }

        [TestCase("resource:\"x\"")]
        [TestCase("resource|\"x\"")]
        [TestCase("resource : \"x\"")]
        [TestCase("resource\t|\t\"x\"")]
        [TestCase("resource\n|\n\"x\"")]
        [TestCase("resource:\n\"x\"")]
        [TestCase("resource: /* c */ \"x\"")]
        [TestCase("RESOURCE:\"x\"")]
        [TestCase("Resource | \"x\"")]
        [TestCase("resource:\"\"\"\nx\n\"\"\"")]
        public void DeserializesFlagSyntax(string text)
        {
            var data = TestDataHelper.ParseKV3Text($"{{ a = {text} }}");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(data["a"].Flag, Is.EqualTo(KVFlag.Resource));
                Assert.That((string)data["a"], Is.EqualTo("x"));
            }
        }

        [Test]
        public void DeserializesNestedFlags()
        {
            var data = TestDataHelper.ParseKV3Text("{ a = subclass: { b = [ panorama : \"p\", entity_name:\"e\", \"plain\" ] } c = [ subclass:{ d = 2 }, { e = 3 } ] }");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(data["a"].Flag, Is.EqualTo(KVFlag.SubClass));
                Assert.That(data["a"]["b"][0].Flag, Is.EqualTo(KVFlag.Panorama));
                Assert.That(data["a"]["b"][1].Flag, Is.EqualTo(KVFlag.EntityName));
                Assert.That(data["a"]["b"][2].Flag, Is.EqualTo(KVFlag.None));
                Assert.That(data["c"][0].Flag, Is.EqualTo(KVFlag.SubClass));
                Assert.That(data["c"][1].Flag, Is.EqualTo(KVFlag.None));
            }
        }

        [TestCase("#[ 01 02 ]", new byte[] { 1, 2 })]
        [TestCase("# [ 01 02 ]", new byte[] { 1, 2 })]
        [TestCase("#[ 01 /* x */ 02 ]", new byte[] { 1, 2 })]
        [TestCase("# /* c */ [ 01 // x\n 02 /* y */ 03 ]", new byte[] { 1, 2, 3 })]
        [TestCase("#[ aB Cd ef ]", new byte[] { 0xAB, 0xCD, 0xEF })]
        [TestCase("#[]", new byte[0])]
        [TestCase("#[ ]", new byte[0])]
        [TestCase("#[\n]", new byte[0])]
        public void DeserializesBinaryBlobSyntax(string text, byte[] expected)
        {
            var data = TestDataHelper.ParseKV3Text($"{{ a = {text} }}");

            Assert.That(data["a"].AsBlob(), Is.EqualTo(expected));
        }

        [TestCase("\n")]
        [TestCase("\r\n")]
        public void LineCommentContinuesAfterBackslash(string newline)
        {
            var data = TestDataHelper.ParseKV3Text($"{{{newline}\t// comment \\{newline}\tb = 2{newline}\tc = 3{newline}}}");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(data.Root.ContainsKey("b"), Is.False);
                Assert.That((int)data["c"], Is.EqualTo(3));
            }
        }

        [Test]
        public void DeserializesCommentsBetweenTokens()
        {
            var data = TestDataHelper.ParseKV3Text("{ /*a*/ a /*b*/ = /*c*/ 1 /*d*/ b = [ /*e*/ 1 /*f*/ , /*g*/ 2 /*h*/ ] /*i*/ c // x\n = 3 }");

            using (Assert.EnterMultipleScope())
            {
                Assert.That((int)data["a"], Is.EqualTo(1));
                Assert.That(data["b"].Values.Select(x => (int)x), Is.EqualTo(ExpectedCommentedArray));
                Assert.That((int)data["c"], Is.EqualTo(3));
            }
        }

        [TestCase("\"\"\"\nabc\"\"\" b = 1\n\"\"\"", "abc\"\"\" b = 1")]
        [TestCase("\"\"\"\nline\\\n\"\"\"\nstill\n\"\"\"", "line\\\n\"\"\"\nstill")]
        [TestCase("\"\"\"\n\\\"\"\"x\n\"\"\"", "\"\"\"x")]
        [TestCase("\"\"\"\nx \\\"\"\" y\n\"\"\"", "x \"\"\" y")]
        [TestCase("\"\"\"\r\nline1\r\nline2\r\n\"\"\"", "line1\nline2")]
        [TestCase("\"\"\"\nx\r\ny\r\n\"\"\"", "x\ny")]
        [TestCase("\"\"\"\n\"\"\"", "")]
        [TestCase("\"\"\"\r\n\"\"\"", "")]
        [TestCase("\"\"\"\n\n\"\"\"", "")]
        [TestCase("\"\"\"\n\n\nx\n\n\"\"\"", "\n\nx\n")]
        [TestCase("\"\"\"\n\tline\n\t\"\"\"\n\tb = 1\n\"\"\"", "\tline\n\t\"\"\"\n\tb = 1")]
        [TestCase("\"\"\"\n\"\"\n\"x\"\n\"\"\"", "\"\"\n\"x\"")]
        [TestCase("\"\"\"\nx\\ny\\t\n\"\"\"", "x\\ny\\t")]
        public void DeserializesMultilineStringSyntax(string text, string expected)
        {
            var data = TestDataHelper.ParseKV3Text($"{{\n\ta = {text}\n}}");

            Assert.That((string)data["a"], Is.EqualTo(expected));
        }

        [Test]
        public void DeserializesMultilineStringsInArrayAndKey()
        {
            var data = TestDataHelper.ParseKV3Text("{\n\ta = [ \"\"\"\nx\n\"\"\", \"\"\"\ny\n\"\"\"]\n\"\"\"\nkey\nname\n\"\"\" = 1\n}");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(data["a"].Values.Select(x => (string)x), Is.EqualTo(ExpectedMultilineArray));
                Assert.That((int)data["key\nname"], Is.EqualTo(1));
            }
        }

        [Test]
        public void DeserializesVerticalTabAndFormFeedAsWhitespace()
        {
            var data = TestDataHelper.ParseKV3Text("{\va = 1\f}");

            Assert.That((int)data["a"], Is.EqualTo(1));
        }

        [Test]
        public void DeserializesContainersWithoutWhitespace()
        {
            var data = TestDataHelper.ParseKV3Text("{a=1 b=[1,2,] c={d=\"x\"} e=[[]] f={g={}} h=[{}] i=[1,\"a\",[2],{j=3},#[01],null,true,1.5,-2]}");

            using (Assert.EnterMultipleScope())
            {
                Assert.That((int)data["a"], Is.EqualTo(1));
                Assert.That(data["b"].Count, Is.EqualTo(2));
                Assert.That((string)data["c"]["d"], Is.EqualTo("x"));
                Assert.That(data["e"][0].Count, Is.EqualTo(0));
                Assert.That(data["f"]["g"].Count, Is.EqualTo(0));
                Assert.That(data["h"][0].ValueType, Is.EqualTo(KVValueType.Collection));
                Assert.That(data["i"].Values.Select(x => x.ValueType), Is.EqualTo(ExpectedMixedArrayTypes));
            }
        }

        [Test]
        public void DeserializesDeeplyNestedArrays()
        {
            var data = TestDataHelper.ParseKV3Text("{ a = " + new string('[', 50) + "1" + new string(']', 50) + " }");

            var value = data["a"];
            for (var i = 0; i < 50; i++)
            {
                value = value[0];
            }

            Assert.That((int)value, Is.EqualTo(1));
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
