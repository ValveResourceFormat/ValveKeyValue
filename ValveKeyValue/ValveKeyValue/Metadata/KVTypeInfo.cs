using System.ComponentModel;
using System.Globalization;

namespace ValveKeyValue.Metadata
{
    /// <summary>
    /// Describes how a type maps to and from KeyValues data.
    /// This type is infrastructure for generated serialization code and is not intended to be used directly.
    /// Instances are created through <see cref="KVMetadata"/>.
    /// </summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public abstract class KVTypeInfo
    {
        private protected KVTypeInfo(Type type)
        {
            Type = type;
        }

        /// <summary>
        /// Gets the type that this instance describes.
        /// </summary>
        public Type Type { get; }

        internal abstract object? ReadBoxed(KVObject value);

        internal abstract KVObject WriteBoxed(object value, KVWriteContext context);

        internal static KeyValueException NoUsableConstructor(string typeName)
            => new($"Type '{typeName}' has no usable constructor; add a public parameterless constructor, a single public constructor, or mark one with [KVConstructor].");

        // Returns the key of an element of a collection that is written as a collection keyed by element indices.
        internal static string GetIndexKey(int index)
            => (uint)index < (uint)IndexKeys.Length ? IndexKeys[index] : index.ToString(CultureInfo.InvariantCulture);

        static readonly string[] IndexKeys = CreateIndexKeys(256);

        static string[] CreateIndexKeys(int count)
        {
            var keys = new string[count];

            for (var i = 0; i < keys.Length; i++)
            {
                keys[i] = i.ToString(CultureInfo.InvariantCulture);
            }

            return keys;
        }
    }

    /// <summary>
    /// Describes how <typeparamref name="T"/> maps to and from KeyValues data.
    /// This type is infrastructure for generated serialization code and is not intended to be used directly.
    /// Instances are created through <see cref="KVMetadata"/>.
    /// </summary>
    /// <typeparam name="T">The described type.</typeparam>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public abstract class KVTypeInfo<T> : KVTypeInfo
    {
        private protected KVTypeInfo()
            : base(typeof(T))
        {
        }

        // A null value reads as null into a reference type or a nullable value type, and cannot be read into any
        // other value type. Every other value is read by ReadCore.
        internal T Read(KVObject value)
        {
            if (value.ValueType == KVValueType.Null)
            {
                if (default(T) is null)
                {
                    return default!;
                }

                throw ConversionNotSupported(value);
            }

            return ReadCore(value);
        }

        // Reads a value that is not null.
        internal abstract T ReadCore(KVObject value);

        internal abstract KVObject Write(T value, KVWriteContext context);

        internal sealed override object? ReadBoxed(KVObject value) => Read(value);

        internal sealed override KVObject WriteBoxed(object value, KVWriteContext context) => Write((T)value, context);

        private protected static NotSupportedException ArrayNotSupported()
            => new($"Cannot convert Array to {typeof(T).Name}.");

        private protected static NotSupportedException ConversionNotSupported(KVObject value)
            => new($"Converting to {typeof(T).Name} is not supported. (type = {value.ValueType})");

        private protected static NotSupportedException EnumerableNotSupported()
            => new($"Cannot deserialize to enumerable type {typeof(T).Name}.");

        private protected static void ThrowIfNotCollection(KVObject value)
        {
            switch (value.ValueType)
            {
                case KVValueType.Collection:
                    return;
                case KVValueType.Array:
                    throw ArrayNotSupported();
                default:
                    throw ConversionNotSupported(value);
            }
        }

        // Converts a single value with the given conversion. Collections and arrays are not single values, and a
        // failed conversion is reported as a NotSupportedException that wraps the original exception.
        private protected static T ReadScalar(KVObject value, Func<KVObject, T> convert)
        {
            switch (value.ValueType)
            {
                case KVValueType.Collection:
                    throw ConversionNotSupported(value);
                case KVValueType.Array:
                    throw ArrayNotSupported();
            }

            try
            {
                return convert(value);
            }
            catch (Exception e)
            {
                throw new NotSupportedException($"Conversion to {typeof(T)} failed. (type = {value.ValueType})", e);
            }
        }
    }
}
