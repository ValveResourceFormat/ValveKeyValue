namespace ValveKeyValue.Abstraction
{
    sealed class KVObjectVisitor
    {
        public KVObjectVisitor(IVisitationListener listener)
        {
            ArgumentNullException.ThrowIfNull(listener);

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

                case KVValueType.Array:
                    var array = (ICollection<KVValue>)value;
                    var allSimple = true;
                    foreach (var item in array)
                    {
                        if (!IsSimpleType(item.ValueType))
                        {
                            allSimple = false;
                            break;
                        }
                    }
                    listener.OnArrayStart(name, value.Flag, array.Count, allSimple);
                    VisitArray(array);
                    listener.OnArrayEnd();
                    break;

                case KVValueType.BinaryBlob:
                case KVValueType.FloatingPoint:
                case KVValueType.FloatingPoint64:
                case KVValueType.Int16:
                case KVValueType.Int32:
                case KVValueType.UInt16:
                case KVValueType.UInt32:
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
                    throw new InvalidOperationException($"Unhandled value type: {value.ValueType}");
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

        static bool IsSimpleType(KVValueType type) => type is
            KVValueType.Null or
            KVValueType.Boolean or
            KVValueType.Int16 or
            KVValueType.Int32 or
            KVValueType.Int64 or
            KVValueType.UInt16 or
            KVValueType.UInt32 or
            KVValueType.UInt64 or
            KVValueType.FloatingPoint or
            KVValueType.FloatingPoint64;
    }
}
