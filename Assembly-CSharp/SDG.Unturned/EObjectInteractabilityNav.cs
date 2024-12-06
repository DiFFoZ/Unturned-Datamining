namespace SDG.Unturned;

public enum EObjectInteractabilityNav
{
    /// <summary>
    /// State doesn't affect AI collision.
    /// </summary>
    NONE,
    /// <summary>
    /// AI collision is blocked when object state is ON.
    /// </summary>
    ON,
    /// <summary>
    /// AI collision is blocked when object state is OFF.
    /// </summary>
    OFF
}
