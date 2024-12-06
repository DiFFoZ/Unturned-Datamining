namespace SDG.Unturned;

public class ServerListComparer_PluginsInverted : ServerListComparer_PluginsDefault
{
    protected override int CompareDetails(SteamServerAdvertisement lhs, SteamServerAdvertisement rhs)
    {
        return -base.CompareDetails(lhs, rhs);
    }
}
