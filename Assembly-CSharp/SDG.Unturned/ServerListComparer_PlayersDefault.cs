namespace SDG.Unturned;

/// <summary>
/// Sort servers by player count high to low.
/// </summary>
public class ServerListComparer_PlayersDefault : ServerListComparer_Base
{
    protected override int CompareDetails(SteamServerAdvertisement lhs, SteamServerAdvertisement rhs)
    {
        if (lhs.players == rhs.players)
        {
            return lhs.name.CompareTo(rhs.name);
        }
        return rhs.players - lhs.players;
    }
}
