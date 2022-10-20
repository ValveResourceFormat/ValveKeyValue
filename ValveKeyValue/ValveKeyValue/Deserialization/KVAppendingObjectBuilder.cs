namespace ValveKeyValue.Deserialization
{
    sealed class KVAppendingObjectBuilder : KVObjectBuilder
    {
        public KVAppendingObjectBuilder(KVObjectBuilder originalBuilder)
        {
            Require.NotNull(originalBuilder, nameof(originalBuilder));

            this.originalBuilder = originalBuilder;
        }

        readonly KVObjectBuilder originalBuilder;

        protected override void FinalizeState()
        {
            base.FinalizeState();

            var stateEntry = StateStack.Peek();
            var originalStateEntry = originalBuilder.StateStack.Peek();

            foreach (var item in stateEntry.Items)
            {
                originalStateEntry.Items.Add(item);
            }
        }
    }
}
