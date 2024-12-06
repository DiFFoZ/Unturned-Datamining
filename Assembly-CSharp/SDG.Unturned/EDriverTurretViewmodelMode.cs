namespace SDG.Unturned;

/// <summary>
/// Controls how first-person arms are moved for turrets operated from the driver's seat.
/// </summary>
internal enum EDriverTurretViewmodelMode
{
    /// <summary>
    /// Default. Pushes first-person arms off-screen while aiming. Originally implemented for the Fighter Jet where
    /// it looks weird if your arms are still visible when the camera zooms in while "aiming."
    /// </summary>
    OffscreenWhileAiming,
    /// <summary>
    /// Push first-person arms off-screen when equipped.
    /// </summary>
    AlwaysOffscreen,
    /// <summary>
    /// No particular use in mind, but included for completeness.
    /// </summary>
    AlwaysOnscreen
}
