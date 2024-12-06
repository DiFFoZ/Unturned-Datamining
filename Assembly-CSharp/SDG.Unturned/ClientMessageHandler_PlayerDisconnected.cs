using SDG.NetPak;

namespace SDG.Unturned;

internal static class ClientMessageHandler_PlayerDisconnected
{
    internal static void ReadMessage(NetPakReader reader)
    {
        if (reader.ReadNetId(out var value))
        {
            SteamPlayer steamPlayer = NetIdRegistry.Get<SteamPlayer>(value);
            if (steamPlayer != null)
            {
                Provider.RemoveClient(steamPlayer);
            }
            else
            {
                UnturnedLog.info($"Received PlayerDisconnected message for unknown NetID: {value}");
            }
        }
    }
}
