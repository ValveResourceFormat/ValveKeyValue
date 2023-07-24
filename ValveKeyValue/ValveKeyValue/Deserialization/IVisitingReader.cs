namespace ValveKeyValue.Deserialization
{
    interface IVisitingReader : IDisposable
    {
        KVHeader ReadHeader();
    }
}
