namespace SDG.Unturned;

/// <summary>
/// Controls whether wheel creates particle kickup effects for the ground surface material underneath.
/// </summary>
internal enum EWheelMotionEffectsMode
{
    /// <summary>
    /// Turn off motion effects. Default for wheels not using collider pose.
    /// </summary>
    None,
    /// <summary>
    /// Enable motion effects. Default for wheels using collider pose.
    /// </summary>
    BothDirections,
    /// <summary>
    /// Enable motion effects, but turn them off while moving backward.
    /// </summary>
    ForwardOnly,
    /// <summary>
    /// Enable motion effects, but turn them off while moving forward.
    /// </summary>
    BackwardOnly
}
