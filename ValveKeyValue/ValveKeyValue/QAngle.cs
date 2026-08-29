namespace ValveKeyValue
{
    /// <summary>
    /// Represents an Euler angle with pitch, yaw, and roll components.
    /// </summary>
    public readonly record struct QAngle(float Pitch, float Yaw, float Roll);
}
