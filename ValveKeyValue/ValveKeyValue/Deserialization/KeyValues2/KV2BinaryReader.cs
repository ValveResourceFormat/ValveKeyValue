using System.Buffers;
using System.Globalization;
using System.Numerics;
using ValveKeyValue.KeyValues2;

namespace ValveKeyValue.Deserialization.KeyValues2
{
    sealed class KV2BinaryReader : IDisposable
    {
        const int ElementIndexNull = -1;
        const int ElementIndexExternal = -2;

        readonly BinaryReader reader;
        bool disposed;

        // Version flags computed from encoding version
        int version;
        IDVersion idVersion;
        bool hasStringTable;
        bool namesInStringTable;
        bool stringCountIsInt;
        bool stringIndicesAreInt;
        bool hasPrefixAttributes;

        string[] stringTable;
        KV2Element[] elements;

        public KV2BinaryReader(Stream stream)
        {
            ArgumentNullException.ThrowIfNull(stream);
            reader = new BinaryReader(stream, System.Text.Encoding.UTF8, leaveOpen: true);
        }

        public KVDocument Read()
        {
            ObjectDisposedException.ThrowIf(disposed, this);

            var header = ReadHeader();

            try
            {
                // v9: prefix attribute containers come before string table
                List<KeyValuePair<string, KVObject>> prefixAttributes = null;

                if (hasPrefixAttributes)
                {
                    prefixAttributes = ReadPrefixAttributes();
                }

                // Read string table
                if (hasStringTable)
                {
                    ReadStringTable();
                }
                else
                {
                    stringTable = [];
                }

                // Read element stubs
                ReadElementIndex();

                // Read attributes for each element
                for (var i = 0; i < elements.Length; i++)
                {
                    ReadElementAttributes(elements[i]);
                }

                if (elements.Length == 0)
                {
                    throw new KeyValueException("No elements found in KV2 binary document.");
                }

                var root = elements[0];

                // Attach prefix attributes if present
                if (prefixAttributes != null && prefixAttributes.Count > 0)
                {
                    foreach (var (key, value) in prefixAttributes)
                    {
                        root.Add(key, value);
                    }
                }

                return new KVDocument(header, root.Name ?? string.Empty, root);
            }
            catch (IOException ex)
            {
                throw new KeyValueException("Error while reading binary KV2 data.", ex);
            }
        }

        #region Header

        KVHeader ReadHeader()
        {
            // Header is a text line: <!-- dmx encoding binary N format FORMATNAME V -->
            // followed by one or more null bytes. Read until \0.
            var headerBytes = new List<byte>();

            while (true)
            {
                var b = reader.ReadByte();
                if (b == 0)
                {
                    break;
                }

                headerBytes.Add(b);
            }

            var headerStr = System.Text.Encoding.UTF8.GetString(headerBytes.ToArray()).Trim();

            if (!headerStr.StartsWith("<!-- dmx encoding ", StringComparison.Ordinal))
            {
                throw new KeyValueException("Invalid KV2 binary header: expected '<!-- dmx encoding ...'");
            }

            if (!headerStr.EndsWith(" -->", StringComparison.Ordinal))
            {
                throw new KeyValueException("Invalid KV2 binary header: expected closing ' -->'");
            }

            var content = headerStr.AsSpan()["<!-- dmx encoding ".Length..^" -->".Length];

            var firstSpace = content.IndexOf(' ');
            if (firstSpace < 0)
            {
                throw new KeyValueException("Invalid KV2 binary header: missing encoding version.");
            }

            var encodingName = content[..firstSpace].ToString();
            content = content[(firstSpace + 1)..];

            var secondSpace = content.IndexOf(' ');
            if (secondSpace < 0)
            {
                throw new KeyValueException("Invalid KV2 binary header: missing 'format' keyword.");
            }

            if (!int.TryParse(content[..secondSpace], CultureInfo.InvariantCulture, out var encodingVersion))
            {
                throw new KeyValueException("Invalid KV2 binary header: encoding version is not a number.");
            }

            content = content[(secondSpace + 1)..];

            if (!content.StartsWith("format ", StringComparison.Ordinal))
            {
                throw new KeyValueException("Invalid KV2 binary header: expected 'format' keyword.");
            }

            content = content["format ".Length..];

            var lastSpace = content.LastIndexOf(' ');
            if (lastSpace < 0)
            {
                throw new KeyValueException("Invalid KV2 binary header: missing format version.");
            }

            var formatName = content[..lastSpace].ToString();

            if (!int.TryParse(content[(lastSpace + 1)..], CultureInfo.InvariantCulture, out var formatVersion))
            {
                throw new KeyValueException("Invalid KV2 binary header: format version is not a number.");
            }

            // Validate version
            version = encodingVersion;
            if (version is not (1 or 2 or 3 or 4 or 5 or 9))
            {
                throw new KeyValueException($"Unsupported DMX binary encoding version: {version}");
            }

            // Compute version flags
            idVersion = version < 3 ? IDVersion.V1 : version < 9 ? IDVersion.V2 : IDVersion.V3;
            hasStringTable = version > 1;
            namesInStringTable = version > 3;
            stringCountIsInt = version >= 4;
            stringIndicesAreInt = version >= 5;
            hasPrefixAttributes = version > 5;

            return new KVHeader
            {
                Encoding = new ValveKeyValue.KeyValues3.KV3ID(encodingName, Version: encodingVersion),
                Format = new ValveKeyValue.KeyValues3.KV3ID(formatName, Version: formatVersion),
            };
        }

        #endregion

        #region Prefix attributes (v9)

        List<KeyValuePair<string, KVObject>> ReadPrefixAttributes()
        {
            var result = new List<KeyValuePair<string, KVObject>>();
            var containerCount = reader.ReadInt32();

            for (var i = 0; i < containerCount; i++)
            {
                var attrCount = reader.ReadInt32();

                for (var j = 0; j < attrCount; j++)
                {
                    // Names are inline (not from string table — table not read yet)
                    var name = ReadNullTerminatedString();
                    var typeByte = reader.ReadByte();
                    var attrType = DmxAttributeTypeHelper.DecodeID(typeByte, IDVersion.V3);
                    var value = ReadAttributeValue(attrType);

                    if (i == 0) // Only first container is meaningful
                    {
                        result.Add(new KeyValuePair<string, KVObject>(name, value));
                    }
                }
            }

            return result;
        }

        #endregion

        #region String table

        void ReadStringTable()
        {
            var count = stringCountIsInt ? reader.ReadInt32() : reader.ReadInt16();
            stringTable = new string[count];

            for (var i = 0; i < count; i++)
            {
                stringTable[i] = ReadNullTerminatedString();
            }
        }

        string ReadStringByIndex()
        {
            if (!hasStringTable)
            {
                return ReadNullTerminatedString();
            }

            var index = stringIndicesAreInt ? reader.ReadInt32() : reader.ReadInt16();

            if (index < 0 || index >= stringTable.Length)
            {
                throw new KeyValueException($"String index {index} out of range (table has {stringTable.Length} entries).");
            }

            return stringTable[index];
        }

        #endregion

        #region Element index

        void ReadElementIndex()
        {
            var elementCount = reader.ReadInt32();
            elements = new KV2Element[elementCount];

            for (var i = 0; i < elementCount; i++)
            {
                // className: string index for v2+ (hasStringTable), inline for v1
                var className = hasStringTable ? ReadStringByIndex() : ReadNullTerminatedString();

                // name: string index for v4+ (namesInStringTable), inline for v1-v3
                var name = namesInStringTable ? ReadStringByIndex() : ReadNullTerminatedString();

                // GUID (16 bytes)
                var guidBytes = reader.ReadBytes(16);
                var guid = new Guid(guidBytes);

                elements[i] = new KV2Element(className, name, guid);
            }
        }

        #endregion

        #region Element attributes

        void ReadElementAttributes(KV2Element element)
        {
            var attrCount = reader.ReadInt32();

            for (var i = 0; i < attrCount; i++)
            {
                var attrName = ReadStringByIndex();
                var typeByte = reader.ReadByte();
                var attrType = DmxAttributeTypeHelper.DecodeID(typeByte, idVersion);

                // Handle pre-v3 AT_OBJECTID: skip 16 bytes
                if (attrType == DmxAttributeType.ObjectId)
                {
                    reader.ReadBytes(16);
                    continue;
                }

                var value = ReadAttributeValue(attrType);
                element.Add(attrName, value);
            }
        }

        KVObject ReadAttributeValue(DmxAttributeType attrType)
        {
            return attrType switch
            {
                DmxAttributeType.Element => ReadElementReference(),
                DmxAttributeType.Int32 => new KVObject(reader.ReadInt32()),
                DmxAttributeType.Float => new KVObject(reader.ReadSingle()),
                DmxAttributeType.Bool => new KVObject(reader.ReadByte() != 0),
                DmxAttributeType.String => new KVObject(ReadStringValue()),
                DmxAttributeType.BinaryBlob => ReadBinaryBlob(),
                DmxAttributeType.Time => new KVObject(new DmxTime(reader.ReadInt32())),
                DmxAttributeType.Color => ReadColor(),
                DmxAttributeType.Vector2 => new KVObject(new Vector2(reader.ReadSingle(), reader.ReadSingle())),
                DmxAttributeType.Vector3 => new KVObject(new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle())),
                DmxAttributeType.Vector4 => new KVObject(new Vector4(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle())),
                DmxAttributeType.QAngle => new KVObject(new QAngle(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle())),
                DmxAttributeType.Quaternion => new KVObject(new Quaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle())),
                DmxAttributeType.Matrix4x4 => ReadMatrix(),
                DmxAttributeType.UInt64 => new KVObject(reader.ReadUInt64()),
                DmxAttributeType.UInt8 => KVObject.Byte(reader.ReadByte()),

                // Array types
                DmxAttributeType.ElementArray => ReadElementArray(),
                DmxAttributeType.Int32Array => ReadTypedArray<int>(KVValueType.Int32Array, () => reader.ReadInt32()),
                DmxAttributeType.FloatArray => ReadTypedArray<float>(KVValueType.FloatArray, () => reader.ReadSingle()),
                DmxAttributeType.BoolArray => ReadTypedArray<bool>(KVValueType.BooleanArray, () => reader.ReadByte() != 0),
                DmxAttributeType.StringArray => ReadTypedArray<string>(KVValueType.StringArray, ReadStringArrayValue),
                DmxAttributeType.BinaryBlobArray => ReadBinaryBlobArray(),
                DmxAttributeType.TimeArray => ReadTypedArray<DmxTime>(KVValueType.TimeSpanArray, () => new DmxTime(reader.ReadInt32())),
                DmxAttributeType.ColorArray => ReadTypedArray<DmxColor>(KVValueType.ColorArray, () => new DmxColor(reader.ReadByte(), reader.ReadByte(), reader.ReadByte(), reader.ReadByte())),
                DmxAttributeType.Vector2Array => ReadTypedArray<Vector2>(KVValueType.Vector2Array, () => new Vector2(reader.ReadSingle(), reader.ReadSingle())),
                DmxAttributeType.Vector3Array => ReadTypedArray<Vector3>(KVValueType.Vector3Array, () => new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle())),
                DmxAttributeType.Vector4Array => ReadTypedArray<Vector4>(KVValueType.Vector4Array, () => new Vector4(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle())),
                DmxAttributeType.QAngleArray => ReadTypedArray<QAngle>(KVValueType.QAngleArray, () => new QAngle(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle())),
                DmxAttributeType.QuaternionArray => ReadTypedArray<Quaternion>(KVValueType.QuaternionArray, () => new Quaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle())),
                DmxAttributeType.Matrix4x4Array => ReadTypedArray<Matrix4x4>(KVValueType.Matrix4x4Array, ReadMatrix4x4Value),
                DmxAttributeType.UInt64Array => ReadTypedArray<ulong>(KVValueType.UInt64Array, () => reader.ReadUInt64()),
                DmxAttributeType.UInt8Array => ReadTypedArray<byte>(KVValueType.ByteArray, () => reader.ReadByte()),

                _ => throw new KeyValueException($"Unhandled attribute type: {attrType}"),
            };
        }

        #endregion

        #region Value readers

        KV2Element ReadElementReference()
        {
            var index = reader.ReadInt32();

            if (index == ElementIndexNull || index == ElementIndexExternal)
            {
                return KV2Element.Null;
            }

            if (index < 0 || index >= elements.Length)
            {
                throw new KeyValueException($"Element index {index} out of range (have {elements.Length} elements).");
            }

            return elements[index];
        }

        string ReadStringValue()
        {
            // Scalar string values use string table indices for v2+ and inline for v1.
            if (!hasStringTable)
            {
                return ReadNullTerminatedString();
            }

            return ReadStringByIndex();
        }

        string ReadStringArrayValue()
        {
            // String values inside string arrays are ALWAYS inline null-terminated,
            // even in versions that use string table indices for scalar strings.
            return ReadNullTerminatedString();
        }

        KVObject ReadBinaryBlob()
        {
            var length = reader.ReadInt32();
            var data = reader.ReadBytes(length);
            return KVObject.Blob(data);
        }

        KVObject ReadColor()
        {
            var r = reader.ReadByte();
            var g = reader.ReadByte();
            var b = reader.ReadByte();
            var a = reader.ReadByte();
            return new KVObject(new DmxColor(r, g, b, a));
        }

        KVObject ReadMatrix()
        {
            return new KVObject(ReadMatrix4x4Value());
        }

        Matrix4x4 ReadMatrix4x4Value()
        {
            return new Matrix4x4(
                reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(),
                reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(),
                reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(),
                reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        }

        KVObject ReadElementArray()
        {
            var count = reader.ReadInt32();
            var list = new List<KV2Element>(count);

            for (var i = 0; i < count; i++)
            {
                var index = reader.ReadInt32();

                if (index == ElementIndexNull || index == ElementIndexExternal)
                {
                    list.Add(KV2Element.Null);
                }
                else if (index < 0 || index >= elements.Length)
                {
                    throw new KeyValueException($"Element index {index} out of range in element array.");
                }
                else
                {
                    list.Add(elements[index]);
                }
            }

            return new KVObject(KVValueType.ElementArray, list);
        }

        KVObject ReadTypedArray<T>(KVValueType valueType, Func<T> readItem)
        {
            var count = reader.ReadInt32();
            var list = new List<T>(count);

            for (var i = 0; i < count; i++)
            {
                list.Add(readItem());
            }

            return new KVObject(valueType, list);
        }

        KVObject ReadBinaryBlobArray()
        {
            var count = reader.ReadInt32();
            var list = new List<byte[]>(count);

            for (var i = 0; i < count; i++)
            {
                var length = reader.ReadInt32();
                var data = reader.ReadBytes(length);
                list.Add(data);
            }

            return new KVObject(KVValueType.BinaryBlobArray, list);
        }

        #endregion

        #region Utility

        string ReadNullTerminatedString()
        {
            var buffer = ArrayPool<byte>.Shared.Rent(32);

            try
            {
                var position = 0;

                while (true)
                {
                    var b = reader.ReadByte();
                    if (b == 0)
                    {
                        break;
                    }

                    if (position >= buffer.Length)
                    {
                        var newBuffer = ArrayPool<byte>.Shared.Rent(buffer.Length * 2);
                        Buffer.BlockCopy(buffer, 0, newBuffer, 0, buffer.Length);
                        ArrayPool<byte>.Shared.Return(buffer);
                        buffer = newBuffer;
                    }

                    buffer[position++] = b;
                }

                return System.Text.Encoding.UTF8.GetString(buffer, 0, position);
            }
            finally
            {
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        #endregion

        public void Dispose()
        {
            if (!disposed)
            {
                reader.Dispose();
                disposed = true;
            }
        }
    }
}
