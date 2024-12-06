namespace SDG.Unturned;

public class ServerListComparer_PerspectiveDefault : ServerListComparer_Base
{
    protected override int CompareDetails(SteamServerAdvertisement lhs, SteamServerAdvertisement rhs)
    {
        if (lhs.cameraMode == rhs.cameraMode)
        {
            return lhs.name.CompareTo(rhs.name);
        }
        return lhs.cameraMode.CompareTo(rhs.cameraMode);
    }
}
