using System.Globalization;
using System.Numerics;
using System.Text;
using ValveKeyValue.KeyValues2;

namespace ValveKeyValue.Deserialization.KeyValues2
{
    sealed class KV2TextReader : IDisposable
    {
        readonly TextReader textReader;
        readonly Dictionary<Guid, KV2Element> elements = [];
        readonly List<(KV2Element Owner, string AttributeName, Guid TargetId)> deferredReferences = [];
        readonly List<(List<KV2Element> List, int Index, Guid TargetId)> deferredArrayReferences = [];
        bool disposed;
        int depth;

        public KV2TextReader(TextReader textReader)
        {
            ArgumentNullException.ThrowIfNull(textReader);
            this.textReader = textReader;
        }

        public KVDocument Read()
        {
            ObjectDisposedException.ThrowIf(disposed, this);

            try
            {
                var header = ReadHeader();
                var topLevelElements = new List<KV2Element>();

                SkipWhitespaceAndComments();

                while (Peek() != -1)
                {
                    topLevelElements.Add(ReadElement());
                    SkipWhitespaceAndComments();
                }

                ResolveReferences();

                // A v4 document can start with a "$prefix_element$" block, the root is the first
                // block after it.
                KV2Element? prefixElement = null;

                if (topLevelElements.Count > 0 && topLevelElements[0].ClassName == KV2Element.PrefixElementClassName)
                {
                    prefixElement = topLevelElements[0];
                    topLevelElements.RemoveAt(0);
                }

                if (topLevelElements.Count == 0)
                {
                    throw new KeyValueException("No elements found in KV2 document.");
                }

                var root = topLevelElements[0];

                return new KV2Document(header, root.Name ?? string.Empty, root, prefixElement);
            }
            catch (Exception ex) when (ex is EndOfStreamException or IOException or FormatException or OverflowException or ArgumentException or InvalidDataException)
            {
                throw new KeyValueException("Error while reading KV2 text data.", ex);
            }
        }

        KVHeader ReadHeader()
        {
            // The header is a comment, terminated by "-->". Valve scans for it rather than
            // reading a line, so trailing content on the same line is allowed.
            SkipWhitespace();

            var sb = new StringBuilder(KV2Header.MaxHeaderLength);

            while (true)
            {
                if (sb.Length > KV2Header.MaxHeaderLength)
                {
                    throw new KeyValueException("Invalid KV2 header: no closing '-->' found.");
                }

                sb.Append(ReadChar());

                if (sb.Length >= 3 && sb[^3] == '-' && sb[^2] == '-' && sb[^1] == '>')
                {
                    break;
                }
            }

            var header = KV2Header.Parse(sb.ToString());
            KV2Header.ValidateEncoding(header, "keyvalues2", "keyvalues2_flat", "keyvalues2_noids");

            return header;
        }

        KV2Element ReadElement()
        {
            var className = ReadQuotedString();

            // Only a top level block can be the prefix container.
            return ReadElementBody(className, isPrefixContainer: className == KV2Element.PrefixElementClassName);
        }

        /// <summary>
        /// Reads a block that has already had its class name consumed. The prefix attribute
        /// container has no name member, so an attribute called "name" stays an ordinary attribute
        /// there, and nothing can reference it, so it is neither given a synthesized id nor added
        /// to the lookup: an empty id is how the binary writer knows the document has no second
        /// copy of the container.
        /// </summary>
        KV2Element ReadElementBody(string className, bool isPrefixContainer = false)
        {
            if (++depth > KV2Element.MaxNestingDepth)
            {
                throw new KeyValueException($"Elements are nested more than {KV2Element.MaxNestingDepth} deep.");
            }

            SkipWhitespaceAndComments();
            ExpectChar('{');
            SkipWhitespaceAndComments();

            var element = new KV2Element(className, string.Empty, Guid.Empty);

            while (Peek() != '}' && Peek() != -1)
            {
                ReadAttribute(element, hoistName: !isPrefixContainer);
                SkipWhitespaceAndComments();
            }

            ExpectChar('}');
            depth--;

            return isPrefixContainer ? element : Register(element);
        }

        /// <summary>
        /// Registers an element by its id. The keyvalues2_noids encoding omits the id of inlined
        /// elements, in which case a fresh one is generated because <see cref="Guid.Empty"/> is
        /// the null reference sentinel. When two elements share an id, Valve keeps the first one
        /// and redirects references to it.
        /// </summary>
        KV2Element Register(KV2Element element)
        {
            if (element.ElementId == Guid.Empty)
            {
                element.ElementId = Guid.NewGuid();
            }

            if (elements.TryGetValue(element.ElementId, out var existing))
            {
                return existing;
            }

            elements.Add(element.ElementId, element);
            return element;
        }

        void ReadAttribute(KV2Element element, bool hoistName)
        {
            var attributeName = ReadQuotedString();
            SkipWhitespaceAndComments();

            var typeName = ReadQuotedString();
            SkipWhitespaceAndComments();

            // Valve dispatches on the type token, and matches "elementid" case sensitively. Any
            // attribute of that type is consumed, but only "id" assigns the element id.
            if (typeName == "elementid")
            {
                var idString = ReadQuotedString();

                if (attributeName.Equals("id", StringComparison.OrdinalIgnoreCase))
                {
                    element.ElementId = ParseGuid(idString);
                }

                return;
            }

            // "unknown" is what the engine side writer emits for a null element reference.
            if (typeName == "unknown")
            {
                element.Add(attributeName, ReadElementReferenceValue(element, attributeName));
                return;
            }

            if (!DmxAttributeTypeHelper.TryGetTypeFromName(typeName, out var attributeType))
            {
                // Anything that is not a type name is the class name of an inline element.
                element.Add(attributeName, ReadElementBody(typeName));
                return;
            }

            if (attributeType == DmxAttributeType.Element)
            {
                element.Add(attributeName, ReadElementReferenceValue(element, attributeName));
                return;
            }

            if (attributeType == DmxAttributeType.ElementArray)
            {
                element.Add(attributeName, ReadElementArray());
                return;
            }

            if (DmxAttributeTypeHelper.IsArray(attributeType))
            {
                element.Add(attributeName, ReadTypedArray(attributeType));
                return;
            }

            var value = ParseTypedValue(attributeType, ReadQuotedString());

            // The element name is a built-in member in Valve's datamodel rather than an attribute.
            if (hoistName && attributeName == "name" && attributeType == DmxAttributeType.String)
            {
                element.Name = (string?)value._ref ?? string.Empty;
                return;
            }

            element.Add(attributeName, value);
        }

        KV2Element ReadElementReferenceValue(KV2Element owner, string attributeName)
        {
            var idString = ReadQuotedString();

            if (string.IsNullOrEmpty(idString))
            {
                return KV2Element.Null;
            }

            var targetId = ParseGuid(idString);

            if (elements.TryGetValue(targetId, out var existing))
            {
                return existing;
            }

            deferredReferences.Add((owner, attributeName, targetId));
            return KV2Element.Null;
        }

        KVObject ReadElementArray()
        {
            ExpectChar('[');
            SkipWhitespaceAndComments();

            var list = new List<KV2Element>();

            while (Peek() != ']' && Peek() != -1)
            {
                var itemTypeName = ReadQuotedString();
                SkipWhitespaceAndComments();

                if (itemTypeName.Equals("element", StringComparison.OrdinalIgnoreCase)
                    || itemTypeName.Equals("unknown", StringComparison.OrdinalIgnoreCase))
                {
                    var idString = ReadQuotedString();

                    if (string.IsNullOrEmpty(idString))
                    {
                        list.Add(KV2Element.Null);
                    }
                    else
                    {
                        var targetId = ParseGuid(idString);

                        if (elements.TryGetValue(targetId, out var existing))
                        {
                            list.Add(existing);
                        }
                        else
                        {
                            deferredArrayReferences.Add((list, list.Count, targetId));
                            list.Add(KV2Element.Null);
                        }
                    }
                }
                else
                {
                    list.Add(ReadElementBody(itemTypeName));
                }

                SkipWhitespaceAndComments();

                if (Peek() == ',')
                {
                    ReadChar();
                    SkipWhitespaceAndComments();
                }
            }

            ExpectChar(']');

            return new KVObject(KVValueType.ElementArray, list);
        }

        KVObject ReadTypedArray(DmxAttributeType attributeType)
        {
            ExpectChar('[');
            SkipWhitespaceAndComments();

            var values = new List<string>();

            while (Peek() != ']' && Peek() != -1)
            {
                values.Add(ReadQuotedString());
                SkipWhitespaceAndComments();

                if (Peek() == ',')
                {
                    ReadChar();
                    SkipWhitespaceAndComments();
                }
            }

            ExpectChar(']');

            return BuildTypedArray(attributeType, values);
        }

        void ResolveReferences()
        {
            foreach (var (owner, attributeName, targetId) in deferredReferences)
            {
                owner[attributeName] = Resolve(targetId);
            }

            foreach (var (list, index, targetId) in deferredArrayReferences)
            {
                list[index] = Resolve(targetId);
            }
        }

        /// <summary>
        /// Resolves an element id to the element it names. An id that is not in the document
        /// belongs to another file, which Valve turns into a stub handle rather than an error.
        /// </summary>
        KV2Element Resolve(Guid targetId)
        {
            if (!elements.TryGetValue(targetId, out var target))
            {
                target = KV2Element.Stub(targetId);
                elements.Add(targetId, target);
            }

            return target;
        }

        #region Value parsing

        static KVObject ParseTypedValue(DmxAttributeType type, string value) => type switch
        {
            DmxAttributeType.Int32 => new KVObject(ParseInt32(value)),
            DmxAttributeType.Float => new KVObject(ParseSingle(value)),
            DmxAttributeType.Bool => new KVObject(ParseBool(value)),
            DmxAttributeType.String => new KVObject(value),
            DmxAttributeType.BinaryBlob => KVObject.Blob(ParseBinaryBlob(value)),
            DmxAttributeType.Time => new KVObject(ParseTime(value)),
            DmxAttributeType.Color => new KVObject(ParseColor(value)),
            DmxAttributeType.Vector2 => new KVObject(ParseVector2(value)),
            DmxAttributeType.Vector3 => new KVObject(ParseVector3(value)),
            DmxAttributeType.Vector4 => new KVObject(ParseVector4(value)),
            DmxAttributeType.QAngle => new KVObject(ParseQAngle(value)),
            DmxAttributeType.Quaternion => new KVObject(ParseQuaternion(value)),
            DmxAttributeType.Matrix4x4 => new KVObject(ParseMatrix4x4(value)),
            DmxAttributeType.UInt8 => KVObject.Byte(byte.Parse(value, CultureInfo.InvariantCulture)),
            DmxAttributeType.UInt64 => new KVObject(ParseUInt64(value)),
            _ => throw new KeyValueException($"Unknown attribute type: {type}"),
        };

        static KVObject BuildTypedArray(DmxAttributeType type, List<string> values) => type switch
        {
            DmxAttributeType.Int32Array => new KVObject(KVValueType.Int32Array, values.ConvertAll(ParseInt32)),
            DmxAttributeType.FloatArray => new KVObject(KVValueType.FloatArray, values.ConvertAll(ParseSingle)),
            DmxAttributeType.BoolArray => new KVObject(KVValueType.BooleanArray, values.ConvertAll(ParseBool)),
            DmxAttributeType.StringArray => new KVObject(KVValueType.StringArray, values),
            DmxAttributeType.BinaryBlobArray => new KVObject(KVValueType.BinaryBlobArray, values.ConvertAll(ParseBinaryBlob)),
            DmxAttributeType.TimeArray => new KVObject(KVValueType.TimeSpanArray, values.ConvertAll(ParseTime)),
            DmxAttributeType.ColorArray => new KVObject(KVValueType.ColorArray, values.ConvertAll(ParseColor)),
            DmxAttributeType.Vector2Array => new KVObject(KVValueType.Vector2Array, values.ConvertAll(ParseVector2)),
            DmxAttributeType.Vector3Array => new KVObject(KVValueType.Vector3Array, values.ConvertAll(ParseVector3)),
            DmxAttributeType.Vector4Array => new KVObject(KVValueType.Vector4Array, values.ConvertAll(ParseVector4)),
            DmxAttributeType.QAngleArray => new KVObject(KVValueType.QAngleArray, values.ConvertAll(ParseQAngle)),
            DmxAttributeType.QuaternionArray => new KVObject(KVValueType.QuaternionArray, values.ConvertAll(ParseQuaternion)),
            DmxAttributeType.Matrix4x4Array => new KVObject(KVValueType.Matrix4x4Array, values.ConvertAll(ParseMatrix4x4)),
            DmxAttributeType.UInt8Array => new KVObject(KVValueType.ByteArray, values.ConvertAll(v => byte.Parse(v, CultureInfo.InvariantCulture))),
            DmxAttributeType.UInt64Array => new KVObject(KVValueType.UInt64Array, values.ConvertAll(ParseUInt64)),
            _ => throw new KeyValueException($"Unknown array type: {type}"),
        };

        static int ParseInt32(string value) => int.Parse(value, CultureInfo.InvariantCulture);

        static float ParseSingle(string value) => float.Parse(value, CultureInfo.InvariantCulture);

        /// <summary>
        /// Valve writes bools with <c>%d</c> and reads them back the same way, so any non zero
        /// number is true. Values written by other tools as "true" or "false" are accepted too.
        /// </summary>
        static bool ParseBool(string value)
        {
            if (bool.TryParse(value, out var parsed))
            {
                return parsed;
            }

            return int.Parse(value, CultureInfo.InvariantCulture) != 0;
        }

        /// <summary>
        /// Times are written as seconds with four decimals, but stored as tenths of milliseconds.
        /// </summary>
        static DmxTime ParseTime(string value)
        {
            var seconds = double.Parse(value, CultureInfo.InvariantCulture);
            var ticks = Math.Floor(seconds * DmxTime.TicksPerSecond + 0.5);

            if (!(ticks >= int.MinValue && ticks <= int.MaxValue))
            {
                throw new KeyValueException($"Time value '{value}' is out of range.");
            }

            return new DmxTime((int)ticks);
        }

        static DmxColor ParseColor(string value)
        {
            var parts = SplitComponents(value, 4, "color");
            return new DmxColor(
                byte.Parse(parts[0], CultureInfo.InvariantCulture),
                byte.Parse(parts[1], CultureInfo.InvariantCulture),
                byte.Parse(parts[2], CultureInfo.InvariantCulture),
                byte.Parse(parts[3], CultureInfo.InvariantCulture));
        }

        static Vector2 ParseVector2(string value)
        {
            var parts = SplitComponents(value, 2, "vector2");
            return new Vector2(ParseSingle(parts[0]), ParseSingle(parts[1]));
        }

        static Vector3 ParseVector3(string value)
        {
            var parts = SplitComponents(value, 3, "vector3");
            return new Vector3(ParseSingle(parts[0]), ParseSingle(parts[1]), ParseSingle(parts[2]));
        }

        static Vector4 ParseVector4(string value)
        {
            var parts = SplitComponents(value, 4, "vector4");
            return new Vector4(ParseSingle(parts[0]), ParseSingle(parts[1]), ParseSingle(parts[2]), ParseSingle(parts[3]));
        }

        static QAngle ParseQAngle(string value)
        {
            var parts = SplitComponents(value, 3, "qangle");
            return new QAngle(ParseSingle(parts[0]), ParseSingle(parts[1]), ParseSingle(parts[2]));
        }

        static Quaternion ParseQuaternion(string value)
        {
            var parts = SplitComponents(value, 4, "quaternion");
            return new Quaternion(ParseSingle(parts[0]), ParseSingle(parts[1]), ParseSingle(parts[2]), ParseSingle(parts[3]));
        }

        static Matrix4x4 ParseMatrix4x4(string value)
        {
            var p = SplitComponents(value, 16, "matrix");
            return new Matrix4x4(
                ParseSingle(p[0]), ParseSingle(p[1]), ParseSingle(p[2]), ParseSingle(p[3]),
                ParseSingle(p[4]), ParseSingle(p[5]), ParseSingle(p[6]), ParseSingle(p[7]),
                ParseSingle(p[8]), ParseSingle(p[9]), ParseSingle(p[10]), ParseSingle(p[11]),
                ParseSingle(p[12]), ParseSingle(p[13]), ParseSingle(p[14]), ParseSingle(p[15]));
        }

        static ulong ParseUInt64(string value)
        {
            if (value.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                return ulong.Parse(value.AsSpan(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            }

            return ulong.Parse(value, CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Valve reads multi component values with <c>scanf</c>, so any run of whitespace
        /// separates two components. Matrices in particular are written across several lines.
        /// </summary>
        static string[] SplitComponents(string value, int expected, string typeName)
        {
            var parts = value.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (parts.Length != expected)
            {
                throw new KeyValueException($"Expected {expected} components for a {typeName} value, got {parts.Length}.");
            }

            return parts;
        }

        /// <summary>
        /// Binary blobs are written as hex spanning several lines, with the rows indented.
        /// </summary>
        static byte[] ParseBinaryBlob(string value)
        {
            var hex = new StringBuilder(value.Length);

            foreach (var c in value)
            {
                if (!char.IsWhiteSpace(c))
                {
                    hex.Append(c);
                }
            }

            return HexStringHelper.ParseHexStringAsByteArray(hex.ToString());
        }

        static Guid ParseGuid(string value)
        {
            if (!Guid.TryParse(value, out var id))
            {
                throw new KeyValueException($"'{value}' is not a valid element id.");
            }

            return id;
        }

        #endregion

        #region Low-level text parsing

        string ReadQuotedString()
        {
            SkipWhitespaceAndComments();
            ExpectChar('"');

            var sb = new StringBuilder();

            while (true)
            {
                var c = ReadChar();

                if (c == '"')
                {
                    break;
                }

                if (c == '\\')
                {
                    // The full set that Valve's CUtlBuffer conversion emits. There are no
                    // multi character escapes.
                    var escaped = ReadChar();
                    sb.Append(escaped switch
                    {
                        'n' => '\n',
                        't' => '\t',
                        'v' => '\v',
                        'b' => '\b',
                        'r' => '\r',
                        'f' => '\f',
                        'a' => '\a',
                        '\\' => '\\',
                        '?' => '?',
                        '\'' => '\'',
                        '"' => '"',
                        _ => throw new KeyValueException($"Unknown escape sequence: \\{escaped}"),
                    });
                }
                else
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }

        int Peek() => textReader.Peek();

        char ReadChar()
        {
            var next = textReader.Read();

            if (next == -1)
            {
                throw new EndOfStreamException("Unexpected end of KV2 text data.");
            }

            return (char)next;
        }

        void ExpectChar(char expected)
        {
            var c = ReadChar();

            if (c != expected)
            {
                throw new KeyValueException($"Expected '{expected}' but got '{c}'.");
            }
        }

        void SkipWhitespace()
        {
            int next;

            while ((next = Peek()) != -1 && KV2Header.IsSpace((char)next))
            {
                textReader.Read();
            }
        }

        void SkipWhitespaceAndComments()
        {
            while (true)
            {
                SkipWhitespace();

                if (Peek() != '/')
                {
                    break;
                }

                textReader.Read();

                if (Peek() != '/')
                {
                    throw new KeyValueException("Unexpected '/' character, only '//' comments are supported.");
                }

                textReader.Read();

                int next;

                while ((next = textReader.Read()) != -1 && next != '\n')
                {
                    // Comments run to the end of the line.
                }
            }
        }

        #endregion

        public void Dispose()
        {
            if (!disposed)
            {
                textReader.Dispose();
                disposed = true;
            }
        }
    }
}
