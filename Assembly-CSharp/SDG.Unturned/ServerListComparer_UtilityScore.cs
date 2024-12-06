namespace SDG.Unturned;

public class ServerListComparer_UtilityScore : ServerListComparer_Base
{
    protected override int CompareDetails(SteamServerAdvertisement lhs, SteamServerAdvertisement rhs)
    {
        if (MathfEx.IsNearlyEqual(lhs.utilityScore, rhs.utilityScore, 0.001f))
        {
            return lhs.steamID.GetHashCode().CompareTo(rhs.steamID.GetHashCode());
        }
        return -lhs.utilityScore.CompareTo(rhs.utilityScore);
    }
}
