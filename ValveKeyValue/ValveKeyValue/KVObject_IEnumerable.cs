using System.Collections;

namespace ValveKeyValue
{
    public readonly partial struct KVObject
    {
        /// <inheritdoc/>
        public IEnumerator<KeyValuePair<string, KVObject>> GetEnumerator()
            => Children.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => Children.GetEnumerator();
    }
}
