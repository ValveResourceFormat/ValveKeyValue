using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using ValveKeyValue.Metadata;

namespace ValveKeyValue
{
    // Writes an enumerable that does not implement IEnumerable<T>, using the runtime type of each element.
    // A non-generic IDictionary is written as a collection keyed by its keys, any other enumerable as an
    // array. These types cannot be deserialized.
    [RequiresUnreferencedCode(KVReflectionTypeInfo.RequiresMessage)]
    [RequiresDynamicCode(KVReflectionTypeInfo.RequiresMessage)]
    sealed class KVUntypedEnumerableTypeInfo<T> : KVTypeInfo<T>
    {
        internal override T ReadCore(KVObject value)
        {
            ThrowIfNotCollection(value);
            throw EnumerableNotSupported();
        }

        internal override KVObject Write(T value, KVWriteContext context)
        {
            context.Enter(value);

            var children = new List<KeyValuePair<string, KVObject>>();

            if (value is IDictionary dictionary)
            {
                var enumerator = dictionary.GetEnumerator();

                while (enumerator.MoveNext())
                {
                    var entry = enumerator.Entry;
                    var name = Convert.ToString(entry.Key, CultureInfo.InvariantCulture)!;
                    children.Add(new KeyValuePair<string, KVObject>(name, WriteElement(entry.Value!, context)));
                }
            }
            else
            {
                foreach (var item in (IEnumerable)value!)
                {
                    children.Add(new KeyValuePair<string, KVObject>(GetIndexKey(children.Count), WriteElement(item, context)));
                }
            }

            return new KVObject(KVValueType.Collection, children);
        }

        static KVObject WriteElement(object value, KVWriteContext context)
            => KVReflectionTypeInfo.Get(value.GetType()).WriteBoxed(value, context);
    }
}
