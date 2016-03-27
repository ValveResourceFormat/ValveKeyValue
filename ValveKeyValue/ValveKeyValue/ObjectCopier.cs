using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.Serialization;

namespace ValveKeyValue
{
    static class ObjectCopier
    {
        public static TObject MakeObject<TObject>(KVObject keyValueObject)
            => MakeObject<TObject>(keyValueObject, new DefaultMapper());

        public static TObject MakeObject<TObject>(KVObject keyValueObject, IPropertyMapper mapper)
        {
            Require.NotNull(keyValueObject, nameof(keyValueObject));
            Require.NotNull(mapper, nameof(mapper));

            object[] enumerableValues;
            if (IsArray(keyValueObject, out enumerableValues))
            {
                object enumerable;
                if (ConstructTypedEnumerable(typeof(TObject), enumerableValues, out enumerable))
                {
                    return (TObject)enumerable;
                }
            }
            else if (IsConstructibleEnumerableType(typeof(TObject)))
            {
                throw new InvalidOperationException($"Cannot deserialize a non-array value to type \"{typeof(TObject).Namespace}.{typeof(TObject).Name}\".");
            }
            else if (IsDictionary(typeof(TObject)))
            {
                return (TObject)MakeDictionary(typeof(TObject), keyValueObject);
            }

            var typedObject = (TObject)FormatterServices.GetSafeUninitializedObject(typeof(TObject));
            CopyObject(keyValueObject, typedObject, mapper);
            return typedObject;
        }

        static void CopyObject<TObject>(KVObject kv, TObject obj, IPropertyMapper mapper)
        {
            Require.NotNull(kv, nameof(kv));

            // Cannot use Require.NotNull here because TObject might be a struct.
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            Require.NotNull(mapper, nameof(mapper));

            foreach (var item in kv.Children)
            {
                var property = mapper.MapFromKeyValue(typeof(TObject), item.Name);
                if (property == null)
                {
                    continue;
                }

                if (item.Value.ValueType != KVValueType.Children)
                {
                    CopyValue(obj, property, item.Value);
                }
                else if (IsDictionary(property.PropertyType))
                {
                    var dictionary = MakeDictionary(property.PropertyType, item);
                    property.SetValue(obj, dictionary);
                }
                else
                {
                    object[] arrayValues;
                    if (IsArray(item, out arrayValues))
                    {
                        CopyList(obj, property, arrayValues);
                    }
                    else if (IsConstructibleEnumerableType(typeof(TObject)))
                    {
                        throw new InvalidOperationException($"Cannot deserialize a non-array value to type \"{typeof(TObject).Namespace}.{typeof(TObject).Name}\".");
                    }
                    else
                    {
                        try
                        {
                            var @object = typeof(ObjectCopier)
                                .GetMethod(nameof(MakeObject), new[] { typeof(KVObject), typeof(IPropertyMapper) })
                                .MakeGenericMethod(property.PropertyType)
                                .Invoke(null, new object[] { item, mapper });
                            property.SetValue(obj, @object);
                        }
                        catch (TargetInvocationException ex)
                        {
                            ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                        }
                    }
                }
            }
        }

        static void CopyValue<TObject>(TObject obj, PropertyInfo property, KVValue value)
        {
            var propertyType = property.PropertyType;
            property.SetValue(obj, Convert.ChangeType(value, propertyType));
        }

        static bool IsArray(KVObject obj, out object[] values)
        {
            values = null;

            if (obj.Value.ValueType != KVValueType.Children)
            {
                return false;
            }

            if (obj.Children.Any(i => !IsNumeric(i.Name)))
            {
                return false;
            }

            var items = obj.Children
                .Select(i => new { Index = int.Parse(i.Name), i.Value })
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

        static readonly Dictionary<Type, Func<Type, object[], object>> EnumerableBuilders = new Dictionary<Type, Func<Type, object[], object>>
        {
            [typeof(List<>)] = (type, values) => InvokeGeneric(nameof(MakeList), type.GetGenericArguments()[0], new[] { values }),
            [typeof(IList<>)] = (type, values) => InvokeGeneric(nameof(MakeList), type.GetGenericArguments()[0], new[] { values }),
            [typeof(Collection<>)] = (type, values) => InvokeGeneric(nameof(MakeCollection), type.GetGenericArguments()[0], new[] { values }),
            [typeof(ICollection<>)] = (type, values) => InvokeGeneric(nameof(MakeCollection), type.GetGenericArguments()[0], new[] { values }),
            [typeof(ObservableCollection<>)] = (type, values) => InvokeGeneric(nameof(MakeObservableCollection), type.GetGenericArguments()[0], new[] { values }),
        };

        static bool ConstructTypedEnumerable(Type type, object[] values, out object typedEnumerable)
        {
            object listObject = null;

            if (type.IsArray)
            {
                var elementType = type.GetElementType();
                var itemArray = Array.CreateInstance(elementType, values.Length);

                for (int i = 0; i < itemArray.Length; i++)
                {
                    itemArray.SetValue(Convert.ChangeType(values[i], elementType), i);
                }

                listObject = itemArray;
            }
            else if (type.IsGenericType)
            {
                Func<Type, object[], object> builder;
                if (EnumerableBuilders.TryGetValue(type.GetGenericTypeDefinition(), out builder))
                {
                    listObject = builder(type, values);
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

            if (!type.IsGenericType)
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

        static void CopyList<TObject>(TObject obj, PropertyInfo property, object[] values)
        {
            object list;
            if (!ConstructTypedEnumerable(property.PropertyType, values, out list))
            {
                throw new NotSupportedException();
            }

            property.SetValue(obj, list);
        }

        static object InvokeGeneric(string methodName, Type genericType, params object[] parameters)
        {
            var method = typeof(ObjectCopier).GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            return method.MakeGenericMethod(genericType).Invoke(null, parameters);
        }

        static List<TElement> MakeList<TElement>(object[] items)
        {
            return items.Select(i => Convert.ChangeType(i, typeof(TElement)))
                .Cast<TElement>()
                .ToList();
        }

        static Collection<TElement> MakeCollection<TElement>(object[] items)
        {
            return new Collection<TElement>(MakeList<TElement>(items));
        }

        static ObservableCollection<TElement> MakeObservableCollection<TElement>(object[] items)
        {
            return new ObservableCollection<TElement>(MakeList<TElement>(items));
        }

        static bool IsNumeric(string str)
        {
            if (str.Length == 0)
            {
                return false;
            }

            int unused;
            return int.TryParse(str, out unused);
        }

        static bool IsDictionary(Type type)
        {
            if (!type.IsGenericType)
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

        static object MakeDictionary(Type type, KVObject kv)
        {
            var dictionary = Activator.CreateInstance(type);
            var genericArguments = type.GetGenericArguments();

            typeof(ObjectCopier)
                .GetMethod(nameof(FillDictionary), BindingFlags.Static | BindingFlags.NonPublic)
                .MakeGenericMethod(genericArguments)
                .Invoke(null, new[] { dictionary, kv });

            return dictionary;
        }

        static void FillDictionary<TKey, TValue>(Dictionary<TKey, TValue> dictionary, KVObject kv)
        {
            foreach (var item in kv.Children)
            {
                var key = (TKey)Convert.ChangeType(item.Name, typeof(TKey));
                var value = (TValue)Convert.ChangeType(item.Value, typeof(TValue));

                dictionary[key] = value;
            }
        }
    }
}
