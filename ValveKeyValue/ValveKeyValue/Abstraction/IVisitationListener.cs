namespace ValveKeyValue.Abstraction
{
    interface IVisitationListener : IDisposable
    {
        void OnObjectStart(string? name, KVFlag flag);

        void OnObjectEnd();

        void OnKeyValuePair(string name, KVObject value);

        void OnArrayStart(string? name, KVFlag flag, int elementCount, bool allSimpleElements);

        void OnArrayValue(KVObject value);

        void OnArrayEnd();
    }
}
