namespace SDG.Unturned;

public class ServerListComparer_GoldInverted : ServerListComparer_GoldDefault
{
    protected override int CompareDetails(SteamServerAdvertisement lhs, SteamServerAdvertisement rhs)
    {
        return -base.CompareDetails(lhs, rhs);
    }
}
