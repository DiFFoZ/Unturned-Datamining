namespace SDG.Unturned;

/// <summary>
/// Sort servers by normalized player count low to high.
/// </summary>
public class ServerListComparer_FullnessInverted : ServerListComparer_FullnessDefault
{
    protected override int CompareDetails(SteamServerAdvertisement lhs, SteamServerAdvertisement rhs)
    {
        return -base.CompareDetails(lhs, rhs);
    }
}
