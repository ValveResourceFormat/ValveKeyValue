namespace ValveKeyValue.KeyValues2
{
    /// <summary>
    /// Identifies which binary ID encoding version to use.
    /// </summary>
    internal enum IDVersion
    {
        /// <summary>v1-v2: AT_OBJECTID at slot 7.</summary>
        V1,

        /// <summary>v3-v5: AT_TIME at slot 7 (replaced AT_OBJECTID).</summary>
        V2,

        /// <summary>v9: new layout with UINT64/UINT8, arrays at +32.</summary>
        V3,
    }

    /// <summary>
    /// Unified DMX attribute type enum. Values match IDv3 scalar IDs.
    /// </summary>
    internal enum DmxAttributeType : byte
    {
        Element = 1,
        Int32 = 2,
        Float = 3,
        Bool = 4,
        String = 5,
        BinaryBlob = 6,
        Time = 7,
        Color = 8,
        Vector2 = 9,
        Vector3 = 10,
        Vector4 = 11,
        QAngle = 12,
        Quaternion = 13,
        Matrix4x4 = 14,
        UInt64 = 15,
        UInt8 = 16,

        // Array types (scalar + 32 for IDv3, scalar + 14 for IDv1/v2)
        ElementArray = 33,
        Int32Array = 34,
        FloatArray = 35,
        BoolArray = 36,
        StringArray = 37,
        BinaryBlobArray = 38,
        TimeArray = 39,
        ColorArray = 40,
        Vector2Array = 41,
        Vector3Array = 42,
        Vector4Array = 43,
        QAngleArray = 44,
        QuaternionArray = 45,
        Matrix4x4Array = 46,
        UInt64Array = 47,
        UInt8Array = 48,

        /// <summary>Pre-v3 AT_OBJECTID at slot 7 (16 bytes, skip and discard).</summary>
        ObjectId = 255,
    }

    internal static class DmxAttributeTypeHelper
    {
        /// <summary>
        /// The type names used by the keyvalues2 text encoding. Valve compares them with
        /// <c>Q_stricmp</c>, so they are matched case insensitively.
        /// </summary>
        static readonly Dictionary<string, DmxAttributeType> TypeNames = new(StringComparer.OrdinalIgnoreCase)
        {
            ["element"] = DmxAttributeType.Element,
            ["int"] = DmxAttributeType.Int32,
            ["float"] = DmxAttributeType.Float,
            ["bool"] = DmxAttributeType.Bool,
            ["string"] = DmxAttributeType.String,
            ["binary"] = DmxAttributeType.BinaryBlob,
            ["vdata"] = DmxAttributeType.BinaryBlob,
            ["time"] = DmxAttributeType.Time,
            ["color"] = DmxAttributeType.Color,
            ["vector2"] = DmxAttributeType.Vector2,
            ["vector3"] = DmxAttributeType.Vector3,
            ["vector4"] = DmxAttributeType.Vector4,
            ["qangle"] = DmxAttributeType.QAngle,
            ["quaternion"] = DmxAttributeType.Quaternion,
            ["matrix"] = DmxAttributeType.Matrix4x4,
            ["uint64"] = DmxAttributeType.UInt64,
            ["uint8"] = DmxAttributeType.UInt8,
            ["element_array"] = DmxAttributeType.ElementArray,
            ["int_array"] = DmxAttributeType.Int32Array,
            ["float_array"] = DmxAttributeType.FloatArray,
            ["bool_array"] = DmxAttributeType.BoolArray,
            ["string_array"] = DmxAttributeType.StringArray,
            ["binary_array"] = DmxAttributeType.BinaryBlobArray,
            ["vdata_array"] = DmxAttributeType.BinaryBlobArray,
            ["time_array"] = DmxAttributeType.TimeArray,
            ["color_array"] = DmxAttributeType.ColorArray,
            ["vector2_array"] = DmxAttributeType.Vector2Array,
            ["vector3_array"] = DmxAttributeType.Vector3Array,
            ["vector4_array"] = DmxAttributeType.Vector4Array,
            ["qangle_array"] = DmxAttributeType.QAngleArray,
            ["quaternion_array"] = DmxAttributeType.QuaternionArray,
            ["matrix_array"] = DmxAttributeType.Matrix4x4Array,
            ["uint64_array"] = DmxAttributeType.UInt64Array,
            ["uint8_array"] = DmxAttributeType.UInt8Array,
        };

        /// <summary>
        /// Looks up a keyvalues2 text type name. A name that is not a type is the class name of
        /// an inline element.
        /// </summary>
        public static bool TryGetTypeFromName(string name, out DmxAttributeType type)
            => TypeNames.TryGetValue(name, out type);

        /// <summary>
        /// Names that are accepted on read but are not what we write. <c>binary</c> is the
        /// canonical spelling of the type <c>vdata</c> also names.
        /// </summary>
        static readonly string[] ReadOnlyAliases = ["vdata", "vdata_array"];

        /// <summary>
        /// The name to write for each type, derived from <see cref="TypeNames"/> so the two
        /// directions cannot drift apart.
        /// </summary>
        static readonly Dictionary<DmxAttributeType, string> CanonicalTypeNames = BuildCanonicalTypeNames();

        static Dictionary<DmxAttributeType, string> BuildCanonicalTypeNames()
        {
            var names = new Dictionary<DmxAttributeType, string>(TypeNames.Count);

            foreach (var (name, type) in TypeNames)
            {
                if (Array.IndexOf(ReadOnlyAliases, name) < 0)
                {
                    names.Add(type, name);
                }
            }

            return names;
        }

        /// <summary>
        /// Gets the keyvalues2 text type name for an attribute type.
        /// </summary>
        public static string GetTypeName(DmxAttributeType type)
        {
            if (!CanonicalTypeNames.TryGetValue(type, out var name))
            {
                throw new KeyValueException($"No DMX text type name for: {type}");
            }

            return name;
        }

        /// <summary>
        /// Gets a value indicating whether an attribute type is one of the array types.
        /// </summary>
        public static bool IsArray(DmxAttributeType type)
            => type >= DmxAttributeType.ElementArray && type <= DmxAttributeType.UInt8Array;

        /// <summary>On-disk distance between a scalar and its array type in IDv1 and IDv2.</summary>
        const byte ArrayOffsetOnDisk = 14;

        /// <summary>Distance between a scalar and its array type in our unified enum, matching IDv3.</summary>
        const byte ArrayOffsetUnified = 32;

        // IDv1/v2: scalars 1-14, arrays 15-28. Difference between scalar and array is 14.
        // IDv1 slot 7 = AT_OBJECTID (not AT_TIME).
        // IDv2 slot 7 = AT_TIME.
        // IDv3: scalars 1-16, arrays 33-48 (scalar + 32).

        /// <summary>
        /// Decode a raw on-disk type byte into our unified DmxAttributeType.
        /// </summary>
        public static DmxAttributeType DecodeID(byte raw, IDVersion version)
        {
            return version switch
            {
                IDVersion.V1 => DecodeV1(raw),
                IDVersion.V2 => DecodeV2(raw),
                IDVersion.V3 => DecodeV3(raw),
                _ => throw new KeyValueException($"Unknown ID version: {version}"),
            };
        }

        /// <summary>
        /// Encode our unified DmxAttributeType to an on-disk type byte.
        /// </summary>
        public static byte EncodeID(DmxAttributeType type, IDVersion version)
        {
            return version switch
            {
                IDVersion.V1 => EncodeV1(type),
                IDVersion.V2 => EncodeV2(type),
                IDVersion.V3 => EncodeV3(type),
                _ => throw new KeyValueException($"Unknown ID version: {version}"),
            };
        }

        /// <summary>
        /// Convert KVValueType to DmxAttributeType.
        /// </summary>
        public static DmxAttributeType FromKVValueType(KVValueType type) => type switch
        {
            KVValueType.Collection => DmxAttributeType.Element,
            KVValueType.Int32 => DmxAttributeType.Int32,
            KVValueType.FloatingPoint => DmxAttributeType.Float,
            KVValueType.Boolean => DmxAttributeType.Bool,
            KVValueType.String => DmxAttributeType.String,
            KVValueType.BinaryBlob => DmxAttributeType.BinaryBlob,
            KVValueType.TimeSpan => DmxAttributeType.Time,
            KVValueType.Color => DmxAttributeType.Color,
            KVValueType.Vector2 => DmxAttributeType.Vector2,
            KVValueType.Vector3 => DmxAttributeType.Vector3,
            KVValueType.Vector4 => DmxAttributeType.Vector4,
            KVValueType.QAngle => DmxAttributeType.QAngle,
            KVValueType.Quaternion => DmxAttributeType.Quaternion,
            KVValueType.Matrix4x4 => DmxAttributeType.Matrix4x4,
            KVValueType.UInt64 => DmxAttributeType.UInt64,
            KVValueType.Byte => DmxAttributeType.UInt8,
            KVValueType.ElementArray => DmxAttributeType.ElementArray,
            KVValueType.Int32Array => DmxAttributeType.Int32Array,
            KVValueType.FloatArray => DmxAttributeType.FloatArray,
            KVValueType.BooleanArray => DmxAttributeType.BoolArray,
            KVValueType.StringArray => DmxAttributeType.StringArray,
            KVValueType.BinaryBlobArray => DmxAttributeType.BinaryBlobArray,
            KVValueType.TimeSpanArray => DmxAttributeType.TimeArray,
            KVValueType.ColorArray => DmxAttributeType.ColorArray,
            KVValueType.Vector2Array => DmxAttributeType.Vector2Array,
            KVValueType.Vector3Array => DmxAttributeType.Vector3Array,
            KVValueType.Vector4Array => DmxAttributeType.Vector4Array,
            KVValueType.QAngleArray => DmxAttributeType.QAngleArray,
            KVValueType.QuaternionArray => DmxAttributeType.QuaternionArray,
            KVValueType.Matrix4x4Array => DmxAttributeType.Matrix4x4Array,
            KVValueType.UInt64Array => DmxAttributeType.UInt64Array,
            KVValueType.ByteArray => DmxAttributeType.UInt8Array,
            _ => throw new KeyValueException($"Cannot convert KVValueType {type} to DmxAttributeType."),
        };

        #region IDv1 (v1-v2): scalars 1-14, arrays 15-28, slot 7 = AT_OBJECTID

        // IDv1 layout:
        // 1=Element, 2=Int32, 3=Float, 4=Bool, 5=String, 6=BinaryBlob,
        // 7=ObjectId(!), 8=Color, 9=Vector2, 10=Vector3, 11=Vector4,
        // 12=QAngle, 13=Quaternion, 14=Matrix4x4
        // Arrays: add 14

        // IDv1 is IDv2 with one difference: slot 7 holds AT_OBJECTID rather than AT_TIME.

        const byte ObjectIdSlot = 7;
        const byte ObjectIdArraySlot = ObjectIdSlot + ArrayOffsetOnDisk;

        static DmxAttributeType DecodeV1(byte raw) => raw switch
        {
            ObjectIdSlot => DmxAttributeType.ObjectId,
            ObjectIdArraySlot => throw new KeyValueException("AT_OBJECTID_ARRAY is not supported."),
            _ => DecodeV2(raw),
        };

        static byte EncodeV1(DmxAttributeType type) => type switch
        {
            DmxAttributeType.Time => throw new KeyValueException("AT_TIME does not exist in IDv1."),
            DmxAttributeType.TimeArray => throw new KeyValueException("AT_TIME_ARRAY does not exist in IDv1."),
            _ => EncodeV2(type),
        };

        #endregion

        #region IDv2 (v3-v5): scalars 1-14, arrays 15-28, slot 7 = AT_TIME

        static DmxAttributeType DecodeV2(byte raw)
        {
            if (raw >= 1 && raw <= 14)
            {
                // 1-14 map directly (Element..Matrix4x4, with 7=Time)
                return (DmxAttributeType)raw;
            }

            if (raw >= 15 && raw <= 28)
            {
                // Arrays: raw - 14 = scalar type, +32 for unified array
                return (DmxAttributeType)(raw - ArrayOffsetOnDisk + ArrayOffsetUnified);
            }

            throw new KeyValueException($"Unknown IDv2 type byte: {raw}");
        }

        static byte EncodeV2(DmxAttributeType type)
        {
            if (type >= DmxAttributeType.Element && type <= DmxAttributeType.Matrix4x4)
            {
                return (byte)type;
            }

            if (type >= DmxAttributeType.ElementArray && type <= DmxAttributeType.Matrix4x4Array)
            {
                return (byte)((byte)type - ArrayOffsetUnified + ArrayOffsetOnDisk);
            }

            throw new KeyValueException($"Cannot encode {type} for IDv2 (UInt64/UInt8 not supported before v9).");
        }

        #endregion

        #region IDv3 (v9): scalars 1-16, arrays 33-48

        static DmxAttributeType DecodeV3(byte raw)
        {
            if (raw >= 1 && raw <= 16)
            {
                return (DmxAttributeType)raw;
            }

            if (raw >= 33 && raw <= 48)
            {
                return (DmxAttributeType)raw;
            }

            throw new KeyValueException($"Unknown IDv3 type byte: {raw}");
        }

        static byte EncodeV3(DmxAttributeType type)
        {
            if (type >= DmxAttributeType.Element && type <= DmxAttributeType.UInt8)
            {
                return (byte)type;
            }

            if (type >= DmxAttributeType.ElementArray && type <= DmxAttributeType.UInt8Array)
            {
                return (byte)type;
            }

            throw new KeyValueException($"Cannot encode {type} for IDv3.");
        }

        #endregion
    }
}
