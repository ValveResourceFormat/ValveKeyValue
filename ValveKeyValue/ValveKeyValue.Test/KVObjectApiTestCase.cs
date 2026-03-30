using System.Globalization;
using System.Linq;

namespace ValveKeyValue.Test
{
    class KVObjectApiTestCase
    {
        private static readonly string[] ExpectedNames_abc = ["a", "b", "c"];
        private static readonly string[] ExpectedValues_xy = ["x", "y"];

        #region String indexer returns KVObject

        [Test]
        public void IndexerReturnsKVObjectForExistingKey()
        {
            var obj = KVObject.ListCollection();
            obj.Add("key", "value");
            var child = obj["key"];

            Assert.That(child, Is.Not.Null);
            Assert.That(child, Is.InstanceOf<KVObject>());
            Assert.That((string)child, Is.EqualTo("value"));
        }

        [Test]
        public void IndexerReturnsNullForMissingKey()
        {
            var obj = KVObject.ListCollection();
            obj.Add("key", "value");
            Assert.That(obj["missing"], Is.Null);
        }

        [Test]
        public void ChainedReadThroughNestedCollections()
        {
            var inner = KVObject.ListCollection();
            inner.Add("b", "deep");
            var obj = KVObject.ListCollection();
            obj.Add("a", inner);

            Assert.That((string)obj["a"]["b"], Is.EqualTo("deep"));
        }

        [Test]
        public void ChainedWriteModifiesTree()
        {
            var inner = KVObject.ListCollection();
            inner.Add("b", "original");
            var obj = KVObject.ListCollection();
            obj.Add("a", inner);

            obj["a"]["b"] = 42;

            Assert.That((int)obj["a"]["b"], Is.EqualTo(42));
        }

        #endregion

        #region Typed constructors

        [Test]
        public void ConstructorWithString()
        {
            var obj = new KVObject("hello");
            Assert.That(obj.ValueType, Is.EqualTo(KVValueType.String));
            Assert.That((string)obj, Is.EqualTo("hello"));
        }

        [Test]
        public void ConstructorWithInt()
        {
            var obj = new KVObject(42);
            Assert.That(obj.ValueType, Is.EqualTo(KVValueType.Int32));
            Assert.That((int)obj, Is.EqualTo(42));
        }

        [Test]
        public void ConstructorWithUInt()
        {
            var obj = new KVObject(42u);
            Assert.That(obj.ValueType, Is.EqualTo(KVValueType.UInt32));
            Assert.That((uint)obj, Is.EqualTo(42u));
        }

        [Test]
        public void ConstructorWithLong()
        {
            var obj = new KVObject(123456789012345L);
            Assert.That(obj.ValueType, Is.EqualTo(KVValueType.Int64));
            Assert.That((long)obj, Is.EqualTo(123456789012345L));
        }

        [Test]
        public void ConstructorWithULong()
        {
            var obj = new KVObject(0x8877665544332211UL);
            Assert.That(obj.ValueType, Is.EqualTo(KVValueType.UInt64));
            Assert.That((ulong)obj, Is.EqualTo(0x8877665544332211UL));
        }

        [Test]
        public void ConstructorWithFloat()
        {
            var obj = new KVObject(3.14f);
            Assert.That(obj.ValueType, Is.EqualTo(KVValueType.FloatingPoint));
            Assert.That((float)obj, Is.EqualTo(3.14f));
        }

        [Test]
        public void ConstructorWithDouble()
        {
            var obj = new KVObject(3.14159265);
            Assert.That(obj.ValueType, Is.EqualTo(KVValueType.FloatingPoint64));
            Assert.That((double)obj, Is.EqualTo(3.14159265));
        }

        [Test]
        public void ConstructorWithBool()
        {
            var obj = new KVObject(true);
            Assert.That(obj.ValueType, Is.EqualTo(KVValueType.Boolean));
            Assert.That((bool)obj, Is.True);
        }

        [Test]
        public void ConstructorWithIntPtr()
        {
            var obj = new KVObject(new IntPtr(0x12345678));
            Assert.That(obj.ValueType, Is.EqualTo(KVValueType.Pointer));
        }

        [Test]
        public void ConstructorWithNullString()
        {
            var obj = new KVObject((string)null);
            Assert.That(obj.ValueType, Is.EqualTo(KVValueType.Null));
            Assert.That(obj.IsNull, Is.True);
        }

        #endregion

        #region Implicit operators from primitives

        [Test]
        public void ImplicitIntToKVObject()
        {
            var obj = KVObject.ListCollection();
            obj.Add("key", 42);

            Assert.That((int)obj["key"], Is.EqualTo(42));
            Assert.That(obj["key"].ValueType, Is.EqualTo(KVValueType.Int32));
        }

        [Test]
        public void ImplicitStringToKVObject()
        {
            var obj = KVObject.ListCollection();
            obj.Add("key", "hello");

            Assert.That((string)obj["key"], Is.EqualTo("hello"));
            Assert.That(obj["key"].ValueType, Is.EqualTo(KVValueType.String));
        }

        [Test]
        public void ImplicitBoolToKVObject()
        {
            var obj = KVObject.ListCollection();
            obj.Add("key", true);

            Assert.That((bool)obj["key"], Is.True);
            Assert.That(obj["key"].ValueType, Is.EqualTo(KVValueType.Boolean));
        }

        [Test]
        public void ImplicitFloatToKVObject()
        {
            var obj = KVObject.ListCollection();
            obj.Add("key", 3.14f);

            Assert.That((float)obj["key"], Is.EqualTo(3.14f));
            Assert.That(obj["key"].ValueType, Is.EqualTo(KVValueType.FloatingPoint));
        }

        #endregion

        #region ToString

        [Test]
        public void ToStringReturnsValueForStringObject()
        {
            var obj = new KVObject("hello");
            Assert.That(obj.ToString(CultureInfo.InvariantCulture), Is.EqualTo("hello"));
        }

        [Test]
        public void ToStringReturnsValueForIntObject()
        {
            var obj = new KVObject(42);
            Assert.That(obj.ToString(CultureInfo.InvariantCulture), Is.EqualTo("42"));
        }

        [Test]
        public void ToStringReturnsCollectionForCollection()
        {
            var obj = KVObject.ListCollection();
            obj.Add("a", "b");
            Assert.That(obj.ToString(CultureInfo.InvariantCulture), Is.EqualTo("[Collection]"));
        }

        #endregion

        #region Explicit operators to primitives

        [Test]
        public void ExplicitCastKVObjectToString()
        {
            var obj = KVObject.ListCollection();
            obj.Add("key", "hello");
            string result = (string)obj["key"];

            Assert.That(result, Is.EqualTo("hello"));
        }

        [Test]
        public void ExplicitCastKVObjectToInt()
        {
            var obj = KVObject.ListCollection();
            obj.Add("key", 99);
            int result = (int)obj["key"];

            Assert.That(result, Is.EqualTo(99));
        }

        #endregion

        #region ValueType property

        [Test]
        public void ValueTypeForString()
        {
            var obj = new KVObject("hello");
            Assert.That(obj.ValueType, Is.EqualTo(KVValueType.String));
        }

        [Test]
        public void ValueTypeForInt()
        {
            var obj = new KVObject(42);
            Assert.That(obj.ValueType, Is.EqualTo(KVValueType.Int32));
        }

        [Test]
        public void ValueTypeForCollection()
        {
            var obj = KVObject.ListCollection();
            obj.Add("child", "v");
            Assert.That(obj.ValueType, Is.EqualTo(KVValueType.Collection));
        }

        [Test]
        public void ValueTypeForNull()
        {
            var obj = KVObject.Null();
            Assert.That(obj.ValueType, Is.EqualTo(KVValueType.Null));
        }

        [Test]
        public void DefaultConstructorCreatesEmptyCollection()
        {
            var obj = new KVObject();
            Assert.That(obj.ValueType, Is.EqualTo(KVValueType.Collection));
            Assert.That(obj.Count, Is.EqualTo(0));
        }

        #endregion

        #region KVObject null

        [Test]
        public void NullKVObjectIsNull()
        {
            var value = KVObject.Null();
            Assert.That(value.IsNull, Is.True);
        }

        [Test]
        public void NullKVObjectValueTypeIsNull()
        {
            var value = KVObject.Null();
            Assert.That(value.ValueType, Is.EqualTo(KVValueType.Null));
        }

        [Test]
        public void FlagMutationPreservesValueType()
        {
            KVObject value = 42;
            value.Flag = KVFlag.Resource;

            Assert.That(value.Flag, Is.EqualTo(KVFlag.Resource));
            Assert.That(value.ValueType, Is.EqualTo(KVValueType.Int32));
            Assert.That((int)value, Is.EqualTo(42));
        }

        #endregion

        #region Mutation methods

        [Test]
        public void AddChildToCollection()
        {
            var obj = KVObject.ListCollection();
            obj.Add("a", "1");
            obj.Add("b", "2");

            Assert.That(obj.Count, Is.EqualTo(2));
            Assert.That((string)obj["b"], Is.EqualTo("2"));
        }

        [Test]
        public void RemoveChildFromCollection()
        {
            var obj = KVObject.ListCollection();
            obj.Add("a", "1");
            obj.Add("b", "2");

            var removed = obj.Remove("a");

            Assert.That(removed, Is.True);
            Assert.That(obj.Count, Is.EqualTo(1));
            Assert.That(obj["a"], Is.Null);
        }

        [Test]
        public void RemoveReturnsFalseForMissingKey()
        {
            var obj = KVObject.ListCollection();
            obj.Add("a", "1");
            Assert.That(obj.Remove("missing"), Is.False);
        }

        [Test]
        public void RemoveAtFromArray()
        {
            var arr = KVObject.Array([
                "x",
                "y",
                "z",
            ]);

            arr.RemoveAt(1);

            Assert.That(arr.Count, Is.EqualTo(2));
            Assert.That((string)arr[0], Is.EqualTo("x"));
            Assert.That((string)arr[1], Is.EqualTo("z"));
        }

        [Test]
        public void ClearCollection()
        {
            var obj = KVObject.ListCollection();
            obj.Add("a", "1");
            obj.Add("b", "2");

            obj.Clear();

            Assert.That(obj.Count, Is.EqualTo(0));
        }

        [Test]
        public void ClearArray()
        {
            var arr = KVObject.Array([
                "x",
                "y",
            ]);

            arr.Clear();

            Assert.That(arr.Count, Is.EqualTo(0));
        }

        #endregion

        #region CreateArray factory

        [Test]
        public void CreateArrayIsArray()
        {
            var arr = KVObject.Array([
                "a",
                "b",
            ]);

            Assert.That(arr.IsArray, Is.True);
            Assert.That(arr.ValueType, Is.EqualTo(KVValueType.Array));
        }

        [Test]
        public void CreateArrayElementsAccessibleByIndex()
        {
            var arr = KVObject.Array([
                "first",
                "second",
                "third",
            ]);

            Assert.That((string)arr[0], Is.EqualTo("first"));
            Assert.That((string)arr[1], Is.EqualTo("second"));
            Assert.That((string)arr[2], Is.EqualTo("third"));
            Assert.That(arr.Count, Is.EqualTo(3));
        }

        #endregion

        #region Collection factory

        [Test]
        public void CollectionFactoryCreatesDictBacked()
        {
            var obj = KVObject.Collection();

            Assert.That(obj.ValueType, Is.EqualTo(KVValueType.Collection));
            Assert.That(obj.Count, Is.EqualTo(0));

            obj.Add("key", "value");
            Assert.That((string)obj["key"], Is.EqualTo("value"));
        }

        [Test]
        public void CollectionFactoryWithChildrenCreatesDictBacked()
        {
            var obj = KVObject.Collection(new[]
            {
                new KeyValuePair<string, KVObject>("a", "1"),
                new KeyValuePair<string, KVObject>("b", "2"),
            });

            Assert.That(obj.ValueType, Is.EqualTo(KVValueType.Collection));
            Assert.That(obj.Count, Is.EqualTo(2));
            Assert.That((string)obj["a"], Is.EqualTo("1"));
            Assert.That((string)obj["b"], Is.EqualTo("2"));
        }

        [Test]
        public void ListCollectionFactoryCreatesListBacked()
        {
            var obj = KVObject.ListCollection();

            Assert.That(obj.ValueType, Is.EqualTo(KVValueType.Collection));
            Assert.That(obj.Count, Is.EqualTo(0));

            obj.Add("key", "value");
            Assert.That((string)obj["key"], Is.EqualTo("value"));
        }

        [Test]
        public void ListCollectionFactoryWithChildrenCreatesListBacked()
        {
            var obj = KVObject.ListCollection(new[]
            {
                new KeyValuePair<string, KVObject>("a", "1"),
                new KeyValuePair<string, KVObject>("b", "2"),
            });

            Assert.That(obj.ValueType, Is.EqualTo(KVValueType.Collection));
            Assert.That(obj.Count, Is.EqualTo(2));
            Assert.That((string)obj["a"], Is.EqualTo("1"));
            Assert.That((string)obj["b"], Is.EqualTo("2"));
        }

        [Test]
        public void ListCollectionAllowsDuplicateKeys()
        {
            var obj = KVObject.ListCollection(new[]
            {
                new KeyValuePair<string, KVObject>("key", "first"),
                new KeyValuePair<string, KVObject>("key", "second"),
            });

            Assert.That(obj.Count, Is.EqualTo(2));
            // GetChild returns first match
            Assert.That((string)obj["key"], Is.EqualTo("first"));
        }

        [Test]
        public void EmptyArrayFactory()
        {
            var arr = KVObject.Array();

            Assert.That(arr.IsArray, Is.True);
            Assert.That(arr.ValueType, Is.EqualTo(KVValueType.Array));
            Assert.That(arr.Count, Is.EqualTo(0));

            arr.Add("element");
            Assert.That(arr.Count, Is.EqualTo(1));
            Assert.That((string)arr[0], Is.EqualTo("element"));
        }

        [Test]
        public void DefaultConstructorCreatesDictBacked()
        {
            var obj = new KVObject();

            // Should be dict-backed, same as Collection()
            obj.Add("x", "1");
            obj.Add("y", "2");

            Assert.That(obj.Count, Is.EqualTo(2));
            Assert.That((string)obj["x"], Is.EqualTo("1"));
            Assert.That((string)obj["y"], Is.EqualTo("2"));
        }

        #endregion

        #region KV3 deserialization produces dict-backed collections

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
            Assert.That(children[0].Key, Is.EqualTo("foo"));
        }

        #endregion

        #region Null string handling

        [Test]
        public void NullStringProducesNullTypedKVObject()
        {
            KVObject value = (string)null;

            Assert.That(value.ValueType, Is.EqualTo(KVValueType.Null));
            Assert.That(value.IsNull, Is.True);
        }

        #endregion

        #region KVObject enumeration

        [Test]
        public void ForeachWorksForCollections()
        {
            var obj = KVObject.ListCollection();
            obj.Add("a", "1");
            obj.Add("b", "2");
            obj.Add("c", "3");

            var names = new List<string>();
            foreach (var (key, value) in obj)
            {
                names.Add(key);
            }

            Assert.That(names, Is.EqualTo(ExpectedNames_abc));
        }

        [Test]
        public void ForeachWorksForArrays()
        {
            var arr = KVObject.Array([
                "x",
                "y",
            ]);

            var values = new List<string>();
            foreach (var (key, value) in arr)
            {
                values.Add((string)value);
            }

            Assert.That(values, Is.EqualTo(ExpectedValues_xy));
        }

        [Test]
        public void ForeachYieldsNothingForScalars()
        {
            var obj = new KVObject("hello");

            var items = new List<KVObject>();
            foreach (var (key, value) in obj)
            {
                items.Add(value);
            }

            Assert.That(items, Is.Empty);
        }

        #endregion
    }
}
