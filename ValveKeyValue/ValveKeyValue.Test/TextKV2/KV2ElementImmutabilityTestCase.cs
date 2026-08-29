namespace ValveKeyValue.Test.TextKV2
{
    /// <summary>
    /// <see cref="KV2Element.Null"/> is a single instance shared by every null reference in every
    /// document, and stubs carry nothing but an identifier. Both have to reject mutation, or one
    /// caller could corrupt unrelated documents.
    /// </summary>
    class KV2ElementImmutabilityTestCase
    {
        static IEnumerable<KV2Element> Immutable()
        {
            yield return KV2Element.Null;
            yield return KV2Element.Stub(Guid.NewGuid());
        }

        [Test]
        public void RejectsPropertyChanges([ValueSource(nameof(Immutable))] KV2Element element)
        {
            Assert.Multiple(() =>
            {
                Assert.That(() => element.ClassName = "x", Throws.InvalidOperationException);
                Assert.That(() => element.Name = "x", Throws.InvalidOperationException);
                Assert.That(() => element.ElementId = Guid.NewGuid(), Throws.InvalidOperationException);
            });
        }

        [Test]
        public void RejectsAttributeChanges([ValueSource(nameof(Immutable))] KV2Element element)
        {
            Assert.Multiple(() =>
            {
                Assert.That(() => element.Add("x", new KVObject(1)), Throws.InvalidOperationException);
                Assert.That(() => element["x"] = new KVObject(1), Throws.InvalidOperationException);
                Assert.That(() => element.Remove("x"), Throws.InvalidOperationException);
                Assert.That(() => element.Clear(), Throws.InvalidOperationException);
            });
        }

        [Test]
        public void HasNoAttributes([ValueSource(nameof(Immutable))] KV2Element element)
        {
            Assert.Multiple(() =>
            {
                Assert.That(element.Count, Is.Zero);
                Assert.That(element.ContainsKey("anything"), Is.False);
                Assert.That(element.TryGetValue("anything", out _), Is.False);
            });
        }

        /// <summary>
        /// The sentinel survives being read out of a document and handed around.
        /// </summary>
        [Test]
        public void NullSentinelIsUnchangedAfterReadingADocument()
        {
            using var stream = TestDataHelper.OpenResource("TextKV2.basic.dmx");
            var root = KVSerializer.Create(KVSerializationFormat.KeyValues2Text).Deserialize(stream).Root;

            Assert.That(root["nullRef"], Is.SameAs(KV2Element.Null));

            Assert.Multiple(() =>
            {
                Assert.That(KV2Element.Null.ClassName, Is.Empty);
                Assert.That(KV2Element.Null.Name, Is.Empty);
                Assert.That(KV2Element.Null.ElementId, Is.EqualTo(Guid.Empty));
                Assert.That(KV2Element.Null.IsStub, Is.False);
            });
        }

        [Test]
        public void StubKeepsItsIdentifier()
        {
            var id = Guid.NewGuid();
            var stub = KV2Element.Stub(id);

            Assert.Multiple(() =>
            {
                Assert.That(stub.ElementId, Is.EqualTo(id));
                Assert.That(stub.IsStub, Is.True);
            });
        }
    }
}
