namespace SDG.Unturned;

public class ServerListComparer_GoldDefault : ServerListComparer_Base
{
    protected override int CompareDetails(SteamServerAdvertisement lhs, SteamServerAdvertisement rhs)
    {
        if (lhs.isPro == rhs.isPro)
        {
            return lhs.name.CompareTo(rhs.name);
        }
        if (!lhs.isPro)
        {
            return 1;
        }
        return -1;
    }
}
