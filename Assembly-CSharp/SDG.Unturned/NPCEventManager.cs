using System;
using SDG.NetTransport;

namespace SDG.Unturned;

public class NPCEventManager
{
    private static readonly ClientStaticMethod<byte, string> SendBroadcast = ClientStaticMethod<byte, string>.Get(ReceiveBroadcast);

    [Obsolete("onEvent provides the instigating player.")]
    public static event NPCEventTriggeredHandler eventTriggered;

    public static event NPCEventHandler onEvent;

    [Obsolete("broadcastEvent provides the instigating player.")]
    public static void triggerEventTriggered(string id)
    {
        if (!string.IsNullOrEmpty(id))
        {
            NPCEventManager.eventTriggered?.Invoke(id);
        }
    }

    public static void broadcastEvent(Player instigatingPlayer, string eventId)
    {
        broadcastEvent(instigatingPlayer, eventId, shouldReplicate: false);
    }

    public static void broadcastEvent(Player instigatingPlayer, string eventId, bool shouldReplicate = false)
    {
        if (!string.IsNullOrEmpty(eventId))
        {
            try
            {
                NPCEventManager.onEvent?.Invoke(instigatingPlayer, eventId);
            }
            catch (Exception e)
            {
                UnturnedLog.exception(e, "Exception raised during server NPC event \"{0}\"", eventId);
            }
            if (shouldReplicate)
            {
                byte arg = (byte)((instigatingPlayer != null && instigatingPlayer.channel != null && instigatingPlayer.channel.owner != null) ? ((byte)instigatingPlayer.channel.owner.channel) : 0);
                SendBroadcast.Invoke(ENetReliability.Reliable, Provider.GatherRemoteClientConnections(), arg, eventId);
            }
        }
    }

    [SteamCall(ESteamCallValidation.ONLY_FROM_SERVER)]
    public static void ReceiveBroadcast(byte channelId, string eventId)
    {
        Player instigatingPlayer = PlayerTool.findSteamPlayerByChannel(channelId)?.player;
        try
        {
            NPCEventManager.onEvent?.Invoke(instigatingPlayer, eventId);
        }
        catch (Exception e)
        {
            UnturnedLog.exception(e, "Exception raised during client NPC event \"{0}\"", eventId);
        }
    }
}
