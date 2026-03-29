using System.Text;

namespace ValveKeyValue.Test
{
    class ConversionCoverageTestCase
    {
        #region 1. Missing implicit operators (short, ushort, IntPtr)

        [Test]
        public void ImplicitShortToKVValue()
        {
            KVValue v = (short)42;
            Assert.That(v.ValueType, Is.EqualTo(KVValueType.Int16));
            Assert.That((short)v, Is.EqualTo((short)42));
        }

        [Test]
        public void ImplicitUShortToKVValue()
        {
            KVValue v = (ushort)42;
            Assert.That(v.ValueType, Is.EqualTo(KVValueType.UInt16));
            Assert.That((ushort)v, Is.EqualTo((ushort)42));
        }

        [Test]
        public void ImplicitIntPtrToKVValue()
        {
            KVValue v = new IntPtr(42);
            Assert.That(v.ValueType, Is.EqualTo(KVValueType.Pointer));
            Assert.That((int)v, Is.EqualTo(42));
        }

        #endregion

        #region 2. Missing explicit operators (IntPtr)

        [Test]
        public void ExplicitKVValueToIntPtr()
        {
            var v = (KVValue)42;
            IntPtr p = (IntPtr)v;
            Assert.That(p, Is.EqualTo(new IntPtr(42)));
        }

        #endregion

        #region 3. KVObject IntPtr and byte operators

        [Test]
        public void KVObjectToIntPtr()
        {
            var obj = new KVObject("test", (KVValue)new IntPtr(123));
            IntPtr p = (IntPtr)obj;
            Assert.That(p, Is.EqualTo(new IntPtr(123)));
        }

        [Test]
        public void KVObjectToByte()
        {
            var obj = new KVObject("test", (KVValue)7);
            byte b = (byte)obj;
            Assert.That(b, Is.EqualTo((byte)7));
        }

        #endregion

        #region 4. Cross-type conversions (store as X, read as Y)

        [Test]
        public void IntToFloat()
        {
            var v = (KVValue)42;
            float f = (float)v;
            Assert.That(f, Is.EqualTo(42.0f));
        }

        [Test]
        public void FloatToInt()
        {
            var v = (KVValue)3.14f;
            int i = (int)v;
            Assert.That(i, Is.EqualTo(3));
        }

        [Test]
        public void IntToLong()
        {
            var v = (KVValue)42;
            long l = (long)v;
            Assert.That(l, Is.EqualTo(42L));
        }

        [Test]
        public void IntToString()
        {
            var v = (KVValue)42;
            string s = (string)v;
            Assert.That(s, Is.EqualTo("42"));
        }

        [Test]
        public void StringToInt()
        {
            var v = (KVValue)"123";
            int i = (int)v;
            Assert.That(i, Is.EqualTo(123));
        }

        #endregion

        #region 5. Error paths for non-convertible types

        [Test]
        public void CollectionToIntThrows()
        {
            var obj = new KVObject("test", new KVObject[] { new KVObject("a", (KVValue)1) });
            Assert.That(() => (int)obj.Value, Throws.InstanceOf<NotSupportedException>());
        }

        [Test]
        public void NullToIntThrows()
        {
            var nullVal = default(KVValue);
            Assert.That(() => (int)nullVal, Throws.InstanceOf<NotSupportedException>());
        }

        [Test]
        public void BinaryBlobToIntThrows()
        {
            var blobVal = KVValue.Blob(new byte[] { 1, 2, 3 });
            Assert.That(() => (int)blobVal, Throws.InstanceOf<NotSupportedException>());
        }

        #endregion

        #region 6. byte[] implicit operator

        [Test]
        public void ImplicitByteArrayToKVValue()
        {
            KVValue v = new byte[] { 1, 2, 3 };
            Assert.That(v.ValueType, Is.EqualTo(KVValueType.BinaryBlob));
            Assert.That(v.AsBlob(), Is.EqualTo(new byte[] { 1, 2, 3 }));
        }

        [Test]
        public void NullByteArrayToKVValueIsNull()
        {
            KVValue v = (byte[])null;
            Assert.That(v.IsNull, Is.True);
        }

        #endregion

        #region 7. KVObject.Blob factory

        [Test]
        public void KVObjectBlobFactory()
        {
            var obj = KVObject.Blob("test", new byte[] { 0xAB, 0xCD });
            Assert.That(obj.Name, Is.EqualTo("test"));
            Assert.That(obj.ValueType, Is.EqualTo(KVValueType.BinaryBlob));
            Assert.That(obj.Value.AsBlob(), Is.EqualTo(new byte[] { 0xAB, 0xCD }));
        }

        #endregion

        /*
        #region 8. KV2Element basic tests

        [Test]
        public void KV2ElementBasicProperties()
        {
            var elementId = Guid.NewGuid();
            var elem = new KV2Element("test") { ElementId = elementId, ClassName = "DmeElement" };

            Assert.That(elem.Name, Is.EqualTo("test"));
            Assert.That(elem.ClassName, Is.EqualTo("DmeElement"));
            Assert.That(elem.ElementId, Is.EqualTo(elementId));
            Assert.That(elem.ElementId, Is.Not.EqualTo(Guid.Empty));
            Assert.That(elem, Is.InstanceOf<KVObject>());
        }

        [Test]
        public void KV2ElementWithChildren()
        {
            var child = new KV2Element("child", (KVValue)"hello") { ElementId = Guid.NewGuid(), ClassName = "DmeChild" };
            var parent = new KV2Element("parent", new KVObject[] { child }) { ElementId = Guid.NewGuid(), ClassName = "DmeParent" };

            Assert.That(parent.Count, Is.EqualTo(1));
            Assert.That(parent["child"], Is.InstanceOf<KV2Element>());
        }

        [Test]
        public void KV2ElementPatternMatching()
        {
            var parent = new KV2Element("parent", new KVObject[] {
                new KV2Element("child", (KVValue)"hello") { ElementId = Guid.NewGuid(), ClassName = "DmeChild" }
            })
            { ElementId = Guid.NewGuid(), ClassName = "DmeParent" };

            KVObject obj = parent;
            Assert.That(obj is KV2Element, Is.True);

            if (obj is KV2Element kv2)
            {
                Assert.That(kv2.ClassName, Is.EqualTo("DmeParent"));
            }
        }

        #endregion
        */

        #region 9. KVObject IConvertible (Convert.ChangeType works directly)

        [Test]
        public void ConvertChangeTypeOnKVObjectInt()
        {
            var obj = new KVObject("test", (KVValue)42);
            var result = Convert.ChangeType(obj, typeof(int));
            Assert.That(result, Is.EqualTo(42));
        }

        [Test]
        public void ConvertChangeTypeOnKVObjectString()
        {
            var obj = new KVObject("test", (KVValue)"hello");
            var result = Convert.ChangeType(obj, typeof(string));
            Assert.That(result, Is.EqualTo("hello"));
        }

        [Test]
        public void ConvertToInt32OnKVObject()
        {
            var obj = new KVObject("test", (KVValue)99);
            var result = Convert.ToInt32(obj);
            Assert.That(result, Is.EqualTo(99));
        }

        [Test]
        public void ConvertToDoubleOnKVObject()
        {
            var obj = new KVObject("test", (KVValue)3.14);
            var result = Convert.ToDouble(obj);
            Assert.That(result, Is.EqualTo(3.14));
        }

        [Test]
        public void ConvertChangeTypeOnIndexerResult()
        {
            var root = new KVObject("root", [
                new KVObject("name", (KVValue)"world"),
                new KVObject("count", (KVValue)7),
            ]);

            Assert.That(Convert.ToString(root["name"]), Is.EqualTo("world"));
            Assert.That(Convert.ToInt32(root["count"]), Is.EqualTo(7));
        }

        [Test]
        public void KVObjectIConvertibleToStringWithProvider()
        {
            var obj = new KVObject("test", (KVValue)3.14f);
            var convertible = (IConvertible)obj;
            var result = convertible.ToString(System.Globalization.CultureInfo.InvariantCulture);
            Assert.That(result, Is.EqualTo("3.14"));
        }

        #endregion

        #region 10. Dictionary-backed collection mutation

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
            data.Add(new KVObject("key3", (KVValue)"value3"));
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

        #endregion
    }
}
