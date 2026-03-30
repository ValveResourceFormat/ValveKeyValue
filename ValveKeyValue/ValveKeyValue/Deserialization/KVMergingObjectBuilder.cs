namespace ValveKeyValue.Deserialization
{
    sealed class KVMergingObjectBuilder : KVObjectBuilder
    {
        public KVMergingObjectBuilder(KVObjectBuilder originalBuilder)
        {
            ArgumentNullException.ThrowIfNull(originalBuilder);

            this.originalBuilder = originalBuilder;
        }

        readonly KVObjectBuilder originalBuilder;

        protected override void FinalizeState()
        {
            base.FinalizeState();

            var stateEntry = StateStack.Peek();
            var originalStateEntry = originalBuilder.StateStack.Peek();

            Merge(from: stateEntry, into: originalStateEntry);
        }

        static void Merge(KVPartialState from, KVPartialState into)
        {
            foreach (var item in from.Items)
            {
                var matchingIndex = into.Items.FindIndex(i => i.Key == item.Key);
                if (matchingIndex < 0)
                {
                    into.Items.Add(item);
                }
                else
                {
                    Merge(from: item.Value, into: into.Items[matchingIndex].Value);
                }
            }
        }

        static void Merge(KVObject from, KVObject into)
        {
            foreach (var (key, child) in from)
            {
                if (into.TryGetValue(key, out var matchingChild))
                {
                    Merge(from: child, into: matchingChild);
                }
                else if (into.ValueType == KVValueType.Collection)
                {
                    into.Add(key, child);
                }
            }
        }
    }
}
