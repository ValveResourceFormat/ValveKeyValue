using System.Numerics;

namespace ValveKeyValue.Test
{
    /// <summary>
    /// The KeyValues2 work added types, accessors and a new collection backing store to
    /// <see cref="KVObject"/>, which every format shares. These cover how those behave in
    /// KeyValues1 and KeyValues3, where most of them should not be usable at all.
    /// </summary>
    class CrossFormatTestCase
    {
        static readonly KVSerializationFormat[] AllFormats =
        [
            KVSerializationFormat.KeyValues1Text,
            KVSerializationFormat.KeyValues1Binary,
            KVSerializationFormat.KeyValues3Text,
        ];

        static KVSerializerOptions BinaryOptions => new() { StringTable = new StringTable() };

        static readonly string[] Acd = ["a", "c", "d"];
        static readonly int[] OneTwo = [1, 2];

        static void Serialize(KVSerializationFormat format, KVObject root)
        {
            using var stream = new MemoryStream();
            var options = format == KVSerializationFormat.KeyValues1Binary ? BinaryOptions : null;
            KVSerializer.Create(format).Serialize(stream, root, "root", options);
        }

        #region DMX-only values are rejected everywhere else

        public static IEnumerable<TestCaseData> DmxOnlyValues()
        {
            foreach (var format in AllFormats)
            {
                yield return new TestCaseData(format, new KVObject(new DmxColor(1, 2, 3, 4))).SetName($"{{m}}({format}, color)");
                yield return new TestCaseData(format, new KVObject(new DmxTime(1))).SetName($"{{m}}({format}, time)");
                yield return new TestCaseData(format, new KVObject(new Vector2(1, 2))).SetName($"{{m}}({format}, vector2)");
                yield return new TestCaseData(format, new KVObject(new Vector3(1, 2, 3))).SetName($"{{m}}({format}, vector3)");
                yield return new TestCaseData(format, new KVObject(new Vector4(1, 2, 3, 4))).SetName($"{{m}}({format}, vector4)");
                yield return new TestCaseData(format, new KVObject(new QAngle(1, 2, 3))).SetName($"{{m}}({format}, qangle)");
                yield return new TestCaseData(format, new KVObject(Quaternion.Identity)).SetName($"{{m}}({format}, quaternion)");
                yield return new TestCaseData(format, new KVObject(Matrix4x4.Identity)).SetName($"{{m}}({format}, matrix)");
                yield return new TestCaseData(format, KVObject.Byte(7)).SetName($"{{m}}({format}, uint8)");
                yield return new TestCaseData(format, KVObject.TypedArray(new List<int> { 1 })).SetName($"{{m}}({format}, int_array)");
                yield return new TestCaseData(format, KVObject.TypedArray(new List<string> { "a" })).SetName($"{{m}}({format}, string_array)");
                yield return new TestCaseData(format, KVObject.TypedArray(new List<KV2Element>())).SetName($"{{m}}({format}, element_array)");
            }
        }

        [TestCaseSource(nameof(DmxOnlyValues))]
        public void RejectsDmxOnlyValues(KVSerializationFormat format, KVObject value)
        {
            var root = KVObject.Collection();
            root.Add("value", value);

            Assert.That(() => Serialize(format, root), Throws.InstanceOf<InvalidOperationException>());
        }

        /// <summary>
        /// Types DMX shares with the other formats have to keep working.
        /// </summary>
        [Test]
        public void StillWritesSharedValueTypes([ValueSource(nameof(AllFormats))] KVSerializationFormat format)
        {
            var root = KVObject.Collection();
            root.Add("int", new KVObject(1));
            root.Add("float", new KVObject(1.5f));
            root.Add("bool", new KVObject(true));
            root.Add("string", new KVObject("text"));
            root.Add("uint64", new KVObject(ulong.MaxValue));

            Assert.That(() => Serialize(format, root), Throws.Nothing);
        }

        #endregion

        #region Collection ordering

        static List<string> KeysOf(KVObject obj)
        {
            var keys = new List<string>();

            foreach (var key in obj.Keys)
            {
                keys.Add(key);
            }

            return keys;
        }

        /// <summary>
        /// The dictionary-backed collection KeyValues3 uses had its backing store changed for the
        /// sake of DMX attribute order, so it has to keep insertion order here too.
        /// </summary>
        [Test]
        public void DictionaryBackedCollectionKeepsInsertionOrderAcrossRemoval()
        {
            var root = KVObject.Collection();
            root.Add("a", new KVObject(1));
            root.Add("b", new KVObject(2));
            root.Add("c", new KVObject(3));
            root.Remove("b");
            root.Add("d", new KVObject(4));

            Assert.That(KeysOf(root), Is.EqualTo(Acd));
        }

        [Test]
        public void ListBackedCollectionKeepsInsertionOrderAcrossRemoval()
        {
            var root = KVObject.ListCollection();
            root.Add("a", new KVObject(1));
            root.Add("b", new KVObject(2));
            root.Add("c", new KVObject(3));
            root.Remove("b");
            root.Add("d", new KVObject(4));

            Assert.That(KeysOf(root), Is.EqualTo(Acd));
        }

        [Test]
        public void KeyValues3OutputFollowsInsertionOrder()
        {
            var root = KVObject.Collection();
            root.Add("zulu", new KVObject(1));
            root.Add("alpha", new KVObject(2));
            root.Add("mike", new KVObject(3));

            using var stream = new MemoryStream();
            KVSerializer.Create(KVSerializationFormat.KeyValues3Text).Serialize(stream, root, "root");

            var text = System.Text.Encoding.UTF8.GetString(stream.ToArray());

            Assert.That(text.IndexOf("zulu", StringComparison.Ordinal), Is.LessThan(text.IndexOf("alpha", StringComparison.Ordinal)));
            Assert.That(text.IndexOf("alpha", StringComparison.Ordinal), Is.LessThan(text.IndexOf("mike", StringComparison.Ordinal)));
        }

        /// <summary>
        /// The list-backed collection is the one that permits duplicate keys, and that has to
        /// survive the backing store change.
        /// </summary>
        [Test]
        public void ListBackedCollectionStillAllowsDuplicateKeys()
        {
            var root = KVObject.ListCollection();
            root.Add("same", new KVObject(1));
            root.Add("same", new KVObject(2));

            Assert.That(root.Count, Is.EqualTo(2));
        }

        [Test]
        public void DictionaryBackedCollectionStillRejectsDuplicateKeys()
        {
            var root = KVObject.Collection();
            root.Add("same", new KVObject(1));

            Assert.That(() => root.Add("same", new KVObject(2)), Throws.ArgumentException);
        }

        #endregion

        #region Typed array and value accessors

        [Test]
        public void TypedArrayRejectsUnsupportedItemTypes()
        {
            Assert.That(() => KVObject.TypedArray(new List<decimal> { 1m }), Throws.ArgumentException);
        }

        [Test]
        public void TypedArrayRejectsNull()
        {
            Assert.That(() => KVObject.TypedArray<int>(null!), Throws.ArgumentNullException);
        }

        [Test]
        public void GetArrayRejectsTheWrongItemType()
        {
            var value = KVObject.TypedArray(new List<int> { 1 });

            Assert.That(() => value.GetArray<float>(), Throws.InvalidOperationException);
        }

        [Test]
        public void GetValueRejectsTheWrongType()
        {
            var value = new KVObject(new Vector3(1, 2, 3));

            Assert.Multiple(() =>
            {
                Assert.That(value.GetValue<Vector3>(), Is.EqualTo(new Vector3(1, 2, 3)));
                Assert.That(() => value.GetValue<Vector2>(), Throws.InvalidOperationException);
            });
        }

        [Test]
        public void GetValueRejectsScalarsThatAreNotBoxed()
        {
            // An int lives in the inline scalar slot, not the reference slot.
            Assert.That(() => new KVObject(42).GetValue<int>(), Throws.InvalidOperationException);
        }

        [Test]
        public void IsTypedArrayIsFalseForOtherCollections()
        {
            Assert.Multiple(() =>
            {
                Assert.That(KVObject.Collection().IsTypedArray, Is.False);
                Assert.That(KVObject.ListCollection().IsTypedArray, Is.False);
                Assert.That(KVObject.Array().IsTypedArray, Is.False);
                Assert.That(new KVObject(1).IsTypedArray, Is.False);
                Assert.That(KVObject.TypedArray(new List<int>()).IsTypedArray, Is.True);
            });
        }

        [Test]
        public void CountWorksForEveryCollectionShape()
        {
            Assert.Multiple(() =>
            {
                Assert.That(KVObject.TypedArray(new List<int> { 1, 2, 3 }).Count, Is.EqualTo(3));
                Assert.That(KVObject.Array([new KVObject(1), new KVObject(2)]).Count, Is.EqualTo(2));
                Assert.That(new KVObject(1).Count, Is.Zero);
            });
        }

        #endregion

        #region Byte, which only DMX writes but every conversion path can reach

        [Test]
        public void ByteConvertsLikeOtherIntegers()
        {
            var value = KVObject.Byte(200);

            Assert.Multiple(() =>
            {
                Assert.That(value.ToByte(null), Is.EqualTo(200));
                Assert.That(value.ToInt32(null), Is.EqualTo(200));
                Assert.That(value.ToInt64(null), Is.EqualTo(200L));
                Assert.That(value.ToDouble(null), Is.EqualTo(200d));
                Assert.That(value.ToString(System.Globalization.CultureInfo.InvariantCulture), Is.EqualTo("200"));
                Assert.That((byte)value, Is.EqualTo(200));
            });
        }

        #endregion

        #region ObjectCopier still handles the formats it always did

        class Simple
        {
            public int Number { get; set; }
            public string Text { get; set; } = string.Empty;
            public byte[] Blob { get; set; } = [];
            public int[] Numbers { get; set; } = [];
            public Simple? Child { get; set; }
        }

        [Test]
        public void TypedRoundTripStillWorksForKeyValues3()
        {
            var root = KVObject.Collection();
            root.Add("number", new KVObject(7));
            root.Add("text", new KVObject("hello"));
            root.Add("blob", KVObject.Blob([1, 2, 3]));

            var child = KVObject.Collection();
            child.Add("number", new KVObject(8));
            root.Add("child", child);

            var numbers = KVObject.Array([new KVObject(1), new KVObject(2)]);
            root.Add("numbers", numbers);

            using var stream = new MemoryStream();
            KVSerializer.Create(KVSerializationFormat.KeyValues3Text).Serialize(stream, root, "root");
            stream.Position = 0;

            var obj = KVSerializer.Create(KVSerializationFormat.KeyValues3Text).Deserialize<Simple>(stream);

            Assert.Multiple(() =>
            {
                Assert.That(obj.Number, Is.EqualTo(7));
                Assert.That(obj.Text, Is.EqualTo("hello"));
                Assert.That(obj.Blob, Is.EqualTo(new byte[] { 1, 2, 3 }));
                Assert.That(obj.Numbers, Is.EqualTo(OneTwo));
                Assert.That(obj.Child!.Number, Is.EqualTo(8));
            });
        }

        /// <summary>
        /// The object copier gained a depth limit, so ordinary nesting has to stay well inside it.
        /// </summary>
        [Test]
        public void TypedDeserializationHandlesReasonableNesting()
        {
            var root = KVObject.Collection();
            var current = root;

            for (var i = 0; i < 20; i++)
            {
                var child = KVObject.Collection();
                child.Add("number", new KVObject(i));
                current.Add("child", child);
                current = child;
            }

            using var stream = new MemoryStream();
            KVSerializer.Create(KVSerializationFormat.KeyValues3Text).Serialize(stream, root, "root");
            stream.Position = 0;

            var obj = KVSerializer.Create(KVSerializationFormat.KeyValues3Text).Deserialize<Simple>(stream);

            var depth = 0;

            for (var node = obj.Child; node != null; node = node.Child)
            {
                depth++;
            }

            Assert.That(depth, Is.EqualTo(20));
        }

        #endregion

        #region Document shape

        [Test]
        public void RootIsPopulatedForEveryFormat()
        {
            var root = KVObject.Collection();
            root.Add("value", new KVObject(1));

            using var stream = new MemoryStream();
            KVSerializer.Create(KVSerializationFormat.KeyValues3Text).Serialize(stream, root, "root");
            stream.Position = 0;

            var doc = KVSerializer.Create(KVSerializationFormat.KeyValues3Text).Deserialize(stream);

            Assert.Multiple(() =>
            {
                Assert.That(doc.Root, Is.Not.Null);
                Assert.That((int)doc.Root["value"], Is.EqualTo(1));
                Assert.That(doc, Is.Not.InstanceOf<KV2Document>());
                Assert.That(doc.Root, Is.Not.InstanceOf<KV2Element>());
            });
        }

        #endregion
    }
}
