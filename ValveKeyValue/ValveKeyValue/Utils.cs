using System;
using System.Globalization;

namespace ValveKeyValue
{
    internal class Utils
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

            if (BitConverter.IsLittleEndian)
            {
                Array.Reverse(data);
            }

            return data;
        }
    }
}
