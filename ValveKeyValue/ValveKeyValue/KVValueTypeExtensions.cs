namespace ValveKeyValue
{
    static class KVValueTypeExtensions
    {
        /// <summary>
        /// Gets a value indicating whether a value type only exists in KeyValues2 (DMX). The
        /// KeyValues1 and KeyValues3 formats have no notation for these, so writing one would
        /// silently produce something that cannot be read back.
        /// </summary>
        public static bool IsKeyValues2Only(this KVValueType type) => type is
            KVValueType.Byte or KVValueType.Color or KVValueType.TimeSpan
            or KVValueType.Vector2 or KVValueType.Vector3 or KVValueType.Vector4
            or KVValueType.QAngle or KVValueType.Quaternion or KVValueType.Matrix4x4
            or (>= KVValueType.ElementArray and <= KVValueType.UInt64Array);

        public static void ThrowIfKeyValues2Only(this KVValueType type)
        {
            if (type.IsKeyValues2Only())
            {
                throw new InvalidOperationException(
                    $"{type} values can only be written by the KeyValues2 serializer.");
            }
        }
    }
}
