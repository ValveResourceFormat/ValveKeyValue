namespace ValveKeyValue.Test
{
    class NestedListsDeserializationTestCase
    {
        [Test]
        public void CanDeserializeNestedLists()
        {
            DataObject dataObject;

            using (var rs = TestDataHelper.OpenResource("Text.list_of_lists.vdf"))
            {
                dataObject = KVSerializer.Create(KVSerializationFormat.KeyValues1Text).Deserialize<DataObject>(rs);
            }

            Assert.That(dataObject, Is.Not.Null);
            Assert.That(dataObject.Values, Is.Not.Null);
            Assert.That(dataObject.Values, Has.Count.EqualTo(2));

            Assert.That(dataObject.Values[0], Is.Not.Null);
            Assert.That(dataObject.Values[0], Has.Count.EqualTo(2));
            Assert.That(dataObject.Values[0][0], Is.EqualTo("first"));
            Assert.That(dataObject.Values[0][1], Is.EqualTo("second"));

            Assert.That(dataObject.Values[1], Is.Not.Null);
            Assert.That(dataObject.Values[1], Has.Count.EqualTo(2));
            Assert.That(dataObject.Values[1][0], Is.EqualTo("third"));
            Assert.That(dataObject.Values[1][1], Is.EqualTo("fourth"));
        }

        class DataObject
        {
            public List<List<string>> Values { get; set; }
        }
    }
}
