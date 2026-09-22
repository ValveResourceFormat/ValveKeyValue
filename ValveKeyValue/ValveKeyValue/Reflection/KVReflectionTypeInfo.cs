using System.Collections;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using ValveKeyValue.Metadata;

namespace ValveKeyValue
{
    // Builds type information by reflecting over types at runtime. Member, element, key, value and parameter
    // types are resolved lazily through KVRuntimeTypeInfo, which also serializes values using their runtime type.
    static class KVReflectionTypeInfo
    {
        internal const string RequiresMessage = "Object mapping without a KVTypeInfo uses reflection over the target type. Calls with a concrete type argument are replaced by the ValveKeyValue source generator. For an open generic type argument, pass a KVTypeInfo<T> obtained from KVSerializer.GetTypeInfo<T>() at a call site with a concrete type argument to make this call trim and AOT compatible.";

        static readonly ConcurrentDictionary<Type, KVTypeInfo> Cache = new();

        static readonly Dictionary<Type, KVTypeInfo> Scalars = new()
        {
            [typeof(bool)] = KVMetadata.Boolean,
            [typeof(byte)] = KVMetadata.Byte,
            [typeof(sbyte)] = KVMetadata.SByte,
            [typeof(char)] = KVMetadata.Char,
            [typeof(short)] = KVMetadata.Int16,
            [typeof(ushort)] = KVMetadata.UInt16,
            [typeof(int)] = KVMetadata.Int32,
            [typeof(uint)] = KVMetadata.UInt32,
            [typeof(long)] = KVMetadata.Int64,
            [typeof(ulong)] = KVMetadata.UInt64,
            [typeof(float)] = KVMetadata.Single,
            [typeof(double)] = KVMetadata.Double,
            [typeof(decimal)] = KVMetadata.Decimal,
            [typeof(string)] = KVMetadata.String,
            [typeof(IntPtr)] = KVMetadata.IntPtr,
            [typeof(byte[])] = KVMetadata.ByteArray,
        };

        [RequiresUnreferencedCode(RequiresMessage)]
        [RequiresDynamicCode(RequiresMessage)]
        public static KVTypeInfo<T> Get<T>() => (KVTypeInfo<T>)Get(typeof(T));

        [RequiresUnreferencedCode(RequiresMessage)]
        [RequiresDynamicCode(RequiresMessage)]
        [DynamicDependency(DynamicallyAccessedMemberTypes.NonPublicMethods, typeof(KVReflectionTypeInfo))]
        public static KVTypeInfo Get(Type type)
        {
            ArgumentNullException.ThrowIfNull(type);

            return Cache.GetOrAdd(type, Create);
        }

        [RequiresUnreferencedCode(RequiresMessage)]
        [RequiresDynamicCode(RequiresMessage)]
        static KVTypeInfo Create(Type type)
        {
            if (Scalars.TryGetValue(type, out var scalar))
            {
                return scalar;
            }

            if (type.IsEnum)
            {
                return (KVTypeInfo)Invoke(nameof(CreateEnum), [type, Enum.GetUnderlyingType(type)]);
            }

            if (Nullable.GetUnderlyingType(type) is { } nullableUnderlyingType)
            {
                return (KVTypeInfo)Invoke(nameof(CreateNullable), [nullableUnderlyingType]);
            }

            if (type.IsConstructedGenericType)
            {
                var definition = type.GetGenericTypeDefinition();
                var arguments = type.GetGenericArguments();

                if (definition == typeof(ILookup<,>) && arguments[0] == typeof(string))
                {
                    return (KVTypeInfo)Invoke(nameof(CreateLookup), [arguments[1]]);
                }

                if (definition == typeof(Dictionary<,>) || definition == typeof(IDictionary<,>) || definition == typeof(IReadOnlyDictionary<,>))
                {
                    return (KVTypeInfo)Invoke(nameof(CreateDictionary), [type, arguments[0], arguments[1]]);
                }
            }

            if (typeof(IDictionary).IsAssignableFrom(type))
            {
                var entryType = FindEnumerableElementType(type);

                if (entryType is { IsConstructedGenericType: true } && entryType.GetGenericTypeDefinition() == typeof(KeyValuePair<,>))
                {
                    var entryArguments = entryType.GetGenericArguments();
                    return (KVTypeInfo)Invoke(nameof(CreateDictionary), [type, entryArguments[0], entryArguments[1]]);
                }

                return (KVTypeInfo)Invoke(nameof(CreateUntypedEnumerable), [type]);
            }

            if (type.IsSZArray)
            {
                return (KVTypeInfo)Invoke(nameof(CreateArray), [type.GetElementType()!]);
            }

            if (FindEnumerableElementType(type) is { } elementType)
            {
                return (KVTypeInfo)Invoke(nameof(CreateCollection), [type, elementType]);
            }

            if (typeof(IEnumerable).IsAssignableFrom(type))
            {
                return (KVTypeInfo)Invoke(nameof(CreateUntypedEnumerable), [type]);
            }

            return (KVTypeInfo)Invoke(nameof(CreateObject), [type]);
        }

        [RequiresUnreferencedCode(RequiresMessage)]
        [RequiresDynamicCode(RequiresMessage)]
        static object Invoke(string methodName, Type[] typeArguments, params object?[] arguments)
        {
            var method = typeof(KVReflectionTypeInfo).GetMethod(methodName, BindingFlags.Static | BindingFlags.NonPublic)!;
            return method.MakeGenericMethod(typeArguments).Invoke(null, BindingFlags.DoNotWrapExceptions, null, arguments, null)!;
        }

        [RequiresUnreferencedCode(RequiresMessage)]
        [RequiresDynamicCode(RequiresMessage)]
        static KVTypeInfo<T> Runtime<T>() => new KVRuntimeTypeInfo<T>();

        static KVTypeInfo<TEnum> CreateEnum<TEnum, TUnderlying>()
            where TEnum : struct, Enum
            where TUnderlying : unmanaged
            => KVMetadata.Enum<TEnum, TUnderlying>((KVTypeInfo<TUnderlying>)Scalars[typeof(TUnderlying)]);

        [RequiresUnreferencedCode(RequiresMessage)]
        [RequiresDynamicCode(RequiresMessage)]
        static KVTypeInfo<T?> CreateNullable<T>()
            where T : struct
            => KVMetadata.Nullable(Get<T>());

        [RequiresUnreferencedCode(RequiresMessage)]
        [RequiresDynamicCode(RequiresMessage)]
        static KVTypeInfo<ILookup<string, TValue>> CreateLookup<TValue>()
            => KVMetadata.Lookup(Runtime<TValue>());

        [RequiresUnreferencedCode(RequiresMessage)]
        [RequiresDynamicCode(RequiresMessage)]
        static KVTypeInfo<TDictionary> CreateDictionary<TDictionary, TKey, TValue>()
            where TDictionary : IEnumerable<KeyValuePair<TKey, TValue>>
            where TKey : notnull
            => KVMetadata.Dictionary<TDictionary, TKey, TValue>(Runtime<TKey>(), Runtime<TValue>());

        [RequiresUnreferencedCode(RequiresMessage)]
        [RequiresDynamicCode(RequiresMessage)]
        static KVTypeInfo<TElement[]> CreateArray<TElement>()
            => KVMetadata.Array(Runtime<TElement>());

        [RequiresUnreferencedCode(RequiresMessage)]
        [RequiresDynamicCode(RequiresMessage)]
        static KVTypeInfo<TCollection> CreateCollection<TCollection, TElement>()
            where TCollection : IEnumerable<TElement>
            => KVMetadata.Collection<TCollection, TElement>(Runtime<TElement>());

        [RequiresUnreferencedCode(RequiresMessage)]
        [RequiresDynamicCode(RequiresMessage)]
        static KVUntypedEnumerableTypeInfo<T> CreateUntypedEnumerable<T>()
            => new KVUntypedEnumerableTypeInfo<T>();

        [RequiresUnreferencedCode(RequiresMessage)]
        [RequiresDynamicCode(RequiresMessage)]
        static Type? FindEnumerableElementType(Type type)
        {
            if (type.IsInterface && type.IsConstructedGenericType && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
            {
                return type.GetGenericArguments()[0];
            }

            foreach (var candidate in type.GetInterfaces())
            {
                if (candidate.IsConstructedGenericType && candidate.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
                    return candidate.GetGenericArguments()[0];
                }
            }

            return null;
        }

        #region Objects

        [RequiresUnreferencedCode(RequiresMessage)]
        [RequiresDynamicCode(RequiresMessage)]
        static KVTypeInfo<T> CreateObject<T>()
        {
            var construction = GetConstruction<T>();
            var members = GetMembers<T>();

            return KVMetadata.Object(
                construction.Create,
                construction.Parameters is { } parameters ? () => parameters : null,
                () => members,
                construction.SetsRequiredMembers);
        }

        readonly record struct Construction<T>(
            Func<object?[], T>? Create,
            KVParameterInfo[]? Parameters,
            bool SetsRequiredMembers);

        [RequiresUnreferencedCode(RequiresMessage)]
        [RequiresDynamicCode(RequiresMessage)]
        static KVMemberInfo<T>[] GetMembers<T>()
        {
            var members = new List<KVMemberInfo<T>>();

            if (IsValueTupleType(typeof(T)))
            {
                foreach (var field in typeof(T).GetFields(BindingFlags.Instance | BindingFlags.Public))
                {
                    members.Add((KVMemberInfo<T>)Invoke(nameof(CreateFieldMember), [typeof(T), field.FieldType], field));
                }

                return [.. members];
            }

            foreach (var property in typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
            {
                if (property.GetCustomAttribute<KVIgnoreAttribute>() != null)
                {
                    continue;
                }

                if (IsRecordEqualityContract(property))
                {
                    continue;
                }

                if (property.GetIndexParameters().Length > 0 || !IsSupportedTypeArgument(property.PropertyType))
                {
                    continue;
                }

                // A property is visible when one of its accessors is public, or when it is
                // explicitly opted in. Opting in makes any accessor usable regardless of visibility.
                var included = property.GetCustomAttribute<KVIncludeAttribute>() != null;
                var canRead = property.GetMethod is { } getter && (included || getter.IsPublic);
                var canWrite = property.SetMethod is { } setter && (included || setter.IsPublic);

                if (!canRead && !canWrite)
                {
                    continue;
                }

                members.Add((KVMemberInfo<T>)Invoke(nameof(CreatePropertyMember), [typeof(T), property.PropertyType], property, canRead, canWrite));
            }

            return [.. members];
        }

        [RequiresUnreferencedCode(RequiresMessage)]
        [RequiresDynamicCode(RequiresMessage)]
        static KVMemberInfo<T> CreatePropertyMember<T, TValue>(PropertyInfo property, bool canRead, bool canWrite)
        {
            var name = property.GetCustomAttribute<KVPropertyAttribute>()?.PropertyName ?? property.Name;
            var isRequired = property.GetCustomAttribute<RequiredMemberAttribute>() != null;

            return KVMetadata.Member(
                name,
                property.Name,
                Runtime<TValue>(),
                canRead ? CreatePropertyGetter<T, TValue>(property) : null,
                canWrite ? CreatePropertySetter<T, TValue>(property) : null,
                isRequired);
        }

        // Accessors of reference types are called through delegates bound to the accessor methods. Accessors of value
        // types, and accessors that cannot be bound, are called through reflection.
        static Func<T, TValue?> CreatePropertyGetter<T, TValue>(PropertyInfo property)
        {
            if (!typeof(T).IsValueType)
            {
                try
                {
                    return property.GetMethod!.CreateDelegate<Func<T, TValue>>();
                }
                catch (ArgumentException)
                {
                }
            }

            return target => (TValue?)property.GetValue(target);
        }

        static KVSetter<T, TValue> CreatePropertySetter<T, TValue>(PropertyInfo property)
        {
            if (!typeof(T).IsValueType)
            {
                try
                {
                    var setter = property.SetMethod!.CreateDelegate<Action<T, TValue>>();
                    return (ref T target, TValue value) => setter(target, value);
                }
                catch (ArgumentException)
                {
                }
            }

            return CreateSetter<T, TValue>(property.SetValue);
        }

        [RequiresUnreferencedCode(RequiresMessage)]
        [RequiresDynamicCode(RequiresMessage)]
        static KVMemberInfo<T> CreateFieldMember<T, TValue>(FieldInfo field)
            => KVMetadata.Member(
                field.Name,
                field.Name,
                Runtime<TValue>(),
                target => (TValue?)field.GetValue(target),
                field.IsInitOnly ? null : CreateSetter<T, TValue>(field.SetValue),
                isRequired: false);

        static KVSetter<T, TValue> CreateSetter<T, TValue>(Action<object?, object?> setValue)
        {
            if (typeof(T).IsValueType)
            {
                return (ref T target, TValue value) =>
                {
                    object boxed = target!;
                    setValue(boxed, value);
                    target = (T)boxed;
                };
            }

            return (ref T target, TValue value) => setValue(target, value);
        }

        // Picks the constructor in this order: the one marked with [KVConstructor], a public parameterless
        // constructor, or the single public constructor. A struct that declares no public constructor is
        // created from its default value. Without a usable constructor, the type cannot be deserialized.
        [RequiresUnreferencedCode(RequiresMessage)]
        [RequiresDynamicCode(RequiresMessage)]
        static Construction<T> GetConstruction<T>()
        {
            var constructors = typeof(T).GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            var marked = Array.FindAll(constructors, static c => c.GetCustomAttribute<KVConstructorAttribute>() != null);

            if (marked.Length > 1)
            {
                var message = $"Type '{typeof(T).Name}' has more than one constructor marked with [KVConstructor].";
                return new(_ => throw new KeyValueException(message), null, false);
            }

            var publicConstructors = Array.FindAll(constructors, static c => c.IsPublic);
            var constructor = marked.Length == 1
                ? marked[0]
                : Array.Find(publicConstructors, static c => c.GetParameters().Length == 0) ?? (publicConstructors.Length == 1 ? publicConstructors[0] : null);

            if (constructor is null)
            {
                return publicConstructors.Length == 0 && typeof(T).IsValueType ? new(static _ => default!, null, false) : default;
            }

            var setsRequiredMembers = constructor.GetCustomAttribute<SetsRequiredMembersAttribute>() != null;
            var parameters = constructor.GetParameters();
            var invoker = ConstructorInvoker.Create(constructor);

            if (parameters.Length == 0)
            {
                return new(_ => (T)invoker.Invoke(), null, setsRequiredMembers);
            }

            var parameterInfos = new KVParameterInfo[parameters.Length];

            for (var i = 0; i < parameters.Length; i++)
            {
                var parameter = parameters[i];
                var parameterType = parameter.ParameterType.IsByRef ? parameter.ParameterType.GetElementType()! : parameter.ParameterType;

                if (!IsSupportedTypeArgument(parameterType))
                {
                    var message = $"Constructor parameter '{parameter.Name}' of type '{parameterType.Name}' on type '{typeof(T).Name}' is not supported.";
                    return new(_ => throw new NotSupportedException(message), null, setsRequiredMembers);
                }

                parameterInfos[i] = (KVParameterInfo)Invoke(nameof(CreateParameter), [parameterType], parameter);
            }

            return new(arguments => (T)invoker.Invoke(arguments), parameterInfos, setsRequiredMembers);
        }

        [RequiresUnreferencedCode(RequiresMessage)]
        [RequiresDynamicCode(RequiresMessage)]
        static KVParameterInfo CreateParameter<T>(ParameterInfo parameter)
            => KVMetadata.Parameter(parameter.Name ?? string.Empty, Runtime<T>(), GetDefaultValue<T>(parameter));

        // ParameterInfo.DefaultValue is null for a default value of a struct type, and the underlying value for some
        // enum default values.
        static T? GetDefaultValue<T>(ParameterInfo parameter)
        {
            if (!parameter.HasDefaultValue || parameter.DefaultValue is not { } value || value is DBNull || value == Missing.Value)
            {
                return default;
            }

            if (value is T typed)
            {
                return typed;
            }

            var type = Nullable.GetUnderlyingType(typeof(T)) ?? typeof(T);

            return type.IsEnum
                ? (T)Enum.ToObject(type, value)
                : (T)Convert.ChangeType(value, type, CultureInfo.InvariantCulture);
        }

        static bool IsSupportedTypeArgument(Type type)
            => !type.IsByRef && !type.IsByRefLike && !type.IsPointer && !type.IsFunctionPointer;

        static bool IsValueTupleType(Type type)
            => type.IsGenericType && type.FullName!.StartsWith("System.ValueTuple`", StringComparison.Ordinal);

        // Records synthesize a protected "Type EqualityContract" property which must not be treated as data.
        static bool IsRecordEqualityContract(PropertyInfo property)
            => property.Name == "EqualityContract"
            && property.PropertyType == typeof(Type)
            && property.GetMethod?.GetCustomAttribute<CompilerGeneratedAttribute>() != null;

        #endregion
    }
}
