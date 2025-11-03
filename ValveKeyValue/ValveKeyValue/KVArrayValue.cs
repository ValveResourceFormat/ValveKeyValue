using System.Collections;

namespace ValveKeyValue
{
    /// <summary>
    /// Represents an array of KeyValue values.
    /// </summary>
    public class KVArrayValue : KVValue, IEnumerable<KVValue>, ICollection<KVValue>, IList<KVValue>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="KVArrayValue"/> class.
        /// </summary>
        public KVArrayValue()
        {
            children = new List<KVValue>();
        }

        readonly List<KVValue> children;

        /// <inheritdoc/>
        public override KVValueType ValueType => KVValueType.Array;

        /// <inheritdoc/>
        public int Count => children.Count;

        /// <inheritdoc/>
        public bool IsReadOnly => false;

        /// <inheritdoc/>
        public override KVValue this[string key]
        {
            get { throw new NotSupportedException($"The indexer on a {nameof(KVArrayValue)} can only be used on integer keys, not strings."); }
        }

        /// <inheritdoc/>
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

        /// <inheritdoc/>
        public void Add(KVValue value)
        {
            ArgumentNullException.ThrowIfNull(value);
            children.Add(value);
        }

        /// <summary>
        /// Adds multiple values to the array.
        /// </summary>
        /// <param name="values">The values to add.</param>
        public void AddRange(IEnumerable<KVValue> values)
        {
            ArgumentNullException.ThrowIfNull(values);
            children.AddRange(values);
        }

        #region IEnumerable<KVValue>

        /// <inheritdoc/>
        public IEnumerator<KVValue> GetEnumerator() => children.GetEnumerator();

        #endregion

        #region IConvertible

        /// <inheritdoc/>
        public override TypeCode GetTypeCode()
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public override bool ToBoolean(IFormatProvider provider)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public override byte ToByte(IFormatProvider provider)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public override char ToChar(IFormatProvider provider)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public override DateTime ToDateTime(IFormatProvider provider)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public override decimal ToDecimal(IFormatProvider provider)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public override double ToDouble(IFormatProvider provider)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public override short ToInt16(IFormatProvider provider)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public override int ToInt32(IFormatProvider provider)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public override long ToInt64(IFormatProvider provider)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public override sbyte ToSByte(IFormatProvider provider)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public override float ToSingle(IFormatProvider provider)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public override string ToString(IFormatProvider provider)
             => ToString();

        /// <inheritdoc/>
        public override object ToType(Type conversionType, IFormatProvider provider)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public override ushort ToUInt16(IFormatProvider provider)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public override uint ToUInt32(IFormatProvider provider)
        {
            throw new NotSupportedException();
        }

        /// <inheritdoc/>
        public override ulong ToUInt64(IFormatProvider provider)
        {
            throw new NotSupportedException();
        }

        #endregion

        #region IEnumerable

        IEnumerator IEnumerable.GetEnumerator() => children.GetEnumerator();

        #endregion

        /// <inheritdoc/>
        public override string ToString() => "[Array]";

        /// <inheritdoc/>
        public void Clear() => children.Clear();

        /// <inheritdoc/>
        public bool Contains(KVValue item)
        {
            ArgumentNullException.ThrowIfNull(item);
            return children.Contains(item);
        }

        /// <inheritdoc/>
        public void CopyTo(KVValue[] array, int arrayIndex)
        {
            ArgumentNullException.ThrowIfNull(array);
            children.CopyTo(array, arrayIndex);
        }

        /// <inheritdoc/>
        public bool Remove(KVValue item)
        {
            ArgumentNullException.ThrowIfNull(item);
            return children.Remove(item);
        }

        /// <inheritdoc/>
        public int IndexOf(KVValue item)
        {
            ArgumentNullException.ThrowIfNull(item);
            return children.IndexOf(item);
        }

        /// <inheritdoc/>
        public void Insert(int index, KVValue item)
        {
            ArgumentNullException.ThrowIfNull(item);
            children.Insert(index, item);
        }

        /// <inheritdoc/>
        public void RemoveAt(int index) => children.RemoveAt(index);
    }
}
