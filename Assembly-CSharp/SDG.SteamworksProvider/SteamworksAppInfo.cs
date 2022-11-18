namespace SDG.SteamworksProvider;

public class SteamworksAppInfo
{
    public uint id { get; protected set; }

    public string name { get; protected set; }

    public string version { get; protected set; }

    public bool isDedicated { get; protected set; }

    public SteamworksAppInfo(uint newID, string newName, string newVersion, bool newIsDedicated)
    {
        id = newID;
        name = newName;
        version = newVersion;
        isDedicated = newIsDedicated;
    }
}
