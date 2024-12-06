using System.Collections.Generic;

namespace SDG.Unturned;

internal struct ServerListCurationOutput
{
    /// <summary>
    /// If false, a deny rule matched the input.
    /// </summary>
    public bool allowed;

    /// <summary>
    /// If true, at least one rule matched the input.
    /// </summary>
    public bool matchedAnyRules;

    /// <summary>
    /// If set, this was the final match.
    /// </summary>
    public ServerListCurationRule allowOrDenyRule;

    /// <summary>
    /// Optional. If set, filled with any rules that matched.
    /// </summary>
    public List<ServerListCurationRule> matchedRules;

    public string labels;
}
