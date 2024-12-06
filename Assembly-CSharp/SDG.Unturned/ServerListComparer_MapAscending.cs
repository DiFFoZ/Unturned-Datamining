namespace SDG.Unturned;

/// <summary>
/// Sort servers by map name A to Z.
/// </summary>
public class ServerListComparer_MapAscending : ServerListComparer_Base
{
    protected override int CompareDetails(SteamServerAdvertisement lhs, SteamServerAdvertisement rhs)
    {
        if (string.Equals(lhs.map, rhs.map))
        {
            return lhs.name.CompareTo(rhs.name);
        }
        return lhs.map.CompareTo(rhs.map);
    }
}
