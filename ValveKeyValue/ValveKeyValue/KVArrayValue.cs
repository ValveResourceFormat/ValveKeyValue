using System.Collections;

namespace ValveKeyValue
{
    public class KVArrayValue : KVValue, IEnumerable<KVValue>, ICollection<KVValue>, IList<KVValue>
    {
        public KVArrayValue()
        {
            children = new List<KVValue>();
        }

        readonly List<KVValue> children;

        public override KVValueType ValueType => KVValueType.Array;

        public int Count => children.Count;

        public bool IsReadOnly => false;

        public override KVValue this[string key]
        {
            get { throw new NotSupportedException($"The indexer on a {nameof(KVArrayValue)} can only be used on integer keys, not strings."); }
        }

        public KVValue this[int key]
        {
            get
            {
                return children[key];
            }
            set
            {
                children[key] = value;
            }
        }

        public void Add(KVValue value)
        {
            Require.NotNull(value, nameof(value));
            children.Add(value);
        }

        public void AddRange(IEnumerable<KVValue> values)
        {
            Require.NotNull(values, nameof(values));
            children.AddRange(values);
        }

        #region IEnumerable<KVValue>

        public IEnumerator<KVValue> GetEnumerator() => children.GetEnumerator();

        #endregion

        #region IConvertible

        public override TypeCode GetTypeCode()
        {
            throw new NotSupportedException();
        }

        public override bool ToBoolean(IFormatProvider provider)
        {
            throw new NotSupportedException();
        }

        public override byte ToByte(IFormatProvider provider)
        {
            throw new NotSupportedException();
        }

        public override char ToChar(IFormatProvider provider)
        {
            throw new NotSupportedException();
        }

        public override DateTime ToDateTime(IFormatProvider provider)
        {
            throw new NotSupportedException();
        }

        public override decimal ToDecimal(IFormatProvider provider)
        {
            throw new NotSupportedException();
        }

        public override double ToDouble(IFormatProvider provider)
        {
            throw new NotSupportedException();
        }

        public override short ToInt16(IFormatProvider provider)
        {
            throw new NotSupportedException();
        }

        public override int ToInt32(IFormatProvider provider)
        {
            throw new NotSupportedException();
        }

        public override long ToInt64(IFormatProvider provider)
        {
            throw new NotSupportedException();
        }

        public override sbyte ToSByte(IFormatProvider provider)
        {
            throw new NotSupportedException();
        }

        public override float ToSingle(IFormatProvider provider)
        {
            throw new NotSupportedException();
        }

        public override string ToString(IFormatProvider provider)
             => ToString();

        public override object ToType(Type conversionType, IFormatProvider provider)
        {
            throw new NotSupportedException();
        }

        public override ushort ToUInt16(IFormatProvider provider)
        {
            throw new NotSupportedException();
        }

        public override uint ToUInt32(IFormatProvider provider)
        {
            throw new NotSupportedException();
        }

        public override ulong ToUInt64(IFormatProvider provider)
        {
            throw new NotSupportedException();
        }

        #endregion

        #region IEnumerable

        IEnumerator IEnumerable.GetEnumerator() => children.GetEnumerator();

        #endregion

        public override string ToString() => "[Array]";

        public void Clear() => children.Clear();

        public bool Contains(KVValue item)
        {
            Require.NotNull(item, nameof(item));
            return children.Contains(item);
        }

        public void CopyTo(KVValue[] array, int arrayIndex)
        {
            Require.NotNull(array, nameof(array));
            children.CopyTo(array, arrayIndex);
        }

        public bool Remove(KVValue item)
        {
            Require.NotNull(item, nameof(item));
            return children.Remove(item);
        }

        public int IndexOf(KVValue item)
        {
            Require.NotNull(item, nameof(item));
            return children.IndexOf(item);
        }

        public void Insert(int index, KVValue item)
        {
            Require.NotNull(item, nameof(item));
            children.Insert(index, item);
        }

        public void RemoveAt(int index) => children.RemoveAt(index);
    }
}
