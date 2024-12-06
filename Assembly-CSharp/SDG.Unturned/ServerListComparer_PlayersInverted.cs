namespace SDG.Unturned;

/// <summary>
/// Sort servers by player count low to high.
/// </summary>
public class ServerListComparer_PlayersInverted : ServerListComparer_PlayersDefault
{
    protected override int CompareDetails(SteamServerAdvertisement lhs, SteamServerAdvertisement rhs)
    {
        return -base.CompareDetails(lhs, rhs);
    }
}
