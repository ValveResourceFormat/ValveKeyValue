using System.Buffers;
using System.Numerics;
using System.Runtime.InteropServices;
using ValveKeyValue.KeyValues2;

namespace ValveKeyValue.Deserialization.KeyValues2
{
    sealed class KV2BinaryReader : IDisposable
    {
        const int ElementIndexNull = -1;
        const int ElementIndexExternal = -2;

        readonly BinaryReader reader;
        bool disposed;

        KV2BinaryVersion version;

        // Strings inside the prefix attribute containers are always inline, because the string
        // table has not been read at that point yet.
        bool readingPrefix;

        string[] stringTable = [];
        KV2Element[] elements = [];
        readonly Dictionary<Guid, KV2Element> stubs = [];

        public KV2BinaryReader(Stream stream)
        {
            ArgumentNullException.ThrowIfNull(stream);
            reader = new BinaryReader(stream, System.Text.Encoding.UTF8, leaveOpen: true);
        }

        public KVDocument Read()
        {
            ObjectDisposedException.ThrowIf(disposed, this);

            try
            {
                stubs.Clear();

                var header = ReadHeader();

                // v9: prefix attribute containers come before the string table.
                var prefixElement = version.HasPrefixAttributes ? ReadPrefixAttributes() : null;

                if (version.HasStringTable)
                {
                    ReadStringTable();
                }
                else
                {
                    stringTable = [];
                }

                ReadElementIndex();

                foreach (var element in elements)
                {
                    ReadElementAttributes(element);
                }

                if (elements.Length == 0)
                {
                    throw new KeyValueException("No elements found in KV2 binary document.");
                }

                var root = elements[0];

                if (prefixElement != null)
                {
                    AdoptPrefixElementId(prefixElement);
                }

                return new KV2Document(header, root.Name ?? string.Empty, root, prefixElement);
            }
            catch (Exception ex) when (ex is EndOfStreamException or IOException or FormatException or OverflowException or ArgumentException or InvalidDataException)
            {
                throw new KeyValueException("Error while reading binary KV2 data.", ex);
            }
        }

        #region Header

        KVHeader ReadHeader()
        {
            // The header is a text line, followed by one or more null bytes.
            var headerBytes = new List<byte>(KV2Header.MaxHeaderLength);

            while (true)
            {
                var b = reader.ReadByte();

                if (b == 0)
                {
                    break;
                }

                if (headerBytes.Count >= KV2Header.MaxHeaderLength)
                {
                    throw new KeyValueException($"Invalid KV2 binary header: no null terminator within {KV2Header.MaxHeaderLength} bytes.");
                }

                headerBytes.Add(b);
            }

            var header = KV2Header.Parse(System.Text.Encoding.UTF8.GetString(CollectionsMarshal.AsSpan(headerBytes)));

            KV2Header.ValidateEncoding(header, "binary", "binary_seqids");

            version = KV2BinaryVersion.For(header.Encoding.Version, allowLegacy: true);

            return header;
        }

        #endregion

        #region Prefix attributes (v9)

        KV2Element? ReadPrefixAttributes()
        {
            var containerCount = ReadCount("prefix attribute container");
            KV2Element? prefix = null;

            readingPrefix = true;

            try
            {
                for (var i = 0; i < containerCount; i++)
                {
                    var attributeCount = ReadCount("prefix attribute");

                    // Only the first container is meaningful, but every one has to be consumed.
                    var container = i == 0 ? new KV2Element(KV2Element.PrefixElementClassName, string.Empty, Guid.Empty) : null;

                    for (var j = 0; j < attributeCount; j++)
                    {
                        // Names are inline, the string table has not been read yet.
                        var name = ReadNullTerminatedString();
                        var attributeType = DmxAttributeTypeHelper.DecodeID(reader.ReadByte(), IDVersion.V3);

                        if (attributeType is DmxAttributeType.Element or DmxAttributeType.ElementArray)
                        {
                            throw new KeyValueException("Prefix attributes cannot reference elements.");
                        }

                        var value = ReadAttributeValue(attributeType);
                        container?.Add(name, value);
                    }

                    prefix ??= container;
                }
            }
            finally
            {
                readingPrefix = false;
            }

            return prefix;
        }

        /// <summary>
        /// Files written by Hammer carry the prefix attributes twice: once in the container read
        /// above, and again as an element right after the root that nothing references. Only that
        /// copy carries an identifier, so it is folded back into the container and otherwise left
        /// where it is, unreachable from the root. Other Valve tools write the container alone, and
        /// the container is then left without an identifier.
        /// </summary>
        void AdoptPrefixElementId(KV2Element prefix)
        {
            if (elements.Length < 2)
            {
                return;
            }

            var duplicate = elements[1];

            if (duplicate.IsStub
                || duplicate.ClassName != KV2Element.PrefixDuplicateClassName
                || duplicate.Name.Length > 0
                || duplicate.Count != prefix.Count)
            {
                return;
            }

            foreach (var (key, _) in prefix.Children)
            {
                if (!duplicate.ContainsKey(key))
                {
                    return;
                }
            }

            prefix.ElementId = duplicate.ElementId;
        }

        #endregion

        #region String table

        void ReadStringTable()
        {
            var count = version.StringCountIsInt ? reader.ReadInt32() : reader.ReadInt16();

            if (count < 0)
            {
                throw new KeyValueException($"Negative string table count: {count}");
            }

            var strings = new List<string>(InitialCapacity(count));

            for (var i = 0; i < count; i++)
            {
                strings.Add(ReadNullTerminatedString());
            }

            stringTable = [.. strings];
        }

        string ReadStringByIndex()
        {
            if (!version.HasStringTable)
            {
                return ReadNullTerminatedString();
            }

            var index = version.StringIndicesAreInt ? reader.ReadInt32() : reader.ReadInt16();

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
            var elementCount = ReadCount("element");
            var read = new List<KV2Element>(InitialCapacity(elementCount));

            for (var i = 0; i < elementCount; i++)
            {
                // Class names go through the string table as soon as there is one (v2+).
                var className = ReadStringByIndex();

                // Element names only go through the table from v4 onwards.
                var name = version.NamesInStringTable ? ReadStringByIndex() : ReadNullTerminatedString();

                var guid = new Guid(reader.ReadBytes(16));

                read.Add(new KV2Element(className, name, guid));
            }

            elements = [.. read];
        }

        #endregion

        #region Element attributes

        void ReadElementAttributes(KV2Element element)
        {
            var attributeCount = ReadCount("attribute");

            for (var i = 0; i < attributeCount; i++)
            {
                var attributeName = ReadStringByIndex();
                var attributeType = DmxAttributeTypeHelper.DecodeID(reader.ReadByte(), version.IdVersion);

                // Binary versions before 3 store AT_OBJECTID in the slot that later became AT_TIME.
                if (attributeType == DmxAttributeType.ObjectId)
                {
                    reader.ReadBytes(16);
                    continue;
                }

                var value = ReadAttributeValue(attributeType);

                // The element name is a built-in member in Valve's datamodel, so an attribute
                // literally called "name" is the element name rather than a child attribute.
                if (attributeType == DmxAttributeType.String && attributeName == "name")
                {
                    element.Name = (string?)value._ref ?? string.Empty;
                    continue;
                }

                element.Add(attributeName, value);
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
                DmxAttributeType.BinaryBlob => KVObject.Blob(ReadBlob()),
                DmxAttributeType.Time => new KVObject(new DmxTime(reader.ReadInt32())),
                DmxAttributeType.Color => new KVObject(ReadColor()),
                DmxAttributeType.Vector2 => new KVObject(new Vector2(reader.ReadSingle(), reader.ReadSingle())),
                DmxAttributeType.Vector3 => new KVObject(new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle())),
                DmxAttributeType.Vector4 => new KVObject(new Vector4(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle())),
                DmxAttributeType.QAngle => new KVObject(new QAngle(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle())),
                DmxAttributeType.Quaternion => new KVObject(new Quaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle())),
                DmxAttributeType.Matrix4x4 => new KVObject(ReadMatrix4x4()),
                DmxAttributeType.UInt64 => new KVObject(reader.ReadUInt64()),
                DmxAttributeType.UInt8 => KVObject.Byte(reader.ReadByte()),

                DmxAttributeType.ElementArray => ReadElementArray(),
                DmxAttributeType.Int32Array => ReadTypedArray(KVValueType.Int32Array, reader.ReadInt32),
                DmxAttributeType.FloatArray => ReadTypedArray(KVValueType.FloatArray, reader.ReadSingle),
                DmxAttributeType.BoolArray => ReadTypedArray(KVValueType.BooleanArray, () => reader.ReadByte() != 0),
                DmxAttributeType.StringArray => ReadTypedArray(KVValueType.StringArray, ReadNullTerminatedString),
                DmxAttributeType.BinaryBlobArray => ReadTypedArray(KVValueType.BinaryBlobArray, ReadBlob),
                DmxAttributeType.TimeArray => ReadTypedArray(KVValueType.TimeSpanArray, () => new DmxTime(reader.ReadInt32())),
                DmxAttributeType.ColorArray => ReadTypedArray(KVValueType.ColorArray, ReadColor),
                DmxAttributeType.Vector2Array => ReadTypedArray(KVValueType.Vector2Array, () => new Vector2(reader.ReadSingle(), reader.ReadSingle())),
                DmxAttributeType.Vector3Array => ReadTypedArray(KVValueType.Vector3Array, () => new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle())),
                DmxAttributeType.Vector4Array => ReadTypedArray(KVValueType.Vector4Array, () => new Vector4(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle())),
                DmxAttributeType.QAngleArray => ReadTypedArray(KVValueType.QAngleArray, () => new QAngle(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle())),
                DmxAttributeType.QuaternionArray => ReadTypedArray(KVValueType.QuaternionArray, () => new Quaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle())),
                DmxAttributeType.Matrix4x4Array => ReadTypedArray(KVValueType.Matrix4x4Array, ReadMatrix4x4),
                DmxAttributeType.UInt64Array => ReadTypedArray(KVValueType.UInt64Array, reader.ReadUInt64),
                DmxAttributeType.UInt8Array => ReadTypedArray(KVValueType.ByteArray, reader.ReadByte),

                _ => throw new KeyValueException($"Unhandled attribute type: {attrType}"),
            };
        }

        #endregion

        #region Value readers

        KV2Element ReadElementReference()
        {
            var index = reader.ReadInt32();

            if (index == ElementIndexNull)
            {
                return KV2Element.Null;
            }

            if (index == ElementIndexExternal)
            {
                return ReadStub();
            }

            if (index < 0 || index >= elements.Length)
            {
                throw new KeyValueException($"Element index {index} out of range (have {elements.Length} elements).");
            }

            return elements[index];
        }

        /// <summary>
        /// Reads an external element reference, which is an index of -2 followed by the target
        /// GUID as a null terminated string. The GUID is kept in a stub element so that it
        /// survives a round trip.
        /// </summary>
        KV2Element ReadStub()
        {
            var idString = ReadNullTerminatedString();

            if (!Guid.TryParse(idString, out var id))
            {
                throw new KeyValueException($"External element reference '{idString}' is not a valid GUID.");
            }

            if (!stubs.TryGetValue(id, out var stub))
            {
                stub = KV2Element.Stub(id);
                stubs.Add(id, stub);
            }

            return stub;
        }

        string ReadStringValue()
        {
            // Scalar string values only go through the string table from v4 onwards, before that
            // the table only holds class names and attribute names. Prefix attributes are read
            // before the table exists, so their strings are always inline.
            if (readingPrefix || !version.NamesInStringTable)
            {
                return ReadNullTerminatedString();
            }

            return ReadStringByIndex();
        }

        byte[] ReadBlob()
        {
            var length = ReadCount("binary blob length");

            // The length is not trusted until the bytes behind it have been read, so grow the
            // buffer in bounded steps rather than allocating what a corrupt file claims.
            var data = new List<byte>(InitialCapacity(length));
            var chunk = new byte[Math.Min(length, 64 * 1024)];
            var remaining = length;

            while (remaining > 0)
            {
                var read = reader.Read(chunk, 0, Math.Min(remaining, chunk.Length));

                // Read returns short at end of stream instead of throwing, which would silently
                // truncate a blob that runs off the end of the file.
                if (read == 0)
                {
                    throw new EndOfStreamException($"Binary blob is {length} bytes but only {length - remaining} remain.");
                }

                data.AddRange(chunk.AsSpan(0, read));
                remaining -= read;
            }

            return [.. data];
        }

        DmxColor ReadColor()
            => new(reader.ReadByte(), reader.ReadByte(), reader.ReadByte(), reader.ReadByte());

        Matrix4x4 ReadMatrix4x4()
        {
            return new Matrix4x4(
                reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(),
                reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(),
                reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(),
                reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        }

        KVObject ReadElementArray()
        {
            var count = ReadCount("element array");
            var list = new List<KV2Element>(InitialCapacity(count));

            for (var i = 0; i < count; i++)
            {
                list.Add(ReadElementReference());
            }

            return new KVObject(KVValueType.ElementArray, list);
        }

        KVObject ReadTypedArray<T>(KVValueType valueType, Func<T> readItem)
        {
            var count = ReadCount("array");
            var list = new List<T>(InitialCapacity(count));

            for (var i = 0; i < count; i++)
            {
                list.Add(readItem());
            }

            return new KVObject(valueType, list);
        }

        #endregion

        #region Utility

        int ReadCount(string what)
        {
            var count = reader.ReadInt32();

            if (count < 0)
            {
                throw new KeyValueException($"Negative {what} count: {count}");
            }

            return count;
        }

        /// <summary>
        /// Capacity to reserve for a count read from the file. The count is not trusted until the
        /// items behind it have been read, so a corrupt or hostile file cannot turn a small stream
        /// into a huge allocation. A truthful count just grows the list as it fills.
        /// </summary>
        static int InitialCapacity(int count) => Math.Min(count, 1024);

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
