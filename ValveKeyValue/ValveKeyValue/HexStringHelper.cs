using System.Globalization;
using System.Runtime.CompilerServices;

namespace ValveKeyValue
{
    internal static class HexStringHelper
    {
        public static byte[] ParseHexStringAsByteArray(string hexadecimalRepresentation)
        {
            Require.NotNull(hexadecimalRepresentation, nameof(hexadecimalRepresentation));

            var data = new byte[hexadecimalRepresentation.Length / 2];
            for (var i = 0; i < data.Length; i++)
            {
                var currentByteText = hexadecimalRepresentation.Substring(i * 2, 2);
                data[i] = byte.Parse(currentByteText, NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            }

            return data;
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
