using System.ComponentModel;

namespace ValveKeyValue.Metadata
{
    /// <summary>
    /// Assigns a member value on an instance that is passed by reference, so that value types can be updated in place.
    /// This delegate is infrastructure for generated serialization code and is not intended to be used directly.
    /// </summary>
    /// <typeparam name="T">The type that declares the member.</typeparam>
    /// <typeparam name="TValue">The type of the member.</typeparam>
    /// <param name="target">The instance to update.</param>
    /// <param name="value">The value to assign.</param>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public delegate void KVSetter<T, in TValue>(ref T target, TValue value);
}
