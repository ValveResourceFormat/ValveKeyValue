namespace ValveKeyValue.KeyValues3
{
    /// <summary>
    /// Represents a KeyValues3 identifier with a name and GUID.
    /// </summary>
    public readonly record struct KV3ID(string Name, Guid Id)
    {
        /// <inheritdoc/>
        /// <remarks>
        /// Returns the <see cref="KV3ID"/> in the format "Name:version{Guid}".
        /// </remarks>
        public override string ToString()
        {
            return $"{Name}:version{{{Id}}}";
        }
    }
}
