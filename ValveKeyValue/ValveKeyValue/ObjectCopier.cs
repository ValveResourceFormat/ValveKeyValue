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

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2062", Justification = "If the lookup value type exists at runtime then it should have enough for us to introspect.")]
        public static TObject MakeObject<[DynamicallyAccessedMembers(Trimming.Constructors | Trimming.Properties)] TObject>(KVObject keyValueObject, IObjectReflector reflector)
        {
            Require.NotNull(keyValueObject, nameof(keyValueObject));
            Require.NotNull(reflector, nameof(reflector));

            if (keyValueObject.Value.ValueType == KVValueType.Collection)
            {
                if (IsLookupWithStringKey(typeof(TObject), out var lookupValueType))
                {
                    return (TObject)MakeLookup(lookupValueType, keyValueObject, reflector);
                }
                else if (IsDictionary(typeof(TObject)))
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
            else if (TryConvertValueTo<TObject>(keyValueObject.Name, keyValueObject.Value, out var converted))
            {
                return converted;
            }
            else
            {
                // TODO: For nullable types this typeof is not that useful
                throw new NotSupportedException($"Converting to {typeof(TObject).Name} is not supported. (key = {keyValueObject.Name}, type = {keyValueObject.Value.ValueType})");
            }
        }

        public static KVObject FromObject(
            [DynamicallyAccessedMembers(Trimming.Properties)] Type objectType,
            object managedObject,
            string topLevelName)
            => FromObjectCore(objectType, managedObject, topLevelName, new DefaultObjectReflector(), new HashSet<object>());

        static KVObject FromObjectCore(
            [DynamicallyAccessedMembers(Trimming.Properties)] Type objectType,
            object managedObject,
            string topLevelName,
            IObjectReflector reflector,
            HashSet<object> visitedObjects)
        {
            if (managedObject == null)
            {
                throw new ArgumentNullException(nameof(managedObject));
            }

            Require.NotNull(topLevelName, nameof(topLevelName));
            Require.NotNull(reflector, nameof(reflector));
            Require.NotNull(visitedObjects, nameof(visitedObjects));

            var transformedValue = ConvertObjectToValue(objectType, managedObject, reflector, visitedObjects);
            return new KVObject(topLevelName, transformedValue);
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2072", Justification = "If the IDictionary's value object already exists at runtime then its properties will too.")]
        static KVValue ConvertObjectToValue(
            [DynamicallyAccessedMembers(Trimming.Properties)] Type objectType,
            object managedObject,
            IObjectReflector reflector,
            HashSet<object> visitedObjects)
        {
            if (!objectType.IsValueType && objectType != typeof(string) && !visitedObjects.Add(managedObject))
            {
                throw new KeyValueException("Serialization failed - circular object reference detected.");
            }

            var attemptedKvValue = ConvertToKVValue(managedObject, objectType);
            if (attemptedKvValue != null)
            {
                return attemptedKvValue;
            }

            var childObjects = new KVCollectionValue();

            if (typeof(IDictionary).IsAssignableFrom(objectType))
            {
                var dictionary = (IDictionary)managedObject;
                var enumerator = dictionary.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    var entry = enumerator.Entry;

                    var childObjectValue = ConvertObjectToValue(entry.Value.GetType(), entry.Value, reflector, visitedObjects);
                    childObjects.Add(new KVObject(entry.Key.ToString(), childObjectValue));
                }
            }
            else if (objectType.IsArray || typeof(IEnumerable).IsAssignableFrom(objectType))
            {
                var counter = 0;
                foreach (var child in (IEnumerable)managedObject)
                {
                    var childKVObject = FromObjectCore(child.GetType(), child, counter.ToString(), reflector, visitedObjects);
                    childObjects.Add(childKVObject);

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

                    childObjects.Add(FromObjectCore(member.Value.GetType(), member.Value, member.Name, reflector, visitedObjects));
                }
            }

            return childObjects;
        }

        static void CopyObject(KVObject kv, [DynamicallyAccessedMembers(Trimming.Properties)] Type objectType, object obj, IObjectReflector reflector)
        {
            Require.NotNull(kv, nameof(kv));
            Require.NotNull(obj, nameof(obj));
            Require.NotNull(reflector, nameof(reflector));

            var members = reflector.GetMembers(objectType, obj).ToDictionary(m => m.Name, m => m, StringComparer.OrdinalIgnoreCase);

            foreach (var item in kv.Children)
            {
                if (!members.TryGetValue(item.Name, out var member))
                {
                    continue;
                }

                var convertedValue = MakeObject(member.MemberType, item, reflector);
                member.Value = convertedValue;
            }
        }

        static bool IsArray(KVObject obj, out KVValue[] values)
        {
            values = null;

            if (obj.Children.Any(i => !IsNumeric(i.Name)))
            {
                return false;
            }

            var items = obj.Children
                .Select(i => new { Index = int.Parse(i.Name, NumberStyles.Number, CultureInfo.InvariantCulture), i.Value })
                .OrderBy(i => i.Index)
                .ToArray();

            for (int i = 0; i < items.Length; i++)
            {
                if (i != items[i].Index)
                {
                    return false;
                }
            }

            values = items.Select(i => i.Value).ToArray();
            return true;
        }

        static bool IsLookupWithStringKey(Type type, out Type valueType)
        {
            valueType = null;

            if (!type.IsConstructedGenericType)
            {
                return false;
            }

            var genericType = type.GetGenericTypeDefinition();
            if (genericType != typeof(ILookup<,>))
            {
                return false;
            }

            var genericArguments = type.GetGenericArguments();
            if (genericArguments.Length != 2)
            {
                return false;
            }

            if (genericArguments[0] != typeof(string))
            {
                return false;
            }

            valueType = genericArguments[1];
            return true;
        }

        static object MakeLookup(
            [DynamicallyAccessedMembers(Trimming.Constructors | Trimming.Properties)] Type valueType,
            IEnumerable<KVObject> items,
            IObjectReflector reflector)
            => InvokeGeneric(nameof(MakeLookupCore), valueType, new object[] { items, reflector });

        static ILookup<string, TValue> MakeLookupCore<[DynamicallyAccessedMembers(Trimming.Constructors | Trimming.Properties)] TValue>(IEnumerable<KVObject> items, IObjectReflector reflector)
        {
            TValue valueConversionFunc(KVObject kv) => ConvertValue<TValue>(kv.Value, reflector);

            return items.ToLookup(kv => kv.Name, valueConversionFunc);
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
            foreach (var item in kv.Children)
            {
                var key = ConvertValue<TKey>(item.Name, reflector);

                if (dictionary.ContainsKey(key))
                {
                    continue;
                }

                var value = ConvertValue<TValue>(item.Value, reflector);
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
            if (value is KVCollectionValue collectionValue)
            {
                return MakeObject(valueType, new KVObject("boo", (KVValue)collectionValue), reflector);
            }

            return Convert.ChangeType(value, valueType);
        }

        static bool TryConvertValueTo<TValue>(string name, object value, out TValue converted)
        {
            if (typeof(TValue) == typeof(IntPtr))
            {
                converted = (TValue)(object)(IntPtr)(KVValue)value;
                return true;
            }

            if (CanConvertValueTo(typeof(TValue)) && value is IConvertible)
            {
                try
                {
                    converted = (TValue)Convert.ChangeType(value, typeof(TValue), CultureInfo.InvariantCulture);
                }
                catch (Exception e)
                {
                    throw new NotSupportedException($"Conversion to {typeof(TValue)} failed. (key = {name}, object type = {value.GetType()})", e);
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

        static KVValue ConvertToKVValue(object value, Type type)
        {
            if (type == typeof(IntPtr))
            {
                return (KVValue)(IntPtr)value;
            }

            return Type.GetTypeCode(type) switch
            {
                TypeCode.Boolean => (KVValue)(bool)value,
                //TypeCode.Byte => throw new NotImplementedException("Converting to byte is not yet supported"),
                //TypeCode.Char => throw new NotImplementedException("Converting to char is not yet supported"),
                //TypeCode.DateTime => throw new NotImplementedException(), // Datetime are not supported
                //TypeCode.DBNull => throw new NotImplementedException(),
                //TypeCode.Decimal => throw new NotImplementedException("Converting to decimal is not yet supported"),
                //TypeCode.Double => throw new NotImplementedException("Converting to double is not yet supported"),
                //TypeCode.Empty => throw new NotImplementedException(), // No type
                TypeCode.Int16 => (KVValue)(int)(short)value, // There is no int16 kv type
                TypeCode.Int32 => (KVValue)(int)value,
                TypeCode.Int64 => (KVValue)(long)value,
                //TypeCode.Object => throw new NotImplementedException(), // Objects are handled separately
                //TypeCode.SByte => throw new NotImplementedException("Converting to sbyte is not yet supported"),
                TypeCode.Single => (KVValue)(float)value,
                TypeCode.String => (KVValue)(string)value,
                TypeCode.UInt16 => (KVValue)(ulong)(ushort)value, // There is no uint16 kv type
                TypeCode.UInt32 => (KVValue)(ulong)(uint)value, // There is no uint32 kv type
                TypeCode.UInt64 => (KVValue)(ulong)value,
                _ => null,
            };
        }
    }
}
