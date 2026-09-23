using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace ValveKeyValue
{
    internal static class HexStringHelper
    {
        public static bool TryParseHexStringAsByteArray(string hexadecimalRepresentation, [NotNullWhen(true)] out byte[]? data)
        {
            ArgumentNullException.ThrowIfNull(hexadecimalRepresentation);

            data = null;

            if (hexadecimalRepresentation.Length % 2 != 0)
            {
                return false;
            }

            var result = new byte[hexadecimalRepresentation.Length / 2];
            for (var i = 0; i < result.Length; i++)
            {
                if (!byte.TryParse(hexadecimalRepresentation.AsSpan(i * 2, 2), NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out result[i]))
                {
                    return false;
                }
            }

            data = result;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static char HexToCharUpper(int value)
        {
            value &= 0xF;
            value += '0';

            if (value > '9')
            {
                value += ('A' - ('9' + 1));
            }

            return (char)value;
        }
    }
}
