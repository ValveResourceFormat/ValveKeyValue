using System.Collections;

namespace ValveKeyValue
{
    public partial class KVObject : IEnumerable<KVObject>
    {
        /// <inheritdoc/>
        public IEnumerator<KVObject> GetEnumerator()
            => Children.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => Children.GetEnumerator();
    }
}
