using System;

namespace ValveKeyValue
{
    public class KVFile : KVObject
    {
        public KVHeader Header { get; }

        public KVFile(KVHeader header, string name, KVValue value) : base(name, value)
        {
            Header = header;
        }
    }
}
