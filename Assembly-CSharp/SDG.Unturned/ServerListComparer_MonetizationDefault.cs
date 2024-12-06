namespace SDG.Unturned;

public class ServerListComparer_MonetizationDefault : ServerListComparer_Base
{
    private int[] orderMap;

    protected override int CompareDetails(SteamServerAdvertisement lhs, SteamServerAdvertisement rhs)
    {
        if (lhs.monetization == rhs.monetization)
        {
            return lhs.name.CompareTo(rhs.name);
        }
        int num = orderMap[(int)lhs.monetization];
        int num2 = orderMap[(int)rhs.monetization];
        return num - num2;
    }

    public ServerListComparer_MonetizationDefault()
    {
        orderMap = new int[5];
        orderMap[2] = 0;
        orderMap[3] = 1;
        orderMap[0] = 2;
        orderMap[4] = 3;
        orderMap[1] = 4;
    }
}
