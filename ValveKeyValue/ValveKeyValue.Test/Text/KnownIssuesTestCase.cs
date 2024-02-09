namespace ValveKeyValue.Test
{
    class KnownIssuesTestCase
    {
        [Test]
        public void CanDeserializeValveResourceFormatSettings()
        {
            VKVConfig config;

            using (var stream = TestDataHelper.OpenResource("Text.vrf_settings_sample.vdf"))
            {
                config = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize<VKVConfig>(stream);
            }

            Assert.That(config, Is.Not.Null);
            Assert.Multiple(() =>
            {
                Assert.That(config.BackgroundColor, Is.EqualTo("#3C3C3C"));
                Assert.That(config.OpenDirectory, Is.EqualTo(@"D:\SteamLibrary\steamapps\common\The Lab\RobotRepair\vr"));
                Assert.That(config.SaveDirectory, Is.EqualTo(@"D:\SteamLibrary\steamapps\common\The Lab\RobotRepair\vr"));
                Assert.That(config.GameSearchPaths, Is.Not.Null & Has.Count.Zero);
            });
        }

        class VKVConfig
        {
            public List<string> GameSearchPaths { get; set; }
            public string BackgroundColor { get; set; }
            public string OpenDirectory { get; set; }
            public string SaveDirectory { get; set; }
        }
    }
}