using System.Collections.Generic;

namespace SDG.Unturned;

public abstract class ServerListComparer_Base : IComparer<SteamServerAdvertisement>
{
    public int Compare(SteamServerAdvertisement lhs, SteamServerAdvertisement rhs)
    {
        if (lhs.isDeniedByServerCurationRule != rhs.isDeniedByServerCurationRule)
        {
            if (!rhs.isDeniedByServerCurationRule)
            {
                return 1;
            }
            return -1;
        }
        return CompareDetails(lhs, rhs);
    }

    protected abstract int CompareDetails(SteamServerAdvertisement lhs, SteamServerAdvertisement rhs);
}
