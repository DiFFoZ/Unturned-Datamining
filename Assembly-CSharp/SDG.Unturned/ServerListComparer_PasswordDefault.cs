namespace SDG.Unturned;

public class ServerListComparer_PasswordDefault : ServerListComparer_Base
{
    protected override int CompareDetails(SteamServerAdvertisement lhs, SteamServerAdvertisement rhs)
    {
        if (lhs.isPassworded == rhs.isPassworded)
        {
            return lhs.name.CompareTo(rhs.name);
        }
        if (!lhs.isPassworded)
        {
            return -1;
        }
        return 1;
    }
}
