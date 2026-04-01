namespace ValveKeyValue
{
    /// <summary>
    /// Represents a DMX time value in tenths of milliseconds (0.1ms = 0.0001s).
    /// </summary>
    public readonly record struct DmxTime(int Ticks);
}
