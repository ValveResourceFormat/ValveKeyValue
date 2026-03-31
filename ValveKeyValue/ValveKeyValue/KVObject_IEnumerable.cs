using System.Collections;

namespace ValveKeyValue
{
    public partial class KVObject
    {
        /// <inheritdoc/>
        public IEnumerator<KeyValuePair<string, KVObject>> GetEnumerator()
            => Children.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => Children.GetEnumerator();
    }
}
