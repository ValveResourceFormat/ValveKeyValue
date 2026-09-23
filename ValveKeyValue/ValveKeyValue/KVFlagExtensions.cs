namespace ValveKeyValue
{
    /// <summary>
    /// Extension methods for <see cref="KVFlag"/>.
    /// </summary>
    public static class KVFlagExtensions
    {
        /// <summary>
        /// Gets the name KeyValues3 text uses for a flag, such as <c>entity_name</c>.
        /// </summary>
        /// <param name="flag">The flag.</param>
        /// <returns>The name, or <see langword="null"/> for <see cref="KVFlag.None"/> and unknown values.</returns>
        public static string? SerializeFlagName(this KVFlag flag)
        {
            return flag switch
            {
                KVFlag.Resource => "resource",
                KVFlag.ResourceName => "resource_name",
                KVFlag.Panorama => "panorama",
                KVFlag.SoundEvent => "soundevent",
                KVFlag.SubClass => "subclass",
                KVFlag.EntityName => "entity_name",
                _ => null,
            };
        }
    }
}
