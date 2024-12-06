namespace SDG.Unturned;

public class ServerListComparer_PasswordInverted : ServerListComparer_PasswordDefault
{
    protected override int CompareDetails(SteamServerAdvertisement lhs, SteamServerAdvertisement rhs)
    {
        return -base.CompareDetails(lhs, rhs);
    }
}
