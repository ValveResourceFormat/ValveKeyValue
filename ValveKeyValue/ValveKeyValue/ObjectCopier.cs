using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Runtime.Serialization;

namespace ValveKeyValue
{
    // TODO: Migrate to IVisitationListener
    static class ObjectCopier
    {
        public static TObject MakeObject<TObject>(KVObject keyValueObject)
            => MakeObject<TObject>(keyValueObject, new DefaultObjectReflector());

        public static object MakeObject(Type objectType, KVObject keyValueObject, IObjectReflector reflector)
            => InvokeGeneric(nameof(MakeObject), objectType, new object[] { keyValueObject, reflector });

        public static TObject MakeObject<TObject>(KVObject keyValueObject, IObjectReflector reflector)
        {
            Require.NotNull(keyValueObject, nameof(keyValueObject));
            Require.NotNull(reflector, nameof(reflector));

            if (keyValueObject.Value.ValueType == KVValueType.Collection)
            {
                object[] enumerableValues;
                Type lookupValueType;
                object enumerable;
                if (IsLookupWithStringKey(typeof(TObject), out lookupValueType))
                {
                    return (TObject)MakeLookup(lookupValueType, keyValueObject);
                }
                else if (IsDictionary(typeof(TObject)))
                {
                    return (TObject)MakeDictionary(typeof(TObject), keyValueObject);
                }
                else if (IsArray(keyValueObject, out enumerableValues) && ConstructTypedEnumerable(typeof(TObject), enumerableValues, out enumerable))
                {
                    return (TObject)enumerable;
                }
                else if (IsConstructibleEnumerableType(typeof(TObject)))
                {
                    throw new InvalidOperationException($"Cannot deserialize a non-array value to type \"{typeof(TObject).Namespace}.{typeof(TObject).Name}\".");
                }

                var typedObject = (TObject)FormatterServices.GetUninitializedObject(typeof(TObject));

                CopyObject(keyValueObject, typedObject, reflector);
                return typedObject;
            }
            else if (CanConvertValueTo(typeof(TObject)))
            {
                return (TObject)Convert.ChangeType(keyValueObject.Value, typeof(TObject));
            }
            else
            {
                throw new NotSupportedException(typeof(TObject).Name);
            }
        }

        public static KVObject FromObject<TObject>(TObject managedObject, string topLevelName)
            => FromObjectCore(managedObject, topLevelName, new DefaultObjectReflector(), new HashSet<object>());

        static KVObject FromObjectCore<TObject>(TObject managedObject, string topLevelName, IObjectReflector reflector, HashSet<object> visitedObjects)
        {
            if (managedObject == null)
            {
                throw new ArgumentNullException(nameof(managedObject));
            }

            Require.NotNull(topLevelName, nameof(topLevelName));
            Require.NotNull(reflector, nameof(reflector));
            Require.NotNull(visitedObjects, nameof(visitedObjects));

            if (!typeof(TObject).IsValueType && typeof(TObject) != typeof(string) && !visitedObjects.Add(managedObject))
            {
                throw new KeyValueException("Serialization failed - circular object reference detected.");
            }

            if (typeof(IConvertible).IsAssignableFrom(typeof(TObject)))
            {
                  return new KVObject(topLevelName, (string)Convert.ChangeType(managedObject, typeof(string)));
            }

            var childObjects = new List<KVObject>();

            if (typeof(IDictionary).IsAssignableFrom(typeof(TObject)))
            {
                var dictionary = (IDictionary)managedObject;
                var enumerator = dictionary.GetEnumerator();
                while (enumerator.MoveNext())
                {
                    var entry = enumerator.Entry;
                    childObjects.Add(new KVObject(entry.Key.ToString(), entry.Value.ToString()));
                }
            }
            else if (typeof(TObject).IsArray || typeof(IEnumerable).IsAssignableFrom(typeof(TObject)))
            {
                var counter = 0;
                foreach (var child in (IEnumerable)managedObject)
                {
                    var childKVObject = CopyObject(child, counter.ToString(), reflector, visitedObjects);
                    childObjects.Add(childKVObject);

                    counter++;
                }
            }
            else
            {
                foreach (var member in reflector.GetMembers(managedObject).OrderBy(p => p.Name))
                {
                    if (!member.MemberType.IsValueType && member.Value is null)
                    {
                        continue;
                    }

                    var name = member.Name;
                    if (!member.IsExplicitName && name.Length > 0 && char.IsUpper(name[0]))
                    {
                        name = char.ToLower(name[0]) + name.Substring(1);
                    }

                    if (typeof(IConvertible).IsAssignableFrom(member.MemberType))
                    {
                        childObjects.Add(new KVObject(name, (string)Convert.ChangeType(member.Value, typeof(string))));
                    }
                    else
                    {
                        childObjects.Add(CopyObject(member.Value, member.Name, reflector, visitedObjects));
                    }
                }
            }

            return new KVObject(topLevelName, childObjects);
        }

        static KVObject CopyObject(object @object, string name, IObjectReflector reflector, HashSet<object> visitedObjects)
        {
            try
            {
                var keyValueRepresentation = (KVObject)typeof(ObjectCopier)
                    .GetMethod(nameof(FromObjectCore), BindingFlags.NonPublic | BindingFlags.Static)
                    .MakeGenericMethod(@object.GetType())
                    .Invoke(null, new[] { @object, name, reflector, visitedObjects });

                return keyValueRepresentation;
            }
            catch (TargetInvocationException ex)
            {
                ExceptionDispatchInfo.Capture(ex.InnerException).Throw();
                return default(KVObject); // Unreachable.
            }
        }

        static void CopyObject<TObject>(KVObject kv, TObject obj, IObjectReflector reflector)
        {
            Require.NotNull(kv, nameof(kv));

            // Cannot use Require.NotNull here because TObject might be a struct.
            if (obj == null)
            {
                throw new ArgumentNullException(nameof(obj));
            }

            Require.NotNull(reflector, nameof(reflector));

            var members = reflector.GetMembers(obj).ToDictionary(m => m.Name, m => m, StringComparer.OrdinalIgnoreCase);

            foreach (var item in kv.Children)
            {
                IObjectMember member;
                if (!members.TryGetValue(item.Name, out member))
                {
                    continue;
                }

                member.Value = MakeObject(member.MemberType, item, reflector);
            }
        }

        static bool IsArray(KVObject obj, out object[] values)
        {
            values = null;

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

        static object MakeLookup(Type valueType, IEnumerable<KVObject> items)
            => InvokeGeneric(nameof(MakeLookupCore), valueType, new object[] { items });

        static ILookup<string, TValue> MakeLookupCore<TValue>(IEnumerable<KVObject> items)
            => items.ToLookup(kv => kv.Name, kv => (TValue)Convert.ChangeType(kv.Value, typeof(TValue)));

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
            else if (type.IsConstructedGenericType)
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

        static bool CanConvertValueTo(Type type)
        {
            return
                type == typeof(bool) ||
                type == typeof(byte) ||
                type == typeof(char) ||
                type == typeof(DateTime) ||
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
    }
}
