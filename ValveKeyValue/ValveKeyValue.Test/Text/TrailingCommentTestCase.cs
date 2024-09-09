namespace ValveKeyValue.Test.Text
{
    class TrailingCommentTestCase
    {
        [Test]
        public void CanReadTrailingEmptyComment()
        {
            var data = """
            "vertexlitgeneric" { 	"$basetexture" "models/props_oil/doors/oil_door" 	"$surfaceprop" "metal"	"%keywords" "tf"	} 

            /
            """;
            var kv = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(data);

            Assert.That(kv.Name, Is.EqualTo("vertexlitgeneric"));
            Assert.That(((string)kv["$basetexture"]), Is.EqualTo("models/props_oil/doors/oil_door"));
            Assert.That(((string)kv["$surfaceprop"]), Is.EqualTo("metal"));
            Assert.That(((string)kv["%keywords"]), Is.EqualTo("tf"));
        }

        [Test]
        public void CanReadTrailingCommentWithText()
        {
            var data = """
            "vertexlitgeneric" { 	"$basetexture" "models/props_oil/doors/oil_door" 	"$surfaceprop" "metal"	"%keywords" "tf"	} 

            // foo
            """;
            var kv = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize(data);

            Assert.That(kv.Name, Is.EqualTo("vertexlitgeneric"));
            Assert.That(((string)kv["$basetexture"]), Is.EqualTo("models/props_oil/doors/oil_door"));
            Assert.That(((string)kv["$surfaceprop"]), Is.EqualTo("metal"));
            Assert.That(((string)kv["%keywords"]), Is.EqualTo("tf"));
        }
    }
}
