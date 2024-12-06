namespace SDG.Unturned;

/// <summary>
/// Controls how rubble affects Nav game object.
/// </summary>
public enum EObjectRubbleNavMode
{
    /// <summary>
    /// Default. Destruction of rubble sections does not affect whether Nav game object is active or not.
    /// </summary>
    Unaffected,
    /// <summary>
    /// AI collision is blocked when any sections are alive. Once all sections are dead AI collision is unblocked.
    /// </summary>
    DeactivateIfAllDead
}
