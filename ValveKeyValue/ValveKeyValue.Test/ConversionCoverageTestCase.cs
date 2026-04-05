using System.Globalization;
using System.Linq;
using System.Text;

namespace ValveKeyValue.Test
{
    class ConversionCoverageTestCase
    {
        #region Short and ushort constructors

        [Test]
        public void ShortConstructorPreservesType()
        {
            var obj = new KVObject((short)42);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(obj.ValueType, Is.EqualTo(KVValueType.Int16));
                Assert.That((short)obj, Is.EqualTo((short)42));
            }
        }

        [Test]
        public void UShortConstructorPreservesType()
        {
            var obj = new KVObject((ushort)42);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(obj.ValueType, Is.EqualTo(KVValueType.UInt16));
                Assert.That((ushort)obj, Is.EqualTo((ushort)42));
            }
        }

        [Test]
        public void ShortConstructorMatchesImplicitOperator()
        {
            var fromCtor = new KVObject((short)-100);
            KVObject fromOp = (short)-100;
            using (Assert.EnterMultipleScope())
            {
                Assert.That(fromCtor.ValueType, Is.EqualTo(fromOp.ValueType));
                Assert.That((short)fromCtor, Is.EqualTo((short)fromOp));
            }
        }

        [Test]
        public void UShortConstructorMatchesImplicitOperator()
        {
            var fromCtor = new KVObject((ushort)500);
            KVObject fromOp = (ushort)500;
            using (Assert.EnterMultipleScope())
            {
                Assert.That(fromCtor.ValueType, Is.EqualTo(fromOp.ValueType));
                Assert.That((ushort)fromCtor, Is.EqualTo((ushort)fromOp));
            }
        }

        #endregion

        #region Convenience conversion overloads

        [Test]
        public void ToByte_FromInt()
        {
            KVObject obj = 42;
            Assert.That(obj.ToByte(null), Is.EqualTo((byte)42));
        }

        [Test]
        public void ToSByte_FromInt()
        {
            KVObject obj = -42;
            Assert.That(obj.ToSByte(null), Is.EqualTo((sbyte)-42));
        }

        [Test]
        public void ToChar_FromString()
        {
            KVObject obj = "A";
            Assert.That(obj.ToChar(null), Is.EqualTo('A'));
        }

        [Test]
        public void ToInt16_FromShort()
        {
            KVObject obj = (short)-1000;
            Assert.That(obj.ToInt16(null), Is.EqualTo((short)-1000));
        }

        [Test]
        public void ToUInt16_FromUShort()
        {
            KVObject obj = (ushort)1000;
            Assert.That(obj.ToUInt16(null), Is.EqualTo((ushort)1000));
        }

        [Test]
        public void ToUInt32_FromUInt()
        {
            KVObject obj = 42U;
            Assert.That(obj.ToUInt32(null), Is.EqualTo(42U));
        }

        [Test]
        public void ToUInt64_FromULong()
        {
            KVObject obj = 42UL;
            Assert.That(obj.ToUInt64(null), Is.EqualTo(42UL));
        }

        [Test]
        public void ToDecimal_FromString()
        {
            KVObject obj = "79228162514264337593543950335";
            Assert.That(obj.ToDecimal(null), Is.EqualTo(79228162514264337593543950335m));
        }

        #endregion

        #region Implicit operators (short, ushort, IntPtr)

        [Test]
        public void ImplicitShortToKVObject()
        {
            KVObject v = (short)42;
            using (Assert.EnterMultipleScope())
            {
                Assert.That(v.ValueType, Is.EqualTo(KVValueType.Int16));
                Assert.That((short)v, Is.EqualTo((short)42));
            }
        }

        [Test]
        public void ImplicitUShortToKVObject()
        {
            KVObject v = (ushort)42;
            using (Assert.EnterMultipleScope())
            {
                Assert.That(v.ValueType, Is.EqualTo(KVValueType.UInt16));
                Assert.That((ushort)v, Is.EqualTo((ushort)42));
            }
        }

        [Test]
        public void ImplicitIntPtrToKVObject()
        {
            KVObject v = new IntPtr(42);
            using (Assert.EnterMultipleScope())
            {
                Assert.That(v.ValueType, Is.EqualTo(KVValueType.Pointer));
                Assert.That((int)v, Is.EqualTo(42));
            }
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
            using (Assert.EnterMultipleScope())
            {
                Assert.That(v.ValueType, Is.EqualTo(KVValueType.BinaryBlob));
                Assert.That(v.AsBlob(), Is.EqualTo(new byte[] { 1, 2, 3 }));
            }
        }

        [Test]
        public void NullByteArrayToKVObjectThrows()
        {
            Assert.That(() => { KVObject v = (byte[])null!; }, Throws.ArgumentNullException);
        }

        #endregion

        #region KVObject.Blob factory

        [Test]
        public void KVObjectBlobFactory()
        {
            var obj = KVObject.Blob(new byte[] { 0xAB, 0xCD });
            using (Assert.EnterMultipleScope())
            {
                Assert.That(obj.ValueType, Is.EqualTo(KVValueType.BinaryBlob));
                Assert.That(obj.AsBlob(), Is.EqualTo(new byte[] { 0xAB, 0xCD }));
            }
        }

        #endregion

        #region Explicit byte[] operator

        [Test]
        public void ExplicitByteArrayFromKVObject()
        {
            var obj = KVObject.Blob(new byte[] { 1, 2, 3 });
            byte[] result = (byte[])obj;
            Assert.That(result, Is.EqualTo(new byte[] { 1, 2, 3 }));
        }

        [Test]
        public void ExplicitByteArrayFromNonBlobThrows()
        {
            var obj = new KVObject(42);
            Assert.That(() => (byte[])obj, Throws.InstanceOf<InvalidOperationException>());
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

            using (Assert.EnterMultipleScope())
            {
                Assert.That(Convert.ToString(root["name"], CultureInfo.InvariantCulture), Is.EqualTo("world"));
                Assert.That(Convert.ToInt32(root["count"], CultureInfo.InvariantCulture), Is.EqualTo(7));
            }
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
            var data = KVSerializer.Create(KVSerializationFormat.KeyValues3Text).Deserialize(stream).Root;

            using (Assert.EnterMultipleScope())
            {
                // Verify initial state
                Assert.That(data.ContainsKey("key1"), Is.True);
                Assert.That((string)data["key1"], Is.EqualTo("value1"));
                Assert.That((int)data["key2"], Is.EqualTo(42));
                Assert.That(data, Has.Count.EqualTo(2));
            }

            // Add puts into dict
            data.Add("key3", "value3");
            using (Assert.EnterMultipleScope())
            {
                Assert.That(data, Has.Count.EqualTo(3));
                Assert.That((string)data["key3"], Is.EqualTo("value3"));
            }

            // Remove removes from dict
            var removed = data.Remove("key1");
            using (Assert.EnterMultipleScope())
            {
                Assert.That(removed, Is.True);
                Assert.That(data.ContainsKey("key1"), Is.False);
                Assert.That(data, Has.Count.EqualTo(2));
            }

            // Clear empties dict
            data.Clear();
            Assert.That(data, Is.Empty);
        }

        [Test]
        public void DictBackedCollectionSetChildViaIndexer()
        {
            var kv3Text = "<!-- kv3 encoding:text:version{e21c7f3c-8a33-41c5-9977-a76d3a32aa0d} format:generic:version{7412167c-06e9-4698-aff2-e63eb59037e7} -->\n{\n\tkey1 = \"value1\"\n}";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(kv3Text));
            var data = KVSerializer.Create(KVSerializationFormat.KeyValues3Text).Deserialize(stream).Root;

            // Set via indexer
            data["key1"] = "updated";
            Assert.That((string)data["key1"], Is.EqualTo("updated"));

            // Set new key via indexer
            data["newkey"] = 99;
            using (Assert.EnterMultipleScope())
            {
                Assert.That((int)data["newkey"], Is.EqualTo(99));
                Assert.That(data, Has.Count.EqualTo(2));
            }
        }

        [Test]
        public void DictBackedCollectionSetNullStoresNullValue()
        {
            var kv3Text = "<!-- kv3 encoding:text:version{e21c7f3c-8a33-41c5-9977-a76d3a32aa0d} format:generic:version{7412167c-06e9-4698-aff2-e63eb59037e7} -->\n{\n\tkey1 = \"value1\"\n\tkey2 = 42\n}";
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(kv3Text));
            var data = KVSerializer.Create(KVSerializationFormat.KeyValues3Text).Deserialize(stream).Root;

            Assert.That(data, Has.Count.EqualTo(2));

            data["key1"] = KVObject.Null();

            Assert.That(data, Has.Count.EqualTo(2));
            using (Assert.EnterMultipleScope())
            {
                Assert.That(data["key1"].IsNull, Is.True);
                Assert.That((int)data["key2"], Is.EqualTo(42));
            }
        }

        #endregion

        #region Implicit byte/sbyte operators

        [Test]
        public void ImplicitByteToKVObject()
        {
            KVObject v = (byte)255;
            using (Assert.EnterMultipleScope())
            {
                Assert.That(v.ValueType, Is.EqualTo(KVValueType.Int32));
                Assert.That((int)v, Is.EqualTo(255));
                Assert.That((byte)v, Is.EqualTo((byte)255));
            }
        }

        [Test]
        public void ImplicitSByteToKVObject()
        {
            KVObject v = (sbyte)-42;
            using (Assert.EnterMultipleScope())
            {
                Assert.That(v.ValueType, Is.EqualTo(KVValueType.Int32));
                Assert.That((int)v, Is.EqualTo(-42));
                Assert.That((sbyte)v, Is.EqualTo((sbyte)-42));
            }
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
            using (Assert.EnterMultipleScope())
            {
                Assert.That(keys[0], Is.EqualTo("x"));
                Assert.That(keys[1], Is.EqualTo("y"));
                Assert.That(keys[2], Is.EqualTo("x"));
            }
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
            using (Assert.EnterMultipleScope())
            {
                Assert.That(values[0], Is.EqualTo("hello"));
                Assert.That(values[1], Is.EqualTo("world"));
            }
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
            using (Assert.EnterMultipleScope())
            {
                Assert.That(values[0], Is.EqualTo(10));
                Assert.That(values[1], Is.EqualTo(20));
                Assert.That(values[2], Is.EqualTo(30));
            }
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

        #region Native type cross-conversion: success paths

        [Test]
        public void BooleanConversions()
        {
            KVObject t = true;
            KVObject f = false;

            using (Assert.EnterMultipleScope())
            {
                Assert.That((bool)t, Is.True);
                Assert.That((bool)f, Is.False);
                Assert.That((int)t, Is.EqualTo(1));
                Assert.That((int)f, Is.Zero);
                Assert.That((long)t, Is.EqualTo(1L));
                Assert.That((uint)t, Is.EqualTo(1U));
                Assert.That((ulong)t, Is.EqualTo(1UL));
                Assert.That((float)t, Is.EqualTo(1.0f));
                Assert.That((double)t, Is.EqualTo(1.0));
                Assert.That((string)t, Is.EqualTo("1"));
                Assert.That((string)f, Is.EqualTo("0"));
                Assert.That(t.ToDecimal(null), Is.EqualTo(1m));
            }
        }

        [Test]
        public void Int16Conversions()
        {
            KVObject v = (short)-42;

            using (Assert.EnterMultipleScope())
            {
                Assert.That((short)v, Is.EqualTo((short)-42));
                Assert.That((int)v, Is.EqualTo(-42));
                Assert.That((long)v, Is.EqualTo(-42L));
                Assert.That((float)v, Is.EqualTo(-42.0f));
                Assert.That((double)v, Is.EqualTo(-42.0));
                Assert.That((string)v, Is.EqualTo("-42"));
                Assert.That(v.ToDecimal(null), Is.EqualTo(-42m));
                Assert.That((bool)v, Is.True);
            }
        }

        [Test]
        public void UInt16Conversions()
        {
            KVObject v = (ushort)60000;

            using (Assert.EnterMultipleScope())
            {
                Assert.That((ushort)v, Is.EqualTo((ushort)60000));
                Assert.That((int)v, Is.EqualTo(60000));
                Assert.That((long)v, Is.EqualTo(60000L));
                Assert.That((uint)v, Is.EqualTo(60000U));
                Assert.That((ulong)v, Is.EqualTo(60000UL));
                Assert.That((float)v, Is.EqualTo(60000.0f));
                Assert.That((double)v, Is.EqualTo(60000.0));
                Assert.That((string)v, Is.EqualTo("60000"));
                Assert.That(v.ToDecimal(null), Is.EqualTo(60000m));
            }
        }

        [Test]
        public void Int32Conversions()
        {
            KVObject v = -100;

            using (Assert.EnterMultipleScope())
            {
                Assert.That((int)v, Is.EqualTo(-100));
                Assert.That((long)v, Is.EqualTo(-100L));
                Assert.That((short)v, Is.EqualTo((short)-100));
                Assert.That((double)v, Is.EqualTo(-100.0));
                Assert.That((string)v, Is.EqualTo("-100"));
                Assert.That(v.ToDecimal(null), Is.EqualTo(-100m));
                Assert.That((bool)v, Is.True);
            }
        }

        [Test]
        public void UInt32Conversions()
        {
            KVObject v = (uint)42;

            using (Assert.EnterMultipleScope())
            {
                Assert.That((uint)v, Is.EqualTo(42U));
                Assert.That((int)v, Is.EqualTo(42));
                Assert.That((long)v, Is.EqualTo(42L));
                Assert.That((ulong)v, Is.EqualTo(42UL));
                Assert.That((float)v, Is.EqualTo(42.0f));
                Assert.That((double)v, Is.EqualTo(42.0));
                Assert.That((string)v, Is.EqualTo("42"));
                Assert.That(v.ToDecimal(null), Is.EqualTo(42m));
                Assert.That((bool)v, Is.True);
            }
        }

        [Test]
        public void Int64Conversions()
        {
            KVObject v = 100_000L;

            using (Assert.EnterMultipleScope())
            {
                Assert.That((long)v, Is.EqualTo(100_000L));
                Assert.That((int)v, Is.EqualTo(100_000));
                Assert.That((float)v, Is.EqualTo(100_000.0f));
                Assert.That((double)v, Is.EqualTo(100_000.0));
                Assert.That((string)v, Is.EqualTo("100000"));
                Assert.That(v.ToDecimal(null), Is.EqualTo(100_000m));
                Assert.That((ulong)v, Is.EqualTo(100_000UL));
            }
        }

        [Test]
        public void UInt64Conversions()
        {
            KVObject v = (ulong)42;

            using (Assert.EnterMultipleScope())
            {
                Assert.That((ulong)v, Is.EqualTo(42UL));
                Assert.That((long)v, Is.EqualTo(42L));
                Assert.That((int)v, Is.EqualTo(42));
                Assert.That((float)v, Is.EqualTo(42.0f));
                Assert.That((double)v, Is.EqualTo(42.0));
                Assert.That((string)v, Is.EqualTo("42"));
                Assert.That(v.ToDecimal(null), Is.EqualTo(42m));
                Assert.That((bool)v, Is.True);
            }
        }

        [Test]
        public void LargeUInt64Conversions()
        {
            KVObject v = ulong.MaxValue;

            using (Assert.EnterMultipleScope())
            {
                Assert.That((ulong)v, Is.EqualTo(ulong.MaxValue));
                Assert.That((float)v, Is.EqualTo((float)ulong.MaxValue));
                Assert.That((double)v, Is.EqualTo((double)ulong.MaxValue));
                Assert.That((string)v, Is.EqualTo("18446744073709551615"));
                Assert.That(v.ToDecimal(null), Is.EqualTo((decimal)ulong.MaxValue));
                Assert.That((bool)v, Is.True);
            }
        }

        [Test]
        public void FloatingPointConversions()
        {
            KVObject v = 3.14f;

            using (Assert.EnterMultipleScope())
            {
                Assert.That((float)v, Is.EqualTo(3.14f));
                Assert.That((double)v, Is.EqualTo(3.14f).Within(0.001));
                Assert.That((int)v, Is.EqualTo(3));
                Assert.That((long)v, Is.EqualTo(3L));
                Assert.That(v.ToDecimal(null), Is.EqualTo(3.14m).Within(0.01m));
                Assert.That((bool)v, Is.True);
                Assert.That((bool)(KVObject)0.0f, Is.False);
            }
        }

        [Test]
        public void FloatingPoint64Conversions()
        {
            KVObject v = 3.14159265;

            using (Assert.EnterMultipleScope())
            {
                Assert.That((double)v, Is.EqualTo(3.14159265));
                Assert.That((float)v, Is.EqualTo(3.14159265f).Within(0.0001f));
                Assert.That((int)v, Is.EqualTo(3));
                Assert.That((long)v, Is.EqualTo(3L));
                Assert.That(v.ToDecimal(null), Is.EqualTo(3.14159265m).Within(0.0001m));
                Assert.That((string)v, Does.Contain("3.14159"));
                Assert.That((bool)v, Is.True);
                Assert.That((bool)(KVObject)0.0, Is.False);
            }
        }

        [Test]
        public void PointerConversions()
        {
            KVObject v = new IntPtr(42);

            using (Assert.EnterMultipleScope())
            {
                Assert.That((IntPtr)v, Is.EqualTo(new IntPtr(42)));
                Assert.That((int)v, Is.EqualTo(42));
                Assert.That((long)v, Is.EqualTo(42L));
                Assert.That((string)v, Is.EqualTo("42"));
                Assert.That((bool)v, Is.True);
            }
        }

        #endregion

        #region Native type cross-conversion: overflow boundaries

        [Test]
        public void UInt32ToInt32ThrowsOnOverflow()
        {
            KVObject v = (uint)3_000_000_000;
            Assert.That(() => (int)v, Throws.InstanceOf<OverflowException>());
        }

        [Test]
        public void NegativeInt32ToUInt32ThrowsOnOverflow()
        {
            KVObject v = -1;
            Assert.That(() => (uint)v, Throws.InstanceOf<OverflowException>());
        }

        [Test]
        public void NegativeInt32ToUInt64ThrowsOnOverflow()
        {
            KVObject v = -1;
            Assert.That(() => (ulong)v, Throws.InstanceOf<OverflowException>());
        }

        [Test]
        public void NegativeInt64ToUInt64ThrowsOnOverflow()
        {
            KVObject v = (long)-1;
            Assert.That(() => (ulong)v, Throws.InstanceOf<OverflowException>());
        }

        [Test]
        public void NegativeInt16ToUInt32ThrowsOnOverflow()
        {
            KVObject v = (short)-1;
            Assert.That(() => (uint)v, Throws.InstanceOf<OverflowException>());
        }

        [Test]
        public void NegativeInt16ToUInt64ThrowsOnOverflow()
        {
            KVObject v = (short)-1;
            Assert.That(() => (ulong)v, Throws.InstanceOf<OverflowException>());
        }

        [Test]
        public void UInt16MaxToInt16ThrowsOnOverflow()
        {
            KVObject v = (ushort)65535;
            Assert.That(() => (short)v, Throws.InstanceOf<OverflowException>());
        }

        [Test]
        public void Int64MaxToInt32ThrowsOnOverflow()
        {
            KVObject v = long.MaxValue;
            Assert.That(() => (int)v, Throws.InstanceOf<OverflowException>());
        }

        [Test]
        public void LargeUInt64ToInt64ThrowsOnOverflow()
        {
            KVObject v = ulong.MaxValue;
            Assert.That(() => (long)v, Throws.InstanceOf<OverflowException>());
        }

        [Test]
        public void LargeUInt64ToInt32ThrowsOnOverflow()
        {
            KVObject v = ulong.MaxValue;
            Assert.That(() => (int)v, Throws.InstanceOf<OverflowException>());
        }

        [Test]
        public void LargeUInt64ToDecimalSucceeds()
        {
            var largeValue = (ulong)long.MaxValue + 1;
            KVObject v = largeValue;
            Assert.That(v.ToDecimal(null), Is.EqualTo((decimal)largeValue));
        }

        #endregion
    }
}
