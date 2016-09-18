using System.Linq;

namespace ValveKeyValue.Deserialization
{
    sealed class KVMergingObjectBuilder : KVObjectBuilder
    {
        public KVMergingObjectBuilder(KVObjectBuilder originalBuilder)
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

            Merge(from: stateEntry, into: originalStateEntry);
        }

        static void Merge(KVPartialState from, KVPartialState into)
        {
            foreach (var item in from.Items)
            {
                var matchingItem = into.Items.FirstOrDefault(i => i.Name == item.Name);
                if (matchingItem == null)
                {
                    into.Items.Add(item);
                }
                else
                {
                    Merge(from: item, into: matchingItem);
                }
            }
        }

        static void Merge(KVObject from, KVObject into)
        {
            foreach (var child in from)
            {
                var matchingChild = into.Children.FirstOrDefault(c => c.Name == child.Name);
                if (matchingChild == null && into.Value.ValueType == KVValueType.Collection)
                {
                    into.Add(child);
                }
                else
                {
                    Merge(from: child, into: matchingChild);
                }
            }
        }
    }
}
