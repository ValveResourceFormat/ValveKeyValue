using System.Collections;
using System.Linq;

namespace ValveKeyValue
{
    class KVCollectionValue : KVValue, IEnumerable<KVObject>
    {
        public KVCollectionValue()
        {
            children = new List<KVObject>();
        }

        readonly List<KVObject> children;

        public override KVValueType ValueType => KVValueType.Collection;

        public override KVValue this[string key] => Get(key)?.Value;

        public void Add(KVObject value)
        {
            ArgumentNullException.ThrowIfNull(value);
            children.Add(value);
        }

        public void AddRange(IEnumerable<KVObject> values)
        {
            ArgumentNullException.ThrowIfNull(values);
            children.AddRange(values);
        }

        public KVObject Get(string name)
        {
            ArgumentNullException.ThrowIfNull(name);
            return children.FirstOrDefault(c => c.Name == name);
        }

        public void Set(string name, KVValue value)
        {
            ArgumentNullException.ThrowIfNull(name);
            ArgumentNullException.ThrowIfNull(value);

            children.RemoveAll(kv => kv.Name == name);
            children.Add(new KVObject(name, value));
        }

        #region IEnumerable<KVObject>

        public IEnumerator<KVObject> GetEnumerator() => children.GetEnumerator();

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

        public override string ToString() => "[Collection]";
    }
}
