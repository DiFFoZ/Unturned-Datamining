using Steamworks;
using Unturned.SystemEx;

namespace SDG.Unturned;

internal readonly struct ServerListCurationInput
{
    public readonly string name;

    public readonly IPv4Address address;

    public readonly ushort queryPort;

    public readonly CSteamID steamId;

    public readonly bool hasName;

    public readonly bool hasAddress;

    public readonly bool hasSteamId;

    public ServerListCurationInput(string name, IPv4Address address, ushort queryPort, CSteamID steamId)
    {
        this.name = name;
        this.address = address;
        this.queryPort = queryPort;
        this.steamId = steamId;
        hasName = !string.IsNullOrEmpty(name);
        hasAddress = address != IPv4Address.Zero;
        hasSteamId = steamId.BPersistentGameServerAccount();
    }

    public ServerListCurationInput(SteamServerAdvertisement advertisement)
        : this(advertisement.name, new IPv4Address(advertisement.ip), advertisement.queryPort, advertisement.steamID)
    {
    }
}
