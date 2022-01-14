using System.Globalization;
using NUnit.Framework;
using ValveKeyValue.Test.Helpers;

namespace ValveKeyValue.Test
{
    class SteamDataTestCase
    {
        [Test]
        public void TeamFortressCleanupCommandsAreBugForBugCompatibleWithValveTier1()
        {
            KVObject data;
            using (var stream = TestDataHelper.OpenResource($"Text.steam_440.vdf"))
            {
                data = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(stream);
            }

            Assert.That(
                (string)data["config"]["cleanupcmds"],
                Is.EqualTo(@"if not exist hl2.exe goto EOF;ren \"));

            Assert.That(
                (string)data["config"][@"..\..\common\Team"],
                Is.EqualTo("Fortress"));

            Assert.That(
                (string)data["config"][@"2\"],
                Is.EqualTo(@" TF2_bak;del /q /s bin\*;cd tf;del /q /s *.cache media\*.mov;mkdir download;cd download;move ..\maps .\;move ..\materials .\;move ..\models .\;move ..\particles .\;move ..\resource .\;move ..\sound .\;move ..\scripts .\;cd resource;del game.ico tf.ttf tf2.ttf tf2build.ttf tf2professor.ttf tf2secondary.ttf tfd.ttf;cd ..\sound\ui;del gamestartup*.mp3 tv_tune*.mp3 holiday\gamestartup_*.mp3;:EOF"));
        }
        
        [TestCase(1)]
        [TestCase(4096)]
        public void CanHandleBlockingStreams(int maxReadAtOnce)
        {
            using var blockingStream = new BlockingStream(maxReadAtOnce);

            KVObject data = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(blockingStream);
            
            Assert.That(data["test"].ToInt32(CultureInfo.InvariantCulture), Is.EqualTo(1337));
        }
    }
}
