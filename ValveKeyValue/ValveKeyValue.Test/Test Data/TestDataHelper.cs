using System.Reflection;
using System.Text;

namespace ValveKeyValue.Test
{
    static class TestDataHelper
    {
        public static Stream OpenResource(string name)
        {
            var resourceName = "ValveKeyValue.Test.Test_Data." + name;
            var stream = typeof(TestDataHelper).GetTypeInfo().Assembly.GetManifestResourceStream(resourceName) ?? throw new FileNotFoundException("Embedded Resource not found.", resourceName);
            return stream;
        }

        public static string ReadTextResource(string name)
        {
            var builder = new StringBuilder();

            using (var stream = OpenResource(name))
            using (var reader = new StreamReader(stream))
            {
                string? line;
                while ((line = reader.ReadLine()) != null)
                {
                    builder.Append(line);
                    builder.Append('\n');
                }
            }

            return builder.ToString();
        }

        public const string KV3Header = "<!-- kv3 encoding:text:version{e21c7f3c-8a33-41c5-9977-a76d3a32aa0d} format:generic:version{7412167c-06e9-4698-aff2-e63eb59037e7} -->\n";

        public static KVDocument ParseKV3Text(string body)
            => KVSerializer.Create(KVSerializationFormat.KeyValues3Text).Deserialize(KV3Header + body);
    }
}
