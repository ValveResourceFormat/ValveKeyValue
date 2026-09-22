using System.Diagnostics.CodeAnalysis;
using ValveKeyValue.Metadata;

namespace ValveKeyValue
{
    // Resolves the reflection-based type information of T on first use, and writes values of a non-sealed
    // reference type using the type information of their runtime type.
    [RequiresUnreferencedCode(KVReflectionTypeInfo.RequiresMessage)]
    [RequiresDynamicCode(KVReflectionTypeInfo.RequiresMessage)]
    sealed class KVRuntimeTypeInfo<T> : KVTypeInfo<T>
    {
        static readonly bool IsPolymorphic = !typeof(T).IsValueType && !typeof(T).IsSealed;

        KVTypeInfo<T>? declared;

        KVTypeInfo<T> Declared => declared ??= KVReflectionTypeInfo.Get<T>();

        internal override T ReadCore(KVObject value) => Declared.ReadCore(value);

        internal override KVObject Write(T value, KVWriteContext context)
        {
            if (IsPolymorphic)
            {
                var runtimeType = value!.GetType();

                if (runtimeType != typeof(T))
                {
                    return KVReflectionTypeInfo.Get(runtimeType).WriteBoxed(value, context);
                }
            }

            return Declared.Write(value, context);
        }
    }
}
