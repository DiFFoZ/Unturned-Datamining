namespace SDG.Unturned;

public class ServerListComparer_CombatInverted : ServerListComparer_CombatDefault
{
    protected override int CompareDetails(SteamServerAdvertisement lhs, SteamServerAdvertisement rhs)
    {
        return -base.CompareDetails(lhs, rhs);
    }
}
