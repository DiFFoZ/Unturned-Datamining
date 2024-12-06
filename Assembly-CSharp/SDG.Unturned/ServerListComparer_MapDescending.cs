namespace SDG.Unturned;

/// <summary>
/// Sort servers by map name Z to A.
/// </summary>
public class ServerListComparer_MapDescending : ServerListComparer_MapAscending
{
    protected override int CompareDetails(SteamServerAdvertisement lhs, SteamServerAdvertisement rhs)
    {
        return -base.CompareDetails(lhs, rhs);
    }
}
