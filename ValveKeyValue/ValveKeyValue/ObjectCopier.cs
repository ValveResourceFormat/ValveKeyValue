using System.Collections;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace ValveKeyValue
{
    static class ObjectCopier
    {
        static readonly JsonSerializerOptions s_options = CreateOptions();
        static readonly ConditionalWeakTable<JsonSerializerContext, JsonSerializerOptions> s_contextOptionsCache = new();

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026", Justification = "Types that exist at runtime are preserved.")]
        [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Types that exist at runtime are preserved.")]
        static JsonSerializerOptions CreateOptions()
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                TypeInfoResolver = new DefaultJsonTypeInfoResolver
                {
                    Modifiers = { ModifyTypeInfo }
                },
                Converters = { new IntPtrConverter() },
            };
            return options;
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026", Justification = "Types that exist at runtime are preserved.")]
        [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Types that exist at runtime are preserved.")]
        static JsonSerializerOptions GetOptions(JsonSerializerContext context)
        {
            return s_contextOptionsCache.GetValue(context, static ctx => new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                TypeInfoResolver = JsonTypeInfoResolver.Combine(ctx, new DefaultJsonTypeInfoResolver())
                    .WithAddedModifier(ModifyTypeInfo),
                Converters = { new IntPtrConverter() },
            });
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070", Justification = "Non-public properties on types that exist at runtime are available.")]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2075", Justification = "Non-public properties on types that exist at runtime are available.")]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026", Justification = "Non-public properties on types that exist at runtime are available.")]
        [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Non-public properties on types that exist at runtime are available.")]
        static void ModifyTypeInfo(JsonTypeInfo typeInfo)
        {
            if (typeInfo.Kind != JsonTypeInfoKind.Object)
                return;

            var seenProps = new HashSet<string>();

            for (var i = typeInfo.Properties.Count - 1; i >= 0; i--)
            {
                var prop = typeInfo.Properties[i];
                var memberInfo = prop.AttributeProvider as MemberInfo;

                if (memberInfo?.GetCustomAttribute<KVIgnoreAttribute>() != null)
                {
                    typeInfo.Properties.RemoveAt(i);
                    continue;
                }

                var kvProp = memberInfo?.GetCustomAttribute<KVPropertyAttribute>();
                if (kvProp != null)
                {
                    prop.Name = kvProp.PropertyName;
                }

                if (memberInfo is PropertyInfo pi)
                {
                    seenProps.Add(pi.Name);
                }
            }

            foreach (var pi in typeInfo.Type.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic))
            {
                if (seenProps.Contains(pi.Name))
                    continue;
                if (pi.GetCustomAttribute<KVIgnoreAttribute>() != null)
                    continue;

                var jsonProp = typeInfo.CreateJsonPropertyInfo(pi.PropertyType, pi.Name);
                jsonProp.Get = pi.CanRead ? pi.GetValue : null;
                jsonProp.Set = pi.CanWrite ? pi.SetValue : null;

                var kvProp = pi.GetCustomAttribute<KVPropertyAttribute>();
                if (kvProp != null)
                {
                    jsonProp.Name = kvProp.PropertyName;
                }

                typeInfo.Properties.Add(jsonProp);
            }
        }

        #region Deserialization (KVObject -> T)

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026", Justification = "Types that exist at runtime are preserved.")]
        [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Types that exist at runtime are preserved.")]
        public static TObject MakeObject<[DynamicallyAccessedMembers(Trimming.Constructors | Trimming.Properties)] TObject>(KVObject keyValueObject)
            => MakeObjectCore<TObject>(keyValueObject, s_options);

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026", Justification = "Types that exist at runtime are preserved.")]
        [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Types that exist at runtime are preserved.")]
        public static TObject MakeObject<[DynamicallyAccessedMembers(Trimming.Constructors | Trimming.Properties)] TObject>(KVObject keyValueObject, JsonSerializerContext context)
            => MakeObjectCore<TObject>(keyValueObject, GetOptions(context));

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026", Justification = "Types that exist at runtime are preserved.")]
        [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Types that exist at runtime are preserved.")]
        static TObject MakeObjectCore<[DynamicallyAccessedMembers(Trimming.Constructors | Trimming.Properties)] TObject>(KVObject keyValueObject, JsonSerializerOptions options)
        {
            ArgumentNullException.ThrowIfNull(keyValueObject);

            if (IsValueTupleType(typeof(TObject)))
            {
                return (TObject)DeserializeValueTuple(typeof(TObject), keyValueObject, options);
            }

            var jsonNode = KVObjectToJsonNode(keyValueObject, typeof(TObject), options);
            return jsonNode.Deserialize<TObject>(options);
        }

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026", Justification = "Types that exist at runtime are preserved.")]
        [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "Types that exist at runtime are preserved.")]
        static JsonNode KVObjectToJsonNode(KVObject kv, Type targetType, JsonSerializerOptions options)
        {
            targetType = Nullable.GetUnderlyingType(targetType) ?? targetType;

            return kv.ValueType switch
            {
                KVValueType.Null => null,
                KVValueType.BinaryBlob => JsonValue.Create(Convert.ToBase64String(kv.AsBlob())),
                KVValueType.Collection => ConvertCollectionToJson(kv, targetType, options),
                KVValueType.Array => ConvertArrayToJson(kv, targetType, options),
                _ => ConvertScalarToJson(kv, targetType),
            };
        }

        static JsonNode ConvertScalarToJson(KVObject kv, Type targetType)
        {
            if (targetType == typeof(DateTime))
            {
                throw new NotSupportedException($"Converting to DateTime is not supported. (type = {kv.ValueType})");
            }

            if (targetType == typeof(bool))
                return JsonValue.Create(kv.ToBoolean(CultureInfo.InvariantCulture));
            if (targetType == typeof(string))
                return JsonValue.Create(kv.ToString(CultureInfo.InvariantCulture));
            if (targetType == typeof(IntPtr))
                return JsonValue.Create(kv.ToInt32(CultureInfo.InvariantCulture));
            if (targetType == typeof(byte))
                return JsonValue.Create((int)kv.ToByte(CultureInfo.InvariantCulture));
            if (targetType == typeof(sbyte))
                return JsonValue.Create((int)kv.ToSByte(CultureInfo.InvariantCulture));
            if (targetType == typeof(short))
                return JsonValue.Create((int)kv.ToInt16(CultureInfo.InvariantCulture));
            if (targetType == typeof(ushort))
                return JsonValue.Create((int)kv.ToUInt16(CultureInfo.InvariantCulture));
            if (targetType == typeof(int))
                return JsonValue.Create(kv.ToInt32(CultureInfo.InvariantCulture));
            if (targetType == typeof(uint))
                return JsonValue.Create(kv.ToUInt32(CultureInfo.InvariantCulture));
            if (targetType == typeof(long))
                return JsonValue.Create(kv.ToInt64(CultureInfo.InvariantCulture));
            if (targetType == typeof(ulong))
                return JsonValue.Create(kv.ToUInt64(CultureInfo.InvariantCulture));
            if (targetType == typeof(float))
                return JsonValue.Create(kv.ToSingle(CultureInfo.InvariantCulture));
            if (targetType == typeof(double))
                return JsonValue.Create(kv.ToDouble(CultureInfo.InvariantCulture));
            if (targetType == typeof(decimal))
                return JsonValue.Create(kv.ToDecimal(CultureInfo.InvariantCulture));
            if (targetType == typeof(char))
                return JsonValue.Create(kv.ToChar(CultureInfo.InvariantCulture).ToString());

            if (targetType.IsEnum)
                return ConvertScalarToJson(kv, Enum.GetUnderlyingType(targetType));

            // Unknown target — structural fallback
            return kv.ValueType switch
            {
                KVValueType.String => JsonValue.Create(kv.ToString(CultureInfo.InvariantCulture)),
                KVValueType.Boolean => JsonValue.Create(kv.ToBoolean(CultureInfo.InvariantCulture)),
                KVValueType.Int16 or KVValueType.Int32 or KVValueType.Pointer
                    => JsonValue.Create(kv.ToInt32(CultureInfo.InvariantCulture)),
                KVValueType.Int64 => JsonValue.Create(kv.ToInt64(CultureInfo.InvariantCulture)),
                KVValueType.UInt16 or KVValueType.UInt32
                    => JsonValue.Create(kv.ToUInt32(CultureInfo.InvariantCulture)),
                KVValueType.UInt64 => JsonValue.Create(kv.ToUInt64(CultureInfo.InvariantCulture)),
                KVValueType.FloatingPoint => JsonValue.Create(kv.ToSingle(CultureInfo.InvariantCulture)),
                KVValueType.FloatingPoint64 => JsonValue.Create(kv.ToDouble(CultureInfo.InvariantCulture)),
                _ => JsonValue.Create(kv.ToString(CultureInfo.InvariantCulture)),
            };
        }

        static JsonNode ConvertCollectionToJson(KVObject kv, Type targetType, JsonSerializerOptions options)
        {
            if (IsDictionary(targetType))
            {
                var valueType = targetType.GetGenericArguments()[1];
                var obj = new JsonObject();
                foreach (var (key, child) in kv)
                {
                    if (!obj.ContainsKey(key))
                    {
                        obj.Add(key, KVObjectToJsonNode(child, valueType, options));
                    }
                }

                return obj;
            }

            if (IsCollectionType(targetType, out var elementType))
            {
                if (!TryGetArrayItems(kv, out var items))
                {
                    throw new InvalidOperationException(
                        $"Cannot deserialize a non-array value to type \"{targetType.Namespace}.{targetType.Name}\".");
                }

                return new JsonArray(items.Select(c => KVObjectToJsonNode(c, elementType, options)).ToArray());
            }

            // POCO — resolve property types from STJ contract model
            var typeInfo = options.GetTypeInfo(targetType);
            var propTypes = new Dictionary<string, Type>(StringComparer.OrdinalIgnoreCase);
            foreach (var prop in typeInfo.Properties)
            {
                propTypes.TryAdd(prop.Name, prop.PropertyType);
            }

            var jsonObj = new JsonObject();
            foreach (var (key, child) in kv)
            {
                if (!jsonObj.ContainsKey(key))
                {
                    var childType = propTypes.GetValueOrDefault(key, typeof(object));
                    jsonObj.Add(key, KVObjectToJsonNode(child, childType, options));
                }
            }

            return jsonObj;
        }

        static JsonArray ConvertArrayToJson(KVObject kv, Type targetType, JsonSerializerOptions options)
        {
            var elementType = GetCollectionElementType(targetType) ?? typeof(object);
            return new JsonArray(kv.GetArrayList().Select(c => KVObjectToJsonNode(c, elementType, options)).ToArray());
        }

        static bool TryGetArrayItems(KVObject kv, out List<KVObject> items)
        {
            items = null;
            var indexed = new List<(int Index, KVObject Value)>();

            foreach (var (key, child) in kv)
            {
                if (!int.TryParse(key, NumberStyles.Number, CultureInfo.InvariantCulture, out var index))
                    return false;
                indexed.Add((index, child));
            }

            indexed.Sort((a, b) => a.Index.CompareTo(b.Index));
            for (var i = 0; i < indexed.Count; i++)
            {
                if (indexed[i].Index != i)
                    return false;
            }

            items = indexed.ConvertAll(i => i.Value);
            return true;
        }

        static bool IsDictionary(Type type)
            => type.IsConstructedGenericType && type.GetGenericTypeDefinition() == typeof(Dictionary<,>);

        static bool IsCollectionType(Type type, out Type elementType)
        {
            elementType = null;
            if (type.IsArray && type != typeof(byte[]))
            {
                elementType = type.GetElementType();
                return true;
            }

            if (type.IsConstructedGenericType)
            {
                var gtd = type.GetGenericTypeDefinition();
                if (gtd == typeof(List<>) || gtd == typeof(IList<>) ||
                    gtd == typeof(Collection<>) || gtd == typeof(ICollection<>) ||
                    gtd == typeof(ObservableCollection<>))
                {
                    elementType = type.GetGenericArguments()[0];
                    return true;
                }
            }

            return false;
        }

        static Type GetCollectionElementType(Type type)
        {
            if (type.IsArray)
                return type.GetElementType();
            if (type.IsConstructedGenericType)
                return type.GetGenericArguments()[0];
            return null;
        }

        #endregion

        #region Serialization (T -> KVObject)

        public static KVObject FromObject(
            [DynamicallyAccessedMembers(Trimming.Properties)] Type objectType,
            object managedObject)
            => ConvertObjectToValue(objectType, managedObject, new HashSet<object>(), s_options);

        public static KVObject FromObject(
            [DynamicallyAccessedMembers(Trimming.Properties)] Type objectType,
            object managedObject,
            JsonSerializerContext context)
            => ConvertObjectToValue(objectType, managedObject, new HashSet<object>(), GetOptions(context));

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2072", Justification = "If the IDictionary's value object already exists at runtime then its properties will too.")]
        static KVObject ConvertObjectToValue(
            [DynamicallyAccessedMembers(Trimming.Properties)] Type objectType,
            object managedObject,
            HashSet<object> visitedObjects,
            JsonSerializerOptions options)
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
                    var childObjectValue = ConvertObjectToValue(entry.Value.GetType(), entry.Value, visitedObjects, options);
                    childItems.Add(new KeyValuePair<string, KVObject>(entry.Key.ToString(), childObjectValue));
                }
            }
            else if (objectType.IsArray || typeof(IEnumerable).IsAssignableFrom(objectType))
            {
                var counter = 0;
                foreach (var child in (IEnumerable)managedObject)
                {
                    var childValue = ConvertObjectToValue(child.GetType(), child, visitedObjects, options);
                    childItems.Add(new KeyValuePair<string, KVObject>(counter.ToString(CultureInfo.InvariantCulture), childValue));
                    counter++;
                }
            }
            else if (IsValueTupleType(objectType))
            {
                foreach (var field in objectType.GetFields(BindingFlags.Instance | BindingFlags.Public)
                    .OrderBy(f => f.Name, StringComparer.InvariantCulture))
                {
                    var value = field.GetValue(managedObject);
                    if (value is null)
                        continue;

                    var childValue = ConvertObjectToValue(value.GetType(), value, visitedObjects, options);
                    childItems.Add(new KeyValuePair<string, KVObject>(field.Name, childValue));
                }
            }
            else
            {
                var typeInfo = options.GetTypeInfo(objectType);
                foreach (var prop in typeInfo.Properties.OrderBy(p => p.Name, StringComparer.InvariantCulture))
                {
                    if (prop.Get is null)
                        continue;

                    var value = prop.Get(managedObject);
                    if (!prop.PropertyType.IsValueType && value is null)
                        continue;

                    var childValue = ConvertObjectToValue(value.GetType(), value, visitedObjects, options);
                    childItems.Add(new KeyValuePair<string, KVObject>(prop.Name, childValue));
                }
            }

            return new KVObject(KVValueType.Collection, childItems);
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
                TypeCode.Byte => new KVObject((int)(byte)value),
                TypeCode.SByte => new KVObject((int)(sbyte)value),
                TypeCode.Int16 => new KVObject((int)(short)value),
                TypeCode.Int32 => new KVObject((int)value),
                TypeCode.Int64 => new KVObject((long)value),
                TypeCode.Single => new KVObject((float)value),
                TypeCode.String => new KVObject((string)value),
                TypeCode.UInt16 => new KVObject((ulong)(ushort)value),
                TypeCode.UInt32 => new KVObject((ulong)(uint)value),
                TypeCode.UInt64 => new KVObject((ulong)value),
                _ => null,
            };
        }

        #endregion

        #region ValueTuple support

        static bool IsValueTupleType(Type type)
            => type.IsGenericType && type.FullName.StartsWith("System.ValueTuple`", StringComparison.Ordinal);

        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2070", Justification = "ValueTuple fields exist at runtime.")]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2067", Justification = "ValueTuple types exist at runtime.")]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2072", Justification = "ValueTuple fields exist at runtime.")]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026", Justification = "ValueTuple field types exist at runtime.")]
        [UnconditionalSuppressMessage("AOT", "IL3050", Justification = "ValueTuple field types exist at runtime.")]
        static object DeserializeValueTuple(Type type, KVObject kv, JsonSerializerOptions options)
        {
            var fields = type.GetFields(BindingFlags.Instance | BindingFlags.Public);
            var boxed = Activator.CreateInstance(type);

            var kvChildren = new Dictionary<string, KVObject>(StringComparer.OrdinalIgnoreCase);
            foreach (var (key, child) in kv)
            {
                kvChildren.TryAdd(key, child);
            }

            foreach (var field in fields)
            {
                if (!kvChildren.TryGetValue(field.Name, out var child))
                    continue;

                object value;
                if (IsValueTupleType(field.FieldType))
                {
                    value = DeserializeValueTuple(field.FieldType, child, options);
                }
                else
                {
                    var jsonNode = KVObjectToJsonNode(child, field.FieldType, options);
                    value = jsonNode.Deserialize(field.FieldType, options);
                }

                field.SetValue(boxed, value);
            }

            return boxed;
        }

        #endregion

        #region Custom JSON converters

        sealed class IntPtrConverter : JsonConverter<IntPtr>
        {
            public override IntPtr Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
            {
                if (reader.TokenType == JsonTokenType.String)
                    return new IntPtr(int.Parse(reader.GetString(), CultureInfo.InvariantCulture));
                return new IntPtr(reader.GetInt32());
            }

            public override void Write(Utf8JsonWriter writer, IntPtr value, JsonSerializerOptions options)
                => writer.WriteNumberValue(value.ToInt32());
        }

        #endregion
    }
}
