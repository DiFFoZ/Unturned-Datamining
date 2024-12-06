namespace SDG.Unturned;

public class ServerListComparer_CombatDefault : ServerListComparer_Base
{
    protected override int CompareDetails(SteamServerAdvertisement lhs, SteamServerAdvertisement rhs)
    {
        if (lhs.isPvP == rhs.isPvP)
        {
            return lhs.name.CompareTo(rhs.name);
        }
        if (!lhs.isPvP)
        {
            return -1;
        }
        return 1;
    }
}
