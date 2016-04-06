namespace ValveKeyValue
{
    /// <summary>
    /// Options to use when deserializing a KeyValues file.
    /// </summary>
    public sealed class KVSerializerOptions
    {
        string[] conditions;

        /// <summary>
        /// Gets or sets a list of conditions to use to match conditional values.
        /// </summary>
        public string[] Conditions
        {
            get { return conditions ?? new string[0]; }
            set { conditions = value; }
        }

        /// <summary>
        /// Gets or sets a way to open any file referenced with <c>#include</c> or <c>#base</c>.
        /// </summary>
        public IIncludedFileLoader FileLoader { get; set; }

        /// <summary>
        /// Gets the default options (used when none are specified).
        /// </summary>
        public static KVSerializerOptions DefaultOptions => new KVSerializerOptions();
    }
}
