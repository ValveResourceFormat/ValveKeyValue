using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;

namespace ValveKeyValue.Metadata
{
    /// <summary>
    /// Creates the type information that maps types to and from KeyValues data.
    /// This type is infrastructure for generated serialization code and is not intended to be used directly.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class KVMetadata
    {
        // Values are read with the same conversions as the explicit conversion operators of KVObject.

        /// <summary>
        /// Gets the type information for <see cref="bool"/>. Strings are read as a number, where non-zero is
        /// <see langword="true"/>, or as <c>true</c> or <c>false</c> in any casing.
        /// </summary>
        public static KVTypeInfo<bool> Boolean { get; } = new KVScalarTypeInfo<bool>(
            static value => value.ValueType == KVValueType.String && bool.TryParse((string)value, out var parsed)
                ? parsed
                : value.ToBoolean(CultureInfo.InvariantCulture),
            static value => new KVObject(value));

        /// <summary>Gets the type information for <see cref="byte"/>. Values are written as <see cref="int"/>.</summary>
        public static KVTypeInfo<byte> Byte { get; } = new KVScalarTypeInfo<byte>(
            static value => value.ToByte(CultureInfo.InvariantCulture),
            static value => new KVObject((int)value));

        /// <summary>Gets the type information for <see cref="sbyte"/>. Values are written as <see cref="int"/>.</summary>
        public static KVTypeInfo<sbyte> SByte { get; } = new KVScalarTypeInfo<sbyte>(
            static value => value.ToSByte(CultureInfo.InvariantCulture),
            static value => new KVObject((int)value));

        /// <summary>Gets the type information for <see cref="char"/>. Values are written as a single-character string.</summary>
        public static KVTypeInfo<char> Char { get; } = new KVScalarTypeInfo<char>(
            static value => value.ToChar(CultureInfo.InvariantCulture),
            static value => new KVObject(value.ToString()));

        /// <summary>Gets the type information for <see cref="short"/>. Values are written as <see cref="int"/>.</summary>
        public static KVTypeInfo<short> Int16 { get; } = new KVScalarTypeInfo<short>(
            static value => value.ToInt16(CultureInfo.InvariantCulture),
            static value => new KVObject((int)value));

        /// <summary>Gets the type information for <see cref="ushort"/>. Values are written as <see cref="ulong"/>.</summary>
        public static KVTypeInfo<ushort> UInt16 { get; } = new KVScalarTypeInfo<ushort>(
            static value => value.ToUInt16(CultureInfo.InvariantCulture),
            static value => new KVObject((ulong)value));

        /// <summary>Gets the type information for <see cref="int"/>.</summary>
        public static KVTypeInfo<int> Int32 { get; } = new KVScalarTypeInfo<int>(
            static value => value.ToInt32(CultureInfo.InvariantCulture),
            static value => new KVObject(value));

        /// <summary>Gets the type information for <see cref="uint"/>. Values are written as <see cref="ulong"/>.</summary>
        public static KVTypeInfo<uint> UInt32 { get; } = new KVScalarTypeInfo<uint>(
            static value => value.ToUInt32(CultureInfo.InvariantCulture),
            static value => new KVObject((ulong)value));

        /// <summary>Gets the type information for <see cref="long"/>.</summary>
        public static KVTypeInfo<long> Int64 { get; } = new KVScalarTypeInfo<long>(
            static value => value.ToInt64(CultureInfo.InvariantCulture),
            static value => new KVObject(value));

        /// <summary>Gets the type information for <see cref="ulong"/>.</summary>
        public static KVTypeInfo<ulong> UInt64 { get; } = new KVScalarTypeInfo<ulong>(
            static value => value.ToUInt64(CultureInfo.InvariantCulture),
            static value => new KVObject(value));

        /// <summary>Gets the type information for <see cref="float"/>.</summary>
        public static KVTypeInfo<float> Single { get; } = new KVScalarTypeInfo<float>(
            static value => value.ToSingle(CultureInfo.InvariantCulture),
            static value => new KVObject(value));

        /// <summary>Gets the type information for <see cref="double"/>.</summary>
        public static KVTypeInfo<double> Double { get; } = new KVScalarTypeInfo<double>(
            static value => value.ToDouble(CultureInfo.InvariantCulture),
            static value => new KVObject(value));

        /// <summary>Gets the type information for <see cref="decimal"/>. Values are written as <see cref="double"/>.</summary>
        public static KVTypeInfo<decimal> Decimal { get; } = new KVScalarTypeInfo<decimal>(
            static value => value.ToDecimal(CultureInfo.InvariantCulture),
            static value => new KVObject((double)value));

        /// <summary>Gets the type information for <see cref="string"/>.</summary>
        public static KVTypeInfo<string> String { get; } = new KVScalarTypeInfo<string>(
            static value => value.ToString(CultureInfo.InvariantCulture),
            static value => new KVObject(value));

        /// <summary>Gets the type information for <see cref="System.IntPtr"/>.</summary>
        public static KVTypeInfo<nint> IntPtr { get; } = new KVScalarTypeInfo<nint>(
            static value => (nint)value,
            static value => new KVObject(value));

        /// <summary>Gets the type information for <see langword="byte[]"/>, which maps to a binary blob.</summary>
        public static KVTypeInfo<byte[]> ByteArray { get; } = new KVByteArrayTypeInfo();

        /// <summary>
        /// Creates the type information for an enum, which maps to its underlying value.
        /// </summary>
        /// <typeparam name="TEnum">The enum type.</typeparam>
        /// <typeparam name="TUnderlying">The underlying type of <typeparamref name="TEnum"/>.</typeparam>
        /// <param name="underlying">The type information of the underlying type, one of the integer scalars of <see cref="KVMetadata"/>.</param>
        /// <returns>The type information for <typeparamref name="TEnum"/>.</returns>
        public static KVTypeInfo<TEnum> Enum<TEnum, TUnderlying>(KVTypeInfo<TUnderlying> underlying)
            where TEnum : struct, System.Enum
            where TUnderlying : unmanaged
        {
            ArgumentNullException.ThrowIfNull(underlying);

            if (Unsafe.SizeOf<TEnum>() != Unsafe.SizeOf<TUnderlying>() || underlying is not KVScalarTypeInfo<TUnderlying> scalar)
            {
                throw new ArgumentException($"'{typeof(TUnderlying).Name}' is not the underlying type of '{typeof(TEnum).Name}'.", nameof(underlying));
            }

            return new KVEnumTypeInfo<TEnum, TUnderlying>(scalar);
        }

        /// <summary>
        /// Creates the type information for a nullable value type. Null values are omitted when writing.
        /// </summary>
        /// <typeparam name="T">The underlying value type.</typeparam>
        /// <param name="underlying">The type information of the underlying type.</param>
        /// <returns>The type information for <see cref="System.Nullable{T}"/>.</returns>
        public static KVTypeInfo<T?> Nullable<T>(KVTypeInfo<T> underlying)
            where T : struct
        {
            ArgumentNullException.ThrowIfNull(underlying);
            return new KVNullableTypeInfo<T>(underlying);
        }

        /// <summary>
        /// Creates the type information for a single-dimensional array, which maps to a KeyValues array
        /// or to a collection whose keys are the element indices.
        /// </summary>
        /// <typeparam name="TElement">The element type.</typeparam>
        /// <param name="element">The type information of the element type.</param>
        /// <returns>The type information for the array type.</returns>
        public static KVTypeInfo<TElement[]> Array<TElement>(KVTypeInfo<TElement> element)
        {
            ArgumentNullException.ThrowIfNull(element);
            return new KVCollectionTypeInfo<TElement[], TElement>(element);
        }

        /// <summary>
        /// Creates the type information for a collection, which maps to a KeyValues array or to a collection
        /// whose keys are the element indices.
        /// </summary>
        /// <remarks>
        /// Arrays, <see cref="List{T}"/>, <see cref="Collection{T}"/>, <see cref="ObservableCollection{T}"/>,
        /// <see cref="IList{T}"/>, <see cref="ICollection{T}"/>, <see cref="IReadOnlyList{T}"/>,
        /// <see cref="IReadOnlyCollection{T}"/> and <see cref="IEnumerable{T}"/> can be deserialized. Any other
        /// collection type can only be serialized.
        /// </remarks>
        /// <typeparam name="TCollection">The collection type.</typeparam>
        /// <typeparam name="TElement">The element type.</typeparam>
        /// <param name="element">The type information of the element type.</param>
        /// <returns>The type information for <typeparamref name="TCollection"/>.</returns>
        public static KVTypeInfo<TCollection> Collection<TCollection, TElement>(KVTypeInfo<TElement> element)
            where TCollection : IEnumerable<TElement>
        {
            ArgumentNullException.ThrowIfNull(element);
            return new KVCollectionTypeInfo<TCollection, TElement>(element);
        }

        /// <summary>
        /// Creates the type information for a dictionary, which maps to a KeyValues collection whose keys are
        /// the dictionary keys. When a key occurs more than once, the first occurrence is used.
        /// </summary>
        /// <remarks>
        /// <see cref="Dictionary{TKey, TValue}"/>, <see cref="IDictionary{TKey, TValue}"/> and
        /// <see cref="IReadOnlyDictionary{TKey, TValue}"/> can be deserialized. Any other dictionary type can only be
        /// serialized.
        /// </remarks>
        /// <typeparam name="TDictionary">The dictionary type.</typeparam>
        /// <typeparam name="TKey">The key type.</typeparam>
        /// <typeparam name="TValue">The value type.</typeparam>
        /// <param name="key">The type information of the key type, which reads each key from a string value.</param>
        /// <param name="value">The type information of the value type.</param>
        /// <returns>The type information for <typeparamref name="TDictionary"/>.</returns>
        public static KVTypeInfo<TDictionary> Dictionary<TDictionary, TKey, TValue>(KVTypeInfo<TKey> key, KVTypeInfo<TValue> value)
            where TDictionary : IEnumerable<KeyValuePair<TKey, TValue>>
            where TKey : notnull
        {
            ArgumentNullException.ThrowIfNull(key);
            ArgumentNullException.ThrowIfNull(value);
            return new KVDictionaryTypeInfo<TDictionary, TKey, TValue>(key, value);
        }

        /// <summary>
        /// Creates the type information for a lookup, which maps to a KeyValues collection that may contain
        /// duplicate keys.
        /// </summary>
        /// <typeparam name="TValue">The value type.</typeparam>
        /// <param name="value">The type information of the value type.</param>
        /// <returns>The type information for <see cref="ILookup{TKey, TElement}"/>.</returns>
        public static KVTypeInfo<ILookup<string, TValue>> Lookup<TValue>(KVTypeInfo<TValue> value)
        {
            ArgumentNullException.ThrowIfNull(value);
            return new KVLookupTypeInfo<TValue>(value);
        }

        /// <summary>
        /// Creates the type information for an object that maps to a KeyValues collection of its members.
        /// </summary>
        /// <typeparam name="T">The object type.</typeparam>
        /// <param name="create">
        /// Creates an instance, for example through a constructor or as the default value of a value type. It receives
        /// one argument per entry returned by <paramref name="parameters"/>, or an empty array when there are none; an
        /// argument missing from KeyValues data is the default value of its parameter description. It is called before
        /// any member is assigned. When it is <see langword="null"/>, the type cannot be deserialized.
        /// </param>
        /// <param name="parameters">
        /// Returns the constructor parameters in declaration order, or is <see langword="null"/> for a parameterless
        /// constructor. Invoked once, on first use.
        /// </param>
        /// <param name="members">
        /// Returns the members of the type in serialization order. Invoked once, on first use, so members may refer to
        /// type information that is still being created, such as the type information of a recursive type.
        /// </param>
        /// <param name="constructorSetsRequiredMembers">
        /// Whether the constructor sets all required members, which disables the check that required members are present
        /// in KeyValues data.
        /// </param>
        /// <returns>The type information for <typeparamref name="T"/>.</returns>
        public static KVTypeInfo<T> Object<T>(
            Func<object?[], T>? create,
            Func<KVParameterInfo[]>? parameters,
            Func<KVMemberInfo<T>[]> members,
            bool constructorSetsRequiredMembers)
        {
            ArgumentNullException.ThrowIfNull(members);
            return new KVObjectTypeInfo<T>(create, parameters, members, constructorSetsRequiredMembers);
        }

        /// <summary>
        /// Creates the description of an object member.
        /// </summary>
        /// <typeparam name="T">The type that declares the member.</typeparam>
        /// <typeparam name="TValue">The type of the member.</typeparam>
        /// <param name="name">The name of the member as it appears in KeyValues data.</param>
        /// <param name="declaredName">The name of the member as it is declared, used to match constructor parameters.</param>
        /// <param name="type">The type information of the member type.</param>
        /// <param name="getter">Reads the member value, or <see langword="null"/> when the member is not serialized.</param>
        /// <param name="setter">Assigns the member value, or <see langword="null"/> when the member is not deserialized.</param>
        /// <param name="isRequired">Whether the member must be present in KeyValues data when deserializing.</param>
        /// <returns>The member description.</returns>
        public static KVMemberInfo<T> Member<T, TValue>(string name, string declaredName, KVTypeInfo<TValue> type, Func<T, TValue?>? getter, KVSetter<T, TValue>? setter, bool isRequired)
        {
            ArgumentNullException.ThrowIfNull(name);
            ArgumentNullException.ThrowIfNull(declaredName);
            ArgumentNullException.ThrowIfNull(type);
            return new KVMemberInfo<T, TValue>(name, declaredName, isRequired, type, getter, setter);
        }

        /// <summary>
        /// Creates the description of a constructor parameter.
        /// </summary>
        /// <typeparam name="T">The type of the parameter.</typeparam>
        /// <param name="name">The declared name of the parameter.</param>
        /// <param name="type">The type information of the parameter type.</param>
        /// <param name="defaultValue">
        /// The value passed to the constructor when the KeyValues data does not contain the parameter: the declared
        /// default value of the parameter, or the default value of <typeparamref name="T"/> when it declares none.
        /// </param>
        /// <returns>The parameter description.</returns>
        public static KVParameterInfo Parameter<T>(string name, KVTypeInfo<T> type, T? defaultValue)
        {
            ArgumentNullException.ThrowIfNull(name);
            ArgumentNullException.ThrowIfNull(type);
            return new KVParameterInfo(name, type, defaultValue);
        }
    }
}
