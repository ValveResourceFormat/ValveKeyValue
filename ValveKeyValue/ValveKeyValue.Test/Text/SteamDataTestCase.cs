using NUnit.Framework;

namespace ValveKeyValue.Test
{
    class SteamDataTestCase
    {
        [Test]
        public void CanReadTeamFortressCleanupCommands()
        {
            KVObject data;
            using (var stream = TestDataHelper.OpenResource($"Text.steam_440.vdf"))
            {
                data = KVSerializer.Deserialize(stream);
            }

            var cleanupcmds = (string)data["config"]["cleanupcmds"];
            var expected = @"if not exist hl2.exe goto EOF;ren ""..\..\common\Team Fortress 2"" TF2_bak;del /q /s bin\*;cd tf;del /q /s *.cache media\*.mov;mkdir download;cd download;move ..\maps .\;move ..\materials .\;move ..\models .\;move ..\particles .\;move ..\resource .\;move ..\sound .\;move ..\scripts .\;cd resource;del game.ico tf.ttf tf2.ttf tf2build.ttf tf2professor.ttf tf2secondary.ttf tfd.ttf;cd ..\sound\ui;del gamestartup*.mp3 tv_tune*.mp3 holiday\gamestartup_*.mp3;:EOF";
            Assert.That(cleanupcmds, Is.EqualTo(expected));
        }
    }
}
