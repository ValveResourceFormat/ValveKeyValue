namespace ValveKeyValue
{
    /// <summary>
    /// Represents a DMX time value in tenths of milliseconds (0.1ms = 0.0001s).
    /// </summary>
    public readonly record struct DmxTime(int Ticks)
    {
        /// <summary>
        /// The number of ticks in one second. The binary encoding stores ticks, while the text
        /// encoding writes seconds with four decimals.
        /// </summary>
        public const int TicksPerSecond = 10000;

        /// <summary>
        /// Gets this time in seconds.
        /// </summary>
        public double TotalSeconds => Ticks / (double)TicksPerSecond;
    }
}
