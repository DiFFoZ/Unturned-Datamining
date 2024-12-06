namespace SDG.Unturned;

/// <summary>
/// Sort servers by ping high to low.
/// </summary>
public class ServerListComparer_PingDescending : ServerListComparer_PingAscending
{
    protected override int CompareDetails(SteamServerAdvertisement lhs, SteamServerAdvertisement rhs)
    {
        return -base.CompareDetails(lhs, rhs);
    }
}
