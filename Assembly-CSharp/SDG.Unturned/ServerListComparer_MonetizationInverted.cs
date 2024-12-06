namespace SDG.Unturned;

public class ServerListComparer_MonetizationInverted : ServerListComparer_MonetizationDefault
{
    protected override int CompareDetails(SteamServerAdvertisement lhs, SteamServerAdvertisement rhs)
    {
        return -base.CompareDetails(lhs, rhs);
    }
}
