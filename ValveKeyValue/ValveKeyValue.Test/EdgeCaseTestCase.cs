using System.Linq;

namespace ValveKeyValue.Test
{
    class EdgeCaseTestCase
    {
        private static readonly string[] ExpectedDuplicateKeyValues = ["first", "second"];
        private static readonly string[] ExpectedOrderABC = ["a", "b", "c"];
        private static readonly string[] ExpectedOrderAB = ["a", "b"];

        #region Chained write creates intermediate structure

        [Test]
        public void ChainedWriteCreatesNewChildInExistingCollection()
        {
            var inner = KVObject.ListCollection();
            inner.Add("existing", "yes");
            var obj = KVObject.ListCollection();
            obj.Add("a", inner);

            obj["a"]["b"] = 42;

            Assert.That((int)obj["a"]["b"], Is.EqualTo(42));
            Assert.That((string)obj["a"]["existing"], Is.EqualTo("yes"));
            Assert.That(obj["a"].Count, Is.EqualTo(2));
        }

        #endregion

        #region Indexer set preserves insertion order in list collection

        [Test]
        public void IndexerSetPreservesOrderInListCollection()
        {
            var obj = KVObject.ListCollection();
            obj.Add("a", "1");
            obj.Add("b", "2");
            obj.Add("c", "3");

            obj["b"] = "updated";

            var names = obj.Children.Select(c => c.Key).ToList();
            Assert.That(names, Is.EqualTo(ExpectedOrderABC));
            Assert.That((string)obj["b"], Is.EqualTo("updated"));
        }

        [Test]
        public void IndexerSetAppendsNewKeyInListCollection()
        {
            var obj = KVObject.ListCollection();
            obj.Add("a", "1");

            obj["b"] = "2";

            Assert.That(obj.Count, Is.EqualTo(2));
            var names = obj.Children.Select(c => c.Key).ToList();
            Assert.That(names, Is.EqualTo(ExpectedOrderAB));
            Assert.That((string)obj["b"], Is.EqualTo("2"));
        }

        [Test]
        public void IndexerSetRemovesDuplicatesAndKeepsFirstPosition()
        {
            var obj = KVObject.ListCollection();
            obj.Add("a", "1");
            obj.Add("b", "first");
            obj.Add("c", "3");
            obj.Add("b", "second");

            obj["b"] = "replaced";

            Assert.That(obj.Count, Is.EqualTo(3));
            var names = obj.Children.Select(c => c.Key).ToList();
            Assert.That(names, Is.EqualTo(ExpectedOrderABC));
            Assert.That((string)obj["b"], Is.EqualTo("replaced"));
        }

        #endregion

        #region Indexer set with null removes child

        [Test]
        public void IndexerSetNullRemovesChildFromListCollection()
        {
            var obj = KVObject.ListCollection();
            obj.Add("a", "1");
            obj.Add("b", "2");

            obj["a"] = null;

            Assert.That(obj["a"], Is.Null);
            Assert.That(obj.Count, Is.EqualTo(1));
            Assert.That((string)obj["b"], Is.EqualTo("2"));
        }

        [Test]
        public void IndexerSetNullOnMissingKeyDoesNotThrow()
        {
            var obj = KVObject.ListCollection();
            obj.Add("a", "1");

            obj["nonexistent"] = null;

            Assert.That(obj.Count, Is.EqualTo(1));
            Assert.That((string)obj["a"], Is.EqualTo("1"));
        }

        #endregion

        #region Add to non-collection throws

        [Test]
        public void AddChildToScalarThrowsInvalidOperationException()
        {
            var obj = new KVObject("hello");

            Assert.That(
                () => obj.Add("child", "value"),
                Throws.InstanceOf<InvalidOperationException>());
        }

        #endregion

        #region Add(KVObject) to non-array throws

        [Test]
        public void AddKVObjectToCollectionThrowsInvalidOperationException()
        {
            var obj = KVObject.ListCollection();
            obj.Add("a", "1");

            Assert.That(
                () => obj.Add(new KVObject(42)),
                Throws.InstanceOf<InvalidOperationException>());
        }

        [Test]
        public void AddKVObjectToScalarThrowsInvalidOperationException()
        {
            var obj = new KVObject("hello");

            Assert.That(
                () => obj.Add(new KVObject(42)),
                Throws.InstanceOf<InvalidOperationException>());
        }

        #endregion

        #region RemoveAt on non-array throws

        [Test]
        public void RemoveAtOnCollectionThrowsInvalidOperationException()
        {
            var obj = KVObject.ListCollection();
            obj.Add("a", "1");
            obj.Add("b", "2");

            Assert.That(
                () => obj.RemoveAt(0),
                Throws.InstanceOf<InvalidOperationException>());
        }

        [Test]
        public void RemoveAtOnScalarThrowsInvalidOperationException()
        {
            var obj = new KVObject("hello");

            Assert.That(
                () => obj.RemoveAt(0),
                Throws.InstanceOf<InvalidOperationException>());
        }

        #endregion

        #region Integer indexer on scalar throws

        [Test]
        public void IntegerIndexerOnScalarThrowsNotSupportedException()
        {
            var obj = new KVObject("hello");

            Assert.That(
                () => { var _ = obj[0]; },
                Throws.InstanceOf<NotSupportedException>());
        }

        #endregion

        #region GetChild on scalar returns null

        [Test]
        public void GetChildOnScalarReturnsNull()
        {
            var obj = new KVObject("hello");

            Assert.That(obj.GetChild("x"), Is.Null);
        }

        [Test]
        public void GetChildOnNullValuedObjectReturnsNull()
        {
            var obj = KVObject.Null();

            Assert.That(obj.GetChild("x"), Is.Null);
        }

        #endregion

        #region ContainsKey on scalar returns false

        [Test]
        public void ContainsKeyOnScalarReturnsFalse()
        {
            var obj = new KVObject("hello");

            Assert.That(obj.ContainsKey("x"), Is.False);
        }

        [Test]
        public void ContainsKeyOnNullValuedObjectReturnsFalse()
        {
            var obj = KVObject.Null();

            Assert.That(obj.ContainsKey("x"), Is.False);
        }

        [Test]
        public void ContainsKeyOnListCollectionReturnsCorrectly()
        {
            var obj = KVObject.ListCollection();
            obj.Add("a", "1");
            obj.Add("b", "2");

            Assert.That(obj.ContainsKey("a"), Is.True);
            Assert.That(obj.ContainsKey("missing"), Is.False);
        }

        #endregion

        #region Count on scalar is 0

        [Test]
        public void CountOnScalarIsZero()
        {
            var obj = new KVObject("hello");

            Assert.That(obj.Count, Is.EqualTo(0));
        }

        [Test]
        public void CountOnNullValuedObjectIsZero()
        {
            var obj = KVObject.Null();

            Assert.That(obj.Count, Is.EqualTo(0));
        }

        #endregion

        #region Empty collection

        [Test]
        public void EmptyCollectionHasCountZero()
        {
            var obj = KVObject.ListCollection();

            Assert.That(obj.Count, Is.EqualTo(0));
        }

        [Test]
        public void EmptyCollectionIteratesEmpty()
        {
            var obj = KVObject.ListCollection();
            var items = obj.Children.ToList();

            Assert.That(items, Is.Empty);
        }

        [Test]
        public void EmptyCollectionIsNotArray()
        {
            var obj = KVObject.ListCollection();

            Assert.That(obj.IsArray, Is.False);
            Assert.That(obj.ValueType, Is.EqualTo(KVValueType.Collection));
        }

        #endregion

        #region Empty array

        [Test]
        public void EmptyArrayHasCountZero()
        {
            var obj = KVObject.Array();

            Assert.That(obj.Count, Is.EqualTo(0));
        }

        [Test]
        public void EmptyArrayIsArray()
        {
            var obj = KVObject.Array();

            Assert.That(obj.IsArray, Is.True);
            Assert.That(obj.ValueType, Is.EqualTo(KVValueType.Array));
        }

        [Test]
        public void EmptyArrayIteratesEmpty()
        {
            var obj = KVObject.Array();
            var items = obj.Children.ToList();

            Assert.That(items, Is.Empty);
        }

        #endregion

        #region KVObject flag mutation

        [Test]
        public void FlagMutationPreservesIntValue()
        {
            KVObject val = 42;
            val.Flag = KVFlag.Resource;

            Assert.That(val.Flag, Is.EqualTo(KVFlag.Resource));
            Assert.That(val.ValueType, Is.EqualTo(KVValueType.Int32));
            Assert.That((int)val, Is.EqualTo(42));
        }

        [Test]
        public void FlagMutationPreservesStringValue()
        {
            KVObject val = "hello";
            val.Flag = KVFlag.SoundEvent;

            Assert.That(val.Flag, Is.EqualTo(KVFlag.SoundEvent));
            Assert.That(val.ValueType, Is.EqualTo(KVValueType.String));
            Assert.That((string)val, Is.EqualTo("hello"));
        }

        [Test]
        public void FlagMutationOnSeparateObjects()
        {
            KVObject val = 99;
            KVObject withResource = 99;
            withResource.Flag = KVFlag.Panorama;
            KVObject withBoth = 99;
            withBoth.Flag = KVFlag.EntityName;

            // Each is a separate object
            Assert.That(val.Flag, Is.EqualTo(KVFlag.None));
            Assert.That(withResource.Flag, Is.EqualTo(KVFlag.Panorama));
            Assert.That(withBoth.Flag, Is.EqualTo(KVFlag.EntityName));
            Assert.That((int)withBoth, Is.EqualTo(99));
        }

        #endregion

        #region TryGetValue returns false for missing

        [Test]
        public void TryGetValueReturnsFalseForMissing()
        {
            var obj = KVObject.ListCollection();
            obj.Add("a", "1");

            var result = obj.TryGetValue("missing", out var child);

            Assert.That(result, Is.False);
            Assert.That(child, Is.Null);
        }

        [Test]
        public void TryGetValueReturnsFalseOnScalar()
        {
            var obj = new KVObject("hello");

            var result = obj.TryGetValue("anything", out var child);

            Assert.That(result, Is.False);
            Assert.That(child, Is.Null);
        }

        #endregion

        #region TryGetValue returns true for existing

        [Test]
        public void TryGetValueReturnsTrueForExisting()
        {
            var obj = KVObject.ListCollection();
            obj.Add("existing", "value");

            var result = obj.TryGetValue("existing", out var child);

            Assert.That(result, Is.True);
            Assert.That(child, Is.Not.Null);
            Assert.That((string)child, Is.EqualTo("value"));
        }

        [Test]
        public void TryGetValueOnDictBackedCollection()
        {
            var kv3Text = "<!-- kv3 encoding:text:version{e21c7f3c-8a33-41c5-9977-a76d3a32aa0d} format:generic:version{7412167c-06e9-4698-aff2-e63eb59037e7} -->\n{\n\tkey1 = \"value1\"\n}";
            using var stream = new System.IO.MemoryStream(System.Text.Encoding.UTF8.GetBytes(kv3Text));
            var obj = KVSerializer.Create(KVSerializationFormat.KeyValues3Text).Deserialize(stream);

            Assert.That(obj.TryGetValue("key1", out var found), Is.True);
            Assert.That((string)found, Is.EqualTo("value1"));

            Assert.That(obj.TryGetValue("missing", out var notFound), Is.False);
            Assert.That(notFound, Is.Null);
        }

        #endregion

        #region Add(string, KVObject) adds named child

        [Test]
        public void AddStringKVObjectAddsNamedChild()
        {
            var obj = KVObject.ListCollection();

            obj.Add("key1", "value1");

            Assert.That(obj.Count, Is.EqualTo(1));
            Assert.That((string)obj["key1"], Is.EqualTo("value1"));
        }

        #endregion

        #region CreateArray from KVObject enumerable

        [Test]
        public void CreateArrayFromKVObjectEnumerable()
        {
            KVObject[] values = ["alpha", "beta", "gamma"];
            var arr = KVObject.Array(values);

            Assert.That(arr.IsArray, Is.True);
            Assert.That(arr.Count, Is.EqualTo(3));
            Assert.That((string)arr[0], Is.EqualTo("alpha"));
            Assert.That((string)arr[1], Is.EqualTo("beta"));
            Assert.That((string)arr[2], Is.EqualTo("gamma"));
        }

        [Test]
        public void CreateArrayFromKVObjectEnumerableChildrenHaveNullKeys()
        {
            KVObject[] values = [1, 2];
            var arr = KVObject.Array(values);

            foreach (var (key, value) in arr)
            {
                Assert.That(key, Is.Null);
            }
        }

        [Test]
        public void CreateArrayFromEmptyKVObjectEnumerable()
        {
            var arr = KVObject.Array(Array.Empty<KVObject>());

            Assert.That(arr.IsArray, Is.True);
            Assert.That(arr.Count, Is.EqualTo(0));
        }

        #endregion

        #region Multiple children with same name in KV1 (list-backed)

        [Test]
        public void DuplicateKeysPreservedInListCollection()
        {
            var obj = KVObject.ListCollection();
            obj.Add("key", "first");
            obj.Add("key", "second");
            obj.Add("other", "value");

            Assert.That(obj.Count, Is.EqualTo(3));
        }

        [Test]
        public void GetChildReturnsFistForDuplicateKeysInListCollection()
        {
            var obj = KVObject.ListCollection();
            obj.Add("key", "first");
            obj.Add("key", "second");

            var child = obj.GetChild("key");

            Assert.That(child, Is.Not.Null);
            Assert.That((string)child, Is.EqualTo("first"));
        }

        [Test]
        public void EnumerationPreservesDuplicateKeysInListCollection()
        {
            var obj = KVObject.ListCollection();
            obj.Add("key", "first");
            obj.Add("key", "second");

            var values = obj.Children
                .Where(c => c.Key == "key")
                .Select(c => (string)c.Value)
                .ToList();

            Assert.That(values, Is.EqualTo(ExpectedDuplicateKeyValues));
        }

        #endregion
    }
}
