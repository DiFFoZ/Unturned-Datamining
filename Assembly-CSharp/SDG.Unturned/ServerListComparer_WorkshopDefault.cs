namespace SDG.Unturned;

public class ServerListComparer_WorkshopDefault : ServerListComparer_Base
{
    protected override int CompareDetails(SteamServerAdvertisement lhs, SteamServerAdvertisement rhs)
    {
        if (lhs.isWorkshop == rhs.isWorkshop)
        {
            return lhs.name.CompareTo(rhs.name);
        }
        if (!lhs.isWorkshop)
        {
            return -1;
        }
        return 1;
    }
}
