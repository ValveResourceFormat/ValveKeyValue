using System.Runtime.InteropServices;

namespace ValveKeyValue
{
    /// <summary>
    /// Options to use when deserializing a KeyValues file.
    /// </summary>
    public sealed class KVSerializerOptions
    {
        /// <summary>
        /// Gets or sets a list of conditions to use to match conditional values.
        /// </summary>
        public IList<string> Conditions { get; } = new List<string>(GetDefaultConditions());

        /// <summary>
        /// Gets or sets a value indicating whether the parser should translate escape sequences (e.g. <c>\n</c>, <c>\t</c>).
        /// </summary>
        public bool HasEscapeSequences { get; set; }

        /// <summary>
        /// Gets or sets a value indicating whether invalid escape sequences should truncate strings rather than throwing a <see cref="InvalidDataException"/>.
        /// </summary>
        public bool EnableValveNullByteBugBehavior { get; set; }

        /// <summary>
        /// Gets or sets a way to open any file referenced with <c>#include</c> or <c>#base</c>.
        /// </summary>
        public IIncludedFileLoader FileLoader { get; set; }


        /// <summary>
        /// Gets or sets the string table used for smaller binary serialization.
        /// </summary>
        public StringTable StringTable { get; set; }

        /// <summary>
        /// Gets the default options (used when none are specified).
        /// </summary>
        public static KVSerializerOptions DefaultOptions => new();

        static IEnumerable<string> GetDefaultConditions()
        {
            // TODO: In the future we will want to skip this for consoles and mobile devices?
            yield return "WIN32";

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                yield return "WINDOWS";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                yield return "LINUX";
                yield return "POSIX";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                yield return "OSX";
                yield return "POSIX";
            }
        }
    }
}
