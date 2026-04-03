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

        public void Visit(string? name, KVObject @object)
        {
            VisitObject(name, @object, false);
        }

        void VisitObject(string? name, KVObject obj, bool isArray)
        {
            switch (obj.ValueType)
            {
                case KVValueType.Collection:
                    listener.OnObjectStart(name, obj.Flag);
                    foreach (var (childKey, child) in obj)
                    {
                        VisitObject(childKey, child, false);
                    }
                    listener.OnObjectEnd();
                    break;

                case KVValueType.Array:
                    var arrayList = obj.GetArrayList();
                    var allSimple = true;
                    foreach (var element in arrayList)
                    {
                        if (!IsSimpleType(element.ValueType))
                        {
                            allSimple = false;
                            break;
                        }
                    }
                    listener.OnArrayStart(name, obj.Flag, arrayList.Count, allSimple);
                    foreach (var element in arrayList)
                    {
                        VisitObject(null, element, true);
                    }
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
                        listener.OnArrayValue(obj);
                        break;
                    }
                    listener.OnKeyValuePair(name!, obj);
                    break;

                default:
                    throw new InvalidOperationException($"Unhandled value type: {obj.ValueType}");
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
