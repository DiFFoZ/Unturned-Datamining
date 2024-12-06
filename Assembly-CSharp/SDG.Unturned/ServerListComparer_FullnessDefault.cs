namespace SDG.Unturned;

/// <summary>
/// Sort servers by normalized player count high to low.
/// </summary>
public class ServerListComparer_FullnessDefault : ServerListComparer_Base
{
    protected override int CompareDetails(SteamServerAdvertisement lhs, SteamServerAdvertisement rhs)
    {
        float normalizedPlayerCount = lhs.NormalizedPlayerCount;
        float normalizedPlayerCount2 = rhs.NormalizedPlayerCount;
        if (MathfEx.IsNearlyEqual(normalizedPlayerCount, normalizedPlayerCount2))
        {
            if (lhs.players == rhs.players)
            {
                return lhs.name.CompareTo(rhs.name);
            }
            return rhs.players - lhs.players;
        }
        return normalizedPlayerCount2.CompareTo(normalizedPlayerCount);
    }
}
