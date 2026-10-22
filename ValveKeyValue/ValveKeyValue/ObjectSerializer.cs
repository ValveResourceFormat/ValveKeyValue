using System.Collections;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace ValveKeyValue
{
    [RequiresUnreferencedCode(Trimming.ObjectMappingMessage)]
    static class ObjectSerializer
    {
        static readonly IObjectReflector Reflector = new DefaultObjectReflector();

        public static KVObject FromObject(
            [DynamicallyAccessedMembers(Trimming.Properties)] Type objectType,
            object managedObject)
            => ConvertObjectToValue(objectType, managedObject, new HashSet<object>(ReferenceEqualityComparer.Instance));

        static KVObject ConvertObjectToValue(
            Type objectType,
            object managedObject,
            HashSet<object> visitedObjects)
        {
            switch (managedObject)
            {
                case KVObject kvObject:
                    return kvObject;

                case KVDocument document:
                    return document.Root;
            }

            if (!objectType.IsValueType && objectType != typeof(string) && !visitedObjects.Add(managedObject))
            {
                throw new KeyValueException("Serialization failed - circular object reference detected.");
            }

            var attemptedKvValue = ConvertToKVObject(managedObject, objectType);
            if (attemptedKvValue != null)
            {
                return attemptedKvValue;
            }

            if (objectType == typeof(byte[]))
            {
                return KVObject.Blob((byte[])managedObject);
            }

            var childItems = new List<KeyValuePair<string, KVObject>>();

            if (typeof(IDictionary).IsAssignableFrom(objectType))
            {
                var dictionary = (IDictionary)managedObject;
                var enumerator = dictionary.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    var entry = enumerator.Entry;

                    var childObjectValue = ConvertObjectToValue(entry.Value!.GetType(), entry.Value, visitedObjects);
                    childItems.Add(new KeyValuePair<string, KVObject>(Convert.ToString(entry.Key, CultureInfo.InvariantCulture)!, childObjectValue));
                }
            }
            else if (typeof(IEnumerable).IsAssignableFrom(objectType))
            {
                var counter = 0;
                foreach (var child in (IEnumerable)managedObject)
                {
                    var childValue = ConvertObjectToValue(child.GetType(), child, visitedObjects);
                    childItems.Add(new KeyValuePair<string, KVObject>(counter.ToString(CultureInfo.InvariantCulture), childValue));

                    counter++;
                }
            }
            else
            {
                foreach (var member in Reflector.GetMembers(objectType, managedObject))
                {
                    var memberValue = member.Value;
                    if (memberValue is null)
                    {
                        continue;
                    }

                    var childValue = ConvertObjectToValue(memberValue.GetType(), memberValue, visitedObjects);
                    childItems.Add(new KeyValuePair<string, KVObject>(member.Name, childValue));
                }
            }

            return new KVObject(KVValueType.Collection, childItems);
        }

        static KVObject? ConvertToKVObject(object value, Type type)
        {
            if (type == typeof(IntPtr))
            {
                return new KVObject((IntPtr)value);
            }

            // GetTypeCode reports an enum's underlying type, and a boxed enum unboxes to that type.
            return Type.GetTypeCode(type) switch
            {
                TypeCode.Boolean => new KVObject((bool)value),
                TypeCode.Char => new KVObject(((char)value).ToString()),
                TypeCode.Decimal => new KVObject((double)(decimal)value),
                TypeCode.Byte => new KVObject((int)(byte)value),
                TypeCode.SByte => new KVObject((int)(sbyte)value),
                TypeCode.Int16 => new KVObject((int)(short)value),
                TypeCode.Int32 => new KVObject((int)value),
                TypeCode.Int64 => new KVObject((long)value),
                TypeCode.Single => new KVObject((float)value),
                TypeCode.Double => new KVObject((double)value),
                TypeCode.String => new KVObject((string)value),
                TypeCode.UInt16 => new KVObject((ulong)(ushort)value),
                TypeCode.UInt32 => new KVObject((ulong)(uint)value),
                TypeCode.UInt64 => new KVObject((ulong)value),
                _ => null,
            };
        }
    }
}
