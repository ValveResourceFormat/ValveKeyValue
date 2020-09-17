using System.Collections.Generic;
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
        public IList<string> Conditions { get; set; } = new List<string>();

        /// <summary>
        /// Gets or sets a value indicating whether gets or sets if the parser should translate escape sequences (e.g. <c>\n</c>, <c>\t</c>).
        /// </summary>
        public bool HasEscapeSequences { get; set; }

        /// <summary>
        /// Gets or sets a way to open any file referenced with <c>#include</c> or <c>#base</c>.
        /// </summary>
        public IIncludedFileLoader FileLoader { get; set; }

        public KVSerializerOptions()
        {
            // TODO: In the future we will want to skip this for consoles and mobile devices?
            Conditions.Add("WIN32");
            
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                Conditions.Add("WINDOWS");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                Conditions.Add("LINUX");
                Conditions.Add("POSIX");
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                Conditions.Add("OSX");
                Conditions.Add("POSIX");
            }
        }
        
        /// <summary>
        /// Gets the default options (used when none are specified).
        /// </summary>
        public static KVSerializerOptions DefaultOptions => new KVSerializerOptions();
    }
}
