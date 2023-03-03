using System;

namespace ValveKeyValue
{
    public class KVFile
    {
        public Guid Encoding { get; set; }
        public Guid Format { get; set; }
        public KVObject Root { get; set; }
    }
}
