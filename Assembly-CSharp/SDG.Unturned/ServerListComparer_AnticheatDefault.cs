namespace SDG.Unturned;

public class ServerListComparer_AnticheatDefault : ServerListComparer_Base
{
    protected override int CompareDetails(SteamServerAdvertisement lhs, SteamServerAdvertisement rhs)
    {
        if (lhs.IsBattlEyeSecure == rhs.IsBattlEyeSecure)
        {
            if (lhs.IsVACSecure == rhs.IsVACSecure)
            {
                return lhs.name.CompareTo(rhs.name);
            }
            if (!lhs.IsVACSecure)
            {
                return 1;
            }
            return -1;
        }
        if (!lhs.IsBattlEyeSecure)
        {
            return 1;
        }
        return -1;
    }
}
