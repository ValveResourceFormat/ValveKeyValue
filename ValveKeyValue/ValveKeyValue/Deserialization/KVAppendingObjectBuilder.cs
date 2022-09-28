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
            KVPartialState originalStateEntry;
			if (originalBuilder.StateStack.Count <= 0)
			{
				// This will occur if a file consists only of #base or #include directives.

				originalStateEntry = new KVPartialState();
				originalStateEntry.Key = stateEntry.Key;
				originalBuilder.StateStack.Push(originalStateEntry);
			}
			else
			{
				originalStateEntry = originalBuilder.StateStack.Peek();
			}

            foreach (var item in stateEntry.Items)
            {
                originalStateEntry.Items.Add(item);
            }
        }
    }
}
