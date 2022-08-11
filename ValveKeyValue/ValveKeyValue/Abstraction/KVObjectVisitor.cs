namespace ValveKeyValue.Abstraction
{
    sealed class KVObjectVisitor
    {
        public KVObjectVisitor(IVisitationListener listener)
        {
            Require.NotNull(listener, nameof(listener));

            this.listener = listener;
        }

        readonly IVisitationListener listener;

        public void Visit(KVObject @object)
        {
            VisitObject(@object.Name, @object.Value, false);
        }

        void VisitObject(string name, KVValue value, bool isArray)
        {
            switch (value.ValueType)
            {
                case KVValueType.Collection:
                    listener.OnObjectStart(name, value.Flag);
                    VisitValue((IEnumerable<KVObject>)value);
                    listener.OnObjectEnd();
                    break;

                case KVValueType.BinaryBlob:
                    // TODO: write binary blobs
                    break;

                case KVValueType.Array:
                    listener.OnArrayStart(name, value.Flag);
                    VisitArray((IEnumerable<KVValue>)value);
                    listener.OnArrayEnd();
                    break;

                case KVValueType.FloatingPoint:
                case KVValueType.Int32:
                case KVValueType.Pointer:
                case KVValueType.String:
                case KVValueType.UInt64:
                case KVValueType.Int64:
                case KVValueType.Boolean:
                case KVValueType.Null:
                    if (isArray)
                    {
                        listener.OnArrayValue(value);
                        break;
                    }
                    listener.OnKeyValuePair(name, value);
                    break;

                default:
                    throw new ArgumentOutOfRangeException(nameof(value.ValueType), value.ValueType, "Unhandled value type.");
            }
        }

        void VisitValue(IEnumerable<KVObject> collection)
        {
            foreach (var item in collection)
            {
                VisitObject(item.Name, item.Value, false);
            }
        }

        void VisitArray(IEnumerable<KVValue> collection)
        {
            foreach (var item in collection)
            {
                VisitObject(null, item, true);
            }
        }
    }
}
