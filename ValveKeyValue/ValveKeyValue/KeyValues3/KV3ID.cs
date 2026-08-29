namespace ValveKeyValue.KeyValues3
{
    /// <summary>
    /// Represents a KeyValues3 identifier with a name and GUID.
    /// </summary>
    public readonly record struct KV3ID(string Name, Guid Id = default, int Version = 0)
    {
        /// <inheritdoc/>
        /// <remarks>
        /// Returns the <see cref="KV3ID"/> in the format "Name:version{Guid}".
        /// </remarks>
        public override string ToString()
        {
            if (Id != default)
            {
                return $"{Name}:version{{{Id}}}";
            }

            return $"{Name} {Version}";
        }
    }
}
