using System.Linq;

namespace ValveKeyValue.Test
{
    class KVObjectApiTestCase
    {
        #region 1. String indexer returns KVObject

        [Test]
        public void IndexerReturnsKVObjectForExistingKey()
        {
            var obj = new KVObject("root", [new KVObject("key", "value")]);
            var child = obj["key"];

            Assert.That(child, Is.Not.Null);
            Assert.That(child, Is.InstanceOf<KVObject>());
            Assert.That((string)child, Is.EqualTo("value"));
        }

        [Test]
        public void IndexerReturnsNullForMissingKey()
        {
            var obj = new KVObject("root", [new KVObject("key", "value")]);
            Assert.That(obj["missing"], Is.Null);
        }

        [Test]
        public void ChainedReadThroughNestedCollections()
        {
            var obj = new KVObject("root", [
                new KVObject("a", [
                    new KVObject("b", "deep")
                ])
            ]);

            Assert.That((string)obj["a"]["b"], Is.EqualTo("deep"));
        }

        [Test]
        public void ChainedWriteModifiesTree()
        {
            var obj = new KVObject("root", [
                new KVObject("a", [
                    new KVObject("b", "original")
                ])
            ]);

            obj["a"]["b"] = 42;

            Assert.That((int)obj["a"]["b"], Is.EqualTo(42));
        }

        #endregion

        #region 1b. Typed constructors

        [Test]
        public void ConstructorWithString()
        {
            var obj = new KVObject("key", "hello");
            Assert.That(obj.ValueType, Is.EqualTo(KVValueType.String));
            Assert.That((string)obj, Is.EqualTo("hello"));
        }

        [Test]
        public void ConstructorWithInt()
        {
            var obj = new KVObject("key", 42);
            Assert.That(obj.ValueType, Is.EqualTo(KVValueType.Int32));
            Assert.That((int)obj, Is.EqualTo(42));
        }

        [Test]
        public void ConstructorWithUInt()
        {
            var obj = new KVObject("key", 42u);
            Assert.That(obj.ValueType, Is.EqualTo(KVValueType.UInt32));
            Assert.That((uint)obj, Is.EqualTo(42u));
        }

        [Test]
        public void ConstructorWithLong()
        {
            var obj = new KVObject("key", 123456789012345L);
            Assert.That(obj.ValueType, Is.EqualTo(KVValueType.Int64));
            Assert.That((long)obj, Is.EqualTo(123456789012345L));
        }

        [Test]
        public void ConstructorWithULong()
        {
            var obj = new KVObject("key", 0x8877665544332211UL);
            Assert.That(obj.ValueType, Is.EqualTo(KVValueType.UInt64));
            Assert.That((ulong)obj, Is.EqualTo(0x8877665544332211UL));
        }

        [Test]
        public void ConstructorWithFloat()
        {
            var obj = new KVObject("key", 3.14f);
            Assert.That(obj.ValueType, Is.EqualTo(KVValueType.FloatingPoint));
            Assert.That((float)obj, Is.EqualTo(3.14f));
        }

        [Test]
        public void ConstructorWithDouble()
        {
            var obj = new KVObject("key", 3.14159265);
            Assert.That(obj.ValueType, Is.EqualTo(KVValueType.FloatingPoint64));
            Assert.That((double)obj, Is.EqualTo(3.14159265));
        }

        [Test]
        public void ConstructorWithBool()
        {
            var obj = new KVObject("key", true);
            Assert.That(obj.ValueType, Is.EqualTo(KVValueType.Boolean));
            Assert.That((bool)obj, Is.True);
        }

        [Test]
        public void ConstructorWithIntPtr()
        {
            var obj = new KVObject("key", new IntPtr(0x12345678));
            Assert.That(obj.ValueType, Is.EqualTo(KVValueType.Pointer));
        }

        [Test]
        public void ConstructorWithNullString()
        {
            var obj = new KVObject("key", (string)null);
            Assert.That(obj.ValueType, Is.EqualTo(KVValueType.Null));
            Assert.That(obj.IsNull, Is.True);
        }

        #endregion

        #region 2. Implicit operators from primitives

        [Test]
        public void ImplicitIntToKVObject()
        {
            var obj = new KVObject("root", [new KVObject("key", "placeholder")]);
            obj["key"] = 42;

            Assert.That((int)obj["key"], Is.EqualTo(42));
            Assert.That(obj["key"].ValueType, Is.EqualTo(KVValueType.Int32));
        }

        [Test]
        public void ImplicitStringToKVObject()
        {
            var obj = new KVObject("root", [new KVObject("key", "placeholder")]);
            obj["key"] = "hello";

            Assert.That((string)obj["key"], Is.EqualTo("hello"));
            Assert.That(obj["key"].ValueType, Is.EqualTo(KVValueType.String));
        }

        [Test]
        public void ImplicitBoolToKVObject()
        {
            var obj = new KVObject("root", [new KVObject("key", "placeholder")]);
            obj["key"] = true;

            Assert.That((bool)obj["key"], Is.True);
            Assert.That(obj["key"].ValueType, Is.EqualTo(KVValueType.Boolean));
        }

        [Test]
        public void ImplicitFloatToKVObject()
        {
            var obj = new KVObject("root", [new KVObject("key", "placeholder")]);
            obj["key"] = 3.14f;

            Assert.That((float)obj["key"], Is.EqualTo(3.14f));
            Assert.That(obj["key"].ValueType, Is.EqualTo(KVValueType.FloatingPoint));
        }

        #endregion

        #region 3. Explicit operators to primitives

        [Test]
        public void ExplicitCastKVObjectToString()
        {
            var obj = new KVObject("root", [new KVObject("key", "hello")]);
            string result = (string)obj["key"];

            Assert.That(result, Is.EqualTo("hello"));
        }

        [Test]
        public void ExplicitCastKVObjectToInt()
        {
            var obj = new KVObject("root", [new KVObject("key", 99)]);
            int result = (int)obj["key"];

            Assert.That(result, Is.EqualTo(99));
        }

        #endregion

        #region 4. ValueType forwarding property

        [Test]
        public void ValueTypeForwardsForString()
        {
            var obj = new KVObject("test", "hello");
            Assert.That(obj.ValueType, Is.EqualTo(obj.Value.ValueType));
            Assert.That(obj.ValueType, Is.EqualTo(KVValueType.String));
        }

        [Test]
        public void ValueTypeForwardsForInt()
        {
            var obj = new KVObject("test", 42);
            Assert.That(obj.ValueType, Is.EqualTo(obj.Value.ValueType));
            Assert.That(obj.ValueType, Is.EqualTo(KVValueType.Int32));
        }

        [Test]
        public void ValueTypeForwardsForCollection()
        {
            var obj = new KVObject("test", [new KVObject("child", "v")]);
            Assert.That(obj.ValueType, Is.EqualTo(obj.Value.ValueType));
            Assert.That(obj.ValueType, Is.EqualTo(KVValueType.Collection));
        }

        [Test]
        public void ValueTypeForwardsForNull()
        {
            var obj = new KVObject("test", default(KVValue));
            Assert.That(obj.ValueType, Is.EqualTo(obj.Value.ValueType));
            Assert.That(obj.ValueType, Is.EqualTo(KVValueType.Null));
        }

        [Test]
        public void DefaultConstructorCreatesEmptyCollection()
        {
            var obj = new KVObject("test");
            Assert.That(obj.ValueType, Is.EqualTo(KVValueType.Collection));
            Assert.That(obj.Count, Is.EqualTo(0));
        }

        #endregion

        #region 5. KVValue readonly record struct

        [Test]
        public void DefaultKVValueIsNull()
        {
            var value = default(KVValue);
            Assert.That(value.IsNull, Is.True);
        }

        [Test]
        public void DefaultKVValueValueTypeIsNull()
        {
            var value = default(KVValue);
            Assert.That(value.ValueType, Is.EqualTo(KVValueType.Null));
        }

        [Test]
        public void WithExpressionPreservesValueType()
        {
            KVValue value = 42;
            var modified = value with { Flag = KVFlag.Resource };

            Assert.That(modified.Flag, Is.EqualTo(KVFlag.Resource));
            Assert.That(modified.ValueType, Is.EqualTo(KVValueType.Int32));
            Assert.That((int)modified, Is.EqualTo(42));
        }

        #endregion

        #region 6. Mutation methods

        [Test]
        public void AddChildToCollection()
        {
            var obj = new KVObject("root", [new KVObject("a", "1")]);
            obj.Add(new KVObject("b", "2"));

            Assert.That(obj.Count, Is.EqualTo(2));
            Assert.That((string)obj["b"], Is.EqualTo("2"));
        }

        [Test]
        public void RemoveChildFromCollection()
        {
            var obj = new KVObject("root", [
                new KVObject("a", "1"),
                new KVObject("b", "2"),
            ]);

            var removed = obj.Remove("a");

            Assert.That(removed, Is.True);
            Assert.That(obj.Count, Is.EqualTo(1));
            Assert.That(obj["a"], Is.Null);
        }

        [Test]
        public void RemoveReturnsFalseForMissingKey()
        {
            var obj = new KVObject("root", [new KVObject("a", "1")]);
            Assert.That(obj.Remove("missing"), Is.False);
        }

        [Test]
        public void RemoveAtFromArray()
        {
            var arr = KVObject.Array("arr", [
                new KVObject(null, "x"),
                new KVObject(null, "y"),
                new KVObject(null, "z"),
            ]);

            arr.RemoveAt(1);

            Assert.That(arr.Count, Is.EqualTo(2));
            Assert.That((string)arr[0], Is.EqualTo("x"));
            Assert.That((string)arr[1], Is.EqualTo("z"));
        }

        [Test]
        public void ClearCollection()
        {
            var obj = new KVObject("root", [
                new KVObject("a", "1"),
                new KVObject("b", "2"),
            ]);

            obj.Clear();

            Assert.That(obj.Count, Is.EqualTo(0));
        }

        [Test]
        public void ClearArray()
        {
            var arr = KVObject.Array("arr", [
                new KVObject(null, "x"),
                new KVObject(null, "y"),
            ]);

            arr.Clear();

            Assert.That(arr.Count, Is.EqualTo(0));
        }

        #endregion

        #region 7. CreateArray factory

        [Test]
        public void CreateArrayIsArray()
        {
            var arr = KVObject.Array("test", [
                new KVObject(null, "a"),
                new KVObject(null, "b"),
            ]);

            Assert.That(arr.IsArray, Is.True);
            Assert.That(arr.ValueType, Is.EqualTo(KVValueType.Array));
        }

        [Test]
        public void CreateArrayElementsAccessibleByIndex()
        {
            var arr = KVObject.Array("test", [
                new KVObject(null, "first"),
                new KVObject(null, "second"),
                new KVObject(null, "third"),
            ]);

            Assert.That((string)arr[0], Is.EqualTo("first"));
            Assert.That((string)arr[1], Is.EqualTo("second"));
            Assert.That((string)arr[2], Is.EqualTo("third"));
            Assert.That(arr.Count, Is.EqualTo(3));
        }

        #endregion

        #region 7b. Collection factory

        [Test]
        public void CollectionFactoryCreatesDictBacked()
        {
            var obj = KVObject.Collection("root");

            Assert.That(obj.ValueType, Is.EqualTo(KVValueType.Collection));
            Assert.That(obj.Count, Is.EqualTo(0));

            obj.Add(new KVObject("key", (KVValue)"value"));
            Assert.That((string)obj["key"], Is.EqualTo("value"));
        }

        [Test]
        public void CollectionFactoryWithChildrenCreatesDictBacked()
        {
            var obj = KVObject.Collection("root", [
                new KVObject("a", (KVValue)"1"),
                new KVObject("b", (KVValue)"2"),
            ]);

            Assert.That(obj.ValueType, Is.EqualTo(KVValueType.Collection));
            Assert.That(obj.Count, Is.EqualTo(2));
            Assert.That((string)obj["a"], Is.EqualTo("1"));
            Assert.That((string)obj["b"], Is.EqualTo("2"));
        }

        [Test]
        public void ListCollectionFactoryCreatesListBacked()
        {
            var obj = KVObject.ListCollection("root");

            Assert.That(obj.ValueType, Is.EqualTo(KVValueType.Collection));
            Assert.That(obj.Count, Is.EqualTo(0));

            obj.Add(new KVObject("key", (KVValue)"value"));
            Assert.That((string)obj["key"], Is.EqualTo("value"));
        }

        [Test]
        public void ListCollectionFactoryWithChildrenCreatesListBacked()
        {
            var obj = KVObject.ListCollection("root", [
                new KVObject("a", (KVValue)"1"),
                new KVObject("b", (KVValue)"2"),
            ]);

            Assert.That(obj.ValueType, Is.EqualTo(KVValueType.Collection));
            Assert.That(obj.Count, Is.EqualTo(2));
            Assert.That((string)obj["a"], Is.EqualTo("1"));
            Assert.That((string)obj["b"], Is.EqualTo("2"));
        }

        [Test]
        public void ListCollectionAllowsDuplicateKeys()
        {
            var obj = KVObject.ListCollection("root", [
                new KVObject("key", (KVValue)"first"),
                new KVObject("key", (KVValue)"second"),
            ]);

            Assert.That(obj.Count, Is.EqualTo(2));
            // GetChild returns first match
            Assert.That((string)obj["key"], Is.EqualTo("first"));
        }

        [Test]
        public void EmptyArrayFactory()
        {
            var arr = KVObject.Array("test");

            Assert.That(arr.IsArray, Is.True);
            Assert.That(arr.ValueType, Is.EqualTo(KVValueType.Array));
            Assert.That(arr.Count, Is.EqualTo(0));

            arr.Add((KVValue)"element");
            Assert.That(arr.Count, Is.EqualTo(1));
            Assert.That((string)arr[0], Is.EqualTo("element"));
        }

        [Test]
        public void DefaultConstructorCreatesDictBacked()
        {
            var obj = new KVObject("root");

            // Should be dict-backed, same as Collection()
            obj.Add(new KVObject("x", (KVValue)"1"));
            obj.Add(new KVObject("y", (KVValue)"2"));

            Assert.That(obj.Count, Is.EqualTo(2));
            Assert.That((string)obj["x"], Is.EqualTo("1"));
            Assert.That((string)obj["y"], Is.EqualTo("2"));
        }

        #endregion

        #region 8. KV3 deserialization produces dict-backed collections

        [Test]
        public void KV3DeserializationContainsKeyWorks()
        {
            using var stream = TestDataHelper.OpenResource("TextKV3.basic.kv3");
            var data = KVSerializer.Create(KVSerializationFormat.KeyValues3Text).Deserialize(stream);

            Assert.That(data.ContainsKey("foo"), Is.True);
            Assert.That(data.ContainsKey("nonexistent"), Is.False);
        }

        [Test]
        public void KV3DeserializationChildrenIterable()
        {
            using var stream = TestDataHelper.OpenResource("TextKV3.basic.kv3");
            var data = KVSerializer.Create(KVSerializationFormat.KeyValues3Text).Deserialize(stream);

            var children = data.Children.ToList();
            Assert.That(children, Has.Count.GreaterThanOrEqualTo(1));
            Assert.That(children[0].Name, Is.EqualTo("foo"));
        }

        #endregion

        #region 9. Null string handling

        [Test]
        public void NullStringProducesNullTypedKVValue()
        {
            KVValue value = (string)null;

            Assert.That(value.ValueType, Is.EqualTo(KVValueType.Null));
            Assert.That(value.IsNull, Is.True);
        }

        #endregion

        #region 10. KVObject enumeration

        [Test]
        public void ForeachWorksForCollections()
        {
            var obj = new KVObject("root", [
                new KVObject("a", "1"),
                new KVObject("b", "2"),
                new KVObject("c", "3"),
            ]);

            var names = new List<string>();
            foreach (var child in obj)
            {
                names.Add(child.Name);
            }

            Assert.That(names, Is.EqualTo(new[] { "a", "b", "c" }));
        }

        [Test]
        public void ForeachWorksForArrays()
        {
            var arr = KVObject.Array("arr", [
                new KVObject(null, "x"),
                new KVObject(null, "y"),
            ]);

            var values = new List<string>();
            foreach (var child in arr)
            {
                values.Add((string)child);
            }

            Assert.That(values, Is.EqualTo(new[] { "x", "y" }));
        }

        [Test]
        public void ForeachYieldsNothingForScalars()
        {
            var obj = new KVObject("scalar", "hello");

            var items = new List<KVObject>();
            foreach (var child in obj)
            {
                items.Add(child);
            }

            Assert.That(items, Is.Empty);
        }

        #endregion
    }
}
