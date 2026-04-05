using System.IO;
using System.Text;

namespace ValveKeyValue.Test
{
    class KVObjectDeepNestingTestCase
    {
        #region Deep nested read

        [Test]
        public void ReadFourLevelsDeep()
        {
            var d = KVObject.ListCollection();
            d.Add("value", "found");

            var c = KVObject.ListCollection();
            c.Add("d", d);

            var b = KVObject.ListCollection();
            b.Add("c", c);

            var a = KVObject.ListCollection();
            a.Add("b", b);

            var root = KVObject.ListCollection();
            root.Add("a", a);

            Assert.That((string)root["a"]["b"]["c"]["d"]["value"], Is.EqualTo("found"));
        }

        [Test]
        public void ReadFourLevelsDeepWithDictCollection()
        {
            var d = KVObject.Collection();
            d.Add("value", "found");

            var c = KVObject.Collection();
            c.Add("d", d);

            var b = KVObject.Collection();
            b.Add("c", c);

            var a = KVObject.Collection();
            a.Add("b", b);

            var root = KVObject.Collection();
            root.Add("a", a);

            Assert.That((string)root["a"]["b"]["c"]["d"]["value"], Is.EqualTo("found"));
        }

        #endregion

        #region Deep nested write

        [Test]
        public void WriteFourLevelsDeepVisibleThroughParent()
        {
            var leaf = KVObject.ListCollection();
            leaf.Add("key", "original");

            var mid = KVObject.ListCollection();
            mid.Add("leaf", leaf);

            var root = KVObject.ListCollection();
            root.Add("mid", mid);

            var midLeaf = root["mid"]["leaf"];
            midLeaf["key"] = "modified";

            Assert.That((string)root["mid"]["leaf"]["key"], Is.EqualTo("modified"));
        }

        [Test]
        public void AddAtDepthVisibleThroughParent()
        {
            var leaf = KVObject.ListCollection();
            leaf.Add("existing", "yes");

            var mid = KVObject.ListCollection();
            mid.Add("leaf", leaf);

            var root = KVObject.ListCollection();
            root.Add("mid", mid);

            var midLeaf = root["mid"]["leaf"];
            midLeaf["new_key"] = 42;

            using (Assert.EnterMultipleScope())
            {
                Assert.That((int)root["mid"]["leaf"]["new_key"], Is.EqualTo(42));
                Assert.That((string)root["mid"]["leaf"]["existing"], Is.EqualTo("yes"));
                Assert.That(root["mid"]["leaf"], Has.Count.EqualTo(2));
            }
        }

        [Test]
        public void RemoveAtDepthVisibleThroughParent()
        {
            var leaf = KVObject.ListCollection();
            leaf.Add("keep", "yes");
            leaf.Add("remove", "bye");

            var mid = KVObject.ListCollection();
            mid.Add("leaf", leaf);

            var root = KVObject.ListCollection();
            root.Add("mid", mid);

            var removed = root["mid"]["leaf"].Remove("remove");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(removed, Is.True);
                Assert.That(root["mid"]["leaf"], Has.Count.EqualTo(1));
                Assert.That((string)root["mid"]["leaf"]["keep"], Is.EqualTo("yes"));
                Assert.That(root["mid"]["leaf"].ContainsKey("remove"), Is.False);
            }
        }

        [Test]
        public void ClearAtDepthVisibleThroughParent()
        {
            var leaf = KVObject.ListCollection();
            leaf.Add("a", "1");
            leaf.Add("b", "2");

            var root = KVObject.ListCollection();
            root.Add("leaf", leaf);

            root["leaf"].Clear();

            Assert.That(root["leaf"], Has.Count.EqualTo(0));
        }

        #endregion

        #region Mutation through copy shares underlying collections

        [Test]
        public void CopyOfCollectionSharesUnderlyingStorage()
        {
            var original = KVObject.ListCollection();
            original.Add("key", "value");

            var copy = original;

            copy.Add("new_key", "new_value");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(original, Has.Count.EqualTo(2));
                Assert.That((string)original["new_key"], Is.EqualTo("new_value"));
            }
        }

        [Test]
        public void DirectMutationOfNestedObjectVisibleThroughParent()
        {
            var inner = KVObject.ListCollection();
            inner.Add("x", "1");

            var outer = KVObject.ListCollection();
            outer.Add("inner", inner);

            // Mutate inner directly
            inner.Add("y", "2");

            // Should be visible through outer
            using (Assert.EnterMultipleScope())
            {
                Assert.That(outer["inner"], Has.Count.EqualTo(2));
                Assert.That((string)outer["inner"]["y"], Is.EqualTo("2"));
            }
        }

        [Test]
        public void ChainedMutationAndDirectMutationBothVisible()
        {
            var inner = KVObject.ListCollection();
            inner.Add("a", "1");

            var outer = KVObject.ListCollection();
            outer.Add("inner", inner);

            // Mutate through chain
            var outerInner = outer["inner"];
            outerInner["b"] = "2";

            // Mutate directly
            inner.Add("c", "3");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(outer["inner"], Has.Count.EqualTo(3));
                Assert.That((string)outer["inner"]["a"], Is.EqualTo("1"));
                Assert.That((string)outer["inner"]["b"], Is.EqualTo("2"));
                Assert.That((string)outer["inner"]["c"], Is.EqualTo("3"));
            }
        }

        #endregion

        #region Array within collection

        [Test]
        public void ArrayWithinCollectionAccessibleByChainedIndex()
        {
            var arr = KVObject.Array([
                "first",
                "second",
                "third",
            ]);

            var root = KVObject.ListCollection();
            root.Add("arr", arr);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(root["arr"].IsArray, Is.True);
                Assert.That((string)root["arr"][0], Is.EqualTo("first"));
                Assert.That((string)root["arr"][1], Is.EqualTo("second"));
                Assert.That((string)root["arr"][2], Is.EqualTo("third"));
            }
        }

        [Test]
        public void AddToArrayWithinCollectionVisibleThroughParent()
        {
            var arr = KVObject.Array([
                "a",
            ]);

            var root = KVObject.ListCollection();
            root.Add("arr", arr);

            root["arr"].Add("b");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(root["arr"], Has.Count.EqualTo(2));
                Assert.That((string)root["arr"][1], Is.EqualTo("b"));
            }
        }

        [Test]
        public void RemoveAtFromArrayWithinCollection()
        {
            var arr = KVObject.Array([
                "x",
                "y",
                "z",
            ]);

            var root = KVObject.ListCollection();
            root.Add("arr", arr);

            root["arr"].RemoveAt(1);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(root["arr"], Has.Count.EqualTo(2));
                Assert.That((string)root["arr"][0], Is.EqualTo("x"));
                Assert.That((string)root["arr"][1], Is.EqualTo("z"));
            }
        }

        #endregion

        #region Collection within array

        [Test]
        public void CollectionWithinArrayAccessibleByIndex()
        {
            var child1 = KVObject.ListCollection();
            child1.Add("name", "first");

            var child2 = KVObject.ListCollection();
            child2.Add("name", "second");

            var arr = KVObject.Array([child1, child2]);

            var root = KVObject.ListCollection();
            root.Add("items", arr);

            using (Assert.EnterMultipleScope())
            {
                Assert.That((string)root["items"][0]["name"], Is.EqualTo("first"));
                Assert.That((string)root["items"][1]["name"], Is.EqualTo("second"));
            }
        }

        [Test]
        public void MutateCollectionWithinArrayViaSpan()
        {
            var child = KVObject.ListCollection();
            child.Add("key", "original");

            var arr = KVObject.Array([child]);

            arr.AsArraySpan()[0].Add("extra", "added");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(arr[0], Has.Count.EqualTo(2));
                Assert.That((string)arr[0]["extra"], Is.EqualTo("added"));
            }
        }

        [Test]
        public void ReplaceArrayElementViaSpanPropagates()
        {
            var arr = KVObject.Array([
                "original",
                "keep",
            ]);

            arr.AsArraySpan()[0] = new KVObject("replaced");

            using (Assert.EnterMultipleScope())
            {
                Assert.That((string)arr[0], Is.EqualTo("replaced"));
                Assert.That((string)arr[1], Is.EqualTo("keep"));
            }
        }

        [Test]
        public void ReplaceArrayElementViaSpanVisibleThroughParent()
        {
            var arr = KVObject.Array([
                "a",
                "b",
            ]);

            var root = KVObject.ListCollection();
            root.Add("arr", arr);

            // Mutate through a copy — shares the same List<KVObject>
            var rootArr = root["arr"];
            rootArr.AsArraySpan()[0] = new KVObject("replaced");

            using (Assert.EnterMultipleScope())
            {
                Assert.That((string)root["arr"][0], Is.EqualTo("replaced"));
                Assert.That((string)root["arr"][1], Is.EqualTo("b"));
            }
        }

        [Test]
        public void IntegerIndexerReadsThroughStructCopy()
        {
            var child1 = KVObject.ListCollection();
            child1.Add("name", "first");

            var child2 = KVObject.ListCollection();
            child2.Add("name", "second");

            var arr = KVObject.Array([child1, child2]);

            var root = KVObject.ListCollection();
            root.Add("items", arr);

            // Chained read: root["items"][0] reads from nested collection
            using (Assert.EnterMultipleScope())
            {
                Assert.That((string)root["items"][0]["name"], Is.EqualTo("first"));
                Assert.That((string)root["items"][1]["name"], Is.EqualTo("second"));
                Assert.That(root["items"], Has.Count.EqualTo(2));
            }
        }

        [Test]
        public void AddToArrayThroughStructCopyPropagates()
        {
            var arr = KVObject.Array([
                "a",
            ]);

            var root = KVObject.ListCollection();
            root.Add("arr", arr);

            // Add through copy
            var rootArr = root["arr"];
            rootArr.Add("b");
            rootArr.Add("c");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(root["arr"], Has.Count.EqualTo(3));
                Assert.That((string)root["arr"][0], Is.EqualTo("a"));
                Assert.That((string)root["arr"][1], Is.EqualTo("b"));
                Assert.That((string)root["arr"][2], Is.EqualTo("c"));
            }
        }

        [Test]
        public void RemoveAtThroughStructCopyPropagates()
        {
            var arr = KVObject.Array([
                "x",
                "y",
                "z",
            ]);

            var root = KVObject.ListCollection();
            root.Add("arr", arr);

            var rootArr = root["arr"];
            rootArr.RemoveAt(1);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(root["arr"], Has.Count.EqualTo(2));
                Assert.That((string)root["arr"][0], Is.EqualTo("x"));
                Assert.That((string)root["arr"][1], Is.EqualTo("z"));
            }
        }

        #endregion

        #region Deep roundtrip (serialize → deserialize → verify)

        [Test]
        public void DeepNestedRoundTripKV1Text()
        {
            var leaf = KVObject.ListCollection();
            leaf.Add("deep_value", "found");
            leaf.Add("deep_int", 42);

            var mid = KVObject.ListCollection();
            mid.Add("leaf", leaf);
            mid.Add("sibling", "here");

            var root = KVObject.ListCollection();
            root.Add("mid", mid);
            root.Add("top_value", "top");

            var doc = new KVDocument(null, "root", root);

            // Serialize
            using var ms = new MemoryStream();
            var serializer = KVSerializer.Create(KVSerializationFormat.KeyValues1Text);
            serializer.Serialize(ms, doc);

            // Deserialize
            ms.Position = 0;
            var result = serializer.Deserialize(ms);

            using (Assert.EnterMultipleScope())
            {
                Assert.That((string)result.Root["top_value"], Is.EqualTo("top"));
                Assert.That((string)result.Root["mid"]["sibling"], Is.EqualTo("here"));
                Assert.That((string)result.Root["mid"]["leaf"]["deep_value"], Is.EqualTo("found"));
                Assert.That((int)result.Root["mid"]["leaf"]["deep_int"], Is.EqualTo(42));
            }
        }

        [Test]
        public void DeepNestedMutationThenRoundTripKV1Text()
        {
            var leaf = KVObject.ListCollection();
            leaf.Add("key", "original");

            var mid = KVObject.ListCollection();
            mid.Add("leaf", leaf);

            var root = KVObject.ListCollection();
            root.Add("mid", mid);

            // Mutate at depth before serialization
            var midLeafMut = root["mid"]["leaf"];
            midLeafMut["key"] = "modified";
            midLeafMut["new"] = "added";

            var doc = new KVDocument(null, "root", root);

            // Serialize
            using var ms = new MemoryStream();
            var serializer = KVSerializer.Create(KVSerializationFormat.KeyValues1Text);
            serializer.Serialize(ms, doc);

            // Deserialize
            ms.Position = 0;
            var result = serializer.Deserialize(ms);

            using (Assert.EnterMultipleScope())
            {
                Assert.That((string)result.Root["mid"]["leaf"]["key"], Is.EqualTo("modified"));
                Assert.That((string)result.Root["mid"]["leaf"]["new"], Is.EqualTo("added"));
            }
        }

        [Test]
        public void DeepNestedRoundTripKV3Text()
        {
            var leaf = KVObject.Collection();
            leaf.Add("deep_value", "found");
            leaf.Add("deep_int", 42);

            var mid = KVObject.Collection();
            mid.Add("leaf", leaf);
            mid.Add("sibling", "here");

            var root = KVObject.Collection();
            root.Add("mid", mid);
            root.Add("top_value", "top");

            var doc = new KVDocument(null, null, root);

            // Serialize
            using var ms = new MemoryStream();
            var serializer = KVSerializer.Create(KVSerializationFormat.KeyValues3Text);
            serializer.Serialize(ms, doc);

            // Deserialize
            ms.Position = 0;
            var result = serializer.Deserialize(ms);

            using (Assert.EnterMultipleScope())
            {
                Assert.That((string)result.Root["top_value"], Is.EqualTo("top"));
                Assert.That((string)result.Root["mid"]["sibling"], Is.EqualTo("here"));
                Assert.That((string)result.Root["mid"]["leaf"]["deep_value"], Is.EqualTo("found"));
                Assert.That((int)result.Root["mid"]["leaf"]["deep_int"], Is.EqualTo(42));
            }
        }

        #endregion

        #region Array roundtrip

        [Test]
        public void ArrayWithinCollectionRoundTripKV1()
        {
            // KV1 text doesn't have native arrays, but arrays serialize as indexed collections
            var arr = KVObject.Array([
                "alpha",
                "beta",
                "gamma",
            ]);

            var root = KVObject.ListCollection();
            root.Add("items", arr);

            var doc = new KVDocument(null, "root", root);

            using var ms = new MemoryStream();
            var serializer = KVSerializer.Create(KVSerializationFormat.KeyValues1Text);
            serializer.Serialize(ms, doc);

            ms.Position = 0;
            var result = serializer.Deserialize(ms);

            Assert.That(result.Root["items"], Has.Count.GreaterThanOrEqualTo(3));
        }

        [Test]
        public void ArrayWithinCollectionRoundTripKV3()
        {
            var arr = KVObject.Array([
                "alpha",
                "beta",
                "gamma",
            ]);

            var root = KVObject.Collection();
            root.Add("items", arr);

            var doc = new KVDocument(null, null, root);

            using var ms = new MemoryStream();
            var serializer = KVSerializer.Create(KVSerializationFormat.KeyValues3Text);
            serializer.Serialize(ms, doc);

            ms.Position = 0;
            var result = serializer.Deserialize(ms);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Root["items"].IsArray, Is.True);
                Assert.That(result.Root["items"], Has.Count.EqualTo(3));
                Assert.That((string)result.Root["items"][0], Is.EqualTo("alpha"));
                Assert.That((string)result.Root["items"][1], Is.EqualTo("beta"));
                Assert.That((string)result.Root["items"][2], Is.EqualTo("gamma"));
            }
        }

        #endregion

        #region Mixed nesting: collections and arrays at multiple levels

        [Test]
        public void MixedNestingCollectionsAndArrays()
        {
            // Build: root → collection → array → collection → scalar
            var innerColl = KVObject.ListCollection();
            innerColl.Add("name", "item1");
            innerColl.Add("value", 100);

            var innerColl2 = KVObject.ListCollection();
            innerColl2.Add("name", "item2");
            innerColl2.Add("value", 200);

            var arr = KVObject.Array([innerColl, innerColl2]);

            var mid = KVObject.ListCollection();
            mid.Add("items", arr);
            mid.Add("count", 2);

            var root = KVObject.ListCollection();
            root.Add("data", mid);

            using (Assert.EnterMultipleScope())
            {
                Assert.That((int)root["data"]["count"], Is.EqualTo(2));
                Assert.That(root["data"]["items"].IsArray, Is.True);
                Assert.That((string)root["data"]["items"][0]["name"], Is.EqualTo("item1"));
                Assert.That((int)root["data"]["items"][0]["value"], Is.EqualTo(100));
                Assert.That((string)root["data"]["items"][1]["name"], Is.EqualTo("item2"));
                Assert.That((int)root["data"]["items"][1]["value"], Is.EqualTo(200));
            }
        }

        #endregion

        #region Dict-backed collection mutation through temporary

        [Test]
        public void DictCollectionAddThroughTemporaryPropagates()
        {
            var inner = KVObject.Collection();
            inner.Add("a", "1");

            var root = KVObject.Collection();
            root.Add("inner", inner);

            root["inner"].Add("b", "2");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(root["inner"], Has.Count.EqualTo(2));
                Assert.That((string)root["inner"]["b"], Is.EqualTo("2"));
            }
        }

        [Test]
        public void DictCollectionRemoveThroughTemporaryPropagates()
        {
            var inner = KVObject.Collection();
            inner.Add("keep", "yes");
            inner.Add("remove", "bye");

            var root = KVObject.Collection();
            root.Add("inner", inner);

            root["inner"].Remove("remove");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(root["inner"], Has.Count.EqualTo(1));
                Assert.That(root["inner"].ContainsKey("remove"), Is.False);
            }
        }

        [Test]
        public void DictCollectionOverwriteThroughLocalPropagates()
        {
            var inner = KVObject.Collection();
            inner.Add("key", "original");

            var root = KVObject.Collection();
            root.Add("inner", inner);

            var innerCopy = root["inner"];
            innerCopy["key"] = "replaced";

            Assert.That((string)root["inner"]["key"], Is.EqualTo("replaced"));
        }

        #endregion

        #region Multi-level chained method calls (2+ temporaries deep)

        [Test]
        public void TwoLevelChainedAddPropagates()
        {
            var leaf = KVObject.ListCollection();
            leaf.Add("x", "1");

            var mid = KVObject.ListCollection();
            mid.Add("leaf", leaf);

            var root = KVObject.ListCollection();
            root.Add("mid", mid);

            // root["mid"] is a temporary, ["leaf"] is another temporary, .Add mutates the shared List
            root["mid"]["leaf"].Add("y", "2");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(root["mid"]["leaf"], Has.Count.EqualTo(2));
                Assert.That((string)root["mid"]["leaf"]["y"], Is.EqualTo("2"));
            }
        }

        [Test]
        public void TwoLevelChainedRemovePropagates()
        {
            var leaf = KVObject.ListCollection();
            leaf.Add("a", "1");
            leaf.Add("b", "2");

            var mid = KVObject.ListCollection();
            mid.Add("leaf", leaf);

            var root = KVObject.ListCollection();
            root.Add("mid", mid);

            root["mid"]["leaf"].Remove("a");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(root["mid"]["leaf"], Has.Count.EqualTo(1));
                Assert.That(root["mid"]["leaf"].ContainsKey("a"), Is.False);
            }
        }

        #endregion

        #region Integer indexer on list-backed collections

        [Test]
        public void IntegerIndexerOnListCollection()
        {
            var obj = KVObject.ListCollection();
            obj.Add("a", "first");
            obj.Add("b", "second");
            obj.Add("c", "third");

            using (Assert.EnterMultipleScope())
            {
                Assert.That((string)obj[0], Is.EqualTo("first"));
                Assert.That((string)obj[1], Is.EqualTo("second"));
                Assert.That((string)obj[2], Is.EqualTo("third"));
            }
        }

        [Test]
        public void IntegerIndexerOnNestedListCollection()
        {
            var inner = KVObject.ListCollection();
            inner.Add("x", "10");
            inner.Add("y", "20");

            var root = KVObject.ListCollection();
            root.Add("inner", inner);

            using (Assert.EnterMultipleScope())
            {
                Assert.That((string)root["inner"][0], Is.EqualTo("10"));
                Assert.That((string)root["inner"][1], Is.EqualTo("20"));
            }
        }

        #endregion

        #region String indexer set on dict-backed collections

        [Test]
        public void DictCollectionIndexerSetOverwritesExisting()
        {
            var root = KVObject.Collection();
            root.Add("key", "original");

            root["key"] = "replaced";

            Assert.That((string)root["key"], Is.EqualTo("replaced"));
        }

        [Test]
        public void DictCollectionIndexerSetAddsNewKey()
        {
            var root = KVObject.Collection();
            root.Add("existing", "1");

            root["new"] = "2";

            using (Assert.EnterMultipleScope())
            {
                Assert.That(root, Has.Count.EqualTo(2));
                Assert.That((string)root["new"], Is.EqualTo("2"));
            }
        }

        [Test]
        public void NestedDictCollectionIndexerSetPropagates()
        {
            var inner = KVObject.Collection();
            inner.Add("key", "original");

            var root = KVObject.Collection();
            root.Add("inner", inner);

            root["inner"]["key"] = "replaced";

            Assert.That((string)root["inner"]["key"], Is.EqualTo("replaced"));
        }

        #endregion

        #region Array within dict-backed parent

        [Test]
        public void ArrayWithinDictBackedParentReadByIndex()
        {
            var arr = KVObject.Array([
                "alpha",
                "beta",
            ]);

            var root = KVObject.Collection();
            root.Add("arr", arr);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(root["arr"].IsArray, Is.True);
                Assert.That((string)root["arr"][0], Is.EqualTo("alpha"));
                Assert.That((string)root["arr"][1], Is.EqualTo("beta"));
            }
        }

        [Test]
        public void ArrayMutationWithinDictBackedParentPropagates()
        {
            var arr = KVObject.Array([
                "a",
            ]);

            var root = KVObject.Collection();
            root.Add("arr", arr);

            root["arr"].Add("b");

            using (Assert.EnterMultipleScope())
            {
                Assert.That(root["arr"], Has.Count.EqualTo(2));
                Assert.That((string)root["arr"][1], Is.EqualTo("b"));
            }
        }

        #endregion

        #region Mixed backing: dict parent with list child and vice versa

        [Test]
        public void ListCollectionInsideDictCollection()
        {
            var inner = KVObject.ListCollection();
            inner.Add("dup", "first");
            inner.Add("dup", "second");

            var root = KVObject.Collection();
            root.Add("inner", inner);

            using (Assert.EnterMultipleScope())
            {
                // Dict parent can hold list-backed child
                Assert.That(root["inner"], Has.Count.EqualTo(2));
                Assert.That((string)root["inner"]["dup"], Is.EqualTo("first")); // first match
                Assert.That((string)root["inner"][0], Is.EqualTo("first"));
                Assert.That((string)root["inner"][1], Is.EqualTo("second"));
            }
        }

        [Test]
        public void DictCollectionInsideListCollection()
        {
            var inner = KVObject.Collection();
            inner.Add("a", "1");
            inner.Add("b", "2");

            var root = KVObject.ListCollection();
            root.Add("inner", inner);

            using (Assert.EnterMultipleScope())
            {
                Assert.That((string)root["inner"]["a"], Is.EqualTo("1"));
                Assert.That((string)root["inner"]["b"], Is.EqualTo("2"));
            }
        }

        #endregion
    }
}
