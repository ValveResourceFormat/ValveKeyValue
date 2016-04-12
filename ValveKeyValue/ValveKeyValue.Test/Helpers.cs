using System.IO;
using System.Text;

namespace ValveKeyValue.Test
{
    class Helpers
    {
        public static string NormalizeLineEndings(string text)
        {
            var builder = new StringBuilder(text.Length);
            using (var reader = new StringReader(text))
            {
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    builder.Append(line);
                    builder.Append("\n");
                }
            }

            return builder.ToString();
        }
    }
}
