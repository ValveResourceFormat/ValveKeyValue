using System.IO;

namespace ValveKeyValue.Test
{
    static class TestDataHelper
    {
        public static Stream OpenResource(string name)
        {
            var resourceName = "ValveKeyValue.Test.Test_Data." + name;
            var stream = typeof(TestDataHelper).Assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                throw new FileNotFoundException("Embedded Resource not found.", resourceName);
            }

            return stream;
        }

        public static string ReadTextResource(string name)
        {
            using (var stream = OpenResource(name))
            using (var reader = new StreamReader(stream))
            {
                return reader.ReadToEnd();
            }
        }
    }
}
