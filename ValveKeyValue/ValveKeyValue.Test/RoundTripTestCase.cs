namespace ValveKeyValue.Test
{
    class RoundTripTestCase
    {
        #region Helpers

        static KVSerializer KV1 => KVSerializer.Create(KVSerializationFormat.KeyValues1Text);
        static KVSerializer KV3 => KVSerializer.Create(KVSerializationFormat.KeyValues3Text);

        static KVDocument DeserializeKV1(string text)
        {
            return KV1.Deserialize(text);
        }

        static KVDocument DeserializeKV3(string text)
        {
            return KV3.Deserialize(text);
        }

        static KVDocument RoundTripKV1(KVDocument data)
        {
            using var ms = new MemoryStream();
            KV1.Serialize(ms, data);
            ms.Seek(0, SeekOrigin.Begin);
            return KV1.Deserialize(ms);
        }

        static KVDocument RoundTripKV3(KVDocument data)
        {
            using var ms = new MemoryStream();
            KV3.Serialize(ms, data);
            ms.Seek(0, SeekOrigin.Begin);
            return KV3.Deserialize(ms);
        }

        #endregion

        #region KV1 text round-trip with mutation

        [Test]
        public void KV1TextRoundTripWithMutation()
        {
            var input = """
                "Root"
                {
                    "name"      "original"
                    "version"   "1"
                }
                """;

            var data = DeserializeKV1(input);
            Assert.That((string)data["name"], Is.EqualTo("original"));

            // Mutate via indexer
            data.Root["name"] = "modified";

            var result = RoundTripKV1(data);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Name, Is.EqualTo("Root"));
                Assert.That((string)result["name"], Is.EqualTo("modified"));
                Assert.That((string)result["version"], Is.EqualTo("1"));
            }
        }

        #endregion

        #region KV3 text round-trip with mutation

        [Test]
        public void KV3TextRoundTripWithMutation()
        {
            var input = "<!-- kv3 encoding:text:version{e21c7f3c-8a33-41c5-9977-a76d3a32aa0d} format:generic:version{7412167c-06e9-4698-aff2-e63eb59037e7} -->\n{\n\tname = \"original\"\n\tcount = 42\n}\n";

            var data = DeserializeKV3(input);
            Assert.That((string)data["name"], Is.EqualTo("original"));

            // Mutate
            data.Root["name"] = "updated";

            var result = RoundTripKV3(data);

            using (Assert.EnterMultipleScope())
            {
                Assert.That((string)result["name"], Is.EqualTo("updated"));
                Assert.That((long)result["count"], Is.EqualTo(42));
            }
        }

        #endregion

        #region KV1 array round-trip

        [Test]
        public void KV1ArrayRoundTrip()
        {
            // KV1 does not have native arrays; arrays are represented as collections with numeric keys.
            var input = """
                "Root"
                {
                    "items"
                    {
                        "0"     "apple"
                        "1"     "banana"
                        "2"     "cherry"
                    }
                }
                """;

            var data = DeserializeKV1(input);
            var items = data["items"];
            Assert.That(items, Has.Count.EqualTo(3));

            var result = RoundTripKV1(data);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result["items"], Has.Count.EqualTo(3));
                Assert.That((string)result["items"]["0"], Is.EqualTo("apple"));
                Assert.That((string)result["items"]["1"], Is.EqualTo("banana"));
                Assert.That((string)result["items"]["2"], Is.EqualTo("cherry"));
            }
        }

        #endregion

        #region KV3 dict-backed collection round-trip

        [Test]
        public void KV3DictBackedCollectionRoundTrip()
        {
            var input = "<!-- kv3 encoding:text:version{e21c7f3c-8a33-41c5-9977-a76d3a32aa0d} format:generic:version{7412167c-06e9-4698-aff2-e63eb59037e7} -->\n{\n\talpha = \"one\"\n\tbeta = 2\n\tgamma = true\n}\n";

            var data = DeserializeKV3(input);

            using (Assert.EnterMultipleScope())
            {
                // KV3 deserializer uses dictionary-backed collections; verify dict lookup works
                Assert.That(data.Root.ContainsKey("alpha"), Is.True);
                Assert.That(data.Root.ContainsKey("nonexistent"), Is.False);

                Assert.That((string)data["alpha"], Is.EqualTo("one"));
                Assert.That((long)data["beta"], Is.EqualTo(2));
                Assert.That((bool)data["gamma"], Is.True);
            }

            var result = RoundTripKV3(data);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Root.ContainsKey("alpha"), Is.True);
                Assert.That((string)result["alpha"], Is.EqualTo("one"));
                Assert.That((long)result["beta"], Is.EqualTo(2));
                Assert.That((bool)result["gamma"], Is.True);
            }
        }

        #endregion

        #region Adding children then serializing

        [Test]
        public void AddingChildrenThenSerializingKV1()
        {
            var root = KVObject.ListCollection();
            root.Add("key1", "value1");
            root.Add("key2", "value2");
            var doc = new KVDocument(null, "Config", root);

            var result = RoundTripKV1(doc);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result.Name, Is.EqualTo("Config"));
                Assert.That(result.Root, Has.Count.EqualTo(2));
                Assert.That((string)result.Root["key1"], Is.EqualTo("value1"));
                Assert.That((string)result.Root["key2"], Is.EqualTo("value2"));
            }
        }

        [Test]
        public void AddingChildrenThenSerializingKV3()
        {
            var root = KVObject.ListCollection();
            root.Add("existing", "yes");
            root.Add("added", "dynamically");
            var doc = new KVDocument(null, "root", root);

            var result = RoundTripKV3(doc);

            using (Assert.EnterMultipleScope())
            {
                Assert.That((string)result["existing"], Is.EqualTo("yes"));
                Assert.That((string)result["added"], Is.EqualTo("dynamically"));
            }
        }

        #endregion

        #region Removing children then serializing

        [Test]
        public void RemovingChildrenThenSerializingKV1()
        {
            var input = """
                "Settings"
                {
                    "keep"      "yes"
                    "remove"    "no"
                    "also_keep" "yes"
                }
                """;

            var data = DeserializeKV1(input);
            Assert.That(data.Root, Has.Count.EqualTo(3));

            var removed = data.Root.Remove("remove");
            Assert.That(removed, Is.True);

            var result = RoundTripKV1(data).Root;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Has.Count.EqualTo(2));
                Assert.That((string)result["keep"], Is.EqualTo("yes"));
                Assert.That(result.ContainsKey("remove"), Is.False);
                Assert.That((string)result["also_keep"], Is.EqualTo("yes"));
            }
        }

        [Test]
        public void RemovingChildrenThenSerializingKV3()
        {
            var input = "<!-- kv3 encoding:text:version{e21c7f3c-8a33-41c5-9977-a76d3a32aa0d} format:generic:version{7412167c-06e9-4698-aff2-e63eb59037e7} -->\n{\n\tkeep = \"yes\"\n\tremove = \"no\"\n\talso_keep = \"yes\"\n}\n";

            var data = DeserializeKV3(input);
            Assert.That(data.Root, Has.Count.EqualTo(3));

            var removed = data.Root.Remove("remove");
            Assert.That(removed, Is.True);

            var result = RoundTripKV3(data).Root;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result, Has.Count.EqualTo(2));
                Assert.That((string)result["keep"], Is.EqualTo("yes"));
                Assert.That(result.ContainsKey("remove"), Is.False);
                Assert.That((string)result["also_keep"], Is.EqualTo("yes"));
            }
        }

        #endregion

        #region Nested collection round-trip

        [Test]
        public void DeeplyNestedCollectionRoundTripKV1()
        {
            var input = """
                "Root"
                {
                    "level1"
                    {
                        "level2"
                        {
                            "level3"
                            {
                                "deep_value"    "found"
                            }
                        }
                        "sibling"   "here"
                    }
                }
                """;

            var data = DeserializeKV1(input);
            Assert.That((string)data["level1"]["level2"]["level3"]["deep_value"], Is.EqualTo("found"));

            var result = RoundTripKV1(data);

            using (Assert.EnterMultipleScope())
            {
                Assert.That((string)result["level1"]["level2"]["level3"]["deep_value"], Is.EqualTo("found"));
                Assert.That((string)result["level1"]["sibling"], Is.EqualTo("here"));
            }
        }

        [Test]
        public void DeeplyNestedCollectionRoundTripKV3()
        {
            var input = "<!-- kv3 encoding:text:version{e21c7f3c-8a33-41c5-9977-a76d3a32aa0d} format:generic:version{7412167c-06e9-4698-aff2-e63eb59037e7} -->\n{\n\touter =\n\t{\n\t\tmiddle =\n\t\t{\n\t\t\tinner =\n\t\t\t{\n\t\t\t\tvalue = \"deep\"\n\t\t\t}\n\t\t}\n\t}\n}\n";

            var data = DeserializeKV3(input);
            Assert.That((string)data["outer"]["middle"]["inner"]["value"], Is.EqualTo("deep"));

            var result = RoundTripKV3(data);

            Assert.That((string)result["outer"]["middle"]["inner"]["value"], Is.EqualTo("deep"));
        }

        #endregion

        #region Binary blob round-trip (KV3)

        [Test]
        public void BinaryBlobRoundTripKV3()
        {
            var input = "<!-- kv3 encoding:text:version{e21c7f3c-8a33-41c5-9977-a76d3a32aa0d} format:generic:version{7412167c-06e9-4698-aff2-e63eb59037e7} -->\n{\n\tblob = #[ 00 11 22 33 44 55 66 77 88 99 AA BB CC DD FF ]\n}\n";

            var expectedBytes = new byte[] { 0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, 0x99, 0xAA, 0xBB, 0xCC, 0xDD, 0xFF };

            var data = DeserializeKV3(input);
            Assert.That(data["blob"].AsBlob(), Is.EqualTo(expectedBytes));

            var result = RoundTripKV3(data);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result["blob"].ValueType, Is.EqualTo(KVValueType.BinaryBlob));
                Assert.That(result["blob"].AsBlob(), Is.EqualTo(expectedBytes));
            }
        }

        [Test]
        public void EmptyBinaryBlobRoundTripKV3()
        {
            var input = "<!-- kv3 encoding:text:version{e21c7f3c-8a33-41c5-9977-a76d3a32aa0d} format:generic:version{7412167c-06e9-4698-aff2-e63eb59037e7} -->\n{\n\tempty_blob = #[  ]\n}\n";

            var data = DeserializeKV3(input);
            Assert.That(data["empty_blob"].AsBlob(), Is.EqualTo(Array.Empty<byte>()));

            var result = RoundTripKV3(data);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result["empty_blob"].ValueType, Is.EqualTo(KVValueType.BinaryBlob));
                Assert.That(result["empty_blob"].AsBlob(), Is.EqualTo(Array.Empty<byte>()));
            }
        }

        #endregion

        #region Flag preservation round-trip (KV3)

        [Test]
        public void FlagPreservationRoundTripKV3()
        {
            var input = "<!-- kv3 encoding:text:version{e21c7f3c-8a33-41c5-9977-a76d3a32aa0d} format:generic:version{7412167c-06e9-4698-aff2-e63eb59037e7} -->\n{\n\tresource_ref = resource:\"materials/default.vmat\"\n\tresource_name_ref = resource_name:\"models/hero.vmdl\"\n\tpanorama_ref = panorama:\"panorama/layout.xml\"\n\tsound_ref = soundevent:\"sounds/bang.vsnd\"\n\tsubclass_ref = subclass:\"some_subclass\"\n\tentity_ref = entity_name:\"npc_hero\"\n\tno_flag = \"plain_value\"\n}\n";

            var data = DeserializeKV3(input);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(data["resource_ref"].Flag, Is.EqualTo(KVFlag.Resource));
                Assert.That(data["resource_name_ref"].Flag, Is.EqualTo(KVFlag.ResourceName));
                Assert.That(data["panorama_ref"].Flag, Is.EqualTo(KVFlag.Panorama));
                Assert.That(data["sound_ref"].Flag, Is.EqualTo(KVFlag.SoundEvent));
                Assert.That(data["subclass_ref"].Flag, Is.EqualTo(KVFlag.SubClass));
                Assert.That(data["entity_ref"].Flag, Is.EqualTo(KVFlag.EntityName));
                Assert.That(data["no_flag"].Flag, Is.EqualTo(KVFlag.None));
            }

            var result = RoundTripKV3(data).Root;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result["resource_ref"].Flag, Is.EqualTo(KVFlag.Resource));
                Assert.That((string)result["resource_ref"], Is.EqualTo("materials/default.vmat"));

                Assert.That(result["resource_name_ref"].Flag, Is.EqualTo(KVFlag.ResourceName));
                Assert.That((string)result["resource_name_ref"], Is.EqualTo("models/hero.vmdl"));

                Assert.That(result["panorama_ref"].Flag, Is.EqualTo(KVFlag.Panorama));
                Assert.That((string)result["panorama_ref"], Is.EqualTo("panorama/layout.xml"));

                Assert.That(result["sound_ref"].Flag, Is.EqualTo(KVFlag.SoundEvent));
                Assert.That((string)result["sound_ref"], Is.EqualTo("sounds/bang.vsnd"));

                Assert.That(result["subclass_ref"].Flag, Is.EqualTo(KVFlag.SubClass));
                Assert.That((string)result["subclass_ref"], Is.EqualTo("some_subclass"));

                Assert.That(result["entity_ref"].Flag, Is.EqualTo(KVFlag.EntityName));
                Assert.That((string)result["entity_ref"], Is.EqualTo("npc_hero"));

                Assert.That(result["no_flag"].Flag, Is.EqualTo(KVFlag.None));
                Assert.That((string)result["no_flag"], Is.EqualTo("plain_value"));
            }
        }

        #endregion

        #region Null value round-trip (KV3)

        [Test]
        public void NullValueRoundTripKV3()
        {
            var input = "<!-- kv3 encoding:text:version{e21c7f3c-8a33-41c5-9977-a76d3a32aa0d} format:generic:version{7412167c-06e9-4698-aff2-e63eb59037e7} -->\n{\n\tnullable = null\n\tnormal = \"exists\"\n}\n";

            var data = DeserializeKV3(input);

            using (Assert.EnterMultipleScope())
            {
                Assert.That(data["nullable"].ValueType, Is.EqualTo(KVValueType.Null));
                Assert.That(data["nullable"].IsNull, Is.True);
                Assert.That((string)data["normal"], Is.EqualTo("exists"));
            }

            var result = RoundTripKV3(data).Root;

            using (Assert.EnterMultipleScope())
            {
                Assert.That(result["nullable"].ValueType, Is.EqualTo(KVValueType.Null));
                Assert.That(result["nullable"].IsNull, Is.True);
                Assert.That((string)result["normal"], Is.EqualTo("exists"));
            }
        }

        #endregion
    }
}
