using System.Globalization;
using ValveKeyValue.KeyValues3;

namespace ValveKeyValue.KeyValues2
{
    /// <summary>
    /// Parsing and formatting of the DMX header line, shared by the text and binary codecs.
    /// </summary>
    /// <remarks>
    /// Valve parses the header with <c>ParseToken("&lt;!-- dmx", "--&gt;")</c> followed by
    /// <c>sscanf("encoding %s %d format %s %d")</c>, so the opening tag is matched case
    /// insensitively and any run of whitespace separates tokens. Names are limited to 63
    /// characters. The legacy <c>&lt;!-- DMXVersion name_vN --&gt;</c> form is encoding version 0,
    /// which has the same layout as version 1.
    /// </remarks>
    internal static class KV2Header
    {
        /// <summary>Maximum length Valve's <c>DmxHeader_t</c> allows for an encoding or format name.</summary>
        public const int MaxNameLength = 63;

        /// <summary>Valve sniffs the header out of the first 168 bytes of a file.</summary>
        public const int MaxHeaderLength = 168;

        const string OpeningTag = "<!--";
        const string ClosingTag = "-->";

        public static KVHeader Parse(string line)
        {
            var span = line.AsSpan().Trim();

            if (!span.StartsWith(OpeningTag, StringComparison.Ordinal))
            {
                throw new KeyValueException("Invalid DMX header: expected it to start with '<!--'.");
            }

            if (!span.EndsWith(ClosingTag, StringComparison.Ordinal))
            {
                throw new KeyValueException("Invalid DMX header: expected it to end with '-->'.");
            }

            var inner = span[OpeningTag.Length..^ClosingTag.Length];
            var tokens = Tokenize(inner);

            if (tokens.Count > 0 && tokens[0].Equals("DMXVersion", StringComparison.OrdinalIgnoreCase))
            {
                if (tokens.Count != 2)
                {
                    throw new KeyValueException("Invalid DMX header: expected '<!-- DMXVersion <name>_v<version> -->'.");
                }

                return ParseLegacy(tokens[1]);
            }

            // <!-- dmx encoding <name> <version> format <name> <version> -->
            if (tokens.Count != 7
                || !tokens[0].Equals("dmx", StringComparison.OrdinalIgnoreCase)
                || !tokens[1].Equals("encoding", StringComparison.OrdinalIgnoreCase)
                || !tokens[4].Equals("format", StringComparison.OrdinalIgnoreCase))
            {
                throw new KeyValueException("Invalid DMX header: expected '<!-- dmx encoding <name> <version> format <name> <version> -->'.");
            }

            return new KVHeader
            {
                Encoding = new KV3ID(tokens[2], Version: ParseVersion(tokens[3], "encoding")),
                Format = new KV3ID(tokens[5], Version: ParseVersion(tokens[6], "format")),
            };
        }

        static KVHeader ParseLegacy(string name)
        {
            // "binary_v2", "sfm_v1", "keyvalues2_v1", "keyvalues2_flat_v1"
            var separator = name.LastIndexOf("_v", StringComparison.OrdinalIgnoreCase);

            if (separator < 0)
            {
                throw new KeyValueException($"Invalid legacy DMX header: '{name}' does not end in '_v<version>'.");
            }

            var baseName = name[..separator];
            var version = ParseVersion(name[(separator + 2)..], "format");

            var encodingName = baseName switch
            {
                "keyvalues2" or "keyvalues2_flat" => baseName,
                _ => "binary",
            };

            var formatName = baseName is "binary" or "keyvalues2" or "keyvalues2_flat" ? "dmx" : baseName;

            return new KVHeader
            {
                // Valve forces encoding version 0 for legacy headers; the layout matches version 1.
                Encoding = new KV3ID(encodingName, Version: 0),
                Format = new KV3ID(formatName, Version: version),
            };
        }

        static int ParseVersion(string value, string what)
        {
            if (!int.TryParse(value, NumberStyles.Integer, CultureInfo.InvariantCulture, out var version))
            {
                throw new KeyValueException($"Invalid DMX header: {what} version '{value}' is not a number.");
            }

            return version;
        }

        static List<string> Tokenize(ReadOnlySpan<char> span)
        {
            var tokens = new List<string>(6);
            var start = -1;

            for (var i = 0; i <= span.Length; i++)
            {
                var isWhitespace = i == span.Length || IsSpace(span[i]);

                if (isWhitespace)
                {
                    if (start >= 0)
                    {
                        tokens.Add(span[start..i].ToString());
                        start = -1;
                    }
                }
                else if (start < 0)
                {
                    start = i;
                }
            }

            return tokens;
        }

        /// <summary>
        /// Matches Valve's <c>V_isspace</c>, which is narrower than <see cref="char.IsWhiteSpace(char)"/>.
        /// </summary>
        public static bool IsSpace(char c) => c is ' ' or '\t' or '\n' or '\v' or '\f' or '\r';

        /// <summary>
        /// Checks that the header names an encoding the calling codec can read. Valve requires the
        /// encoding name to equal the serializer's own name, so a document belonging to the other
        /// encoding is a mismatch rather than something to attempt.
        /// </summary>
        /// <param name="header">The parsed header.</param>
        /// <param name="expected">The encoding names this codec handles.</param>
        public static void ValidateEncoding(KVHeader header, params string[] expected)
        {
            foreach (var name in expected)
            {
                if (string.Equals(header.Encoding.Name, name, StringComparison.Ordinal))
                {
                    return;
                }
            }

            throw new KeyValueException(
                $"DMX encoding '{header.Encoding.Name}' cannot be read by this codec, expected {string.Join(" or ", expected)}.");
        }

        public static string Format(KVHeader header)
        {
            ValidateName(header.Encoding.Name, "encoding");
            ValidateName(header.Format.Name, "format");

            return string.Create(
                CultureInfo.InvariantCulture,
                $"<!-- dmx encoding {header.Encoding.Name} {header.Encoding.Version} format {header.Format.Name} {header.Format.Version} -->");
        }

        static void ValidateName(string? name, string what)
        {
            if (string.IsNullOrEmpty(name))
            {
                throw new KeyValueException($"DMX header {what} name must not be empty.");
            }

            if (name.Length > MaxNameLength)
            {
                throw new KeyValueException($"DMX header {what} name '{name}' is longer than {MaxNameLength} characters.");
            }

            foreach (var c in name)
            {
                if (IsSpace(c))
                {
                    throw new KeyValueException($"DMX header {what} name '{name}' must not contain whitespace.");
                }
            }
        }
    }
}
