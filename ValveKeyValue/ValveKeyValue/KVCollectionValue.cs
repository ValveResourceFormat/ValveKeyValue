using System.Collections;
using System.Linq;

#nullable enable
namespace ValveKeyValue
{
    /// <summary>
    /// Represents a collection value.
    /// </summary>
    public class KVCollectionValue : KVValue, IEnumerable<KVObject>
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="KVCollectionValue"/> class.
        /// </summary>
        public KVCollectionValue()
        {
            children = new List<KVObject>();
        }

        readonly List<KVObject> children;

        /// <inheritdoc/>
        public override KVValueType ValueType => KVValueType.Collection;

        /// <inheritdoc/>
        public override KVValue? this[string key] => Get(key)?.Value;

        /// <summary>
        /// Adds the specified key-value object to the collection of child elements.
        /// </summary>
        /// <param name="value">The key-value object to add to the collection. Cannot be null.</param>
        public void Add(KVObject value)
        {
            ArgumentNullException.ThrowIfNull(value);
            children.Add(value);
        }

        /// <summary>
        /// Adds the elements of the specified collection to the end of the current collection.
        /// </summary>
        /// <param name="values">The collection of <see cref="KVObject"/> instances to add. Cannot be null.</param>
        public void AddRange(IEnumerable<KVObject> values)
        {
            ArgumentNullException.ThrowIfNull(values);
            children.AddRange(values);
        }

        /// <summary>
        /// Retrieves the first child element with the specified name, using the given string comparison option.
        /// </summary>
        /// <param name="name">The name of the child element to locate. Cannot be null.</param>
        /// <param name="comparisonType">One of the enumeration values that determines how the name comparison is performed. The default is
        /// StringComparison.CurrentCulture.</param>
        /// <returns>A KVObject representing the first matching child element if found; otherwise, null.</returns>
        public KVObject? Get(string name, StringComparison comparisonType = StringComparison.CurrentCulture)
        {
            ArgumentNullException.ThrowIfNull(name);
            return children.FirstOrDefault(c => string.Equals(c.Name, name, comparisonType));
        }

        /// <summary>
        /// Sets the value associated with the specified name, replacing any existing entry with the same name.
        /// </summary>
        /// <remarks>If an entry with the specified name already exists, it is removed before adding the
        /// new value. This method ensures that only one entry with the given name exists after the operation.</remarks>
        /// <param name="name">The name of the key to set. Cannot be null.</param>
        /// <param name="value">The value to associate with the specified name. Cannot be null.</param>
        public void Set(string name, KVValue value)
        {
            ArgumentNullException.ThrowIfNull(name);
            ArgumentNullException.ThrowIfNull(value);

            children.RemoveAll(kv => kv.Name == name);
            children.Add(new KVObject(name, value));
        }

        #region IEnumerable<KVObject>

        /// <summary>
        /// Returns an enumerator that iterates through the collection of child KVObject instances.
        /// </summary>
        /// <returns>An enumerator for the collection of child KVObject objects.</returns>
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

        IEnumerator IEnumerable.GetEnumerator() => children.GetEnumerator();

        #endregion

        /// <inheritdoc/>
        public override string ToString() => "[Collection]";
    }
}
