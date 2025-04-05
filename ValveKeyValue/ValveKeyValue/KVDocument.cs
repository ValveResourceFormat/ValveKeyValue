namespace ValveKeyValue
{
    public class KVDocument : KVObject
    {
        public KVHeader Header { get; }

        public KVDocument(KVHeader header, string name, KVValue value) : base(name, value)
        {
            Header = header;
        }
    }
}
