namespace ValveKeyValue.Abstraction
{
    interface IVisitationListener : IDisposable
    {
        void OnObjectStart(string name);

        void OnObjectEnd();

        void OnKeyValuePair(string name, KVValue value);

        void OnArrayStart(string name);

        void OnArrayValue(KVValue value);

        void OnArrayEnd();
    }
}
