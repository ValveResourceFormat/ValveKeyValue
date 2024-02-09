namespace ValveKeyValue.Test
{
    class KVObjectTestCase
    {
        [Test]
        public void IndexerSetterWorks()
        {
            var obj = new KVObject("root", Array.Empty<KVObject>());

            Assert.That(obj["this_is_set_in_test"], Is.Null);
            obj["this_is_set_in_test"] = "some cool data";
            Assert.That((string)obj["this_is_set_in_test"], Is.EqualTo("some cool data"));
        }

        [Test]
        public void AddCallWorks()
        {
            var obj = new KVObject("root", Array.Empty<KVObject>());

            Assert.That(obj["this_is_set_in_test"], Is.Null);
            obj.Add(new KVObject("this_is_set_in_test", "some cool data"));
            Assert.That((string)obj["this_is_set_in_test"], Is.EqualTo("some cool data"));
        }
    }
}
