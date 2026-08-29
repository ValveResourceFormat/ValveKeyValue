using System.Buffers;
using System.Globalization;
using System.Numerics;
using ValveKeyValue.KeyValues2;
using ValveKeyValue.KeyValues3;

namespace ValveKeyValue.Serialization.KeyValues2
{
    sealed class KV2TextWriter : IDisposable
    {
        /// <summary>Version used when the document did not come from a keyvalues2 document.</summary>
        const int DefaultEncodingVersion = 1;

        /// <summary>First version that can encode uint8, uint64 and a prefix element.</summary>
        const int PrefixAndSourceTwoTypesVersion = 4;

        /// <summary>The characters Valve's CUtlBuffer conversion escapes.</summary>
        static readonly SearchValues<char> CharsToEscape = SearchValues.Create("\n\t\v\b\r\f\a\\?'\"");

        /// <summary>Number of floats Valve puts on one row of a matrix value.</summary>
        const int MatrixRowLength = 4;

        /// <summary>Number of bytes Valve puts on one row of a binary blob.</summary>
        const int BlobRowLength = 40;

        readonly StreamWriter writer;
        readonly KVHeader? sourceHeader;

        /// <summary>keyvalues2_flat writes every element as a top level block.</summary>
        bool flat;

        /// <summary>keyvalues2_noids omits the id of elements that are written inline.</summary>
        bool noIds;

        readonly Dictionary<KV2Element, int> referenceCounts = new(ReferenceEqualityComparer.Instance);
        readonly List<KV2Element> discoveryOrder = [];
        readonly HashSet<KV2Element> topLevel = new(ReferenceEqualityComparer.Instance);
        readonly HashSet<KV2Element> writing = new(ReferenceEqualityComparer.Instance);

        int indentation;

        public KV2TextWriter(Stream stream, KVHeader? header = null)
        {
            ArgumentNullException.ThrowIfNull(stream);

            sourceHeader = header;
            writer = new StreamWriter(stream, new System.Text.UTF8Encoding(), bufferSize: 16 * 1024, leaveOpen: true)
            {
                NewLine = "\n",
            };
        }

        public void Write(KVDocument doc)
        {
            ArgumentNullException.ThrowIfNull(doc);

            var root = doc.Root as KV2Element
                ?? throw new KeyValueException("KV2 text writer requires a KV2Element as the root object.");

            KV2BinaryWriter.ThrowIfUnwritableRoot(root);

            var prefixElement = (doc as KV2Document)?.PrefixElement;

            var encodingName = sourceHeader?.Encoding.Name;
            flat = encodingName == "keyvalues2_flat";
            noIds = encodingName == "keyvalues2_noids";

            referenceCounts.Clear();
            discoveryOrder.Clear();
            topLevel.Clear();
            writing.Clear();
            indentation = 0;

            CountReferences(root, depth: 0);

            // Valve inlines an element at its only usage site, and writes it as a top level block
            // as soon as it is referenced more than once. The root is always a top level block.
            topLevel.Add(root);

            foreach (var element in discoveryOrder)
            {
                if (flat || referenceCounts[element] > 1 || ClassNameNeedsTopLevel(element))
                {
                    topLevel.Add(element);
                }
            }

            WriteHeader(prefixElement);

            if (prefixElement != null)
            {
                ThrowIfPrefixReferencesElements(prefixElement);
                WritePrefixElement(prefixElement);
            }

            foreach (var element in discoveryOrder)
            {
                if (topLevel.Contains(element))
                {
                    WriteTopLevelElement(element);
                }
            }
        }

        /// <summary>
        /// An inline element is written as its class name where a type token would otherwise go,
        /// so a class name that is also a DMX type name cannot be inlined without producing a
        /// document the reader mis-parses. Writing it as a top level block keeps it unambiguous.
        /// </summary>
        static bool ClassNameNeedsTopLevel(KV2Element element)
            => element.ClassName is not null
                && (DmxAttributeTypeHelper.TryGetTypeFromName(element.ClassName, out _)
                    || element.ClassName.Equals("unknown", StringComparison.OrdinalIgnoreCase)
                    || element.ClassName.Equals("elementid", StringComparison.OrdinalIgnoreCase));

        /// <summary>
        /// The prefix container is written before the element index exists, so it can only hold
        /// plain values. The binary writer rejects references outright; matching it here keeps a
        /// text document convertible to binary.
        /// </summary>
        static void ThrowIfPrefixReferencesElements(KV2Element prefixElement)
        {
            foreach (var (_, child) in prefixElement.Children)
            {
                if (child is KV2Element || child.ValueType == KVValueType.ElementArray)
                {
                    throw new KeyValueException("Prefix attributes cannot reference elements.");
                }
            }
        }

        #region Reference counting

        void CountReferences(KV2Element element, int depth)
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

            if (referenceCounts.TryGetValue(element, out var count))
            {
                referenceCounts[element] = count + 1;
                return;
            }

            referenceCounts.Add(element, 1);
            discoveryOrder.Add(element);

            foreach (var (key, child) in element.Children)
            {
                if (KV2Element.IsElementName(key, child))
                {
                    continue;
                }

                if (child is KV2Element childElement)
                {
                    CountReferences(childElement, depth + 1);
                }
                else if (child.ValueType == KVValueType.ElementArray)
                {
                    foreach (var item in child.GetArray<KV2Element>())
                    {
                        CountReferences(item, depth + 1);
                    }
                }
            }
        }

        #endregion

        #region Header

        void WriteHeader(KV2Element? prefixElement)
        {
            var encodingName = sourceHeader?.Encoding.Name;
            var isTextEncoding = encodingName is "keyvalues2" or "keyvalues2_flat" or "keyvalues2_noids";

            // The encoding name and version describe our own output, only the format carries over.
            var version = isTextEncoding ? Math.Max(sourceHeader!.Encoding.Version, 1) : DefaultEncodingVersion;

            if (version < PrefixAndSourceTwoTypesVersion && (prefixElement != null || KV2Element.NeedsSourceTwoTypes(discoveryOrder)))
            {
                version = PrefixAndSourceTwoTypesVersion;
            }

            var header = new KVHeader
            {
                Encoding = new KV3ID(isTextEncoding ? encodingName! : "keyvalues2", Version: version),
                Format = new KV3ID(sourceHeader?.Format.Name ?? "dmx", Version: sourceHeader?.Format.Version ?? 1),
            };

            writer.Write(KV2Header.Format(header));
            writer.WriteLine();
        }


        #endregion

        #region Element writing

        void WriteTopLevelElement(KV2Element element)
        {
            WriteElementBody(element, writeId: true);
            writer.WriteLine();

            // Valve separates top level blocks with a blank line.
            writer.WriteLine();
        }

        /// <summary>
        /// Writes the prefix attribute container. Its class name is always the sentinel, because
        /// that name is how a reader tells the container from the root; writing anything else
        /// would make the container parse back as the root and lose the real one.
        /// </summary>
        void WritePrefixElement(KV2Element prefixElement)
        {
            WriteQuotedString(KV2Element.PrefixElementClassName);
            writer.WriteLine();
            WriteIndentation();
            writer.Write('{');
            writer.WriteLine();

            indentation++;

            WriteAttributeName("id");
            WriteQuotedString("elementid");
            writer.Write(' ');
            WriteQuotedString(prefixElement.ElementId.ToString());
            writer.WriteLine();

            // The container has no name member, so an attribute called "name" is an ordinary
            // attribute here rather than the element name.
            foreach (var (key, child) in prefixElement.Children)
            {
                WriteAttribute(key, child);
            }

            indentation--;

            WriteIndentation();
            writer.Write('}');
            writer.WriteLine();
            writer.WriteLine();
        }

        /// <summary>
        /// Writes the class name and body of an element, leaving the cursor right after the
        /// closing brace so that the caller can follow it with a comma or a line break.
        /// </summary>
        void WriteElementBody(KV2Element element, bool writeId)
        {
            WriteQuotedString(element.ClassName ?? string.Empty);
            writer.WriteLine();
            WriteIndentation();
            writer.Write('{');
            writer.WriteLine();

            indentation++;
            writing.Add(element);

            if (writeId)
            {
                WriteAttributeName("id");
                WriteQuotedString("elementid");
                writer.Write(' ');
                WriteQuotedString(element.ElementId.ToString());
                writer.WriteLine();
            }

            // Source 1 always carries a name attribute, even when it is empty.
            WriteAttributeName("name");
            WriteQuotedString("string");
            writer.Write(' ');
            WriteQuotedString(element.Name ?? string.Empty);
            writer.WriteLine();

            foreach (var (key, child) in element.Children)
            {
                if (KV2Element.IsElementName(key, child))
                {
                    continue;
                }

                WriteAttribute(key, child);
            }

            writing.Remove(element);
            indentation--;

            WriteIndentation();
            writer.Write('}');
        }

        void WriteAttribute(string key, KVObject value)
        {
            if (value is KV2Element element)
            {
                WriteAttributeName(key);

                if (IsInlined(element))
                {
                    WriteElementBody(element, writeId: !noIds);
                    writer.WriteLine();
                }
                else
                {
                    WriteQuotedString("element");
                    writer.Write(' ');
                    WriteQuotedString(KV2Element.IsNullReference(element) ? string.Empty : element.ElementId.ToString());
                    writer.WriteLine();
                }

                return;
            }

            // A DMX element attribute must be a KV2Element; a plain collection has no class name
            // or id. Checked before anything is written, so the stream is not left truncated.
            if (value.ValueType == KVValueType.Collection)
            {
                throw new KeyValueException(
                    $"A collection attribute must be a {nameof(KV2Element)} to be written as a DMX element.");
            }

            WriteAttributeName(key);

            if (value.ValueType == KVValueType.ElementArray)
            {
                WriteQuotedString("element_array");
                WriteElementArray(value.GetArray<KV2Element>());
                return;
            }

            if (value.IsTypedArray)
            {
                WriteQuotedString(GetTypeName(value.ValueType));
                WriteTypedArray(value);
                return;
            }

            WriteQuotedString(GetTypeName(value.ValueType));
            writer.Write(' ');

            if (value.ValueType == KVValueType.Matrix4x4)
            {
                WriteWrappedValue(MatrixRows((Matrix4x4)value._ref!));
            }
            else if (value.ValueType == KVValueType.BinaryBlob)
            {
                WriteWrappedValue(BlobRows(value.AsBlob()));
            }
            else
            {
                WriteQuotedString(FormatScalar(value));
                writer.WriteLine();
            }
        }

        bool IsInlined(KV2Element element)
            => !KV2Element.IsNullReference(element) && !element.IsStub && !topLevel.Contains(element) && !writing.Contains(element);

        void WriteElementArray(List<KV2Element> list)
        {
            // Valve leaves a space after the type token when the value starts on the next line.
            writer.Write(' ');
            writer.WriteLine();
            WriteIndentation();
            writer.Write('[');
            writer.WriteLine();
            indentation++;

            for (var i = 0; i < list.Count; i++)
            {
                var item = list[i];

                WriteIndentation();

                if (IsInlined(item))
                {
                    WriteElementBody(item, writeId: !noIds);
                }
                else
                {
                    WriteQuotedString("element");
                    writer.Write(' ');
                    WriteQuotedString(KV2Element.IsNullReference(item) ? string.Empty : item.ElementId.ToString());
                }

                // Exactly one comma between items, none after the last one.
                if (i < list.Count - 1)
                {
                    writer.Write(',');
                }

                writer.WriteLine();
            }

            indentation--;
            WriteIndentation();
            writer.Write(']');
            writer.WriteLine();
        }

        void WriteTypedArray(KVObject value)
        {
            writer.Write(' ');
            writer.WriteLine();
            WriteIndentation();
            writer.Write('[');
            writer.WriteLine();
            indentation++;

            var items = FormatArrayItems(value);

            for (var i = 0; i < items.Count; i++)
            {
                WriteIndentation();
                WriteQuotedString(items[i]);

                if (i < items.Count - 1)
                {
                    writer.Write(',');
                }

                writer.WriteLine();
            }

            indentation--;
            WriteIndentation();
            writer.Write(']');
            writer.WriteLine();
        }

        /// <summary>
        /// Writes a value that Valve spreads over several lines: the opening quote goes on its
        /// own line, then one indented row per line, then the closing quote.
        /// </summary>
        void WriteWrappedValue(List<string> rows)
        {
            writer.WriteLine();
            WriteIndentation();
            writer.Write('"');
            writer.WriteLine();

            indentation++;

            foreach (var row in rows)
            {
                WriteIndentation();
                writer.Write(row);
                writer.WriteLine();
            }

            indentation--;

            WriteIndentation();
            writer.Write('"');
            writer.WriteLine();
        }

        static List<string> MatrixRows(Matrix4x4 m)
        {
            var values = new[]
            {
                m.M11, m.M12, m.M13, m.M14,
                m.M21, m.M22, m.M23, m.M24,
                m.M31, m.M32, m.M33, m.M34,
                m.M41, m.M42, m.M43, m.M44,
            };

            var rows = new List<string>(values.Length / MatrixRowLength);

            for (var i = 0; i < values.Length; i += MatrixRowLength)
            {
                rows.Add($"{FormatFloat(values[i])} {FormatFloat(values[i + 1])} {FormatFloat(values[i + 2])} {FormatFloat(values[i + 3])}");
            }

            return rows;
        }

        static List<string> BlobRows(byte[] data)
        {
            var rows = new List<string>((data.Length / BlobRowLength) + 1);

            for (var i = 0; i < data.Length; i += BlobRowLength)
            {
                rows.Add(Convert.ToHexString(data, i, Math.Min(BlobRowLength, data.Length - i)));
            }

            return rows;
        }



        #endregion

        #region Value formatting

        static string FormatScalar(KVObject value) => value.ValueType switch
        {
            KVValueType.Boolean => value.ToBoolean(null) ? "1" : "0",
            KVValueType.Int32 => value.ToInt32(null).ToString(CultureInfo.InvariantCulture),
            KVValueType.Byte => value.ToByte(null).ToString(CultureInfo.InvariantCulture),
            KVValueType.UInt64 => FormatUInt64(value.ToUInt64(null)),
            KVValueType.FloatingPoint => FormatFloat(value.ToSingle(null)),
            KVValueType.String => (string?)value._ref ?? string.Empty,
            KVValueType.TimeSpan => FormatTime((DmxTime)value._ref!),
            KVValueType.Color => FormatColor((DmxColor)value._ref!),
            KVValueType.Vector2 => FormatVector2((Vector2)value._ref!),
            KVValueType.Vector3 => FormatVector3((Vector3)value._ref!),
            KVValueType.Vector4 => FormatVector4((Vector4)value._ref!),
            KVValueType.QAngle => FormatQAngle((QAngle)value._ref!),
            KVValueType.Quaternion => FormatQuaternion((Quaternion)value._ref!),
            _ => throw new KeyValueException($"No DMX text representation for: {value.ValueType}"),
        };

        static List<string> FormatArrayItems(KVObject value) => value.ValueType switch
        {
            KVValueType.Int32Array => value.GetArray<int>().ConvertAll(v => v.ToString(CultureInfo.InvariantCulture)),
            KVValueType.FloatArray => value.GetArray<float>().ConvertAll(FormatFloat),
            KVValueType.BooleanArray => value.GetArray<bool>().ConvertAll(v => v ? "1" : "0"),
            KVValueType.StringArray => value.GetArray<string>().ConvertAll(v => v ?? string.Empty),
            KVValueType.BinaryBlobArray => value.GetArray<byte[]>().ConvertAll(Convert.ToHexString),
            KVValueType.TimeSpanArray => value.GetArray<DmxTime>().ConvertAll(FormatTime),
            KVValueType.ColorArray => value.GetArray<DmxColor>().ConvertAll(FormatColor),
            KVValueType.Vector2Array => value.GetArray<Vector2>().ConvertAll(FormatVector2),
            KVValueType.Vector3Array => value.GetArray<Vector3>().ConvertAll(FormatVector3),
            KVValueType.Vector4Array => value.GetArray<Vector4>().ConvertAll(FormatVector4),
            KVValueType.QAngleArray => value.GetArray<QAngle>().ConvertAll(FormatQAngle),
            KVValueType.QuaternionArray => value.GetArray<Quaternion>().ConvertAll(FormatQuaternion),
            KVValueType.Matrix4x4Array => value.GetArray<Matrix4x4>().ConvertAll(m => string.Join(' ', MatrixRows(m))),
            KVValueType.ByteArray => value.GetArray<byte>().ConvertAll(v => v.ToString(CultureInfo.InvariantCulture)),
            KVValueType.UInt64Array => value.GetArray<ulong>().ConvertAll(FormatUInt64),
            _ => throw new KeyValueException($"No DMX text representation for array type: {value.ValueType}"),
        };

        /// <summary>
        /// Valve writes floats with <c>%.10f</c> and then trims the trailing zeros, so a value
        /// never comes out in exponent form.
        /// </summary>
        static string FormatFloat(float value)
        {
            if (!float.IsFinite(value))
            {
                return value.ToString(CultureInfo.InvariantCulture);
            }

            var text = value.ToString("F10", CultureInfo.InvariantCulture).TrimEnd('0').TrimEnd('.');

            return text.Length == 0 || text == "-" ? "0" : text;
        }

        /// <summary>Times are stored as ticks but written as seconds with four decimals.</summary>
        static string FormatTime(DmxTime time)
            => time.TotalSeconds.ToString("F4", CultureInfo.InvariantCulture);

        /// <summary>uint64 values are written as hex, the way Hammer does.</summary>
        static string FormatUInt64(ulong value)
            => string.Create(CultureInfo.InvariantCulture, $"0x{value:x}");

        static string FormatColor(DmxColor c)
            => string.Create(CultureInfo.InvariantCulture, $"{c.R} {c.G} {c.B} {c.A}");

        static string FormatVector2(Vector2 v) => $"{FormatFloat(v.X)} {FormatFloat(v.Y)}";

        static string FormatVector3(Vector3 v) => $"{FormatFloat(v.X)} {FormatFloat(v.Y)} {FormatFloat(v.Z)}";

        static string FormatVector4(Vector4 v) => $"{FormatFloat(v.X)} {FormatFloat(v.Y)} {FormatFloat(v.Z)} {FormatFloat(v.W)}";

        static string FormatQAngle(QAngle a) => $"{FormatFloat(a.Pitch)} {FormatFloat(a.Yaw)} {FormatFloat(a.Roll)}";

        static string FormatQuaternion(Quaternion q) => $"{FormatFloat(q.X)} {FormatFloat(q.Y)} {FormatFloat(q.Z)} {FormatFloat(q.W)}";

        static string GetTypeName(KVValueType type)
            => DmxAttributeTypeHelper.GetTypeName(DmxAttributeTypeHelper.FromKVValueType(type));

        #endregion

        #region Low-level writing

        void WriteAttributeName(string name)
        {
            WriteIndentation();
            WriteQuotedString(name);
            writer.Write(' ');
        }

        void WriteQuotedString(string text)
        {
            writer.Write('"');

            // Most names, type tokens and values contain nothing to escape, so write those in one
            // go rather than a character at a time.
            var remaining = text.AsSpan();

            while (true)
            {
                var next = remaining.IndexOfAny(CharsToEscape);

                if (next < 0)
                {
                    writer.Write(remaining);
                    break;
                }

                writer.Write(remaining[..next]);
                writer.Write(EscapeFor(remaining[next]));
                remaining = remaining[(next + 1)..];
            }

            writer.Write('"');
        }

        static string EscapeFor(char c) => c switch
        {
            '\n' => "\\n",
            '\t' => "\\t",
            '\v' => "\\v",
            '\b' => "\\b",
            '\r' => "\\r",
            '\f' => "\\f",
            '\a' => "\\a",
            '\\' => "\\\\",
            '?' => "\\?",
            '\'' => "\\'",
            _ => "\\\"",
        };

        void WriteIndentation()
        {
            for (var i = 0; i < indentation; i++)
            {
                writer.Write('\t');
            }
        }

        #endregion

        public void Dispose()
        {
            writer.Flush();
            writer.Dispose();
        }
    }
}
