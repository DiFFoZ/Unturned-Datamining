namespace SDG.Unturned;

/// <summary>
/// Sort servers by name Z to A.
/// </summary>
public class ServerListComparer_NameDescending : ServerListComparer_NameAscending
{
    protected override int CompareDetails(SteamServerAdvertisement lhs, SteamServerAdvertisement rhs)
    {
        return -base.CompareDetails(lhs, rhs);
    }
}
