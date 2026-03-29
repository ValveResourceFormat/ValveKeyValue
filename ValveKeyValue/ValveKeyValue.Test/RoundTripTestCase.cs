namespace ValveKeyValue.Test
{
    class RoundTripTestCase
    {
        #region Helpers

        static KVSerializer KV1 => KVSerializer.Create(KVSerializationFormat.KeyValues1Text);
        static KVSerializer KV3 => KVSerializer.Create(KVSerializationFormat.KeyValues3Text);

        static KVObject DeserializeKV1(string text)
        {
            return KV1.Deserialize(text);
        }

        static KVObject DeserializeKV3(string text)
        {
            return KV3.Deserialize(text);
        }

        static KVDocument RoundTripKV1(KVObject data)
        {
            using var ms = new MemoryStream();
            KV1.Serialize(ms, data);
            ms.Seek(0, SeekOrigin.Begin);
            return KV1.Deserialize(ms);
        }

        static KVDocument RoundTripKV3(KVObject data)
        {
            using var ms = new MemoryStream();
            KV3.Serialize(ms, data);
            ms.Seek(0, SeekOrigin.Begin);
            return KV3.Deserialize(ms);
        }

        #endregion

        #region 1. KV1 text round-trip with mutation

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
            data["name"] = new KVObject("name", "modified");

            var result = RoundTripKV1(data);

            Assert.Multiple(() =>
            {
                Assert.That(result.Name, Is.EqualTo("Root"));
                Assert.That((string)result["name"], Is.EqualTo("modified"));
                Assert.That((string)result["version"], Is.EqualTo("1"));
            });
        }

        #endregion

        #region 2. KV3 text round-trip with mutation

        [Test]
        public void KV3TextRoundTripWithMutation()
        {
            var input = "<!-- kv3 encoding:text:version{e21c7f3c-8a33-41c5-9977-a76d3a32aa0d} format:generic:version{7412167c-06e9-4698-aff2-e63eb59037e7} -->\n{\n\tname = \"original\"\n\tcount = 42\n}\n";

            var data = DeserializeKV3(input);
            Assert.That((string)data["name"], Is.EqualTo("original"));

            // Mutate
            data["name"] = new KVObject("name", (KVValue)"updated");

            var result = RoundTripKV3(data);

            Assert.Multiple(() =>
            {
                Assert.That((string)result["name"], Is.EqualTo("updated"));
                Assert.That((long)result["count"], Is.EqualTo(42));
            });
        }

        #endregion

        #region 3. KV1 array round-trip

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
            Assert.That(items.Count, Is.EqualTo(3));

            var result = RoundTripKV1(data);

            Assert.Multiple(() =>
            {
                Assert.That(result["items"].Count, Is.EqualTo(3));
                Assert.That((string)result["items"]["0"], Is.EqualTo("apple"));
                Assert.That((string)result["items"]["1"], Is.EqualTo("banana"));
                Assert.That((string)result["items"]["2"], Is.EqualTo("cherry"));
            });
        }

        #endregion

        #region 4. KV3 dict-backed collection round-trip

        [Test]
        public void KV3DictBackedCollectionRoundTrip()
        {
            var input = "<!-- kv3 encoding:text:version{e21c7f3c-8a33-41c5-9977-a76d3a32aa0d} format:generic:version{7412167c-06e9-4698-aff2-e63eb59037e7} -->\n{\n\talpha = \"one\"\n\tbeta = 2\n\tgamma = true\n}\n";

            var data = DeserializeKV3(input);

            // KV3 deserializer uses dictionary-backed collections; verify dict lookup works
            Assert.That(data.ContainsKey("alpha"), Is.True);
            Assert.That(data.ContainsKey("nonexistent"), Is.False);

            Assert.That((string)data["alpha"], Is.EqualTo("one"));
            Assert.That((long)data["beta"], Is.EqualTo(2));
            Assert.That((bool)data["gamma"], Is.True);

            var result = RoundTripKV3(data);

            Assert.Multiple(() =>
            {
                Assert.That(result.ContainsKey("alpha"), Is.True);
                Assert.That((string)result["alpha"], Is.EqualTo("one"));
                Assert.That((long)result["beta"], Is.EqualTo(2));
                Assert.That((bool)result["gamma"], Is.True);
            });
        }

        #endregion

        #region 5. Adding children then serializing

        [Test]
        public void AddingChildrenThenSerializingKV1()
        {
            var root = new KVObject("Config", new List<KVObject>());

            root.Add(new KVObject("key1", "value1"));
            root.Add(new KVObject("key2", "value2"));

            var result = RoundTripKV1(root);

            Assert.Multiple(() =>
            {
                Assert.That(result.Name, Is.EqualTo("Config"));
                Assert.That(result.Count, Is.EqualTo(2));
                Assert.That((string)result["key1"], Is.EqualTo("value1"));
                Assert.That((string)result["key2"], Is.EqualTo("value2"));
            });
        }

        [Test]
        public void AddingChildrenThenSerializingKV3()
        {
            var children = new List<KVObject>
            {
                new("existing", (KVValue)"yes"),
            };
            var root = new KVObject("root", (IEnumerable<KVObject>)children);

            root.Add(new KVObject("added", (KVValue)"dynamically"));

            var result = RoundTripKV3(root);

            Assert.Multiple(() =>
            {
                Assert.That((string)result["existing"], Is.EqualTo("yes"));
                Assert.That((string)result["added"], Is.EqualTo("dynamically"));
            });
        }

        #endregion

        #region 6. Removing children then serializing

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
            Assert.That(data.Count, Is.EqualTo(3));

            var removed = data.Remove("remove");
            Assert.That(removed, Is.True);

            var result = RoundTripKV1(data);

            Assert.Multiple(() =>
            {
                Assert.That(result.Count, Is.EqualTo(2));
                Assert.That((string)result["keep"], Is.EqualTo("yes"));
                Assert.That(result["remove"], Is.Null);
                Assert.That((string)result["also_keep"], Is.EqualTo("yes"));
            });
        }

        [Test]
        public void RemovingChildrenThenSerializingKV3()
        {
            var input = "<!-- kv3 encoding:text:version{e21c7f3c-8a33-41c5-9977-a76d3a32aa0d} format:generic:version{7412167c-06e9-4698-aff2-e63eb59037e7} -->\n{\n\tkeep = \"yes\"\n\tremove = \"no\"\n\talso_keep = \"yes\"\n}\n";

            var data = DeserializeKV3(input);
            Assert.That(data.Count, Is.EqualTo(3));

            var removed = data.Remove("remove");
            Assert.That(removed, Is.True);

            var result = RoundTripKV3(data);

            Assert.Multiple(() =>
            {
                Assert.That(result.Count, Is.EqualTo(2));
                Assert.That((string)result["keep"], Is.EqualTo("yes"));
                Assert.That(result["remove"], Is.Null);
                Assert.That((string)result["also_keep"], Is.EqualTo("yes"));
            });
        }

        #endregion

        #region 7. Nested collection round-trip

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

            Assert.Multiple(() =>
            {
                Assert.That((string)result["level1"]["level2"]["level3"]["deep_value"], Is.EqualTo("found"));
                Assert.That((string)result["level1"]["sibling"], Is.EqualTo("here"));
            });
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

        #region 8. Binary blob round-trip (KV3)

        [Test]
        public void BinaryBlobRoundTripKV3()
        {
            var input = "<!-- kv3 encoding:text:version{e21c7f3c-8a33-41c5-9977-a76d3a32aa0d} format:generic:version{7412167c-06e9-4698-aff2-e63eb59037e7} -->\n{\n\tblob = #[ 00 11 22 33 44 55 66 77 88 99 AA BB CC DD FF ]\n}\n";

            var expectedBytes = new byte[] { 0x00, 0x11, 0x22, 0x33, 0x44, 0x55, 0x66, 0x77, 0x88, 0x99, 0xAA, 0xBB, 0xCC, 0xDD, 0xFF };

            var data = DeserializeKV3(input);
            Assert.That(data["blob"].Value.AsBlob(), Is.EqualTo(expectedBytes));

            var result = RoundTripKV3(data);

            Assert.Multiple(() =>
            {
                Assert.That(result["blob"].ValueType, Is.EqualTo(KVValueType.BinaryBlob));
                Assert.That(result["blob"].Value.AsBlob(), Is.EqualTo(expectedBytes));
            });
        }

        [Test]
        public void EmptyBinaryBlobRoundTripKV3()
        {
            var input = "<!-- kv3 encoding:text:version{e21c7f3c-8a33-41c5-9977-a76d3a32aa0d} format:generic:version{7412167c-06e9-4698-aff2-e63eb59037e7} -->\n{\n\tempty_blob = #[  ]\n}\n";

            var data = DeserializeKV3(input);
            Assert.That(data["empty_blob"].Value.AsBlob(), Is.EqualTo(Array.Empty<byte>()));

            var result = RoundTripKV3(data);

            Assert.Multiple(() =>
            {
                Assert.That(result["empty_blob"].ValueType, Is.EqualTo(KVValueType.BinaryBlob));
                Assert.That(result["empty_blob"].Value.AsBlob(), Is.EqualTo(Array.Empty<byte>()));
            });
        }

        #endregion

        #region 9. Flag preservation round-trip (KV3)

        [Test]
        public void FlagPreservationRoundTripKV3()
        {
            var input = "<!-- kv3 encoding:text:version{e21c7f3c-8a33-41c5-9977-a76d3a32aa0d} format:generic:version{7412167c-06e9-4698-aff2-e63eb59037e7} -->\n{\n\tresource_ref = resource:\"materials/default.vmat\"\n\tresource_name_ref = resource_name:\"models/hero.vmdl\"\n\tpanorama_ref = panorama:\"panorama/layout.xml\"\n\tsound_ref = soundevent:\"sounds/bang.vsnd\"\n\tsubclass_ref = subclass:\"some_subclass\"\n\tentity_ref = entity_name:\"npc_hero\"\n\tno_flag = \"plain_value\"\n}\n";

            var data = DeserializeKV3(input);

            Assert.Multiple(() =>
            {
                Assert.That(data["resource_ref"].Value.Flag, Is.EqualTo(KVFlag.Resource));
                Assert.That(data["resource_name_ref"].Value.Flag, Is.EqualTo(KVFlag.ResourceName));
                Assert.That(data["panorama_ref"].Value.Flag, Is.EqualTo(KVFlag.Panorama));
                Assert.That(data["sound_ref"].Value.Flag, Is.EqualTo(KVFlag.SoundEvent));
                Assert.That(data["subclass_ref"].Value.Flag, Is.EqualTo(KVFlag.SubClass));
                Assert.That(data["entity_ref"].Value.Flag, Is.EqualTo(KVFlag.EntityName));
                Assert.That(data["no_flag"].Value.Flag, Is.EqualTo(KVFlag.None));
            });

            var result = RoundTripKV3(data);

            Assert.Multiple(() =>
            {
                Assert.That(result["resource_ref"].Value.Flag, Is.EqualTo(KVFlag.Resource));
                Assert.That((string)result["resource_ref"], Is.EqualTo("materials/default.vmat"));

                Assert.That(result["resource_name_ref"].Value.Flag, Is.EqualTo(KVFlag.ResourceName));
                Assert.That((string)result["resource_name_ref"], Is.EqualTo("models/hero.vmdl"));

                Assert.That(result["panorama_ref"].Value.Flag, Is.EqualTo(KVFlag.Panorama));
                Assert.That((string)result["panorama_ref"], Is.EqualTo("panorama/layout.xml"));

                Assert.That(result["sound_ref"].Value.Flag, Is.EqualTo(KVFlag.SoundEvent));
                Assert.That((string)result["sound_ref"], Is.EqualTo("sounds/bang.vsnd"));

                Assert.That(result["subclass_ref"].Value.Flag, Is.EqualTo(KVFlag.SubClass));
                Assert.That((string)result["subclass_ref"], Is.EqualTo("some_subclass"));

                Assert.That(result["entity_ref"].Value.Flag, Is.EqualTo(KVFlag.EntityName));
                Assert.That((string)result["entity_ref"], Is.EqualTo("npc_hero"));

                Assert.That(result["no_flag"].Value.Flag, Is.EqualTo(KVFlag.None));
                Assert.That((string)result["no_flag"], Is.EqualTo("plain_value"));
            });
        }

        #endregion

        #region 10. Null value round-trip (KV3)

        [Test]
        public void NullValueRoundTripKV3()
        {
            var input = "<!-- kv3 encoding:text:version{e21c7f3c-8a33-41c5-9977-a76d3a32aa0d} format:generic:version{7412167c-06e9-4698-aff2-e63eb59037e7} -->\n{\n\tnullable = null\n\tnormal = \"exists\"\n}\n";

            var data = DeserializeKV3(input);

            Assert.Multiple(() =>
            {
                Assert.That(data["nullable"].ValueType, Is.EqualTo(KVValueType.Null));
                Assert.That(data["nullable"].IsNull, Is.True);
                Assert.That((string)data["normal"], Is.EqualTo("exists"));
            });

            var result = RoundTripKV3(data);

            Assert.Multiple(() =>
            {
                Assert.That(result["nullable"].ValueType, Is.EqualTo(KVValueType.Null));
                Assert.That(result["nullable"].IsNull, Is.True);
                Assert.That((string)result["normal"], Is.EqualTo("exists"));
            });
        }

        #endregion
    }
}
