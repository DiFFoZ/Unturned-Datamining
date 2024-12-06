namespace SDG.Unturned;

public class ServerListComparer_AnticheatInverted : ServerListComparer_AnticheatDefault
{
    protected override int CompareDetails(SteamServerAdvertisement lhs, SteamServerAdvertisement rhs)
    {
        return -base.CompareDetails(lhs, rhs);
    }
}
