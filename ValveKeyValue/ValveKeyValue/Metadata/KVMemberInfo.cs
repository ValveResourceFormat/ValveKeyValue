using System.ComponentModel;

namespace ValveKeyValue.Metadata
{
    /// <summary>
    /// Describes a member of <typeparamref name="T"/> that maps to a KeyValues child.
    /// This type is infrastructure for generated serialization code and is not intended to be used directly.
    /// Instances are created through <see cref="KVMetadata.Member"/>.
    /// </summary>
    /// <typeparam name="T">The type that declares the member.</typeparam>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public abstract class KVMemberInfo<T>
    {
        private protected KVMemberInfo(string name, string declaredName, bool isRequired)
        {
            Name = name;
            DeclaredName = declaredName;
            IsRequired = isRequired;
        }

        /// <summary>
        /// Gets the name of the member as it appears in KeyValues data.
        /// </summary>
        public string Name { get; }

        /// <summary>
        /// Gets the name of the member as it is declared on the type.
        /// </summary>
        public string DeclaredName { get; }

        /// <summary>
        /// Gets a value indicating whether the member must be present in KeyValues data when deserializing.
        /// </summary>
        public bool IsRequired { get; }

        /// <summary>
        /// Gets a value indicating whether the member value can be read, which makes it part of serialized output.
        /// </summary>
        public abstract bool CanRead { get; }

        /// <summary>
        /// Gets a value indicating whether the member value can be assigned when deserializing.
        /// </summary>
        public abstract bool CanWrite { get; }

        // Reads the member value and assigns it. Only called when the member can be written.
        internal abstract void ReadInto(ref T target, KVObject value);

        // Returns null when the member value is null. Only called when the member can be read.
        internal abstract KVObject? WriteValue(T target, KVWriteContext context);
    }

    sealed class KVMemberInfo<T, TValue> : KVMemberInfo<T>
    {
        public KVMemberInfo(string name, string declaredName, bool isRequired, KVTypeInfo<TValue> type, Func<T, TValue?>? getter, KVSetter<T, TValue>? setter)
            : base(name, declaredName, isRequired)
        {
            this.type = type;
            this.getter = getter;
            this.setter = setter;
        }

        readonly KVTypeInfo<TValue> type;
        readonly Func<T, TValue?>? getter;
        readonly KVSetter<T, TValue>? setter;

        public override bool CanRead => getter is not null;

        public override bool CanWrite => setter is not null;

        internal override void ReadInto(ref T target, KVObject value) => setter!(ref target, type.Read(value));

        internal override KVObject? WriteValue(T target, KVWriteContext context)
        {
            var value = getter!(target);
            return value is null ? null : type.Write(value, context);
        }
    }
}
