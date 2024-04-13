namespace ValveKeyValue.Abstraction
{
    interface IVisitationListener : IDisposable
    {
        void OnObjectStart(string name, KVFlag flag);

        void OnObjectEnd();

        void OnKeyValuePair(string name, KVValue value);

        void OnArrayStart(string name, KVFlag flag);

        void OnArrayValue(KVValue value);

        void OnArrayEnd();
    }
}
