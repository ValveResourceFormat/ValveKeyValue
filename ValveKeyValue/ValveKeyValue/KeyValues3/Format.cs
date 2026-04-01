namespace ValveKeyValue.KeyValues3
{
    /// <summary>
    /// Known KeyValues3 format identifiers.
    /// </summary>
    public static class Format
    {
        /// <summary>Generic format for arbitrary data, with no assumptions about how the data will be used.</summary>
        public static Guid Generic { get; } = new(new byte[] { 0x7C, 0x16, 0x12, 0x74, 0xE9, 0x06, 0x98, 0x46, 0xAF, 0xF2, 0xE6, 0x3E, 0xB5, 0x90, 0x37, 0xE7 });
    }
}
