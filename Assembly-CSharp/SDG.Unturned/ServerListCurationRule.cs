using System.Text.RegularExpressions;
using Steamworks;
using Unturned.SystemEx;

namespace SDG.Unturned;

internal class ServerListCurationRule
{
    public EServerListCurationRuleType ruleType;

    /// <summary>
    /// Note: Port (if set) refers to the Steam query port.
    /// </summary>
    public EServerListCurationAction action;

    /// <summary>
    /// If true, negate whether this rule matches. i.e., binary NOT.
    /// </summary>
    public bool inverted;

    public string description;

    public string label;

    public Regex[] regexes;

    public IPv4Filter[] ipv4Filters;

    public CSteamID[] steamIds;

    public ServerListCurationFile owner;
}
