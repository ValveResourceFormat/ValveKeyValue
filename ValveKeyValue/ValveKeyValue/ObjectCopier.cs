using System.Collections;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;

namespace ValveKeyValue
{
    // TODO: Migrate to IVisitationListener
    static class ObjectCopier
    {
        public static TObject MakeObject<[DynamicallyAccessedMembers(Trimming.Constructors | Trimming.Properties)] TObject>(KVObject keyValueObject)
            => MakeObject<TObject>(keyValueObject, new DefaultObjectReflector());

        public static object MakeObject(
            [DynamicallyAccessedMembers(Trimming.Properties)] Type objectType, KVObject keyValueObject, IObjectReflector reflector)
            => InvokeGeneric(nameof(MakeObject), objectType, new object[] { keyValueObject, reflector });

        public static TObject MakeObject<[DynamicallyAccessedMembers(Trimming.Constructors | Trimming.Properties)] TObject>(KVObject keyValueObject, IObjectReflector reflector)
        {
            ArgumentNullException.ThrowIfNull(keyValueObject);
            ArgumentNullException.ThrowIfNull(reflector);

            if (keyValueObject.ValueType == KVValueType.Collection)
            {
                if (IsDictionary(typeof(TObject)))
                {
                    return (TObject)MakeDictionary(typeof(TObject), keyValueObject, reflector);
                }
                else if (IsArray(keyValueObject, out var enumerableValues) && ConstructTypedEnumerable(typeof(TObject), enumerableValues, reflector, out var enumerable))
                {
                    return (TObject)enumerable;
                }
                else if (IsConstructibleEnumerableType(typeof(TObject)))
                {
                    throw new InvalidOperationException($"Cannot deserialize a non-array value to type \"{typeof(TObject).Namespace}.{typeof(TObject).Name}\".");
                }

                // The object must remain boxed until it is fully initiallized, as this is the only way
                // that we can build a struct due to the nature of struct copying.
                var typedObject = RuntimeHelpers.GetUninitializedObject(typeof(TObject));
                CopyObject(keyValueObject, typeof(TObject), typedObject, reflector);
                return (TObject)typedObject;
            }
            else if (keyValueObject.ValueType == KVValueType.Array)
            {
                var arrayValues = keyValueObject.GetArrayList().Select(c => (object)c).ToArray();
                if (ConstructTypedEnumerable(typeof(TObject), arrayValues, reflector, out var enumerable))
                {
                    return (TObject)enumerable;
                }

                throw new NotSupportedException($"Cannot convert Array to {typeof(TObject).Name}.");
            }
            else if (TryConvertValueTo<TObject>(keyValueObject, out var converted))
            {
                return converted;
            }
            else
            {
                // TODO: For nullable types this typeof is not that useful
                throw new NotSupportedException($"Converting to {typeof(TObject).Name} is not supported. (type = {keyValueObject.ValueType})");
            }
        }

        public static KVObject FromObject(
            [DynamicallyAccessedMembers(Trimming.Properties)] Type objectType,
            object managedObject)
            => ConvertObjectToValue(objectType, managedObject, new DefaultObjectReflector(), new HashSet<object>());

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2072", Justification = "If the IDictionary's value object already exists at runtime then its properties will too.")]
        static KVObject ConvertObjectToValue(
            [DynamicallyAccessedMembers(Trimming.Properties)] Type objectType,
            object managedObject,
            IObjectReflector reflector,
            HashSet<object> visitedObjects)
        {
            if (!objectType.IsValueType && objectType != typeof(string) && !visitedObjects.Add(managedObject))
            {
                throw new KeyValueException("Serialization failed - circular object reference detected.");
            }

            var attemptedKvValue = ConvertToKVObject(managedObject, objectType);
            if (attemptedKvValue != null)
            {
                return attemptedKvValue;
            }

            var childItems = new List<KeyValuePair<string, KVObject>>();

            if (typeof(IDictionary).IsAssignableFrom(objectType))
            {
                var dictionary = (IDictionary)managedObject;
                var enumerator = dictionary.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    var entry = enumerator.Entry;

                    var childObjectValue = ConvertObjectToValue(entry.Value.GetType(), entry.Value, reflector, visitedObjects);
                    childItems.Add(new KeyValuePair<string, KVObject>(entry.Key.ToString(), childObjectValue));
                }
            }
            else if (objectType.IsArray || typeof(IEnumerable).IsAssignableFrom(objectType))
            {
                var counter = 0;
                foreach (var child in (IEnumerable)managedObject)
                {
                    var childValue = ConvertObjectToValue(child.GetType(), child, reflector, visitedObjects);
                    childItems.Add(new KeyValuePair<string, KVObject>(counter.ToString(CultureInfo.InvariantCulture), childValue));

                    counter++;
                }
            }
            else
            {
                foreach (var member in reflector.GetMembers(objectType, managedObject).OrderBy(p => p.Name, StringComparer.InvariantCulture))
                {
                    if (!member.MemberType.IsValueType && member.Value is null)
                    {
                        continue;
                    }

                    var childValue = ConvertObjectToValue(member.Value.GetType(), member.Value, reflector, visitedObjects);
                    childItems.Add(new KeyValuePair<string, KVObject>(member.Name, childValue));
                }
            }

            return new KVObject(KVValueType.Collection, childItems);
        }

        static void CopyObject(KVObject kv, [DynamicallyAccessedMembers(Trimming.Properties)] Type objectType, object obj, IObjectReflector reflector)
        {
            ArgumentNullException.ThrowIfNull(kv);
            ArgumentNullException.ThrowIfNull(obj);
            ArgumentNullException.ThrowIfNull(reflector);

            var members = reflector.GetMembers(objectType, obj).ToDictionary(m => m.Name, m => m, StringComparer.OrdinalIgnoreCase);

            foreach (var (key, child) in kv)
            {
                if (!members.TryGetValue(key, out var member))
                {
                    continue;
                }

                var convertedValue = MakeObject(member.MemberType, child, reflector);
                member.Value = convertedValue;
            }
        }

        static bool IsArray(KVObject obj, out object[] values)
        {
            values = null;

            if (obj.Any(kvp => !IsNumeric(kvp.Key)))
            {
                return false;
            }

            var items = obj
                .Select(kvp => new { Index = int.Parse(kvp.Key, NumberStyles.Number, CultureInfo.InvariantCulture), kvp.Value })
                .OrderBy(i => i.Index)
                .ToArray();

            for (int i = 0; i < items.Length; i++)
            {
                if (i != items[i].Index)
                {
                    return false;
                }
            }

            values = items.Select(i => (object)i.Value).ToArray();
            return true;
        }

        static readonly Dictionary<Type, Func<Type, object[], IObjectReflector, object>> EnumerableBuilders = new()
        {
            [typeof(List<>)] = (type, values, reflector) => InvokeGeneric(nameof(MakeList), type.GetGenericArguments()[0], new object[] { values, reflector }),
            [typeof(IList<>)] = (type, values, reflector) => InvokeGeneric(nameof(MakeList), type.GetGenericArguments()[0], new object[] { values, reflector }),
            [typeof(Collection<>)] = (type, values, reflector) => InvokeGeneric(nameof(MakeCollection), type.GetGenericArguments()[0], new object[] { values, reflector }),
            [typeof(ICollection<>)] = (type, values, reflector) => InvokeGeneric(nameof(MakeCollection), type.GetGenericArguments()[0], new object[] { values, reflector }),
            [typeof(ObservableCollection<>)] = (type, values, reflector) => InvokeGeneric(nameof(MakeObservableCollection), type.GetGenericArguments()[0], new object[] { values, reflector }),
        };

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2072", Justification = "If our T[] array exists then so much the element T.")]
        [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "If our T[] array exists then so much the element T.")]
        static bool ConstructTypedEnumerable(
            Type type,
            object[] values,
            IObjectReflector reflector,
            out object typedEnumerable)
        {
            object listObject = null;

            if (type.IsArray)
            {
                var elementType = type.GetElementType();
                var itemArray = Array.CreateInstance(elementType, values.Length);

                for (int i = 0; i < itemArray.Length; i++)
                {
                    var item = ConvertValue(values[i], elementType, reflector);
                    itemArray.SetValue(item, i);
                }

                listObject = itemArray;
            }
            else if (type.IsConstructedGenericType)
            {
                if (EnumerableBuilders.TryGetValue(type.GetGenericTypeDefinition(), out var builder))
                {
                    listObject = builder(type, values, reflector);
                }
            }

            typedEnumerable = listObject;
            return listObject != null;
        }

        static bool IsConstructibleEnumerableType(Type type)
        {
            if (type.IsArray)
            {
                return true;
            }

            if (!type.IsConstructedGenericType)
            {
                return false;
            }

            var gtd = type.GetGenericTypeDefinition();

            if (EnumerableBuilders.ContainsKey(gtd))
            {
                return true;
            }

            return false;
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2060", Justification = "Analysis cannot follow MakeGenericMethod. All callers validated manually.")]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2111", Justification = "Analysis cannot follow MakeGenericMethod. All callers validated manually.")]
        [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Analysis cannot follow MakeGenericMethod. All callers validated manually.")]
        static object InvokeGeneric(string methodName, Type genericType, params object[] parameters)
        {
            var method = typeof(ObjectCopier)
                .GetTypeInfo()
                .GetDeclaredMethods(methodName)
                .Single(m => m.IsStatic && m.GetParameters().Length == parameters.Length);

            try
            {
                return method.MakeGenericMethod(genericType).Invoke(null, parameters);
            }
            catch (TargetInvocationException ex) when (ex.InnerException != null)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                throw; // Unreachable
            }
        }

        static List<TElement> MakeList<[DynamicallyAccessedMembers(Trimming.Constructors | Trimming.Properties)] TElement>(object[] items, IObjectReflector reflector)
        {
            var list = new List<TElement>(capacity: items.Length);
            foreach (var item in items)
            {
                list.Add(ConvertValue<TElement>(item, reflector));
            }
            return list;
        }

        static Collection<TElement> MakeCollection<[DynamicallyAccessedMembers(Trimming.Constructors | Trimming.Properties)] TElement>(object[] items, IObjectReflector reflector)
        {
            return new Collection<TElement>(MakeList<TElement>(items, reflector));
        }

        static ObservableCollection<TElement> MakeObservableCollection<[DynamicallyAccessedMembers(Trimming.Constructors | Trimming.Properties)] TElement>(object[] items, IObjectReflector reflector)
        {
            return new ObservableCollection<TElement>(MakeList<TElement>(items, reflector));
        }

        static bool IsNumeric(string str)
        {
            if (str.Length == 0)
            {
                return false;
            }

            return int.TryParse(str, NumberStyles.Number, CultureInfo.InvariantCulture, out _);
        }

        static bool IsDictionary(Type type)
        {
            if (!type.IsConstructedGenericType)
            {
                return false;
            }

            var genericType = type.GetGenericTypeDefinition();
            if (genericType != typeof(Dictionary<,>))
            {
                return false;
            }

            return true;
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2060", Justification = "Analysis cannot follow MakeGenericMethod but we should be clear by here anyway.")]
        [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Analysis cannot follow MakeGenericMethod but we should be clear by here anyway.")]
        static object MakeDictionary(
            [DynamicallyAccessedMembers(Trimming.Constructors | Trimming.Properties)] Type type,
            KVObject kv,
            IObjectReflector reflector)
        {
            var dictionary = Activator.CreateInstance(type);
            var genericArguments = type.GetGenericArguments();

            var method = typeof(ObjectCopier)
                .GetMethod(nameof(FillDictionary), BindingFlags.Static | BindingFlags.NonPublic);
            method.MakeGenericMethod(genericArguments)
                .Invoke(null, new[] { dictionary, kv, reflector });

            return dictionary;
        }

        static void FillDictionary<[DynamicallyAccessedMembers(Trimming.Constructors | Trimming.Properties)] TKey, [DynamicallyAccessedMembers(Trimming.Constructors | Trimming.Properties)] TValue>(Dictionary<TKey, TValue> dictionary, KVObject kv, IObjectReflector reflector)
        {
            foreach (var (childKey, child) in kv)
            {
                var key = ConvertValue<TKey>(childKey, reflector);

                if (dictionary.ContainsKey(key))
                {
                    continue;
                }

                var value = ConvertValue<TValue>(child, reflector);
                dictionary.Add(key, value);
            }
        }

        static TValue ConvertValue<[DynamicallyAccessedMembers(Trimming.Constructors | Trimming.Properties)] TValue>(object value, IObjectReflector reflector)
            => (TValue)ConvertValue(value, typeof(TValue), reflector);

        static object ConvertValue(
            object value,
            [DynamicallyAccessedMembers(Trimming.Constructors | Trimming.Properties)] Type valueType,
            IObjectReflector reflector)
        {
            if (value is KVObject kvObject)
            {
                if (kvObject.ValueType == KVValueType.Collection)
                {
                    return MakeObject(valueType, kvObject, reflector);
                }

                if (kvObject.ValueType == KVValueType.BinaryBlob && valueType == typeof(byte[]))
                {
                    return kvObject.AsBlob();
                }

                return Convert.ChangeType(kvObject.ToType(valueType, null), valueType, CultureInfo.InvariantCulture);
            }

            return Convert.ChangeType(value, valueType, CultureInfo.InvariantCulture);
        }

        static bool TryConvertValueTo<TValue>(KVObject value, out TValue converted)
        {
            if (typeof(TValue) == typeof(IntPtr))
            {
                converted = (TValue)(object)(IntPtr)value;
                return true;
            }

            if (typeof(TValue) == typeof(byte[]) && value.ValueType == KVValueType.BinaryBlob)
            {
                converted = (TValue)(object)value.AsBlob();
                return true;
            }

            if (typeof(TValue).IsEnum)
            {
                var underlyingType = Enum.GetUnderlyingType(typeof(TValue));
                var underlyingValue = value.ToType(underlyingType, CultureInfo.InvariantCulture);
                converted = (TValue)Enum.ToObject(typeof(TValue), underlyingValue);
                return true;
            }

            if (CanConvertValueTo(typeof(TValue)))
            {
                try
                {
                    converted = (TValue)value.ToType(typeof(TValue), CultureInfo.InvariantCulture);
                }
                catch (Exception e)
                {
                    throw new NotSupportedException($"Conversion to {typeof(TValue)} failed. (type = {value.ValueType})", e);
                }

                return true;
            }

            converted = default;
            return false;
        }

        static bool CanConvertValueTo(Type type)
        {
            return
                type == typeof(bool) ||
                type == typeof(byte) ||
                type == typeof(char) ||
                type == typeof(decimal) ||
                type == typeof(double) ||
                type == typeof(float) ||
                type == typeof(int) ||
                type == typeof(long) ||
                type == typeof(uint) ||
                type == typeof(ulong) ||
                type == typeof(ushort) ||
                type == typeof(sbyte) ||
                type == typeof(short) ||
                type == typeof(string);
        }

        static KVObject ConvertToKVObject(object value, Type type)
        {
            if (type == typeof(IntPtr))
            {
                return new KVObject((IntPtr)value);
            }

            if (type.IsEnum)
            {
                type = Enum.GetUnderlyingType(type);
                value = Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
            }

            return Type.GetTypeCode(type) switch
            {
                TypeCode.Boolean => new KVObject((bool)value),
                TypeCode.Byte => new KVObject((int)(byte)value), // There is no byte kv type
                TypeCode.SByte => new KVObject((int)(sbyte)value), // There is no sbyte kv type
                TypeCode.Int16 => new KVObject((int)(short)value), // There is no int16 kv type
                TypeCode.Int32 => new KVObject((int)value),
                TypeCode.Int64 => new KVObject((long)value),
                TypeCode.Single => new KVObject((float)value),
                TypeCode.String => new KVObject((string)value),
                TypeCode.UInt16 => new KVObject((ulong)(ushort)value), // There is no uint16 kv type
                TypeCode.UInt32 => new KVObject((ulong)(uint)value), // There is no uint32 kv type
                TypeCode.UInt64 => new KVObject((ulong)value),
                _ => null,
            };
        }
    }
}
