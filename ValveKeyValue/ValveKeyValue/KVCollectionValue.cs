using System.Collections;
using System.Linq;

namespace ValveKeyValue
{
    /// <summary>
    /// Represents a collection of named KeyValue children.
    /// </summary>
    public class KVCollectionValue : KVValue, IEnumerable<KVObject>
    {
        /// <inheritdoc cref="KVCollectionValue"/>
        public KVCollectionValue()
        {
            children = new List<KVObject>();
        }

        readonly List<KVObject> children;

        /// <inheritdoc/>
        public override KVValueType ValueType => KVValueType.Collection;

        /// <inheritdoc cref="KVArrayValue.Count"/>
        public int Count => children.Count;

        /// <inheritdoc/>
        public override KVValue this[string key] => Get(key)?.Value;

        /// <inheritdoc cref="KVObject.Add(KVObject)"/>
        public void Add(KVObject value)
        {
            ArgumentNullException.ThrowIfNull(value);
            children.Add(value);
        }

        /// <inheritdoc cref="KVArrayValue.AddRange"/>
        public void AddRange(IEnumerable<KVObject> values)
        {
            ArgumentNullException.ThrowIfNull(values);
            children.AddRange(values);
        }

        /// <summary>
        /// Gets a child by name.
        /// </summary>
        public KVObject Get(string name)
        {
            ArgumentNullException.ThrowIfNull(name);
            return children.FirstOrDefault(c => c.Name == name);
        }

        /// <summary>
        /// Sets or replaces a child by name.
        /// </summary>
        public void Set(string name, KVValue value)
        {
            ArgumentNullException.ThrowIfNull(name);
            ArgumentNullException.ThrowIfNull(value);

            children.RemoveAll(kv => kv.Name == name);
            children.Add(new KVObject(name, value));
        }

        #region IEnumerable<KVObject>

        /// <inheritdoc/>
        public IEnumerator<KVObject> GetEnumerator() => children.GetEnumerator();

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

        /// <inheritdoc/>
        IEnumerator IEnumerable.GetEnumerator() => children.GetEnumerator();

        #endregion

        /// <inheritdoc/>
        public override string ToString() => "[Collection]";
    }
}
