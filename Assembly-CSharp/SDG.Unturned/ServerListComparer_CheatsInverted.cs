namespace SDG.Unturned;

public class ServerListComparer_CheatsInverted : ServerListComparer_CheatsDefault
{
    protected override int CompareDetails(SteamServerAdvertisement lhs, SteamServerAdvertisement rhs)
    {
        return -base.CompareDetails(lhs, rhs);
    }
}
