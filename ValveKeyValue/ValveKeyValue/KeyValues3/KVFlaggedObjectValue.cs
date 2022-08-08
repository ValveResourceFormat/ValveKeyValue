using System;

namespace ValveKeyValue.KeyValues3
{
    class KVFlaggedObjectValue<TObject> : KVObjectValue<TObject>
        where TObject : IConvertible
    {
        public KVFlag Flag { get; private set; }

        public KVFlaggedObjectValue(TObject value, KVFlag flag, KVValueType valueType)
            : base(value, valueType)
        {
            Flag = flag;
        }
    }
}
