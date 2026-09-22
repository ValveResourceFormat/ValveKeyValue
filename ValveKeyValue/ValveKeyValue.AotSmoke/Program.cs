using System.Globalization;
using System.Text;
using ValveKeyValue.Metadata;

namespace ValveKeyValue.AotSmoke
{
    // Maps typed objects through calls that the source generator intercepts. When published with Native AOT this
    // proves that the generated type information works without reflection; the exit code is non-zero on a mismatch.
    static class Program
    {
        const string SettingsText = """
            "settings"
            {
                "Name"      "smoke"
                "Theme"     "2"
                "Volume"    "75"
                "Recent"
                {
                    "0"     "first.vpk"
                    "1"     "second.vpk"
                }
                "Windows"
                {
                    "main"
                    {
                        "Width"     "1280"
                        "Height"    "720"
                    }
                    "tools"
                    {
                        "Width"     "400"
                        "Height"    "300"
                    }
                }
            }
            """;

        static readonly KVSerializer KV1 = KVSerializer.Create(KVSerializationFormat.KeyValues1Text);
        static readonly KVSerializer KV3 = KVSerializer.Create(KVSerializationFormat.KeyValues3Text);

        static int failures;

        static int Main()
        {
            DeserializesAndSerializesObjectGraph();
            RoundTripsPositionalRecord();
            PassesTypeInformationToGenericCode();

            Console.WriteLine(failures == 0 ? "All checks passed." : $"{failures} check(s) failed.");
            return failures == 0 ? 0 : 1;
        }

        static void DeserializesAndSerializesObjectGraph()
        {
            Settings settings;

            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(SettingsText)))
            {
                settings = KV1.Deserialize<Settings>(stream);
            }

            Check("name", settings.Name, "smoke");
            Check("theme", settings.Theme, Theme.Dark);
            Check("volume", settings.Volume, 75);
            Check("recent", string.Join(",", settings.Recent ?? []), "first.vpk,second.vpk");
            Check("main window", settings.Windows?["main"], new Window(1280, 720));
            Check("tools window", settings.Windows?["tools"], new Window(400, 300));

            var changed = new Settings
            {
                Name = settings.Name,
                Theme = settings.Theme,
                Recent = settings.Recent,
                Windows = settings.Windows,
            };

            string text;

            using (var stream = new MemoryStream())
            {
                KV1.Serialize(stream, changed, "settings");
                text = Encoding.UTF8.GetString(stream.ToArray());
            }

            Console.WriteLine(text);

            Settings roundTripped;

            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(text)))
            {
                roundTripped = KV1.Deserialize<Settings>(stream);
            }

            Check("round trip", roundTripped.Windows?["main"], new Window(1280, 720));
            Check("round trip theme", roundTripped.Theme, Theme.Dark);
            Check("round trip volume", roundTripped.Volume, null);
        }

        static void RoundTripsPositionalRecord()
        {
            var point = new Point(3, -4);
            var (text, spans) = KV3.SerializeWithSourceMap(point, "point");

            Console.WriteLine(text);

            Point back;

            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(text)))
            {
                back = KV3.Deserialize<Point>(stream);
            }

            Check("record", back, point);
            Check("source map", spans.Count > 0, true);
        }

        static void PassesTypeInformationToGenericCode()
        {
            var windows = Load(KV1, "\"windows\"\n{\n\t\"a\"\n\t{\n\t\t\"Width\"\t\"1\"\n\t}\n}", KVSerializer.GetTypeInfo<Dictionary<string, Window>>());

            Check("generic helper", windows["a"], new Window(1, 0));
        }

        // Generic code cannot be intercepted; it receives type information from a call site with a concrete type.
        static T Load<T>(KVSerializer serializer, string text, KVTypeInfo<T> typeInfo)
        {
            using var stream = new MemoryStream(Encoding.UTF8.GetBytes(text));
            return serializer.Deserialize(stream, typeInfo);
        }

        static void Check<T>(string name, T actual, T expected)
        {
            if (EqualityComparer<T>.Default.Equals(actual, expected))
            {
                return;
            }

            failures++;
            Console.WriteLine(string.Create(CultureInfo.InvariantCulture, $"FAIL {name}: expected '{expected}', got '{actual}'"));
        }
    }

    enum Theme
    {
        Light = 1,
        Dark = 2,
    }

    sealed class Settings
    {
        public string? Name { get; set; }

        public Theme Theme { get; set; }

        public int? Volume { get; set; }

        public List<string>? Recent { get; set; }

        public Dictionary<string, Window>? Windows { get; set; }
    }

    sealed record Window(int Width, int Height);

    sealed record Point(int X, int Y, int Z = 7);
}
