using System;
using System.Collections.Generic;
using System.Net;
using SDG.NetPak;
using SDG.NetTransport;
using SDG.Provider;
using Steamworks;
using UnityEngine;

namespace SDG.Unturned;

public class SteamPlayer : SteamConnectedClientBase
{
    public static Action<SteamPlayer, string, byte[]> OnSteamAuthTicketForWebApiReceived;

    private NetId _netId;

    private SteamPlayerID _playerID;

    private Transform _model;

    private Player _player;

    private bool _isPro;

    private int _channel;

    /// <summary>
    /// Not an actual Steam ID or BattlEye ID, instead this is used to map player references to and from BE.
    /// </summary>
    internal int battlEyeId;

    private bool _isAdmin;

    private float[] pings;

    private float _ping;

    private float _joined;

    public byte face;

    private byte _hair;

    private byte _beard;

    private Color _skin;

    private Color _color;

    private Color _markerColor;

    private bool _hand;

    public int shirtItem;

    public int pantsItem;

    public int hatItem;

    public int backpackItem;

    public int vestItem;

    public int maskItem;

    public int glassesItem;

    public int[] skinItems;

    public string[] skinTags;

    public string[] skinDynamicProps;

    public Dictionary<ushort, int> itemSkins;

    [Obsolete("This will be removed in a future version!")]
    public Dictionary<ushort, int> vehicleSkins;

    private Dictionary<Guid, int> vehicleGuidToSkinItemDefId;

    public HashSet<ushort> modifiedItems;

    private bool submittedModifiedItems;

    private EPlayerSkillset _skillset;

    private string _language;

    public float timeLastPacketWasReceivedFromClient;

    public float timeLastPingRequestWasSentToClient;

    public float lastChat;

    public float nextVote;

    public bool isVoiceChatLocallyMuted;

    public bool isTextChatLocallyMuted;

    [Obsolete("This field should not have been externally used and will be removed in a future version.")]
    public float rpcCredits;

    public float lastReceivedPingRequestRealtime;

    /// <summary>
    /// Next time method is allowed to be called.
    /// </summary>
    public float[] rpcAllowedTimes = new float[NetReflection.rateLimitedMethodsCount];

    /// <summary>
    /// Number of times client has tried to invoke this method while rate-limited.
    /// </summary>
    internal int[] rpcHitCount = new int[NetReflection.rateLimitedMethodsCount];

    internal EClientPlatform clientPlatform;

    private static readonly ClientStaticMethod<string> SendGetSteamAuthTicketForWebApiRequest = ClientStaticMethod<string>.Get(ReceiveGetSteamAuthTicketForWebApiRequest);

    internal static readonly ServerStaticMethod SendGetSteamAuthTicketForWebApiResponse = ServerStaticMethod.Get(ReceiveGetSteamAuthTicketForWebApiResponse);

    internal HashSet<Guid> validatedGuids = new HashSet<Guid>();

    private HashSet<string> requestedSteamAuthTicketIdentities = new HashSet<string>();

    private HashSet<string> receivedSteamAuthTicketIdentities = new HashSet<string>();

    public SteamPlayerID playerID => _playerID;

    public Transform model => _model;

    public Player player => _player;

    public bool isPro
    {
        get
        {
            if (OptionsSettings.streamer && playerID.steamID != Provider.user)
            {
                return false;
            }
            return _isPro;
        }
    }

    public int channel => _channel;

    public bool isAdmin
    {
        get
        {
            if (OptionsSettings.streamer && playerID.steamID != Provider.user)
            {
                return false;
            }
            return _isAdmin;
        }
        set
        {
            _isAdmin = value;
        }
    }

    public float ping => _ping;

    public float joined => _joined;

    public byte hair => _hair;

    public byte beard => _beard;

    public Color skin => _skin;

    public Color color => _color;

    public Color markerColor => _markerColor;

    public bool IsLeftHanded => _hand;

    [Obsolete("Renamed to IsLeftHanded")]
    public bool hand => _hand;

    public EPlayerSkillset skillset => _skillset;

    public string language => _language;

    public CSteamID lobbyID { get; private set; }

    /// <summary>
    /// True for offline or listen server host.
    /// </summary>
    public bool IsLocalServerHost { get; private set; }

    public NetId GetNetId()
    {
        return _netId;
    }

    public void SetVoiceChatLocallyMuted(bool newVoiceChatLocallyMuted)
    {
        if (isVoiceChatLocallyMuted != newVoiceChatLocallyMuted)
        {
            isVoiceChatLocallyMuted = newVoiceChatLocallyMuted;
            LocalPlayerBlocklist.SetVoiceChatMuted(playerID.steamID, isVoiceChatLocallyMuted);
        }
    }

    public void SetTextChatLocallyMuted(bool newTextChatLocallyMuted)
    {
        if (isTextChatLocallyMuted != newTextChatLocallyMuted)
        {
            isTextChatLocallyMuted = newTextChatLocallyMuted;
            LocalPlayerBlocklist.SetTextChatMuted(playerID.steamID, isTextChatLocallyMuted);
        }
    }

    public bool getItemSkinItemDefID(ushort itemID, out int itemdefid)
    {
        itemdefid = 0;
        if (itemSkins == null)
        {
            return false;
        }
        return itemSkins.TryGetValue(itemID, out itemdefid);
    }

    [Obsolete("This will be removed in a future version!")]
    public bool getVehicleSkinItemDefID(ushort vehicleID, out int itemdefid)
    {
        itemdefid = 0;
        if (vehicleSkins == null)
        {
            return false;
        }
        return vehicleSkins.TryGetValue(vehicleID, out itemdefid);
    }

    /// <summary>
    /// Get Steam item definition ID equipped for given vehicle.
    /// </summary>
    /// <returns>True if a skin was available.</returns>
    public bool GetVehicleSkinItemDefId(InteractableVehicle vehicle, out int itemdefid)
    {
        itemdefid = 0;
        if (vehicle == null || vehicleGuidToSkinItemDefId == null)
        {
            return false;
        }
        VehicleAsset vehicleAsset = vehicle.asset.FindSharedSkinVehicleAsset();
        if (vehicleAsset == null)
        {
            return false;
        }
        return vehicleGuidToSkinItemDefId.TryGetValue(vehicleAsset.GUID, out itemdefid);
    }

    public bool getTagsAndDynamicPropsForItem(int item, out string tags, out string dynamic_props)
    {
        tags = string.Empty;
        dynamic_props = string.Empty;
        for (int i = 0; i < skinItems.Length; i++)
        {
            if (skinItems[i] == item)
            {
                if (i < skinTags.Length && i < skinDynamicProps.Length)
                {
                    tags = skinTags[i];
                    dynamic_props = skinDynamicProps[i];
                    return true;
                }
                return false;
            }
        }
        return false;
    }

    /// <summary>
    /// Build econ details struct from tags and dynamic_props.
    /// Note that details cannot be modified because it's a struct and has copies of the data.
    /// </summary>
    public bool getDynamicEconDetails(ushort itemID, out DynamicEconDetails details)
    {
        if (!getItemSkinItemDefID(itemID, out var itemdefid))
        {
            details = default(DynamicEconDetails);
            return false;
        }
        return getDynamicEconDetailsForItemDef(itemdefid, out details);
    }

    public bool getDynamicEconDetailsForItemDef(int itemdefid, out DynamicEconDetails details)
    {
        if (!getTagsAndDynamicPropsForItem(itemdefid, out var tags, out var dynamic_props))
        {
            details = default(DynamicEconDetails);
            return false;
        }
        details = new DynamicEconDetails(tags, dynamic_props);
        return true;
    }

    public bool getStatTrackerValue(ushort itemID, out EStatTrackerType type, out int kills)
    {
        if (!getDynamicEconDetails(itemID, out var details))
        {
            type = EStatTrackerType.NONE;
            kills = -1;
            return false;
        }
        return details.getStatTrackerValue(out type, out kills);
    }

    public bool getRagdollEffect(ushort itemID, out ERagdollEffect effect)
    {
        if (!getDynamicEconDetails(itemID, out var details))
        {
            effect = ERagdollEffect.NONE;
            return false;
        }
        return details.getRagdollEffect(out effect);
    }

    public ushort getParticleEffectForItemDef(int itemdefid)
    {
        if (getDynamicEconDetailsForItemDef(itemdefid, out var details))
        {
            return details.getParticleEffect();
        }
        return 0;
    }

    public void incrementStatTrackerValue(ushort itemID, EPlayerStat stat)
    {
        if (!getItemSkinItemDefID(itemID, out var itemdefid) || !getTagsAndDynamicPropsForItem(itemdefid, out var tags, out var dynamic_props))
        {
            return;
        }
        DynamicEconDetails dynamicEconDetails = new DynamicEconDetails(tags, dynamic_props);
        if (!dynamicEconDetails.getStatTrackerValue(out var type, out var kills))
        {
            return;
        }
        switch (type)
        {
        default:
            return;
        case EStatTrackerType.TOTAL:
            if (stat != EPlayerStat.KILLS_ANIMALS && stat != EPlayerStat.KILLS_PLAYERS && stat != EPlayerStat.KILLS_ZOMBIES_MEGA && stat != EPlayerStat.KILLS_ZOMBIES_NORMAL)
            {
                return;
            }
            break;
        case EStatTrackerType.PLAYER:
            if (stat != EPlayerStat.KILLS_PLAYERS)
            {
                return;
            }
            break;
        }
        if (!modifiedItems.Contains(itemID))
        {
            modifiedItems.Add(itemID);
        }
        kills++;
        for (int i = 0; i < skinItems.Length; i++)
        {
            if (skinItems[i] == itemdefid)
            {
                if (i < skinDynamicProps.Length)
                {
                    skinDynamicProps[i] = dynamicEconDetails.getPredictedDynamicPropsJsonForStatTracker(type, kills);
                }
                break;
            }
        }
    }

    public void commitModifiedDynamicProps()
    {
        if (modifiedItems.Count < 1 || submittedModifiedItems)
        {
            return;
        }
        submittedModifiedItems = true;
        SteamInventoryUpdateHandle_t handle = SteamInventory.StartUpdateProperties();
        int num = 0;
        foreach (ushort modifiedItem in modifiedItems)
        {
            if (Characters.getPackageForItemID(modifiedItem, out var itemInstanceId) && getStatTrackerValue(modifiedItem, out var type, out var kills))
            {
                string statTrackerPropertyName = Provider.provider.economyService.getStatTrackerPropertyName(type);
                if (!string.IsNullOrEmpty(statTrackerPropertyName))
                {
                    SteamInventory.SetProperty(handle, new SteamItemInstanceID_t(itemInstanceId), statTrackerPropertyName, kills);
                    num++;
                }
            }
        }
        SteamInventory.SubmitUpdateProperties(handle, out Provider.provider.economyService.commitResult);
        UnturnedLog.info($"Submitted {num} item property update(s)");
    }

    /// <summary>
    /// Add a recent ping sample to the average ping window.
    /// Updates ping based on the average of several recent ping samples.
    /// </summary>
    /// <param name="value">Most recent ping value.</param>
    public void lag(float value)
    {
        value = Mathf.Clamp01(value);
        _ping = value;
        for (int num = pings.Length - 1; num > 0; num--)
        {
            pings[num] = pings[num - 1];
            if (pings[num] > 0.001f)
            {
                _ping += pings[num];
            }
        }
        _ping /= pings.Length;
        pings[0] = value;
    }

    /// <returns>True if both players exist, are both members of groups, and are both members of the same group.</returns>
    public bool isMemberOfSameGroupAs(Player other)
    {
        if (player != null && other != null)
        {
            return player.quests.isMemberOfSameGroupAs(other);
        }
        return false;
    }

    /// <returns>True if both players exist, are both members of groups, and are both members of the same group.</returns>
    public bool isMemberOfSameGroupAs(SteamPlayer other)
    {
        if (other != null)
        {
            return isMemberOfSameGroupAs(other.player);
        }
        return false;
    }

    /// <summary>
    /// Get real IPv4 address of remote player NOT the relay server.
    /// </summary>
    /// <returns>True if address was available, and not flagged as a relay server.</returns>
    public bool getIPv4Address(out uint address)
    {
        if (base.transportConnection != null)
        {
            return base.transportConnection.TryGetIPv4Address(out address);
        }
        address = 0u;
        return false;
    }

    /// <summary>
    /// See above, returns zero if failed.
    /// </summary>
    public uint getIPv4AddressOrZero()
    {
        getIPv4Address(out var address);
        return address;
    }

    /// <summary>
    /// Get real address of remote player NOT a relay server.
    /// </summary>
    /// <returns>Null if address was unavailable.</returns>
    public IPAddress getAddress()
    {
        if (base.transportConnection != null)
        {
            return base.transportConnection.GetAddress();
        }
        return null;
    }

    /// <summary>
    /// Get string representation of remote end point.
    /// </summary>
    /// <returns>Null if address was unavailable.</returns>
    public string getAddressString(bool withPort)
    {
        if (base.transportConnection != null)
        {
            return base.transportConnection.GetAddressString(withPort);
        }
        return null;
    }

    public bool Equals(SteamPlayer otherClient)
    {
        if (otherClient != null)
        {
            return playerID.Equals(otherClient.playerID);
        }
        return false;
    }

    public override bool Equals(object obj)
    {
        return Equals(obj as SteamPlayer);
    }

    public override int GetHashCode()
    {
        return playerID.GetHashCode();
    }

    /// <summary>
    /// Players can set a "nickname" which is only shown to the members in their group.
    /// </summary>
    internal string GetLocalDisplayName()
    {
        if (!string.IsNullOrEmpty(playerID.nickName) && playerID.steamID != Provider.client && player != null && player.quests != null && Player.player != null && player.quests.isMemberOfSameGroupAs(Player.player))
        {
            return playerID.nickName;
        }
        return playerID.characterName;
    }

    /// <summary>
    /// Can be used by plugins to verify player is on a particular server.
    ///
    /// OnSteamAuthTicketForWebApiReceived will be invoked when the response is received.
    /// Note that the client doesn't send anything if the request to Steam fails, so plugins may wish to kick
    /// players if a certain amount of time passes. (e.g., if a cheat is canceling the request)
    /// </summary>
    public void RequestSteamAuthTicketForWebApi(string identity)
    {
        if (string.IsNullOrWhiteSpace(identity))
        {
            throw new ArgumentException("cannot be null or empty", "identity");
        }
        if (requestedSteamAuthTicketIdentities.Add(identity))
        {
            UnturnedLog.info($"Sending request to {base.transportConnection} for Steam auth ticket for web API identity \"{identity}\"");
            SendGetSteamAuthTicketForWebApiRequest.Invoke(ENetReliability.Reliable, base.transportConnection, identity);
        }
    }

    [SteamCall(ESteamCallValidation.ONLY_FROM_SERVER)]
    public static void ReceiveGetSteamAuthTicketForWebApiRequest(string identity)
    {
        UnturnedLog.info("Received request to get Steam auth ticket for web API identity \"" + identity + "\"");
        Provider.RequestSteamAuthTicketForWebApi(identity);
    }

    [SteamCall(ESteamCallValidation.SERVERSIDE)]
    public static void ReceiveGetSteamAuthTicketForWebApiResponse(in ServerInvocationContext context)
    {
        NetPakReader reader = context.reader;
        SteamPlayer callingPlayer = context.GetCallingPlayer();
        if (!reader.ReadString(out var value, 5))
        {
            context.Kick("Unable to read Steam auth ticket web API identity");
            return;
        }
        if (!callingPlayer.requestedSteamAuthTicketIdentities.Contains(value))
        {
            context.Kick("Server did not request Steam auth ticket for provided web API identity");
            return;
        }
        if (!callingPlayer.receivedSteamAuthTicketIdentities.Add(value))
        {
            context.Kick("Client sent duplicate Steam auth ticket for web API response");
            return;
        }
        if (!reader.ReadUInt16(out var value2))
        {
            context.Kick("Unable to read Steam web API auth ticket length");
            return;
        }
        if (value2 > 2560)
        {
            context.Kick("Steam web API auth ticket longer than maximum");
            return;
        }
        byte[] array = new byte[value2];
        if (!reader.ReadBytes(array))
        {
            context.Kick("Unable to read Steam web API auth ticket contents");
            return;
        }
        UnturnedLog.info($"Received response from {callingPlayer.transportConnection} for Steam auth ticket for web API identity \"{value}\" length: {value2}");
        OnSteamAuthTicketForWebApiReceived?.TryInvoke("OnSteamAuthTicketForWebApiReceived", callingPlayer, value, array);
    }

    public SteamPlayer(ITransportConnection transportConnection, NetId netId, SteamPlayerID newPlayerID, Transform newModel, bool newPro, bool newAdmin, int newChannel, byte newFace, byte newHair, byte newBeard, Color newSkin, Color newColor, Color newMarkerColor, bool newHand, int newShirtItem, int newPantsItem, int newHatItem, int newBackpackItem, int newVestItem, int newMaskItem, int newGlassesItem, int[] newSkinItems, string[] newSkinTags, string[] newSkinDynamicProps, EPlayerSkillset newSkillset, string newLanguage, CSteamID newLobbyID, EClientPlatform clientPlatform)
    {
        base.transportConnection = transportConnection;
        _netId = netId;
        NetIdRegistry.Assign(_netId, this);
        IsLocalServerHost = transportConnection != null && !Dedicator.IsDedicatedServer;
        bool flag = newPlayerID.steamID == Provider.client;
        if (!flag && !Dedicator.IsDedicatedServer)
        {
            LocalPlayerBlocklist.GetBlockStatus(newPlayerID.steamID, out isVoiceChatLocallyMuted, out isTextChatLocallyMuted);
        }
        _playerID = newPlayerID;
        _model = newModel;
        model.name = playerID.characterName + " [" + playerID.playerName + "]";
        model.GetComponent<SteamChannel>().id = newChannel;
        model.GetComponent<SteamChannel>().owner = this;
        model.GetComponent<SteamChannel>().IsLocalPlayer = flag;
        model.GetComponent<SteamChannel>().setup();
        _player = model.GetComponent<Player>();
        _player.AssignNetIdBlock(_netId);
        _isPro = newPro;
        _channel = newChannel;
        isAdmin = newAdmin;
        face = newFace;
        _hair = newHair;
        _beard = newBeard;
        _skin = newSkin;
        _color = newColor;
        _markerColor = newMarkerColor;
        _hand = newHand;
        _skillset = newSkillset;
        _language = newLanguage;
        shirtItem = newShirtItem;
        pantsItem = newPantsItem;
        hatItem = newHatItem;
        backpackItem = newBackpackItem;
        vestItem = newVestItem;
        maskItem = newMaskItem;
        glassesItem = newGlassesItem;
        skinItems = newSkinItems;
        skinTags = newSkinTags;
        skinDynamicProps = newSkinDynamicProps;
        itemSkins = new Dictionary<ushort, int>();
        vehicleSkins = new Dictionary<ushort, int>();
        vehicleGuidToSkinItemDefId = new Dictionary<Guid, int>();
        modifiedItems = new HashSet<ushort>();
        for (int i = 0; i < skinItems.Length; i++)
        {
            int num = skinItems[i];
            if (num == 0)
            {
                continue;
            }
            Provider.provider.economyService.getInventoryTargetID(num, out var item_guid, out var vehicle_guid);
            ItemAsset itemAsset = Assets.find<ItemAsset>(item_guid);
            VehicleAsset vehicleAsset = VehicleTool.FindVehicleByGuidAndHandleRedirects(vehicle_guid);
            if (itemAsset != null)
            {
                if (!itemSkins.ContainsKey(itemAsset.id))
                {
                    itemSkins.Add(itemAsset.id, num);
                }
            }
            else if (vehicleAsset != null)
            {
                if (!vehicleSkins.ContainsKey(vehicleAsset.id))
                {
                    vehicleSkins.Add(vehicleAsset.id, num);
                }
                vehicleGuidToSkinItemDefId[vehicleAsset.GUID] = num;
            }
        }
        pings = new float[4];
        timeLastPacketWasReceivedFromClient = Time.realtimeSinceStartup;
        lastChat = Time.realtimeSinceStartup;
        nextVote = Time.realtimeSinceStartup;
        lastReceivedPingRequestRealtime = Time.realtimeSinceStartup;
        _joined = Time.realtimeSinceStartup;
        lobbyID = newLobbyID;
        this.clientPlatform = clientPlatform;
    }
}
