using System.Globalization;
using System.Runtime.CompilerServices;

namespace ValveKeyValue
{
    internal static class HexStringHelper
    {
        public static byte[] ParseHexStringAsByteArray(string hexadecimalRepresentation)
        {
            ArgumentNullException.ThrowIfNull(hexadecimalRepresentation);

            if (hexadecimalRepresentation.Length % 2 != 0)
            {
                throw new InvalidDataException($"Hex string has odd length ({hexadecimalRepresentation.Length}), expected even number of hex characters.");
            }

            var data = new byte[hexadecimalRepresentation.Length / 2];
            for (var i = 0; i < data.Length; i++)
            {
                data[i] = byte.Parse(hexadecimalRepresentation.AsSpan(i * 2, 2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            }

            return data;
        }

        public static string ByteArrayToHexString(byte[] data)
        {
            ArgumentNullException.ThrowIfNull(data);

            return Convert.ToHexStringLower(data);
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
