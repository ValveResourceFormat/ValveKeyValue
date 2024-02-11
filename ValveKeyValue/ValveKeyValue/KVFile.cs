namespace ValveKeyValue
{
    public class KVFile : KVObject
    {
        public KVFile(string name, KVValue value) : base(name, value)
        {
            // KV3 will require a header field that contains format/encoding here.
        }
    }
}
