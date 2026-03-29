using System.Linq;

namespace ValveKeyValue.Test
{
    class EdgeCaseTestCase
    {
        #region 1. Chained write creates intermediate structure

        [Test]
        public void ChainedWriteCreatesNewChildInExistingCollection()
        {
            var obj = new KVObject("root", [
                new KVObject("a", [
                    new KVObject("existing", "yes")
                ])
            ]);

            obj["a"]["b"] = 42;

            Assert.That((int)obj["a"]["b"], Is.EqualTo(42));
            Assert.That((string)obj["a"]["existing"], Is.EqualTo("yes"));
            Assert.That(obj["a"].Count, Is.EqualTo(2));
        }

        #endregion

        #region 2. Indexer set with null removes child

        [Test]
        public void IndexerSetNullRemovesChildFromListCollection()
        {
            var obj = new KVObject("root", [
                new KVObject("a", "1"),
                new KVObject("b", "2"),
            ]);

            obj["a"] = null;

            Assert.That(obj["a"], Is.Null);
            Assert.That(obj.Count, Is.EqualTo(1));
            Assert.That((string)obj["b"], Is.EqualTo("2"));
        }

        [Test]
        public void IndexerSetNullOnMissingKeyDoesNotThrow()
        {
            var obj = new KVObject("root", [
                new KVObject("a", "1"),
            ]);

            obj["nonexistent"] = null;

            Assert.That(obj.Count, Is.EqualTo(1));
            Assert.That((string)obj["a"], Is.EqualTo("1"));
        }

        #endregion

        #region 3. Add to non-collection throws

        [Test]
        public void AddChildToScalarThrowsInvalidOperationException()
        {
            var obj = new KVObject("scalar", "hello");

            Assert.That(
                () => obj.Add(new KVObject("child", "value")),
                Throws.InstanceOf<InvalidOperationException>());
        }

        #endregion

        #region 4. Add(KVValue) to non-array throws

        [Test]
        public void AddKVValueToCollectionThrowsInvalidOperationException()
        {
            var obj = new KVObject("root", [new KVObject("a", "1")]);

            Assert.That(
                () => obj.Add((KVValue)42),
                Throws.InstanceOf<InvalidOperationException>());
        }

        [Test]
        public void AddKVValueToScalarThrowsInvalidOperationException()
        {
            var obj = new KVObject("scalar", "hello");

            Assert.That(
                () => obj.Add((KVValue)42),
                Throws.InstanceOf<InvalidOperationException>());
        }

        #endregion

        #region 5. RemoveAt on non-array throws

        [Test]
        public void RemoveAtOnCollectionThrowsInvalidOperationException()
        {
            var obj = new KVObject("root", [
                new KVObject("a", "1"),
                new KVObject("b", "2"),
            ]);

            Assert.That(
                () => obj.RemoveAt(0),
                Throws.InstanceOf<InvalidOperationException>());
        }

        [Test]
        public void RemoveAtOnScalarThrowsInvalidOperationException()
        {
            var obj = new KVObject("scalar", "hello");

            Assert.That(
                () => obj.RemoveAt(0),
                Throws.InstanceOf<InvalidOperationException>());
        }

        #endregion

        #region 6. Integer indexer on scalar throws

        [Test]
        public void IntegerIndexerOnScalarThrowsNotSupportedException()
        {
            var obj = new KVObject("scalar", "hello");

            Assert.That(
                () => { var _ = obj[0]; },
                Throws.InstanceOf<NotSupportedException>());
        }

        #endregion

        #region 7. GetChild on scalar returns null

        [Test]
        public void GetChildOnScalarReturnsNull()
        {
            var obj = new KVObject("scalar", "hello");

            Assert.That(obj.GetChild("x"), Is.Null);
        }

        [Test]
        public void GetChildOnNullValuedObjectReturnsNull()
        {
            var obj = new KVObject("nullobj");

            Assert.That(obj.GetChild("x"), Is.Null);
        }

        #endregion

        #region 8. ContainsKey on scalar returns false

        [Test]
        public void ContainsKeyOnScalarReturnsFalse()
        {
            var obj = new KVObject("scalar", "hello");

            Assert.That(obj.ContainsKey("x"), Is.False);
        }

        [Test]
        public void ContainsKeyOnNullValuedObjectReturnsFalse()
        {
            var obj = new KVObject("nullobj");

            Assert.That(obj.ContainsKey("x"), Is.False);
        }

        #endregion

        #region 9. Count on scalar is 0

        [Test]
        public void CountOnScalarIsZero()
        {
            var obj = new KVObject("scalar", "hello");

            Assert.That(obj.Count, Is.EqualTo(0));
        }

        [Test]
        public void CountOnNullValuedObjectIsZero()
        {
            var obj = new KVObject("nullobj");

            Assert.That(obj.Count, Is.EqualTo(0));
        }

        #endregion

        #region 10. Empty collection

        [Test]
        public void EmptyCollectionHasCountZero()
        {
            var obj = new KVObject("x", Array.Empty<KVObject>());

            Assert.That(obj.Count, Is.EqualTo(0));
        }

        [Test]
        public void EmptyCollectionIteratesEmpty()
        {
            var obj = new KVObject("x", Array.Empty<KVObject>());
            var items = obj.Children.ToList();

            Assert.That(items, Is.Empty);
        }

        [Test]
        public void EmptyCollectionIsNotArray()
        {
            var obj = new KVObject("x", Array.Empty<KVObject>());

            Assert.That(obj.IsArray, Is.False);
            Assert.That(obj.ValueType, Is.EqualTo(KVValueType.Collection));
        }

        #endregion

        #region 11. Empty array

        [Test]
        public void EmptyArrayHasCountZero()
        {
            var obj = new KVObject("x", Array.Empty<KVValue>());

            Assert.That(obj.Count, Is.EqualTo(0));
        }

        [Test]
        public void EmptyArrayIsArray()
        {
            var obj = new KVObject("x", Array.Empty<KVValue>());

            Assert.That(obj.IsArray, Is.True);
            Assert.That(obj.ValueType, Is.EqualTo(KVValueType.Array));
        }

        [Test]
        public void EmptyArrayIteratesEmpty()
        {
            var obj = new KVObject("x", Array.Empty<KVValue>());
            var items = obj.Children.ToList();

            Assert.That(items, Is.Empty);
        }

        #endregion

        #region 12. KVValue with expressions preserve all fields

        [Test]
        public void WithExpressionPreservesIntValue()
        {
            KVValue val = 42;
            var modified = val with { Flag = KVFlag.Resource };

            Assert.That(modified.Flag, Is.EqualTo(KVFlag.Resource));
            Assert.That(modified.ValueType, Is.EqualTo(KVValueType.Int32));
            Assert.That((int)modified, Is.EqualTo(42));
        }

        [Test]
        public void WithExpressionPreservesStringValue()
        {
            KVValue val = "hello";
            var modified = val with { Flag = KVFlag.SoundEvent };

            Assert.That(modified.Flag, Is.EqualTo(KVFlag.SoundEvent));
            Assert.That(modified.ValueType, Is.EqualTo(KVValueType.String));
            Assert.That((string)modified, Is.EqualTo("hello"));
        }

        [Test]
        public void WithExpressionChangingFlagPreservesOriginal()
        {
            KVValue val = 99;
            var withResource = val with { Flag = KVFlag.Panorama };
            var withBoth = withResource with { Flag = KVFlag.EntityName };

            // Original is unmodified (value type)
            Assert.That(val.Flag, Is.EqualTo(KVFlag.None));
            Assert.That(withResource.Flag, Is.EqualTo(KVFlag.Panorama));
            Assert.That(withBoth.Flag, Is.EqualTo(KVFlag.EntityName));
            Assert.That((int)withBoth, Is.EqualTo(99));
        }

        #endregion

        #region 13. KVValue equality

        [Test]
        public void TwoKVValuesWithSameIntAreEqual()
        {
            KVValue a = 42;
            KVValue b = 42;

            Assert.That(a, Is.EqualTo(b));
            Assert.That(a == b, Is.True);
        }

        [Test]
        public void TwoKVValuesWithSameStringAreEqual()
        {
            KVValue a = "hello";
            KVValue b = "hello";

            Assert.That(a, Is.EqualTo(b));
            Assert.That(a == b, Is.True);
        }

        [Test]
        public void TwoDefaultKVValuesAreEqual()
        {
            var a = default(KVValue);
            var b = default(KVValue);

            Assert.That(a, Is.EqualTo(b));
            Assert.That(a == b, Is.True);
        }

        [Test]
        public void DifferentValuesAreNotEqual()
        {
            KVValue a = 42;
            KVValue b = 99;

            Assert.That(a, Is.Not.EqualTo(b));
            Assert.That(a != b, Is.True);
        }

        [Test]
        public void DifferentTypesAreNotEqual()
        {
            KVValue a = 42;
            KVValue b = "42";

            Assert.That(a, Is.Not.EqualTo(b));
        }

        #endregion

        #region 14. TryGetChild returns false for missing

        [Test]
        public void TryGetChildReturnsFalseForMissing()
        {
            var obj = new KVObject("root", [new KVObject("a", "1")]);

            var result = obj.TryGetChild("missing", out var child);

            Assert.That(result, Is.False);
            Assert.That(child, Is.Null);
        }

        [Test]
        public void TryGetChildReturnsFalseOnScalar()
        {
            var obj = new KVObject("scalar", "hello");

            var result = obj.TryGetChild("anything", out var child);

            Assert.That(result, Is.False);
            Assert.That(child, Is.Null);
        }

        #endregion

        #region 15. TryGetChild returns true for existing

        [Test]
        public void TryGetChildReturnsTrueForExisting()
        {
            var obj = new KVObject("root", [new KVObject("existing", "value")]);

            var result = obj.TryGetChild("existing", out var child);

            Assert.That(result, Is.True);
            Assert.That(child, Is.Not.Null);
            Assert.That(child.Name, Is.EqualTo("existing"));
            Assert.That((string)child, Is.EqualTo("value"));
        }

        #endregion

        #region 16. Add(string, KVValue) adds named child

        [Test]
        public void AddStringKVValueAddsNamedChild()
        {
            var obj = new KVObject("root", Array.Empty<KVObject>());

            obj.Add("key1", (KVValue)"value1");

            Assert.That(obj.Count, Is.EqualTo(1));
            Assert.That((string)obj["key1"], Is.EqualTo("value1"));
        }

        #endregion

        #region 18. CreateArray from KVValue enumerable

        [Test]
        public void CreateArrayFromKVValueEnumerable()
        {
            var values = new KVValue[] { "alpha", "beta", "gamma" };
            var arr = KVObject.Array("a", values);

            Assert.That(arr.IsArray, Is.True);
            Assert.That(arr.Count, Is.EqualTo(3));
            Assert.That((string)arr[0], Is.EqualTo("alpha"));
            Assert.That((string)arr[1], Is.EqualTo("beta"));
            Assert.That((string)arr[2], Is.EqualTo("gamma"));
        }

        [Test]
        public void CreateArrayFromKVValueEnumerableWrapsInUnnamedObjects()
        {
            var values = new KVValue[] { 1, 2 };
            var arr = KVObject.Array("a", values);

            foreach (var child in arr)
            {
                Assert.That(child.Name, Is.Null);
            }
        }

        [Test]
        public void CreateArrayFromEmptyKVValueEnumerable()
        {
            var arr = KVObject.Array("a", Array.Empty<KVValue>());

            Assert.That(arr.IsArray, Is.True);
            Assert.That(arr.Count, Is.EqualTo(0));
        }

        #endregion

        #region 19. Multiple children with same name in KV1 (list-backed)

        [Test]
        public void DuplicateKeysPreservedInListCollection()
        {
            var obj = new KVObject("root", [
                new KVObject("key", "first"),
                new KVObject("key", "second"),
                new KVObject("other", "value"),
            ]);

            Assert.That(obj.Count, Is.EqualTo(3));
        }

        [Test]
        public void GetChildReturnsFistForDuplicateKeysInListCollection()
        {
            var obj = new KVObject("root", [
                new KVObject("key", "first"),
                new KVObject("key", "second"),
            ]);

            var child = obj.GetChild("key");

            Assert.That(child, Is.Not.Null);
            Assert.That((string)child, Is.EqualTo("first"));
        }

        [Test]
        public void EnumerationPreservesDuplicateKeysInListCollection()
        {
            var obj = new KVObject("root", [
                new KVObject("key", "first"),
                new KVObject("key", "second"),
            ]);

            var values = obj.Children
                .Where(c => c.Name == "key")
                .Select(c => (string)c)
                .ToList();

            Assert.That(values, Is.EqualTo(new[] { "first", "second" }));
        }

        #endregion
    }
}
