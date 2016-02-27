namespace ValveKeyValue
{
    class KVStringValue : KVValue
    {
        public KVStringValue(string value)
        {
            Require.NotNull(value, nameof(value));
            this.value = value;
        }

        readonly string value;

        internal override bool TryConvert(out string value)
        {
            value = this.value;
            return true;
        }

        internal override bool TryConvert(out bool value)
        {
            int intVal;
            if (!int.TryParse(this.value, out intVal))
            {
                value = default(bool);
                return false;
            }

            value = intVal == 1;
            return true;
        }
    }
}
