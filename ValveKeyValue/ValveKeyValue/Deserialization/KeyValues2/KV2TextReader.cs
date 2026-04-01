using System.Globalization;
using System.Numerics;
using System.Text;
using ValveKeyValue.KeyValues3;

namespace ValveKeyValue.Deserialization.KeyValues2
{
    sealed class KV2TextReader : IDisposable
    {
        readonly TextReader textReader;
        readonly Dictionary<Guid, KV2Element> elements = [];
        readonly List<(KV2Element owner, string attrName, Guid targetId)> deferredReferences = [];
        readonly List<(List<KV2Element> list, int index, Guid targetId)> deferredArrayReferences = [];
        bool disposed;

        public KV2TextReader(TextReader textReader)
        {
            ArgumentNullException.ThrowIfNull(textReader);
            this.textReader = textReader;
        }

        public KVDocument Read()
        {
            ObjectDisposedException.ThrowIf(disposed, this);

            var header = ReadHeader();
            var topLevelElements = new List<KV2Element>();

            SkipWhitespaceAndComments();

            while (Peek() != -1)
            {
                var element = ReadElement();
                topLevelElements.Add(element);
                SkipWhitespaceAndComments();
            }

            ResolveReferences();

            if (topLevelElements.Count == 0)
            {
                throw new KeyValueException("No elements found in KV2 document.");
            }

            var root = topLevelElements[0];
            return new KVDocument(header, root.Name ?? string.Empty, root);
        }

        KVHeader ReadHeader()
        {
            // Header: <!-- dmx encoding keyvalues2 N format FORMATNAME V -->
            SkipWhitespace();

            var line = ReadLine();

            if (line == null || !line.StartsWith("<!-- dmx encoding ", StringComparison.Ordinal))
            {
                throw new KeyValueException("Invalid KV2 header: expected '<!-- dmx encoding ...'");
            }

            if (!line.EndsWith(" -->", StringComparison.Ordinal))
            {
                throw new KeyValueException("Invalid KV2 header: expected closing ' -->'");
            }

            // Parse: <!-- dmx encoding keyvalues2 1 format dmx 1 -->
            var content = line.AsSpan()["<!-- dmx encoding ".Length..^" -->".Length];

            // Split into parts: "keyvalues2 1 format dmx 1"
            var firstSpace = content.IndexOf(' ');
            if (firstSpace < 0)
            {
                throw new KeyValueException("Invalid KV2 header: missing encoding version.");
            }

            var encodingName = content[..firstSpace].ToString();
            content = content[(firstSpace + 1)..];

            // Parse encoding version
            var secondSpace = content.IndexOf(' ');
            if (secondSpace < 0)
            {
                throw new KeyValueException("Invalid KV2 header: missing 'format' keyword.");
            }

            if (!int.TryParse(content[..secondSpace], CultureInfo.InvariantCulture, out var encodingVersion))
            {
                throw new KeyValueException("Invalid KV2 header: encoding version is not a number.");
            }

            content = content[(secondSpace + 1)..];

            // Expect "format"
            if (!content.StartsWith("format ", StringComparison.Ordinal))
            {
                throw new KeyValueException("Invalid KV2 header: expected 'format' keyword.");
            }

            content = content["format ".Length..];

            // Parse format name and version
            var lastSpace = content.LastIndexOf(' ');
            if (lastSpace < 0)
            {
                throw new KeyValueException("Invalid KV2 header: missing format version.");
            }

            var formatName = content[..lastSpace].ToString();

            if (!int.TryParse(content[(lastSpace + 1)..], CultureInfo.InvariantCulture, out var formatVersion))
            {
                throw new KeyValueException("Invalid KV2 header: format version is not a number.");
            }

            return new KVHeader
            {
                Encoding = new KV3ID(encodingName, Version: encodingVersion),
                Format = new KV3ID(formatName, Version: formatVersion),
            };
        }

        KV2Element ReadElement()
        {
            var className = ReadQuotedString();
            return ReadElementBody(className);
        }

        void ReadAttribute(KV2Element element)
        {
            var attrName = ReadQuotedString();
            SkipWhitespaceAndComments();

            // Must be a quoted type name or inline element class name
            var typeName = ReadQuotedString();
            SkipWhitespaceAndComments();

            var next = Peek();

            // Check for pseudo-attributes
            if (attrName == "id" && typeName == "elementid")
            {
                var guidStr = ReadQuotedString();
                SkipWhitespaceAndComments();
                element.ElementId = Guid.Parse(guidStr);

                if (element.ElementId != Guid.Empty)
                {
                    elements[element.ElementId] = element;
                }

                return;
            }

            if (attrName == "name" && typeName == "string")
            {
                var nameValue = ReadQuotedString();
                SkipWhitespaceAndComments();
                element.Name = nameValue;
                return;
            }

            // Inline element: "attr" "ClassName" { ... }
            if (next == '{')
            {
                var inlineElement = ReadElementBody(typeName);
                element.Add(attrName, inlineElement);
                return;
            }

            // Element array: "attr" "element_array" [ ... ]
            if (next == '[' && typeName == "element_array")
            {
                ReadElementArray(element, attrName);
                return;
            }

            // Typed array: "attr" "type_array" [ ... ]
            if (next == '[')
            {
                var value = ReadTypedArray(typeName);
                element.Add(attrName, value);
                return;
            }

            // Element reference: "attr" "element" "GUID" or "attr" "element" ""
            if (typeName == "element")
            {
                var guidStr = ReadQuotedString();
                SkipWhitespaceAndComments();

                if (string.IsNullOrEmpty(guidStr))
                {
                    element.Add(attrName, KV2Element.Null);
                }
                else
                {
                    var targetId = Guid.Parse(guidStr);

                    if (elements.TryGetValue(targetId, out var existing))
                    {
                        element.Add(attrName, existing);
                    }
                    else
                    {
                        // Defer
                        deferredReferences.Add((element, attrName, targetId));
                        element.Add(attrName, KV2Element.Null); // placeholder
                    }
                }

                return;
            }

            // Normal typed attribute: "attr" "type" "value"
            var attrValueStr = ReadQuotedString();
            SkipWhitespaceAndComments();

            var attrValue = ParseTypedValue(typeName, attrValueStr);
            element.Add(attrName, attrValue);
        }

        KV2Element ReadElementBody(string className)
        {
            SkipWhitespaceAndComments();
            ExpectChar('{');
            SkipWhitespaceAndComments();

            var element = new KV2Element(className, string.Empty, Guid.Empty);

            while (Peek() != '}' && Peek() != -1)
            {
                ReadAttribute(element);
                SkipWhitespaceAndComments();
            }

            ExpectChar('}');

            if (element.ElementId != Guid.Empty)
            {
                elements[element.ElementId] = element;
            }

            return element;
        }

        void ReadElementArray(KV2Element element, string attrName)
        {
            ExpectChar('[');
            SkipWhitespaceAndComments();

            var list = new List<KV2Element>();

            while (Peek() != ']' && Peek() != -1)
            {
                var itemTypeName = ReadQuotedString();
                SkipWhitespaceAndComments();

                if (itemTypeName == "element")
                {
                    // GUID reference in array
                    var guidStr = ReadQuotedString();
                    SkipWhitespaceAndComments();

                    if (string.IsNullOrEmpty(guidStr))
                    {
                        list.Add(KV2Element.Null);
                    }
                    else
                    {
                        var targetId = Guid.Parse(guidStr);

                        if (elements.TryGetValue(targetId, out var existing))
                        {
                            list.Add(existing);
                        }
                        else
                        {
                            var index = list.Count;
                            list.Add(KV2Element.Null); // placeholder
                            deferredArrayReferences.Add((list, index, targetId));
                        }
                    }
                }
                else
                {
                    // Inline element in array
                    var inlineElement = ReadElementBody(itemTypeName);
                    list.Add(inlineElement);
                }

                // Skip optional comma
                SkipWhitespaceAndComments();
                if (Peek() == ',')
                {
                    ReadChar();
                    SkipWhitespaceAndComments();
                }
            }

            ExpectChar(']');
            element.Add(attrName, new KVObject(KVValueType.ElementArray, list));
        }

        KVObject ReadTypedArray(string typeName)
        {
            ExpectChar('[');
            SkipWhitespaceAndComments();

            // Determine element type from array type name
            var (valueType, elementTypeName) = ParseArrayTypeName(typeName);

            var values = new List<string>();

            while (Peek() != ']' && Peek() != -1)
            {
                var val = ReadQuotedString();
                values.Add(val);
                SkipWhitespaceAndComments();

                // Skip optional comma
                if (Peek() == ',')
                {
                    ReadChar();
                    SkipWhitespaceAndComments();
                }
            }

            ExpectChar(']');

            return BuildTypedArray(valueType, elementTypeName, values);
        }

        static (KVValueType, string) ParseArrayTypeName(string typeName) => typeName switch
        {
            "int_array" => (KVValueType.Int32Array, "int"),
            "float_array" => (KVValueType.FloatArray, "float"),
            "bool_array" => (KVValueType.BooleanArray, "bool"),
            "string_array" => (KVValueType.StringArray, "string"),
            "binary_array" or "vdata_array" => (KVValueType.BinaryBlobArray, "binary"),
            "time_array" => (KVValueType.TimeSpanArray, "time"),
            "color_array" => (KVValueType.ColorArray, "color"),
            "vector2_array" => (KVValueType.Vector2Array, "vector2"),
            "vector3_array" => (KVValueType.Vector3Array, "vector3"),
            "vector4_array" => (KVValueType.Vector4Array, "vector4"),
            "qangle_array" => (KVValueType.QAngleArray, "qangle"),
            "quaternion_array" => (KVValueType.QuaternionArray, "quaternion"),
            "matrix_array" => (KVValueType.Matrix4x4Array, "matrix"),
            "uint8_array" => (KVValueType.ByteArray, "uint8"),
            "uint64_array" => (KVValueType.UInt64Array, "uint64"),
            _ => throw new KeyValueException($"Unknown array type: {typeName}"),
        };

        static KVObject BuildTypedArray(KVValueType valueType, string elementTypeName, List<string> values)
        {
            return valueType switch
            {
                KVValueType.Int32Array => new KVObject(valueType, values.ConvertAll(v => int.Parse(v, CultureInfo.InvariantCulture))),
                KVValueType.FloatArray => new KVObject(valueType, values.ConvertAll(v => float.Parse(v, CultureInfo.InvariantCulture))),
                KVValueType.BooleanArray => new KVObject(valueType, values.ConvertAll(v => v == "1" || v.Equals("true", StringComparison.OrdinalIgnoreCase))),
                KVValueType.StringArray => new KVObject(valueType, values),
                KVValueType.BinaryBlobArray => new KVObject(valueType, values.ConvertAll(v => HexStringHelper.ParseHexStringAsByteArray(v))),
                KVValueType.TimeSpanArray => new KVObject(valueType, values.ConvertAll(v => new DmxTime(int.Parse(v, CultureInfo.InvariantCulture)))),
                KVValueType.ColorArray => new KVObject(valueType, values.ConvertAll(ParseColor)),
                KVValueType.Vector2Array => new KVObject(valueType, values.ConvertAll(ParseVector2)),
                KVValueType.Vector3Array => new KVObject(valueType, values.ConvertAll(ParseVector3)),
                KVValueType.Vector4Array => new KVObject(valueType, values.ConvertAll(ParseVector4)),
                KVValueType.QAngleArray => new KVObject(valueType, values.ConvertAll(ParseQAngle)),
                KVValueType.QuaternionArray => new KVObject(valueType, values.ConvertAll(ParseQuaternion)),
                KVValueType.Matrix4x4Array => new KVObject(valueType, values.ConvertAll(ParseMatrix4x4)),
                KVValueType.ByteArray => new KVObject(valueType, values.ConvertAll(v => byte.Parse(v, CultureInfo.InvariantCulture))),
                KVValueType.UInt64Array => new KVObject(valueType, values.ConvertAll(v => ulong.Parse(v, CultureInfo.InvariantCulture))),
                _ => throw new KeyValueException($"Unhandled array value type: {valueType}"),
            };
        }

        static KVObject ParseTypedValue(string typeName, string valueStr) => typeName switch
        {
            "int" => new KVObject(int.Parse(valueStr, CultureInfo.InvariantCulture)),
            "float" => new KVObject(float.Parse(valueStr, CultureInfo.InvariantCulture)),
            "bool" => new KVObject(valueStr == "1" || valueStr.Equals("true", StringComparison.OrdinalIgnoreCase)),
            "string" => new KVObject(valueStr),
            "binary" or "vdata" => KVObject.Blob(ParseBinaryBlob(valueStr)),
            "time" => new KVObject(new DmxTime(int.Parse(valueStr, CultureInfo.InvariantCulture))),
            "color" => new KVObject(ParseColor(valueStr)),
            "vector2" => new KVObject(ParseVector2(valueStr)),
            "vector3" => new KVObject(ParseVector3(valueStr)),
            "vector4" => new KVObject(ParseVector4(valueStr)),
            "qangle" => new KVObject(ParseQAngle(valueStr)),
            "quaternion" => new KVObject(ParseQuaternion(valueStr)),
            "matrix" => new KVObject(ParseMatrix4x4(valueStr)),
            "uint8" => KVObject.Byte(byte.Parse(valueStr, CultureInfo.InvariantCulture)),
            "uint64" => new KVObject(ParseUInt64(valueStr)),
            _ => throw new KeyValueException($"Unknown attribute type: {typeName}"),
        };

        static DmxColor ParseColor(string s)
        {
            var parts = s.Split(' ');
            return new DmxColor(
                byte.Parse(parts[0], CultureInfo.InvariantCulture),
                byte.Parse(parts[1], CultureInfo.InvariantCulture),
                byte.Parse(parts[2], CultureInfo.InvariantCulture),
                byte.Parse(parts[3], CultureInfo.InvariantCulture));
        }

        static Vector2 ParseVector2(string s)
        {
            var parts = s.Split(' ');
            return new Vector2(
                float.Parse(parts[0], CultureInfo.InvariantCulture),
                float.Parse(parts[1], CultureInfo.InvariantCulture));
        }

        static Vector3 ParseVector3(string s)
        {
            var parts = s.Split(' ');
            return new Vector3(
                float.Parse(parts[0], CultureInfo.InvariantCulture),
                float.Parse(parts[1], CultureInfo.InvariantCulture),
                float.Parse(parts[2], CultureInfo.InvariantCulture));
        }

        static Vector4 ParseVector4(string s)
        {
            var parts = s.Split(' ');
            return new Vector4(
                float.Parse(parts[0], CultureInfo.InvariantCulture),
                float.Parse(parts[1], CultureInfo.InvariantCulture),
                float.Parse(parts[2], CultureInfo.InvariantCulture),
                float.Parse(parts[3], CultureInfo.InvariantCulture));
        }

        static QAngle ParseQAngle(string s)
        {
            var parts = s.Split(' ');
            return new QAngle(
                float.Parse(parts[0], CultureInfo.InvariantCulture),
                float.Parse(parts[1], CultureInfo.InvariantCulture),
                float.Parse(parts[2], CultureInfo.InvariantCulture));
        }

        static Quaternion ParseQuaternion(string s)
        {
            var parts = s.Split(' ');
            return new Quaternion(
                float.Parse(parts[0], CultureInfo.InvariantCulture),
                float.Parse(parts[1], CultureInfo.InvariantCulture),
                float.Parse(parts[2], CultureInfo.InvariantCulture),
                float.Parse(parts[3], CultureInfo.InvariantCulture));
        }

        static Matrix4x4 ParseMatrix4x4(string s)
        {
            var parts = s.Split(' ');
            return new Matrix4x4(
                float.Parse(parts[0], CultureInfo.InvariantCulture),
                float.Parse(parts[1], CultureInfo.InvariantCulture),
                float.Parse(parts[2], CultureInfo.InvariantCulture),
                float.Parse(parts[3], CultureInfo.InvariantCulture),
                float.Parse(parts[4], CultureInfo.InvariantCulture),
                float.Parse(parts[5], CultureInfo.InvariantCulture),
                float.Parse(parts[6], CultureInfo.InvariantCulture),
                float.Parse(parts[7], CultureInfo.InvariantCulture),
                float.Parse(parts[8], CultureInfo.InvariantCulture),
                float.Parse(parts[9], CultureInfo.InvariantCulture),
                float.Parse(parts[10], CultureInfo.InvariantCulture),
                float.Parse(parts[11], CultureInfo.InvariantCulture),
                float.Parse(parts[12], CultureInfo.InvariantCulture),
                float.Parse(parts[13], CultureInfo.InvariantCulture),
                float.Parse(parts[14], CultureInfo.InvariantCulture),
                float.Parse(parts[15], CultureInfo.InvariantCulture));
        }

        static ulong ParseUInt64(string s)
        {
            if (s.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
            {
                return ulong.Parse(s.AsSpan(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            }

            return ulong.Parse(s, CultureInfo.InvariantCulture);
        }

        static byte[] ParseBinaryBlob(string s)
        {
            // Binary blobs in KV2 text can span multiple lines with whitespace
            var hex = new StringBuilder(s.Length);
            foreach (var c in s)
            {
                if (!char.IsWhiteSpace(c))
                {
                    hex.Append(c);
                }
            }

            return HexStringHelper.ParseHexStringAsByteArray(hex.ToString());
        }

        void ResolveReferences()
        {
            foreach (var (owner, attrName, targetId) in deferredReferences)
            {
                if (elements.TryGetValue(targetId, out var target))
                {
                    owner[attrName] = target;
                }
                else
                {
                    throw new KeyValueException($"Unresolved element reference: {targetId}");
                }
            }

            foreach (var (list, index, targetId) in deferredArrayReferences)
            {
                if (elements.TryGetValue(targetId, out var target))
                {
                    list[index] = target;
                }
                else
                {
                    throw new KeyValueException($"Unresolved element reference in array: {targetId}");
                }
            }
        }

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
                    var escaped = ReadChar();
                    sb.Append(escaped switch
                    {
                        'n' => '\n',
                        't' => '\t',
                        '\\' => '\\',
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

        string ReadLine() => textReader.ReadLine();

        int Peek() => textReader.Peek();

        char ReadChar()
        {
            var next = textReader.Read();
            if (next == -1)
            {
                throw new EndOfStreamException("Unexpected end of stream.");
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
            while ((next = Peek()) != -1 && char.IsWhiteSpace((char)next))
            {
                ReadChar();
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

                // Check for // comment
                textReader.Read(); // consume first /
                if (Peek() == '/')
                {
                    textReader.Read(); // consume second /
                    // Skip until end of line
                    while (true)
                    {
                        var next = textReader.Read();
                        if (next == -1 || next == '\n')
                        {
                            break;
                        }
                    }
                }
                else
                {
                    throw new KeyValueException("Unexpected '/' character.");
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
