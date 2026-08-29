using System.Numerics;
using ValveKeyValue.KeyValues2;
using ValveKeyValue.KeyValues3;

namespace ValveKeyValue.Serialization.KeyValues2
{
    sealed class KV2BinaryWriter : IDisposable
    {
        /// <summary>Valve reads string table indices as signed shorts before version 5.</summary>
        const int MaxShortStringTableEntries = short.MaxValue;

        const int ElementIndexNull = -1;
        const int ElementIndexExternal = -2;

        readonly BinaryWriter writer;
        readonly KVHeader? sourceHeader;

        KV2BinaryVersion version;
        bool sequentialIds;

        // Strings inside the prefix attribute containers are always inline, they are written
        // before the string table.
        bool writingPrefix;

        List<KV2Element> allElements = [];
        Dictionary<KV2Element, int> elementIndexMap = new(ReferenceEqualityComparer.Instance);
        StringTable stringTable = new();

        // Slot in the element list holding the second copy of the prefix attributes, or -1 when the
        // document has none. It is written under a different class name and keeps every attribute,
        // so the two passes over the list have to recognise it.
        int prefixDuplicateIndex = -1;

        public KV2BinaryWriter(Stream stream, KVHeader? header = null)
        {
            ArgumentNullException.ThrowIfNull(stream);

            sourceHeader = header;
            writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true);
        }

        /// <summary>
        /// Writes a KV2 document in binary format.
        /// </summary>
        public void Write(KVDocument doc)
        {
            ArgumentNullException.ThrowIfNull(doc);

            var rootElement = doc.Root as KV2Element
                ?? throw new KeyValueException("KV2 binary writer requires a KV2Element as the root object.");

            ThrowIfUnwritableRoot(rootElement);

            var prefixElement = (doc as KV2Document)?.PrefixElement;

            allElements = [];
            elementIndexMap = new Dictionary<KV2Element, int>(ReferenceEqualityComparer.Instance);
            stringTable = new StringTable();
            prefixDuplicateIndex = -1;

            CollectElements(rootElement, new HashSet<KV2Element>(ReferenceEqualityComparer.Instance), depth: 0);

            SelectVersion(prefixElement);

            // Only a container that has an identifier of its own gets the second copy, because that
            // identifier is the only thing the copy adds and the only place it can be read from.
            if (version.HasPrefixAttributes && prefixElement != null && prefixElement.ElementId != Guid.Empty)
            {
                InsertPrefixDuplicate(prefixElement);
            }

            BuildStringTable();

            WriteHeader();

            if (version.HasPrefixAttributes)
            {
                WritePrefixAttributes(prefixElement);
            }
            else if (prefixElement != null)
            {
                throw new KeyValueException($"A prefix element requires binary encoding version {KV2BinaryVersion.SourceTwo}, not {version.Version}.");
            }

            if (version.HasStringTable)
            {
                WriteStringTable();
            }

            WriteElementIndex();
            WriteAllAttributes();
        }

        #region Version selection

        /// <summary>
        /// The encoding name and version always describe our own output, only the format carries
        /// over from the source document. A version is inherited only from a binary source.
        /// </summary>
        void SelectVersion(KV2Element? prefixElement)
        {
            var encodingName = sourceHeader?.Encoding.Name;
            sequentialIds = encodingName == "binary_seqids";

            // Legacy headers are encoding version 0, which has the same layout as version 1.
            var selected = encodingName is "binary" or "binary_seqids"
                ? Math.Max(sourceHeader!.Encoding.Version, 1)
                : KV2BinaryVersion.Default;

            if (selected < KV2BinaryVersion.SourceTwo && (prefixElement != null || KV2Element.NeedsSourceTwoTypes(allElements)))
            {
                selected = KV2BinaryVersion.SourceTwo;
            }

            version = KV2BinaryVersion.For(selected, allowLegacy: false);
        }


        #endregion

        #region Collection

        void CollectElements(KV2Element element, HashSet<KV2Element> visited, int depth)
        {
            KV2Element.ThrowIfUnwritable(element);

            if (KV2Element.IsNullReference(element) || element.IsStub)
            {
                return;
            }

            if (depth > KV2Element.MaxNestingDepth)
            {
                throw new KeyValueException($"Elements are nested more than {KV2Element.MaxNestingDepth} deep.");
            }

            if (!visited.Add(element))
            {
                return;
            }

            elementIndexMap[element] = allElements.Count;
            allElements.Add(element);

            foreach (var (_, child) in element.Children)
            {
                if (child is KV2Element childElement)
                {
                    CollectElements(childElement, visited, depth + 1);
                }
                else if (child.ValueType == KVValueType.ElementArray)
                {
                    foreach (var item in child.GetArray<KV2Element>())
                    {
                        CollectElements(item, visited, depth + 1);
                    }
                }
            }
        }

        /// <summary>
        /// Files written by Hammer carry the prefix attributes twice: once in the container before
        /// the string table, and again as an element right after the root that nothing references.
        /// Element references are indices into the element list, so everything below the root moves
        /// down one slot. Other Valve tools write the container alone.
        /// </summary>
        void InsertPrefixDuplicate(KV2Element prefixElement)
        {
            for (var i = allElements.Count - 1; i >= 1; i--)
            {
                elementIndexMap[allElements[i]] = i + 1;
            }

            // The copy is not registered in the index map: references to an element that is both
            // the prefix container and part of the graph must resolve to its place in the graph.
            allElements.Insert(1, prefixElement);
            prefixDuplicateIndex = 1;
        }

        /// <remarks>Prefix attributes are written before the table, so their strings are always inline.</remarks>
        void BuildStringTable()
        {
            if (!version.HasStringTable)
            {
                return;
            }

            for (var i = 0; i < allElements.Count; i++)
            {
                var element = allElements[i];
                var isPrefixDuplicate = i == prefixDuplicateIndex;

                // Class names and attribute names go through the table from version 2 onwards.
                stringTable.GetOrAdd(ClassNameOf(element, isPrefixDuplicate));

                if (version.NamesInStringTable)
                {
                    stringTable.GetOrAdd(NameOf(element, isPrefixDuplicate));
                }

                foreach (var (key, child) in element.Children)
                {
                    if (!isPrefixDuplicate && KV2Element.IsElementName(key, child))
                    {
                        continue;
                    }

                    stringTable.GetOrAdd(key);

                    // Scalar string values only go through the table from version 4 onwards, and
                    // strings inside string arrays never do.
                    if (version.NamesInStringTable && child.ValueType == KVValueType.String)
                    {
                        stringTable.GetOrAdd((string?)child._ref ?? string.Empty);
                    }
                }
            }

            if (!version.StringIndicesAreInt && stringTable.Count > MaxShortStringTableEntries)
            {
                throw new KeyValueException(
                    $"Binary encoding version {version.Version} stores string table indices as signed shorts, " +
                    $"but the document needs {stringTable.Count} entries. Write it as version 5 or later.");
            }
        }

        #endregion

        #region Binary writing

        void WriteHeader()
        {
            var encodingName = sequentialIds ? "binary_seqids" : "binary";

            var header = new KVHeader
            {
                Encoding = new KV3ID(encodingName, Version: version.Version),
                Format = new KV3ID(sourceHeader?.Format.Name ?? "dmx", Version: sourceHeader?.Format.Version ?? 1),
            };

            writer.Write(System.Text.Encoding.UTF8.GetBytes(KV2Header.Format(header)));
            writer.Write((byte)'\n');
            writer.Write((byte)0);
        }

        void WritePrefixAttributes(KV2Element? prefixElement)
        {
            if (prefixElement == null)
            {
                writer.Write(0);
                return;
            }

            writer.Write(1);
            writer.Write(prefixElement.Count);

            writingPrefix = true;

            try
            {
                foreach (var (key, child) in prefixElement.Children)
                {
                    // Names are inline, the string table has not been written yet.
                    WriteNullTerminatedString(key);

                    var attributeType = GetDmxType(child);

                    if (attributeType is DmxAttributeType.Element or DmxAttributeType.ElementArray)
                    {
                        throw new KeyValueException("Prefix attributes cannot reference elements.");
                    }

                    writer.Write(DmxAttributeTypeHelper.EncodeID(attributeType, IDVersion.V3));
                    WriteAttributeValue(attributeType, child);
                }
            }
            finally
            {
                writingPrefix = false;
            }
        }

        void WriteStringTable()
        {
            var strings = stringTable.ToArray();

            if (version.StringCountIsInt)
            {
                writer.Write(strings.Length);
            }
            else
            {
                writer.Write(checked((short)strings.Length));
            }

            foreach (var s in strings)
            {
                WriteNullTerminatedString(s);
            }
        }

        void WriteStringIndex(string value)
        {
            var index = stringTable.GetOrAdd(value);

            if (version.StringIndicesAreInt)
            {
                writer.Write(index);
            }
            else
            {
                writer.Write(checked((short)index));
            }
        }

        void WriteStringValue(string value)
        {
            // Scalar string values only go through the table from version 4 onwards.
            if (writingPrefix || !version.NamesInStringTable)
            {
                WriteNullTerminatedString(value);
                return;
            }

            WriteStringIndex(value);
        }

        void WriteElementIndex()
        {
            writer.Write(allElements.Count);

            Span<byte> idBytes = stackalloc byte[16];

            for (var i = 0; i < allElements.Count; i++)
            {
                var element = allElements[i];
                var isPrefixDuplicate = i == prefixDuplicateIndex;
                var className = ClassNameOf(element, isPrefixDuplicate);
                var name = NameOf(element, isPrefixDuplicate);

                // Class names go through the table as soon as there is one (version 2+).
                if (version.HasStringTable)
                {
                    WriteStringIndex(className);
                }
                else
                {
                    WriteNullTerminatedString(className);
                }

                // Element names only go through the table from version 4 onwards.
                if (version.NamesInStringTable)
                {
                    WriteStringIndex(name);
                }
                else
                {
                    WriteNullTerminatedString(name);
                }

                var id = sequentialIds ? new Guid(i + 1, (short)0, (short)0, 0, 0, 0, 0, 0, 0, 0, 0) : element.ElementId;
                id.TryWriteBytes(idBytes);
                writer.Write(idBytes);
            }
        }

        void WriteAllAttributes()
        {
            for (var i = 0; i < allElements.Count; i++)
            {
                var element = allElements[i];

                // The prefix container has no name member, so an attribute called "name" is an
                // ordinary attribute there rather than the element name.
                var isPrefixDuplicate = i == prefixDuplicateIndex;
                var count = 0;

                foreach (var (key, child) in element.Children)
                {
                    if (isPrefixDuplicate || !KV2Element.IsElementName(key, child))
                    {
                        count++;
                    }
                }

                writer.Write(count);

                // Attributes are written in the order they are stored. Valve's attribute list is
                // built by prepending and walked head first when saving, so a round trip through
                // Valve preserves file order too.
                foreach (var (key, child) in element.Children)
                {
                    if (!isPrefixDuplicate && KV2Element.IsElementName(key, child))
                    {
                        continue;
                    }

                    if (version.HasStringTable)
                    {
                        WriteStringIndex(key);
                    }
                    else
                    {
                        WriteNullTerminatedString(key);
                    }

                    var attributeType = GetDmxType(child);
                    writer.Write(DmxAttributeTypeHelper.EncodeID(attributeType, version.IdVersion));
                    WriteAttributeValue(attributeType, child);
                }
            }
        }


        // The copy of the prefix attributes is written under Valve's base class name and without a
        // name, whatever the container itself carries.
        static string ClassNameOf(KV2Element element, bool isPrefixDuplicate)
            => isPrefixDuplicate ? KV2Element.PrefixDuplicateClassName : element.ClassName ?? string.Empty;

        static string NameOf(KV2Element element, bool isPrefixDuplicate)
            => isPrefixDuplicate ? string.Empty : element.Name ?? string.Empty;

        static DmxAttributeType GetDmxType(KVObject value)
        {
            if (value is KV2Element)
            {
                return DmxAttributeType.Element;
            }

            // A DMX element attribute must be a KV2Element; a plain collection has no class name
            // or id and cannot be written as one.
            if (value.ValueType == KVValueType.Collection)
            {
                throw new KeyValueException(
                    $"A collection attribute must be a {nameof(KV2Element)} to be written as a DMX element.");
            }

            return DmxAttributeTypeHelper.FromKVValueType(value.ValueType);
        }

        void WriteAttributeValue(DmxAttributeType attrType, KVObject value)
        {
            switch (attrType)
            {
                case DmxAttributeType.Element:
                    WriteElementReference((KV2Element)value);
                    break;
                case DmxAttributeType.Int32:
                    writer.Write(value.ToInt32(null));
                    break;
                case DmxAttributeType.Float:
                    writer.Write(value.ToSingle(null));
                    break;
                case DmxAttributeType.Bool:
                    writer.Write((byte)(value.ToBoolean(null) ? 1 : 0));
                    break;
                case DmxAttributeType.String:
                    WriteStringValue((string?)value._ref ?? string.Empty);
                    break;
                case DmxAttributeType.BinaryBlob:
                    WriteBlob(value.AsBlob());
                    break;
                case DmxAttributeType.Time:
                    writer.Write(((DmxTime)value._ref!).Ticks);
                    break;
                case DmxAttributeType.Color:
                    WriteColor((DmxColor)value._ref!);
                    break;
                case DmxAttributeType.Vector2:
                    WriteVector2((Vector2)value._ref!);
                    break;
                case DmxAttributeType.Vector3:
                    WriteVector3((Vector3)value._ref!);
                    break;
                case DmxAttributeType.Vector4:
                    WriteVector4((Vector4)value._ref!);
                    break;
                case DmxAttributeType.QAngle:
                    WriteQAngle((QAngle)value._ref!);
                    break;
                case DmxAttributeType.Quaternion:
                    WriteQuaternion((Quaternion)value._ref!);
                    break;
                case DmxAttributeType.Matrix4x4:
                    WriteMatrix4x4((Matrix4x4)value._ref!);
                    break;
                case DmxAttributeType.UInt64:
                    writer.Write(unchecked((ulong)value._scalar));
                    break;
                case DmxAttributeType.UInt8:
                    writer.Write((byte)value._scalar);
                    break;

                case DmxAttributeType.ElementArray:
                    WriteArray(value.GetArray<KV2Element>(), WriteElementReference);
                    break;
                case DmxAttributeType.Int32Array:
                    WriteArray(value.GetArray<int>(), writer.Write);
                    break;
                case DmxAttributeType.FloatArray:
                    WriteArray(value.GetArray<float>(), writer.Write);
                    break;
                case DmxAttributeType.BoolArray:
                    WriteArray(value.GetArray<bool>(), v => writer.Write((byte)(v ? 1 : 0)));
                    break;
                case DmxAttributeType.StringArray:
                    // Strings inside string arrays are always inline, in every version.
                    WriteArray(value.GetArray<string>(), s => WriteNullTerminatedString(s ?? string.Empty));
                    break;
                case DmxAttributeType.BinaryBlobArray:
                    WriteArray(value.GetArray<byte[]>(), WriteBlob);
                    break;
                case DmxAttributeType.TimeArray:
                    WriteArray(value.GetArray<DmxTime>(), v => writer.Write(v.Ticks));
                    break;
                case DmxAttributeType.ColorArray:
                    WriteArray(value.GetArray<DmxColor>(), WriteColor);
                    break;
                case DmxAttributeType.Vector2Array:
                    WriteArray(value.GetArray<Vector2>(), WriteVector2);
                    break;
                case DmxAttributeType.Vector3Array:
                    WriteArray(value.GetArray<Vector3>(), WriteVector3);
                    break;
                case DmxAttributeType.Vector4Array:
                    WriteArray(value.GetArray<Vector4>(), WriteVector4);
                    break;
                case DmxAttributeType.QAngleArray:
                    WriteArray(value.GetArray<QAngle>(), WriteQAngle);
                    break;
                case DmxAttributeType.QuaternionArray:
                    WriteArray(value.GetArray<Quaternion>(), WriteQuaternion);
                    break;
                case DmxAttributeType.Matrix4x4Array:
                    WriteArray(value.GetArray<Matrix4x4>(), WriteMatrix4x4);
                    break;
                case DmxAttributeType.UInt64Array:
                    WriteArray(value.GetArray<ulong>(), writer.Write);
                    break;
                case DmxAttributeType.UInt8Array:
                    WriteArray(value.GetArray<byte>(), writer.Write);
                    break;

                default:
                    throw new KeyValueException($"Unhandled attribute type in writer: {attrType}");
            }
        }

        /// <summary>
        /// Writes an element reference: an index into the element list, -1 for null, or -2
        /// followed by the target id as a null terminated string for an element owned by
        /// another file.
        /// </summary>
        void WriteElementReference(KV2Element element)
        {
            if (KV2Element.IsNullReference(element))
            {
                writer.Write(ElementIndexNull);
                return;
            }

            if (elementIndexMap.TryGetValue(element, out var index))
            {
                writer.Write(index);
                return;
            }

            writer.Write(ElementIndexExternal);
            WriteNullTerminatedString(element.ElementId.ToString());
        }


        /// <summary>
        /// A root that reads as a null reference or a stub would be skipped by the element walk,
        /// leaving a document with no elements that cannot be read back. Report it instead.
        /// </summary>
        internal static void ThrowIfUnwritableRoot(KV2Element root)
        {
            if (root.IsStub)
            {
                throw new KeyValueException("The root element is a stub, which has no attributes to write.");
            }

            if (KV2Element.IsNullReference(root))
            {
                throw new KeyValueException(
                    $"The root element has no {nameof(KV2Element.ElementId)}, which is the null reference sentinel. Assign it one.");
            }
        }

        void WriteBlob(byte[] data)
        {
            writer.Write(data.Length);
            writer.Write(data);
        }

        void WriteColor(DmxColor c)
        {
            writer.Write(c.R);
            writer.Write(c.G);
            writer.Write(c.B);
            writer.Write(c.A);
        }

        void WriteVector2(Vector2 v)
        {
            writer.Write(v.X);
            writer.Write(v.Y);
        }

        void WriteVector3(Vector3 v)
        {
            writer.Write(v.X);
            writer.Write(v.Y);
            writer.Write(v.Z);
        }

        void WriteVector4(Vector4 v)
        {
            writer.Write(v.X);
            writer.Write(v.Y);
            writer.Write(v.Z);
            writer.Write(v.W);
        }

        void WriteQAngle(QAngle a)
        {
            writer.Write(a.Pitch);
            writer.Write(a.Yaw);
            writer.Write(a.Roll);
        }

        void WriteQuaternion(Quaternion q)
        {
            writer.Write(q.X);
            writer.Write(q.Y);
            writer.Write(q.Z);
            writer.Write(q.W);
        }

        void WriteMatrix4x4(Matrix4x4 m)
        {
            writer.Write(m.M11); writer.Write(m.M12); writer.Write(m.M13); writer.Write(m.M14);
            writer.Write(m.M21); writer.Write(m.M22); writer.Write(m.M23); writer.Write(m.M24);
            writer.Write(m.M31); writer.Write(m.M32); writer.Write(m.M33); writer.Write(m.M34);
            writer.Write(m.M41); writer.Write(m.M42); writer.Write(m.M43); writer.Write(m.M44);
        }

        void WriteArray<T>(List<T> list, Action<T> writeItem)
        {
            writer.Write(list.Count);

            foreach (var item in list)
            {
                writeItem(item);
            }
        }

        void WriteNullTerminatedString(string value)
        {
            // Strings are terminated by a null byte, so one inside the value would end the field
            // early and desync everything after it.
            if (value.Contains('\0', StringComparison.Ordinal))
            {
                throw new KeyValueException("A string written to binary DMX cannot contain a null character.");
            }

            // BinaryWriter.Write(ReadOnlySpan<char>) encodes with the writer's UTF-8 encoding and
            // writes no length prefix, so it avoids the intermediate byte array.
            writer.Write(value.AsSpan());
            writer.Write((byte)0);
        }

        #endregion

        public void Dispose()
        {
            writer.Flush();
            writer.Dispose();
        }
    }
}
