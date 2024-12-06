namespace SDG.Unturned;

internal enum EWheelSteeringMode
{
    /// <summary>
    /// Wheel does not affect steering.
    /// </summary>
    None,
    /// <summary>
    /// Set steering angle according to <see cref="P:SDG.Unturned.VehicleAsset.steerMin" /> and <see cref="P:SDG.Unturned.VehicleAsset.steerMax" />.
    /// </summary>
    SteeringAngle,
    /// <summary>
    /// Increase or decrease motor torque to rotate vehicle in-place. (Tanks)
    /// </summary>
    CrawlerTrack
}
