using System.Collections;

namespace ValveKeyValue
{
    /// <summary>
    /// Represents a dynamic KeyValue object.
    /// </summary>
    public partial class KVObject : IEnumerable<KVObject>
    {
        /// <inheritdoc/>
        public IEnumerator<KVObject> GetEnumerator()
            => Children.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
            => Children.GetEnumerator();
    }
}
