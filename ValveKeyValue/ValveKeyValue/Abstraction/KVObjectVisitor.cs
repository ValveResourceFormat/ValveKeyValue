namespace ValveKeyValue.Abstraction
{
    /// <summary>
    /// Walks a tree of <see cref="KVObject"/>s, announcing each node to a listener. Only the
    /// KeyValues1 and KeyValues3 codecs use this; KeyValues2 is a graph rather than a tree and
    /// reads and writes itself directly.
    /// </summary>
    sealed class KVObjectVisitor
    {
        public KVObjectVisitor(IVisitationListener listener)
        {
            ArgumentNullException.ThrowIfNull(listener);

            this.listener = listener;
        }

        /// <summary>
        /// How deeply objects may nest before the walk gives up. A KeyValues2 document is a graph
        /// and can contain reference cycles, so an object graph reaching this walk is not
        /// guaranteed to be finite, and a stack overflow cannot be caught.
        /// </summary>
        const int MaxDepth = 256;

        readonly IVisitationListener listener;

        public void Visit(string? name, KVObject @object)
        {
            VisitObject(name, @object, false, depth: 0);
        }

        void VisitObject(string? name, KVObject obj, bool isArray, int depth)
        {
            // Every listener is a tree format, so a KeyValues2-only value can never be written.
            // Rejecting here rather than in each listener keeps the rule in one place.
            obj.ValueType.ThrowIfKeyValues2Only();

            if (depth > MaxDepth)
            {
                throw new KeyValueException(
                    $"Object nesting went deeper than {MaxDepth} levels, which usually means the data contains a reference cycle.");
            }

            switch (obj.ValueType)
            {
                case KVValueType.Collection:
                    listener.OnObjectStart(name, obj.Flag);
                    foreach (var (childKey, child) in obj)
                    {
                        VisitObject(childKey, child, false, depth + 1);
                    }
                    listener.OnObjectEnd();
                    break;

                case KVValueType.Array:
                    var arrayList = obj.AsArraySpan();
                    var allSimple = true;
                    foreach (var element in arrayList)
                    {
                        if (!IsSimpleType(element.ValueType))
                        {
                            allSimple = false;
                            break;
                        }
                    }
                    listener.OnArrayStart(name, obj.Flag, arrayList.Length, allSimple);
                    foreach (var element in arrayList)
                    {
                        VisitObject(null, element, true, depth + 1);
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
                    throw new InvalidOperationException($"Unhandled value type: {obj.ValueType}.");
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
