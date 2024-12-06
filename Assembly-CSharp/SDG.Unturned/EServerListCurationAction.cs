namespace SDG.Unturned;

/// <summary>
/// Determines how to handle a server if it matches a rule.
/// </summary>
internal enum EServerListCurationAction
{
    /// <summary>
    /// Apply label and continue processing rules. 
    /// </summary>
    Label,
    /// <summary>
    /// Show the server in the list.
    /// </summary>
    Allow,
    /// <summary>
    /// Hide the server from the list.
    /// </summary>
    Deny
}
