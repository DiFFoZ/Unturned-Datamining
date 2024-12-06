namespace SDG.Unturned;

/// <summary>
/// For <see cref="F:SDG.Unturned.EWheelSteeringMode.CrawlerTrack" />, indicates how a positive motor torque (forward) rotates
/// the vehicle.
/// </summary>
internal enum ECrawlerTrackForwardMode
{
    /// <summary>
    /// Wheels on the left side are Clockwise and wheels on the right side are Counter-Clockwise.
    /// </summary>
    Auto,
    /// <summary>
    /// Positive motor torque on this wheel rotates the vehicle clockwise.
    /// </summary>
    Clockwise,
    /// <summary>
    /// Positive motor torque on this wheel rotates the vehicle counter-clockwise.
    /// </summary>
    CounterClockwise
}
