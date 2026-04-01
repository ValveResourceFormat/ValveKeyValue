namespace ValveKeyValue.Abstraction
{
    interface IVisitationListener : IDisposable
    {
        bool OnObjectStart(string name, KVFlag flag, KVObject obj);

        void OnObjectEnd();

        void OnKeyValuePair(string name, KVObject value);

        void OnArrayStart(string name, KVFlag flag, int elementCount, bool allSimpleElements);

        void OnArrayValue(KVObject value);

        void OnArrayEnd();
    }
}
