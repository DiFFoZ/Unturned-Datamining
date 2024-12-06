namespace SDG.Unturned;

/// <summary>
/// Sort servers by max player count low to high.
/// </summary>
public class ServerListComparer_MaxPlayersInverted : ServerListComparer_MaxPlayersDefault
{
    protected override int CompareDetails(SteamServerAdvertisement lhs, SteamServerAdvertisement rhs)
    {
        return -base.CompareDetails(lhs, rhs);
    }
}
