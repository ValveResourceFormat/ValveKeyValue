using System.Globalization;
using ValveKeyValue.Abstraction;

namespace ValveKeyValue.Serialization.KeyValues2
{
    sealed class KV2TextWriter : IVisitationListener
    {
        readonly StreamWriter writer;
        int indentation;

        readonly HashSet<KV2Element> written = new(ReferenceEqualityComparer.Instance);
        int skipDepth;

        public KV2TextWriter(Stream stream, KVHeader header = null)
        {
            ArgumentNullException.ThrowIfNull(stream);

            writer = new StreamWriter(stream, new System.Text.UTF8Encoding(), bufferSize: 1024, leaveOpen: true)
            {
                NewLine = "\n"
            };

            WriteHeader(header);
        }

        #region IVisitationListener

        public bool OnObjectStart(string name, KVFlag flag, KVObject obj)
        {
            if (obj is KV2Element elem)
            {
                if (IsNullElement(elem))
                {
                    WriteElementReference(name, string.Empty);
                    skipDepth++;
                    return true;
                }

                if (written.Contains(elem))
                {
                    WriteElementReference(name, elem.ElementId.ToString());
                    skipDepth++;
                    return true;
                }

                // First encounter — write inline
                WriteIndentation();

                if (name != null)
                {
                    WriteQuotedString(name);
                    writer.Write(' ');
                }

                WriteElementHeader(elem);
                written.Add(elem);
                return false;
            }

            // KVDocument root — write the root element's header
            if (obj is KVDocument doc && doc.Root is KV2Element rootElem)
            {
                WriteElementHeader(rootElem);
                written.Add(rootElem);
                return false;
            }

            return false;
        }

        public void OnObjectEnd()
        {
            if (skipDepth > 0)
            {
                skipDepth--;
                return;
            }

            indentation--;
            WriteIndentation();
            writer.Write('}');
            writer.WriteLine();
        }

        public void OnKeyValuePair(string name, KVObject value)
        {
            if (value is KV2Element elem)
            {
                if (IsNullElement(elem))
                {
                    WriteElementReference(name, string.Empty);
                }

                return;
            }

            WriteIndentation();
            WriteQuotedString(name);
            writer.Write(' ');

            if (value.IsTypedArray)
            {
                WriteTypedArray(value);
                return;
            }

            WriteQuotedString(GetTypeName(value.ValueType));
            writer.Write(' ');
            WriteQuotedString(FormatScalarValue(value));
            writer.WriteLine();
        }

        public void OnArrayStart(string name, KVFlag flag, int elementCount, bool allSimpleElements) { }
        public void OnArrayValue(KVObject value) { }
        public void OnArrayEnd() { }

        #endregion

        #region Element writing

        void WriteElementReference(string name, string guidStr)
        {
            WriteIndentation();
            WriteQuotedString(name);
            writer.Write(' ');
            WriteQuotedString("element");
            writer.Write(' ');
            WriteQuotedString(guidStr);
            writer.WriteLine();
        }

        void WriteElementHeader(KV2Element element)
        {
            WriteQuotedString(element.ClassName);
            writer.WriteLine();
            WriteIndentation();
            writer.Write('{');
            writer.WriteLine();
            indentation++;

            WriteIndentation();
            WriteQuotedString("id");
            writer.Write(' ');
            WriteQuotedString("elementid");
            writer.Write(' ');
            WriteQuotedString(element.ElementId.ToString());
            writer.WriteLine();

            WriteIndentation();
            WriteQuotedString("name");
            writer.Write(' ');
            WriteQuotedString("string");
            writer.Write(' ');
            WriteQuotedString(element.Name ?? string.Empty);
            writer.WriteLine();
        }

        /// <summary>
        /// Writes a complete element inline. Used by element arrays for items
        /// that the visitor doesn't traverse directly.
        /// </summary>
        void WriteInlineElement(KV2Element element)
        {
            WriteElementHeader(element);
            written.Add(element);

            foreach (var (key, child) in element.Children)
            {
                if (child is KV2Element childElem)
                {
                    if (IsNullElement(childElem))
                    {
                        WriteElementReference(key, string.Empty);
                    }
                    else if (written.Contains(childElem))
                    {
                        WriteElementReference(key, childElem.ElementId.ToString());
                    }
                    else
                    {
                        WriteIndentation();
                        WriteQuotedString(key);
                        writer.Write(' ');
                        WriteInlineElement(childElem);
                    }
                }
                else
                {
                    OnKeyValuePair(key, child);
                }
            }

            indentation--;
            WriteIndentation();
            writer.Write('}');
            writer.WriteLine();
        }

        static bool IsNullElement(KV2Element elem)
            => ReferenceEquals(elem, KV2Element.Null) || elem.ElementId == Guid.Empty;

        #endregion

        #region Typed arrays

        void WriteTypedArray(KVObject value)
        {
            if (value.ValueType == KVValueType.ElementArray)
            {
                WriteElementArray(value);
                return;
            }

            WriteQuotedString(GetArrayTypeName(value.ValueType));
            writer.WriteLine();
            WriteIndentation();
            writer.Write('[');
            writer.WriteLine();
            indentation++;

            WriteTypedArrayItems(value);

            indentation--;
            WriteIndentation();
            writer.Write(']');
            writer.WriteLine();
        }

        void WriteElementArray(KVObject value)
        {
            WriteQuotedString("element_array");
            writer.WriteLine();
            WriteIndentation();
            writer.Write('[');
            writer.WriteLine();
            indentation++;

            var list = value.GetArray<KV2Element>();
            for (var i = 0; i < list.Count; i++)
            {
                var item = list[i];

                if (item == null || IsNullElement(item))
                {
                    WriteIndentation();
                    WriteQuotedString("element");
                    writer.Write(' ');
                    WriteQuotedString(string.Empty);
                    writer.WriteLine();
                }
                else if (written.Contains(item))
                {
                    WriteIndentation();
                    WriteQuotedString("element");
                    writer.Write(' ');
                    WriteQuotedString(item.ElementId.ToString());
                    writer.WriteLine();
                }
                else
                {
                    WriteIndentation();
                    WriteInlineElement(item);
                }

                if (i < list.Count - 1)
                {
                    writer.Write(',');
                }
            }

            indentation--;
            WriteIndentation();
            writer.Write(']');
            writer.WriteLine();
        }

        void WriteTypedArrayItems(KVObject value)
        {
            switch (value.ValueType)
            {
                case KVValueType.Int32Array:
                    WriteListItems(value.GetArray<int>(), v => v.ToString(CultureInfo.InvariantCulture));
                    break;
                case KVValueType.FloatArray:
                    WriteListItems(value.GetArray<float>(), v => v.ToString(CultureInfo.InvariantCulture));
                    break;
                case KVValueType.BooleanArray:
                    WriteListItems(value.GetArray<bool>(), v => v ? "1" : "0");
                    break;
                case KVValueType.StringArray:
                    WriteListItems(value.GetArray<string>(), v => v);
                    break;
                case KVValueType.BinaryBlobArray:
                    WriteListItems(value.GetArray<byte[]>(), HexStringHelper.ByteArrayToHexString);
                    break;
                case KVValueType.TimeSpanArray:
                    WriteListItems(value.GetArray<DmxTime>(), v => v.Ticks.ToString(CultureInfo.InvariantCulture));
                    break;
                case KVValueType.ColorArray:
                    WriteListItems(value.GetArray<DmxColor>(), v => $"{v.R} {v.G} {v.B} {v.A}");
                    break;
                case KVValueType.Vector2Array:
                case KVValueType.Vector3Array:
                case KVValueType.Vector4Array:
                case KVValueType.QAngleArray:
                case KVValueType.QuaternionArray:
                case KVValueType.Matrix4x4Array:
                    WriteListItemsFromKVObject(value);
                    break;
                case KVValueType.ByteArray:
                    WriteListItems(value.GetArray<byte>(), v => v.ToString(CultureInfo.InvariantCulture));
                    break;
                case KVValueType.UInt64Array:
                    WriteListItems(value.GetArray<ulong>(), v => v.ToString(CultureInfo.InvariantCulture));
                    break;
            }
        }

        void WriteListItemsFromKVObject(KVObject value)
        {
            var list = (System.Collections.IList)value._ref;
            for (var i = 0; i < list.Count; i++)
            {
                WriteIndentation();
                var item = new KVObject(value.ValueType switch
                {
                    KVValueType.Vector2Array => KVValueType.Vector2,
                    KVValueType.Vector3Array => KVValueType.Vector3,
                    KVValueType.Vector4Array => KVValueType.Vector4,
                    KVValueType.QAngleArray => KVValueType.QAngle,
                    KVValueType.QuaternionArray => KVValueType.Quaternion,
                    KVValueType.Matrix4x4Array => KVValueType.Matrix4x4,
                    _ => throw new KeyValueException($"Unexpected array type: {value.ValueType}"),
                }, 0, list[i], KVFlag.None);
                WriteQuotedString(item.ToString(CultureInfo.InvariantCulture));

                if (i < list.Count - 1)
                {
                    writer.Write(',');
                }

                writer.WriteLine();
            }
        }

        void WriteListItems<T>(List<T> list, Func<T, string> formatter)
        {
            for (var i = 0; i < list.Count; i++)
            {
                WriteIndentation();
                WriteQuotedString(formatter(list[i]));

                if (i < list.Count - 1)
                {
                    writer.Write(',');
                }

                writer.WriteLine();
            }
        }

        #endregion

        #region Formatting helpers

        static string FormatScalarValue(KVObject value) => value.ValueType switch
        {
            KVValueType.Boolean => value.ToBoolean(null) ? "1" : "0",
            KVValueType.BinaryBlob => HexStringHelper.ByteArrayToHexString(value.AsBlob()),
            _ => value.ToString(CultureInfo.InvariantCulture),
        };

        static string GetTypeName(KVValueType type) => type switch
        {
            KVValueType.Int32 => "int",
            KVValueType.FloatingPoint => "float",
            KVValueType.Boolean => "bool",
            KVValueType.String => "string",
            KVValueType.BinaryBlob => "binary",
            KVValueType.TimeSpan => "time",
            KVValueType.Color => "color",
            KVValueType.Vector2 => "vector2",
            KVValueType.Vector3 => "vector3",
            KVValueType.Vector4 => "vector4",
            KVValueType.QAngle => "qangle",
            KVValueType.Quaternion => "quaternion",
            KVValueType.Matrix4x4 => "matrix",
            KVValueType.Byte => "uint8",
            KVValueType.UInt64 => "uint64",
            _ => throw new KeyValueException($"No DMX type name for: {type}"),
        };

        static string GetArrayTypeName(KVValueType type) => type switch
        {
            KVValueType.Int32Array => "int_array",
            KVValueType.FloatArray => "float_array",
            KVValueType.BooleanArray => "bool_array",
            KVValueType.StringArray => "string_array",
            KVValueType.BinaryBlobArray => "binary_array",
            KVValueType.TimeSpanArray => "time_array",
            KVValueType.ColorArray => "color_array",
            KVValueType.Vector2Array => "vector2_array",
            KVValueType.Vector3Array => "vector3_array",
            KVValueType.Vector4Array => "vector4_array",
            KVValueType.QAngleArray => "qangle_array",
            KVValueType.QuaternionArray => "quaternion_array",
            KVValueType.Matrix4x4Array => "matrix_array",
            KVValueType.ByteArray => "uint8_array",
            KVValueType.UInt64Array => "uint64_array",
            _ => throw new KeyValueException($"No DMX array type name for: {type}"),
        };

        void WriteQuotedString(string text)
        {
            writer.Write('"');

            foreach (var c in text)
            {
                switch (c)
                {
                    case '"': writer.Write("\\\""); break;
                    case '\\': writer.Write("\\\\"); break;
                    case '\n': writer.Write("\\n"); break;
                    case '\t': writer.Write("\\t"); break;
                    default: writer.Write(c); break;
                }
            }

            writer.Write('"');
        }

        void WriteIndentation()
        {
            for (var i = 0; i < indentation; i++)
            {
                writer.Write('\t');
            }
        }

        void WriteHeader(KVHeader header)
        {
            var encodingName = header?.Encoding.Name ?? "keyvalues2";
            var encodingVersion = header?.Encoding.Version ?? 1;
            var formatName = header?.Format.Name ?? "dmx";
            var formatVersion = header?.Format.Version ?? 1;

            writer.Write("<!-- dmx encoding ");
            writer.Write(encodingName);
            writer.Write(' ');
            writer.Write(encodingVersion.ToString(CultureInfo.InvariantCulture));
            writer.Write(" format ");
            writer.Write(formatName);
            writer.Write(' ');
            writer.Write(formatVersion.ToString(CultureInfo.InvariantCulture));
            writer.Write(" -->");
            writer.WriteLine();
        }

        #endregion

        public void Dispose()
        {
            writer.Flush();
            writer.Dispose();
        }
    }
}
