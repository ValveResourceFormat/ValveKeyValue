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

            if (StateStack.Count <= 0)
			{
				// This will occur if an #include file does not exist.
				return;
			}

            var stateEntry = StateStack.Peek();
            var originalStateEntry = originalBuilder.StateStack.Peek();

            foreach (var item in stateEntry.Items)
            {
                originalStateEntry.Items.Add(item);
            }
        }
    }
}
