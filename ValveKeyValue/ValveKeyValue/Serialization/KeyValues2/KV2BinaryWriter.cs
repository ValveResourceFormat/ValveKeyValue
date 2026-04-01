using System.Numerics;
using ValveKeyValue.KeyValues2;

namespace ValveKeyValue.Serialization.KeyValues2
{
    sealed class KV2BinaryWriter : IDisposable
    {
        const int DefaultEncodingVersion = 5;

        readonly BinaryWriter writer;
        readonly int version;
        readonly IDVersion idVersion;
        readonly bool hasStringTable;
        readonly bool namesInStringTable;
        readonly bool stringCountIsInt;
        readonly bool stringIndicesAreInt;

        readonly KVHeader header;

        // Collected during Write()
        List<KV2Element> allElements;
        Dictionary<KV2Element, int> elementIndexMap;
        StringTable stringTable;


        public KV2BinaryWriter(Stream stream, KVHeader header = null)
        {
            ArgumentNullException.ThrowIfNull(stream);

            this.header = header;
            writer = new BinaryWriter(stream, System.Text.Encoding.UTF8, leaveOpen: true);

            // Use the header's encoding version only if it's already a binary encoding.
            // If the source was text (keyvalues2), default to v5 for binary output.
            version = header?.Encoding.Name == "binary" ? header.Encoding.Version : DefaultEncodingVersion;

            if (version is not (1 or 2 or 3 or 4 or 5 or 9))
            {
                throw new KeyValueException($"Unsupported DMX binary encoding version for writing: {version}");
            }

            idVersion = version < 3 ? IDVersion.V1 : version < 9 ? IDVersion.V2 : IDVersion.V3;
            hasStringTable = version > 1;
            namesInStringTable = version > 3;
            stringCountIsInt = version >= 4;
            stringIndicesAreInt = version >= 5;
        }

        /// <summary>
        /// Writes a KV2 document in binary format.
        /// </summary>
        public void Write(KVDocument doc)
        {
            ArgumentNullException.ThrowIfNull(doc);

            var rootElement = doc.Root as KV2Element
                ?? throw new KeyValueException("KV2 binary writer requires a KV2Element as the root object.");

            // Collect all unique elements via graph traversal
            allElements = [];
            elementIndexMap = new Dictionary<KV2Element, int>(ReferenceEqualityComparer.Instance);
            stringTable = new StringTable();

            CollectElements(rootElement, new HashSet<KV2Element>(ReferenceEqualityComparer.Instance));

            // Collect strings from all elements.
            // Names and scalar string values go into the string table.
            // String array values are always inline (not in string table).
            foreach (var elem in allElements)
            {
                AddString(elem.ClassName);
                AddString(elem.Name ?? string.Empty);

                foreach (var (key, child) in elem.Children)
                {
                    AddString(key);

                    if (child.ValueType == KVValueType.String)
                    {
                        AddString((string)child._ref ?? string.Empty);
                    }
                }
            }

            // Write everything
            WriteHeader();

            // v9: prefix attribute containers (written before string table)
            if (version > 5)
            {
                writer.Write(0); // 0 prefix containers
            }

            if (hasStringTable)
            {
                WriteStringTable();
            }

            WriteElementIndex();
            WriteAllAttributes();
        }

        void AddString(string value)
        {
            if (hasStringTable)
            {
                stringTable.GetOrAdd(value);
            }
        }

        void CollectElements(KV2Element element, HashSet<KV2Element> visited)
        {
            if (element == null || ReferenceEquals(element, KV2Element.Null) || element.ElementId == Guid.Empty)
            {
                return;
            }

            if (!visited.Add(element))
            {
                return;
            }

            var index = allElements.Count;
            allElements.Add(element);
            elementIndexMap[element] = index;

            foreach (var (_, child) in element.Children)
            {
                if (child is KV2Element childElement)
                {
                    CollectElements(childElement, visited);
                }
                else if (child.ValueType == KVValueType.ElementArray)
                {
                    foreach (var item in child.GetArray<KV2Element>())
                    {
                        CollectElements(item, visited);
                    }
                }
            }
        }

        #region Binary writing

        void WriteHeader()
        {
            var encodingName = header?.Encoding.Name ?? "binary";
            var encodingVersion = version;
            var formatName = header?.Format.Name ?? "dmx";
            var formatVersion = header?.Format.Version ?? 1;

            var headerStr = $"<!-- dmx encoding {encodingName} {encodingVersion} format {formatName} {formatVersion} -->\n";
            writer.Write(System.Text.Encoding.UTF8.GetBytes(headerStr));
            writer.Write((byte)0); // null terminator
        }

        void WriteStringTable()
        {
            var strings = stringTable.ToArray();

            if (stringCountIsInt)
            {
                writer.Write(strings.Length);
            }
            else
            {
                writer.Write((short)strings.Length);
            }

            foreach (var s in strings)
            {
                WriteNullTerminatedString(s);
            }
        }

        void WriteStringValue(string value)
        {
            // Scalar string values use string table indices for v2+ and inline for v1.
            WriteStringIndex(value);
        }

        void WriteStringArrayValue(string value)
        {
            // String values in string arrays are ALWAYS inline null-terminated,
            // even in versions that use string table indices for scalar strings.
            WriteNullTerminatedString(value);
        }

        void WriteStringIndex(string value)
        {
            if (!hasStringTable)
            {
                WriteNullTerminatedString(value);
                return;
            }

            var index = stringTable.GetOrAdd(value);

            if (stringIndicesAreInt)
            {
                writer.Write(index);
            }
            else
            {
                writer.Write((short)index);
            }
        }

        void WriteElementIndex()
        {
            writer.Write(allElements.Count);

            foreach (var elem in allElements)
            {
                // className
                if (namesInStringTable)
                {
                    WriteStringIndex(elem.ClassName);
                }
                else
                {
                    WriteNullTerminatedString(elem.ClassName);
                }

                // name
                if (namesInStringTable)
                {
                    WriteStringIndex(elem.Name ?? string.Empty);
                }
                else
                {
                    WriteNullTerminatedString(elem.Name ?? string.Empty);
                }

                // GUID (16 bytes)
                writer.Write(elem.ElementId.ToByteArray());
            }
        }

        void WriteAllAttributes()
        {
            foreach (var elem in allElements)
            {
                // Get children as list for reverse-order writing
                var children = new List<KeyValuePair<string, KVObject>>(elem.Children);

                writer.Write(children.Count);

                // C++ writes attributes in reverse order
                for (var i = children.Count - 1; i >= 0; i--)
                {
                    var (attrName, attrValue) = children[i];
                    WriteStringIndex(attrName);

                    var dmxType = GetDmxType(attrValue);
                    writer.Write(DmxAttributeTypeHelper.EncodeID(dmxType, idVersion));

                    WriteAttributeValue(dmxType, attrValue);
                }
            }
        }

        static DmxAttributeType GetDmxType(KVObject value)
        {
            if (value is KV2Element)
            {
                return DmxAttributeType.Element;
            }

            return DmxAttributeTypeHelper.FromKVValueType(value.ValueType);
        }

        void WriteAttributeValue(DmxAttributeType attrType, KVObject value)
        {
            switch (attrType)
            {
                case DmxAttributeType.Element:
                    WriteElementRef(value);
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
                    WriteStringValue((string)value._ref ?? string.Empty);
                    break;
                case DmxAttributeType.BinaryBlob:
                    WriteBinaryBlob(value);
                    break;
                case DmxAttributeType.Time:
                    writer.Write(((DmxTime)value._ref).Ticks);
                    break;
                case DmxAttributeType.Color:
                    WriteColor((DmxColor)value._ref);
                    break;
                case DmxAttributeType.Vector2:
                    WriteVector2((Vector2)value._ref);
                    break;
                case DmxAttributeType.Vector3:
                    WriteVector3((Vector3)value._ref);
                    break;
                case DmxAttributeType.Vector4:
                    WriteVector4((Vector4)value._ref);
                    break;
                case DmxAttributeType.QAngle:
                    WriteQAngle((QAngle)value._ref);
                    break;
                case DmxAttributeType.Quaternion:
                    WriteQuaternion((Quaternion)value._ref);
                    break;
                case DmxAttributeType.Matrix4x4:
                    WriteMatrix4x4((Matrix4x4)value._ref);
                    break;
                case DmxAttributeType.UInt64:
                    writer.Write(unchecked((ulong)value._scalar));
                    break;
                case DmxAttributeType.UInt8:
                    writer.Write((byte)value._scalar);
                    break;

                // Array types
                case DmxAttributeType.ElementArray:
                    WriteElementArray(value);
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
                    WriteArray(value.GetArray<string>(), s => WriteStringArrayValue(s ?? string.Empty));
                    break;
                case DmxAttributeType.BinaryBlobArray:
                    WriteBinaryBlobArray(value);
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

        void WriteElementRef(KVObject value)
        {
            if (value is KV2Element elem && !ReferenceEquals(elem, KV2Element.Null) && elem.ElementId != Guid.Empty)
            {
                if (elementIndexMap.TryGetValue(elem, out var index))
                {
                    writer.Write(index);
                    return;
                }
            }

            // Null reference
            writer.Write(-1);
        }

        void WriteBinaryBlob(KVObject value)
        {
            var data = value.AsBlob();
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

        void WriteElementArray(KVObject value)
        {
            var list = value.GetArray<KV2Element>();
            writer.Write(list.Count);

            foreach (var elem in list)
            {
                if (elem == null || ReferenceEquals(elem, KV2Element.Null) || elem.ElementId == Guid.Empty)
                {
                    writer.Write(-1);
                }
                else if (elementIndexMap.TryGetValue(elem, out var index))
                {
                    writer.Write(index);
                }
                else
                {
                    writer.Write(-1);
                }
            }
        }

        void WriteArray<T>(List<T> list, Action<T> writeItem)
        {
            writer.Write(list.Count);

            foreach (var item in list)
            {
                writeItem(item);
            }
        }

        void WriteBinaryBlobArray(KVObject value)
        {
            var list = value.GetArray<byte[]>();
            writer.Write(list.Count);

            foreach (var blob in list)
            {
                writer.Write(blob.Length);
                writer.Write(blob);
            }
        }

        void WriteNullTerminatedString(string value)
        {
            var bytes = System.Text.Encoding.UTF8.GetBytes(value);
            writer.Write(bytes);
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
