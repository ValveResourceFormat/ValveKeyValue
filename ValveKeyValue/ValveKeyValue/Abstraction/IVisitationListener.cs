namespace ValveKeyValue.Abstraction
{
    interface IVisitationListener
    {
        void OnObjectStart(string name);

        void OnObjectEnd();

        void OnKeyValuePair(string name, KVValue value);
    }
}
