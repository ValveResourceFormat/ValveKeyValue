namespace ValveKeyValue
{
    /// <summary>
    /// Flags for KeyValue values.
    /// </summary>
    public enum KVFlag
    {
        /// <summary>No flag.</summary>
        None = 0,

        /// <summary>Resource reference.</summary>
        Resource = 1,

        /// <summary>Resource name reference.</summary>
        ResourceName = 2,

        /// <summary>Panorama reference.</summary>
        Panorama = 3,

        /// <summary>Sound event reference.</summary>
        SoundEvent = 4,

        /// <summary>Sub-class reference.</summary>
        SubClass = 5,

        /// <summary>Entity name reference.</summary>
        EntityName = 6,
    }
}
