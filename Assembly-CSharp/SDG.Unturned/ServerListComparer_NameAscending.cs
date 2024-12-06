namespace SDG.Unturned;

/// <summary>
/// Sort servers by name A to Z.
/// </summary>
public class ServerListComparer_NameAscending : ServerListComparer_Base
{
    protected override int CompareDetails(SteamServerAdvertisement lhs, SteamServerAdvertisement rhs)
    {
        return lhs.name.CompareTo(rhs.name);
    }
}
