using System.Text;

namespace ValveKeyValue.Test
{
    class MultipleRootObjectsTestCase
    {
        const string Kv3Header = "<!-- kv3 encoding:text:version{e21c7f3c-8a33-41c5-9977-a76d3a32aa0d} format:generic:version{7412167c-06e9-4698-aff2-e63eb59037e7} -->\n";

        [Test]
        public void SecondRootObjectInTextThrows()
        {
            using var stream = TestDataHelper.OpenResource("Text.multiple_root_objects.vdf");
            var ex = Assert.Throws<KeyValueException>(
                () => KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(stream));
            Assert.That(ex.Message, Does.Contain("line 6, column 1"));
        }

        [Test]
        public void SecondTopLevelPairInTextThrows()
        {
            const string text = "\"a\"\t\"1\"\n\"b\"\t\"2\"\n";

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));
            Assert.Throws<KeyValueException>(
                () => KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(stream));
        }

        [TestCase("\"x\" = \"y\"", TestName = "Kv3TrailingKeyValuePairThrows")]
        [TestCase("\"x\"", TestName = "Kv3TrailingStringThrows")]
        [TestCase("{ b = 2 }", TestName = "Kv3SecondRootObjectThrows")]
        [TestCase("[1, 2]", TestName = "Kv3SecondRootArrayThrows")]
        [TestCase("#[00112233]", TestName = "Kv3TrailingBinaryBlobThrows")]
        [TestCase("resource:\"path/to/file.vmdl\"", TestName = "Kv3TrailingFlaggedValueThrows")]
        [TestCase("}", TestName = "Kv3TrailingObjectEndThrows")]
        [TestCase("]", TestName = "Kv3TrailingArrayEndThrows")]
        public void DataAfterRootValueInKV3Throws(string trailer)
        {
            var text = Kv3Header + "{\n\ta = 1\n}\n" + trailer + "\n";

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));
            Assert.Throws<KeyValueException>(
                () => KVSerializer.Create(KVSerializationFormat.KeyValues3Text).Deserialize(stream));
        }

        [Test]
        public void StrayObjectEndInTextThrows()
        {
            const string text = "\"a\"\n{\n}\n}\n";

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));
            var ex = Assert.Throws<KeyValueException>(
                () => KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(stream));
            Assert.That(ex.Message, Does.Contain("Found data after the root object at line 4, column 1"));
        }

        [Test]
        public void SecondRootObjectAfterStrayObjectEndInTextThrows()
        {
            const string text = "\"a\"\n{\n}\n}\n\"b\"\n{\n}\n";

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));
            Assert.Throws<KeyValueException>(
                () => KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(stream));
        }

        [TestCase("[$NEVERDEFINED]")]
        [TestCase("[!$NEVERDEFINED]")]
        public void TrailingConditionalAfterRootInTextThrows(string conditional)
        {
            var text = "\"a\"\n{\n\t\"k\" \"v\"\n}\n" + conditional + "\n";

            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));
            var ex = Assert.Throws<KeyValueException>(
                () => KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(stream));
            Assert.That(ex.Message, Does.Contain("Found data after the root object at line 5, column 1"));
        }

        [Test]
        public void SecondRootObjectInBinaryThrows()
        {
            var data = new byte[]
            {
                0x00, // object: first
                    0x66, 0x69, 0x72, 0x73, 0x74, 0x00,
                    0x01, // string: a = 1
                        0x61, 0x00,
                        0x31, 0x00,
                0x08, // end of first
                0x00, // object: second
                    0x73, 0x65, 0x63, 0x6F, 0x6E, 0x64, 0x00,
                    0x01, // string: c = 3
                        0x63, 0x00,
                        0x33, 0x00,
                0x08, // end of second
                0x08, // end of document
            };

            using var stream = new MemoryStream(data);
            Assert.Throws<KeyValueException>(
                () => KVSerializer.Create(KVSerializationFormat.KeyValues1Binary).Deserialize(stream));
        }
    }
}
