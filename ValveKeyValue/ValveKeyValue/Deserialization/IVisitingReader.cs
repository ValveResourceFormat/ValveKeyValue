using System;

namespace ValveKeyValue.Deserialization
{
    interface IVisitingReader : IDisposable
    {
        void ReadObject();
    }
}
