namespace ValveKeyValue
{
    public class KVDocument : KVObject
    {
        public KVDocument(string name, KVValue value) : base(name, value)
        {
            // KV3 will require a header field that contains format/encoding here.
        }
    }
}
