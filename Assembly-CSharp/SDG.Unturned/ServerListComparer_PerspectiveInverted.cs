namespace SDG.Unturned;

public class ServerListComparer_PerspectiveInverted : ServerListComparer_PerspectiveDefault
{
    protected override int CompareDetails(SteamServerAdvertisement lhs, SteamServerAdvertisement rhs)
    {
        return -base.CompareDetails(lhs, rhs);
    }
}
