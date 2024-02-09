namespace ValveKeyValue.Test
{
    class NestedObjectGraphTestCase
    {
        [Test]
        public void IsNotNull()
        {
            Assert.That(data, Is.Not.Null);
        }

        [Test]
        public void OGInt()
        {
            Assert.That(data.OGInt, Is.EqualTo(3));
        }

        [Test]
        public void FooObj()
        {
            Assert.That(data.FooObj, Is.Not.Null);
        }

        [Test]
        public void FooStr()
        {
            Assert.That(data.FooObj?.FooStr, Is.EqualTo("blah"));
        }

        [Test]
        public void BarObj()
        {
            Assert.That(data.FooObj?.BarObj, Is.Not.Null);
        }

        [Test]
        public void Baz()
        {
            Assert.That(data.FooObj?.BarObj?.Baz, Is.EqualTo("blahdiladila"));
        }

        ObjectGraph data;

        [OneTimeSetUp]
        public void SetUp()
        {
            using var stream = TestDataHelper.OpenResource("Text.nested_object_graph.vdf");
            data = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize<ObjectGraph>(stream);
        }

        class ObjectGraph
        {
            public int OGInt { get; set; }

            public Foo FooObj { get; set; }

            public class Foo
            {
                public string FooStr { get; set; }

                public Bar BarObj { get; set; }

                public class Bar
                {
                    public string Baz { get; set; }
                }
            }
        }
    }
}
