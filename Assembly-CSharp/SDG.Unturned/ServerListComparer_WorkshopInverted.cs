namespace SDG.Unturned;

public class ServerListComparer_WorkshopInverted : ServerListComparer_WorkshopDefault
{
    protected override int CompareDetails(SteamServerAdvertisement lhs, SteamServerAdvertisement rhs)
    {
        return -base.CompareDetails(lhs, rhs);
    }
}
