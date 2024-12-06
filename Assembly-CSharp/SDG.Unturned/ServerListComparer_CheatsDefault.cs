namespace SDG.Unturned;

public class ServerListComparer_CheatsDefault : ServerListComparer_Base
{
    protected override int CompareDetails(SteamServerAdvertisement lhs, SteamServerAdvertisement rhs)
    {
        if (lhs.hasCheats == rhs.hasCheats)
        {
            return lhs.name.CompareTo(rhs.name);
        }
        if (!lhs.hasCheats)
        {
            return -1;
        }
        return 1;
    }
}
