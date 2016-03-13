using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
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
                if (CreateTypedEnumerable(typeof(TObject), enumerableValues, out enumerable))
                {
                    return (TObject)enumerable;
                }
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

            foreach (var item in kv.Items)
            {
                var property = mapper.MapFromKeyValue(typeof(TObject), item.Name);
                if (property == null)
                {
                    continue;
                }

                if (item.Value != null)
                {
                    CopyValue(obj, property, item.Value);
                }
                else
                {
                    object[] arrayValues;
                    if (IsArray(item, out arrayValues))
                    {
                        CopyList(obj, property, arrayValues);
                    }
                    else
                    {
                        var @object = typeof(ObjectCopier)
                            .GetMethod(nameof(MakeObject), new[] { typeof(KVObject), typeof(IPropertyMapper) })
                            .MakeGenericMethod(property.PropertyType)
                            .Invoke(null, new object[] { item, mapper });
                        property.SetValue(obj, @object);
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

            if (obj.Value != null)
            {
                return false;
            }

            if (obj.Items.Any(i => i.Value == null))
            {
                return false;
            }

            if (obj.Items.Any(i => !IsNumeric(i.Name)))
            {
                return false;
            }

            var items = obj.Items
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

        static bool CreateTypedEnumerable(Type type, object[] values, out object typedEnumerable)
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
                if (type.GetGenericTypeDefinition() == typeof(List<>) || type.GetGenericTypeDefinition() == typeof(IList<>))
                {
                    listObject = InvokeGeneric(nameof(MakeList), type.GetGenericArguments()[0], new[] { values });
                }
                else if (type.GetGenericTypeDefinition() == typeof(Collection<>) || type.GetGenericTypeDefinition() == typeof(ICollection<>))
                {
                    listObject = InvokeGeneric(nameof(MakeCollection), type.GetGenericArguments()[0], new[] { values });
                }
                else if (type.GetGenericTypeDefinition() == typeof(ObservableCollection<>))
                {
                    listObject = InvokeGeneric(nameof(MakeObservableCollection), type.GetGenericArguments()[0], new[] { values });
                }
            }

            typedEnumerable = listObject;
            return listObject != null;
        }

        static void CopyList<TObject>(TObject obj, PropertyInfo property, object[] values)
        {
            var propertyType = property.PropertyType;
            object list;
            if (!CreateTypedEnumerable(property.PropertyType, values, out list))
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
    }
}
