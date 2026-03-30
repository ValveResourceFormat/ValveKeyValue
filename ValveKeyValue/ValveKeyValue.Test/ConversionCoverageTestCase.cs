using System.Globalization;
using System.Linq;
using System.Text;

namespace ValveKeyValue.Test
{
    class ConversionCoverageTestCase
    {
        #region Missing implicit operators (short, ushort, IntPtr)

        [Test]
        public void ImplicitShortToKVObject()
        {
            KVObject v = (short)42;
            Assert.That(v.ValueType, Is.EqualTo(KVValueType.Int16));
            Assert.That((short)v, Is.EqualTo((short)42));
        }

        [Test]
        public void ImplicitUShortToKVObject()
        {
            KVObject v = (ushort)42;
            Assert.That(v.ValueType, Is.EqualTo(KVValueType.UInt16));
            Assert.That((ushort)v, Is.EqualTo((ushort)42));
        }

        [Test]
        public void ImplicitIntPtrToKVObject()
        {
            KVObject v = new IntPtr(42);
            Assert.That(v.ValueType, Is.EqualTo(KVValueType.Pointer));
            Assert.That((int)v, Is.EqualTo(42));
        }

        #endregion

        #region Missing explicit operators (IntPtr)

        [Test]
        public void ExplicitKVObjectToIntPtr()
        {
            KVObject v = 42;
            IntPtr p = (IntPtr)v;
            Assert.That(p, Is.EqualTo(new IntPtr(42)));
        }

        #endregion

        #region KVObject IntPtr and byte operators

        [Test]
        public void KVObjectToIntPtr()
        {
            KVObject obj = new IntPtr(123);
            IntPtr p = (IntPtr)obj;
            Assert.That(p, Is.EqualTo(new IntPtr(123)));
        }

        [Test]
        public void KVObjectToByte()
        {
            KVObject obj = 7;
            byte b = (byte)obj;
            Assert.That(b, Is.EqualTo((byte)7));
        }

        #endregion

        #region Cross-type conversions (store as X, read as Y)

        [Test]
        public void IntToFloat()
        {
            KVObject v = 42;
            float f = (float)v;
            Assert.That(f, Is.EqualTo(42.0f));
        }

        [Test]
        public void FloatToInt()
        {
            KVObject v = 3.14f;
            int i = (int)v;
            Assert.That(i, Is.EqualTo(3));
        }

        [Test]
        public void IntToLong()
        {
            KVObject v = 42;
            long l = (long)v;
            Assert.That(l, Is.EqualTo(42L));
        }

        [Test]
        public void IntToString()
        {
            KVObject v = 42;
            string s = (string)v;
            Assert.That(s, Is.EqualTo("42"));
        }

        [Test]
        public void StringToInt()
        {
            KVObject v = "123";
            int i = (int)v;
            Assert.That(i, Is.EqualTo(123));
        }

        #endregion

        #region Error paths for non-convertible types

        [Test]
        public void CollectionToIntThrows()
        {
            var obj = KVObject.ListCollection();
            obj.Add("a", 1);
            Assert.That(() => (int)obj, Throws.InstanceOf<NotSupportedException>());
        }

        [Test]
        public void NullToIntThrows()
        {
            var nullVal = KVObject.Null();
            Assert.That(() => (int)nullVal, Throws.InstanceOf<NotSupportedException>());
        }

        [Test]
        public void BinaryBlobToIntThrows()
        {
            var blobVal = KVObject.Blob(new byte[] { 1, 2, 3 });
            Assert.That(() => (int)blobVal, Throws.InstanceOf<NotSupportedException>());
        }

        #endregion

        #region byte[] implicit operator

        [Test]
        public void ImplicitByteArrayToKVObject()
        {
            KVObject v = new byte[] { 1, 2, 3 };
            Assert.That(v.ValueType, Is.EqualTo(KVValueType.BinaryBlob));
            Assert.That(v.AsBlob(), Is.EqualTo(new byte[] { 1, 2, 3 }));
        }

        [Test]
        public void NullByteArrayToKVObjectIsNull()
        {
            KVObject v = (byte[])null;
            Assert.That(v.IsNull, Is.True);
        }

        #endregion

        #region KVObject.Blob factory

        [Test]
        public void KVObjectBlobFactory()
        {
            var obj = KVObject.Blob(new byte[] { 0xAB, 0xCD });
            Assert.That(obj.ValueType, Is.EqualTo(KVValueType.BinaryBlob));
            Assert.That(obj.AsBlob(), Is.EqualTo(new byte[] { 0xAB, 0xCD }));
        }

        #endregion

        #region KVObject IConvertible (Convert.ChangeType works directly)

        [Test]
        public void ConvertChangeTypeOnKVObjectInt()
        {
            KVObject obj = 42;
            var result = Convert.ChangeType(obj, typeof(int), CultureInfo.InvariantCulture);
            Assert.That(result, Is.EqualTo(42));
        }

        [Test]
        public void ConvertChangeTypeOnKVObjectString()
        {
            KVObject obj = "hello";
            var result = Convert.ChangeType(obj, typeof(string), CultureInfo.InvariantCulture);
            Assert.That(result, Is.EqualTo("hello"));
        }

        [Test]
        public void ConvertToInt32OnKVObject()
        {
            KVObject obj = 99;
            var result = Convert.ToInt32(obj, CultureInfo.InvariantCulture);
            Assert.That(result, Is.EqualTo(99));
        }

        [Test]
        public void ConvertToDoubleOnKVObject()
        {
            KVObject obj = 3.14;
            var result = Convert.ToDouble(obj, CultureInfo.InvariantCulture);
            Assert.That(result, Is.EqualTo(3.14));
        }

        [Test]
        public void ConvertChangeTypeOnIndexerResult()
        {
            var root = KVObject.ListCollection();
            root.Add("name", "world");
            root.Add("count", 7);

            Assert.That(Convert.ToString(root["name"], CultureInfo.InvariantCulture), Is.EqualTo("world"));
            Assert.That(Convert.ToInt32(root["count"], CultureInfo.InvariantCulture), Is.EqualTo(7));
        }

        [Test]
        public void KVObjectIConvertibleToStringWithProvider()
        {
            KVObject obj = 3.14f;
            var convertible = (IConvertible)obj;
            var result = convertible.ToString(System.Globalization.CultureInfo.InvariantCulture);
            Assert.That(result, Is.EqualTo("3.14"));
        }

        #endregion

        #region Dictionary-backed collection mutation

        [Test]
        public void DictBackedCollectionAddAndLookup()
        {
            var kv3Text = "<!-- kv3 encoding:text:version{e21c7f3c-8a33-41c5-9977-a76d3a32aa0d} format:generic:version{7412167c-06e9-4698-aff2-e63eb59037e7} -->\n{\n\tkey1 = \"value1\"\n\tkey2 = 42\n}";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(kv3Text));
            var data = KVSerializer.Create(KVSerializationFormat.KeyValues3Text).Deserialize(stream);

            // Verify initial state
            Assert.That(data.ContainsKey("key1"), Is.True);
            Assert.That((string)data["key1"], Is.EqualTo("value1"));
            Assert.That((int)data["key2"], Is.EqualTo(42));
            Assert.That(data.Count, Is.EqualTo(2));

            // Add puts into dict
            data.Add("key3", "value3");
            Assert.That(data.Count, Is.EqualTo(3));
            Assert.That((string)data["key3"], Is.EqualTo("value3"));

            // Remove removes from dict
            var removed = data.Remove("key1");
            Assert.That(removed, Is.True);
            Assert.That(data.ContainsKey("key1"), Is.False);
            Assert.That(data.Count, Is.EqualTo(2));

            // Clear empties dict
            data.Clear();
            Assert.That(data.Count, Is.EqualTo(0));
        }

        [Test]
        public void DictBackedCollectionSetChildViaIndexer()
        {
            var kv3Text = "<!-- kv3 encoding:text:version{e21c7f3c-8a33-41c5-9977-a76d3a32aa0d} format:generic:version{7412167c-06e9-4698-aff2-e63eb59037e7} -->\n{\n\tkey1 = \"value1\"\n}";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(kv3Text));
            var data = KVSerializer.Create(KVSerializationFormat.KeyValues3Text).Deserialize(stream);

            // Set via indexer
            data["key1"] = "updated";
            Assert.That((string)data["key1"], Is.EqualTo("updated"));

            // Set new key via indexer
            data["newkey"] = 99;
            Assert.That((int)data["newkey"], Is.EqualTo(99));
            Assert.That(data.Count, Is.EqualTo(2));
        }

        [Test]
        public void DictBackedCollectionSetNullRemovesKey()
        {
            var kv3Text = "<!-- kv3 encoding:text:version{e21c7f3c-8a33-41c5-9977-a76d3a32aa0d} format:generic:version{7412167c-06e9-4698-aff2-e63eb59037e7} -->\n{\n\tkey1 = \"value1\"\n\tkey2 = 42\n}";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(kv3Text));
            var data = KVSerializer.Create(KVSerializationFormat.KeyValues3Text).Deserialize(stream);

            Assert.That(data.Count, Is.EqualTo(2));

            data["key1"] = null;

            Assert.That(data.ContainsKey("key1"), Is.False);
            Assert.That(data["key1"], Is.Null);
            Assert.That(data.Count, Is.EqualTo(1));
            Assert.That((int)data["key2"], Is.EqualTo(42));
        }

        #endregion

        #region Implicit byte/sbyte operators

        [Test]
        public void ImplicitByteToKVObject()
        {
            KVObject v = (byte)255;
            Assert.That(v.ValueType, Is.EqualTo(KVValueType.Int32));
            Assert.That((int)v, Is.EqualTo(255));
            Assert.That((byte)v, Is.EqualTo((byte)255));
        }

        [Test]
        public void ImplicitSByteToKVObject()
        {
            KVObject v = (sbyte)-42;
            Assert.That(v.ValueType, Is.EqualTo(KVValueType.Int32));
            Assert.That((int)v, Is.EqualTo(-42));
            Assert.That((sbyte)v, Is.EqualTo((sbyte)-42));
        }

        [Test]
        public void ImplicitByteInAdd()
        {
            var obj = KVObject.Collection();
            obj.Add("val", (byte)10);
            Assert.That((int)obj["val"], Is.EqualTo(10));
        }

        #endregion

        #region Keys and Values properties

        [Test]
        public void KeysOnDictCollection()
        {
            var obj = KVObject.Collection();
            obj.Add("a", 1);
            obj.Add("b", 2);
            obj.Add("c", 3);

            var keys = obj.Keys.ToList();
            Assert.That(keys, Has.Member("a").And.Member("b").And.Member("c").And.Count.EqualTo(3));
        }

        [Test]
        public void KeysOnListCollection()
        {
            var obj = KVObject.ListCollection();
            obj.Add("x", 10);
            obj.Add("y", 20);
            obj.Add("x", 30); // duplicate key

            var keys = obj.Keys.ToList();
            Assert.That(keys, Has.Count.EqualTo(3));
            Assert.That(keys[0], Is.EqualTo("x"));
            Assert.That(keys[1], Is.EqualTo("y"));
            Assert.That(keys[2], Is.EqualTo("x"));
        }

        [Test]
        public void KeysOnScalarIsEmpty()
        {
            KVObject obj = 42;
            Assert.That(obj.Keys.Any(), Is.False);
        }

        [Test]
        public void KeysOnArrayIsEmpty()
        {
            var arr = KVObject.Array();
            arr.Add(1);
            arr.Add(2);
            Assert.That(arr.Keys.Any(), Is.False);
        }

        [Test]
        public void ValuesOnDictCollection()
        {
            var obj = KVObject.Collection();
            obj.Add("a", 1);
            obj.Add("b", 2);

            var values = obj.Values.Select(v => (int)v).ToList();
            Assert.That(values, Has.Member(1).And.Member(2).And.Count.EqualTo(2));
        }

        [Test]
        public void ValuesOnListCollection()
        {
            var obj = KVObject.ListCollection();
            obj.Add("a", "hello");
            obj.Add("b", "world");

            var values = obj.Values.Select(v => (string)v).ToList();
            Assert.That(values, Has.Count.EqualTo(2));
            Assert.That(values[0], Is.EqualTo("hello"));
            Assert.That(values[1], Is.EqualTo("world"));
        }

        [Test]
        public void ValuesOnArray()
        {
            var arr = KVObject.Array();
            arr.Add(10);
            arr.Add(20);
            arr.Add(30);

            var values = arr.Values.Select(v => (int)v).ToList();
            Assert.That(values, Has.Count.EqualTo(3));
            Assert.That(values[0], Is.EqualTo(10));
            Assert.That(values[1], Is.EqualTo(20));
            Assert.That(values[2], Is.EqualTo(30));
        }

        [Test]
        public void ValuesOnScalarIsEmpty()
        {
            KVObject obj = "hello";
            Assert.That(obj.Values.Any(), Is.False);
        }

        [Test]
        public void ValuesOnNullIsEmpty()
        {
            var obj = KVObject.Null();
            Assert.That(obj.Values.Any(), Is.False);
        }

        #endregion
    }
}
