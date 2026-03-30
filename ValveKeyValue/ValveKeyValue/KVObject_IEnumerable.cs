using System.Collections;

namespace ValveKeyValue
{
    public partial class KVObject : IEnumerable<KeyValuePair<string, KVObject>>
    {
        /// <inheritdoc/>
        public IEnumerator<KeyValuePair<string, KVObject>> GetEnumerator()
            => Children.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => Children.GetEnumerator();
    }
}
