namespace ValveKeyValue.Metadata
{
    sealed class KVWriteContext
    {
        readonly HashSet<object> visited = new(ReferenceEqualityComparer.Instance);

        // Every reference-type instance may appear only once in a serialized graph. Values of a value type are not tracked.
        public void Enter<T>(T value)
        {
            if (typeof(T).IsValueType)
            {
                return;
            }

            if (!visited.Add(value!))
            {
                throw new KeyValueException("Serialization failed - circular object reference detected.");
            }
        }
    }
}
