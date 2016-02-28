using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;

namespace ValveKeyValue
{
    static class ObjectCopier
    {
        public static void CopyObject<TObject>(KVObject kv, TObject obj)
        {
            CopyObject(kv, obj, new DefaultMapper());
        }

        public static void CopyObject<TObject>(KVObject kv, TObject obj, IPropertyMapper mapper)
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
                var name = mapper.MapFromKeyValue(item.Name);

                if (item.Value != null)
                {
                    CopyValue(obj, name, item.Value);
                }
                else
                {
                    object[] arrayValues;
                    if (IsArray(item, out arrayValues))
                    {
                        CopyList(obj, name, arrayValues);
                    }
                    else
                    {
                        throw new NotImplementedException();
                    }
                }
            }
        }

        public static void CopyValue<TObject>(TObject obj, string mappedName, KVValue value)
        {
            var property = typeof(TObject).GetProperty(mappedName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property == null)
            {
                return;
            }

            var propertyType = property.PropertyType;
            property.SetValue(obj, Convert.ChangeType(value, propertyType));
        }

        static void CopyList<TObject>(TObject obj, string mappedName, object[] values)
        {
            var property = typeof(TObject).GetProperty(mappedName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property == null)
            {
                return;
            }

            var propertyType = property.PropertyType;
            if (propertyType.IsArray)
            {
                var elementType = propertyType.GetElementType();
                var itemArray = Array.CreateInstance(elementType, values.Length);

                for (int i = 0; i < itemArray.Length; i++)
                {
                    itemArray.SetValue(Convert.ChangeType(values[i], elementType), i);
                }

                property.SetValue(obj, itemArray);
            }
            else if (propertyType.IsGenericType)
            {
                if (propertyType.GetGenericTypeDefinition() == typeof(List<>) || propertyType.GetGenericTypeDefinition() == typeof(IList<>))
                {
                    var list = InvokeGeneric(nameof(MakeList), propertyType.GetGenericArguments()[0], new[] { values });
                    property.SetValue(obj, list);
                }
                else if (propertyType.GetGenericTypeDefinition() == typeof(Collection<>) || propertyType.GetGenericTypeDefinition() == typeof(ICollection<>))
                {
                    var list = InvokeGeneric(nameof(MakeCollection), propertyType.GetGenericArguments()[0], new[] { values });
                    property.SetValue(obj, list);
                }
                else if (propertyType.GetGenericTypeDefinition() == typeof(ObservableCollection<>))
                {
                    var list = InvokeGeneric(nameof(MakeObservableCollection), propertyType.GetGenericArguments()[0], new[] { values });
                    property.SetValue(obj, list);
                }
                else
                {
                    throw new NotSupportedException();
                }
            }
            else
            {
                throw new NotImplementedException();
            }
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
