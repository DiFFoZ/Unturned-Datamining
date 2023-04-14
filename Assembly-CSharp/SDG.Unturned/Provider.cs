using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using BattlEye;
using SDG.Framework.Modules;
using SDG.HostBans;
using SDG.NetPak;
using SDG.NetTransport;
using SDG.NetTransport.Loopback;
using SDG.Provider;
using SDG.Provider.Services.Multiplayer;
using SDG.SteamworksProvider;
using Steamworks;
using UnityEngine;
using UnityEngine.Networking;
using Unturned.SystemEx;
using Unturned.UnityEx;

namespace SDG.Unturned;

public class Provider : MonoBehaviour
{
    public delegate void BattlEyeKickCallback(SteamPlayer client, string reason);

    internal struct ServerRequiredWorkshopFile
    {
        public ulong fileId;

        public DateTime timestamp;
    }

    public delegate void LoginSpawningHandler(SteamPlayerID playerID, ref Vector3 point, ref float yaw, ref EPlayerStance initialStance, ref bool needsNewSpawnpoint);

    public delegate void CommenceShutdownHandler();

    [Obsolete]
    public delegate void ServerWritingPacketHandler(CSteamID remoteSteamId, ESteamPacket type, byte[] payload, int size, int channel);

    internal struct WorkshopRequestLog
    {
        public int sender;

        public float realTime;
    }

    internal class CachedWorkshopResponse
    {
        public ENPCHoliday holiday;

        public CSteamID server;

        public uint ip;

        public List<ServerRequiredWorkshopFile> requiredFiles = new List<ServerRequiredWorkshopFile>();

        public float realTime;

        internal bool FindRequiredFile(ulong fileId, out ServerRequiredWorkshopFile details)
        {
            foreach (ServerRequiredWorkshopFile requiredFile in requiredFiles)
            {
                if (requiredFile.fileId == fileId)
                {
                    details = requiredFile;
                    return true;
                }
            }
            details = default(ServerRequiredWorkshopFile);
            return false;
        }
    }

    public delegate void ServerReadingPacketHandler(CSteamID remoteSteamId, byte[] payload, int offset, int size, int channel);

    public delegate void ServerConnected(CSteamID steamID);

    public delegate void ServerDisconnected(CSteamID steamID);

    public delegate void ServerHosted();

    public delegate void ServerShutdown();

    [Obsolete]
    public delegate void CheckValid(ValidateAuthTicketResponse_t callback, ref bool isValid);

    public delegate void CheckValidWithExplanation(ValidateAuthTicketResponse_t callback, ref bool isValid, ref string explanation);

    public delegate void CheckBanStatusHandler(CSteamID steamID, uint remoteIP, ref bool isBanned, ref string banReason, ref uint banRemainingDuration);

    public delegate void CheckBanStatusWithHWIDHandler(SteamPlayerID playerID, uint remoteIP, ref bool isBanned, ref string banReason, ref uint banRemainingDuration);

    public delegate void RequestBanPlayerHandler(CSteamID instigator, CSteamID playerToBan, uint ipToBan, ref string reason, ref uint duration, ref bool shouldVanillaBan);

    public delegate void RequestBanPlayerHandlerV2(CSteamID instigator, CSteamID playerToBan, uint ipToBan, IEnumerable<byte[]> hwidsToBan, ref string reason, ref uint duration, ref bool shouldVanillaBan);

    public delegate void RequestUnbanPlayerHandler(CSteamID instigator, CSteamID playerToUnban, ref bool shouldVanillaUnban);

    public delegate void QueuePositionUpdated();

    public delegate void RejectingPlayerCallback(CSteamID steamID, ESteamRejection rejection, string explanation);

    private class CachedFavorite
    {
        public uint ip;

        public ushort queryPort;

        public bool isFavorited;

        public bool matchesServer(uint ip, ushort queryPort)
        {
            if (this.ip == ip)
            {
                return this.queryPort == queryPort;
            }
            return false;
        }
    }

    public delegate void ClientConnected();

    public delegate void ClientDisconnected();

    public delegate void EnemyConnected(SteamPlayer player);

    public delegate void EnemyDisconnected(SteamPlayer player);

    public delegate void BackendRealtimeAvailableHandler();

    public delegate void IconQueryCallback(Texture2D icon, bool responsibleForDestroy);

    public struct IconQueryParams
    {
        public string url;

        public IconQueryCallback callback;

        public bool shouldCache;

        public IconQueryParams(string url, IconQueryCallback callback, bool shouldCache = true)
        {
            this.url = url;
            this.callback = callback;
            this.shouldCache = shouldCache;
        }
    }

    private class PendingIconRequest
    {
        public string url;

        public IconQueryCallback callback;

        public bool shouldCache;
    }

    public static readonly string STEAM_IC = "Steam";

    public static readonly string STEAM_DC = "<color=#2784c6>Steam</color>";

    public static readonly AppId_t APP_ID = new AppId_t(304930u);

    public static readonly AppId_t PRO_ID = new AppId_t(306460u);

    public static readonly string APP_NAME = "Unturned";

    public static readonly string APP_AUTHOR = "Nelson Sexton";

    public static readonly int CLIENT_TIMEOUT = 30;

    internal static readonly float PING_REQUEST_INTERVAL = 1f;

    private static bool isCapturingScreenshot;

    private static StaticResourceRef<Material> screenshotBlitMaterial = new StaticResourceRef<Material>("Materials/ScreenshotBlit");

    private static Callback<ScreenshotRequested_t> screenshotRequestedCallback;

    private static string privateLanguage;

    internal static bool languageIsEnglish;

    private static string _path;

    public static Local localization;

    internal static IntPtr battlEyeClientHandle = IntPtr.Zero;

    internal static BEClient.BECL_GAME_DATA battlEyeClientInitData = null;

    internal static BEClient.BECL_BE_DATA battlEyeClientRunData = null;

    private static bool battlEyeHasRequiredRestart = false;

    internal static readonly NetLength battlEyeBufferSize = new NetLength(4095u);

    internal static IntPtr battlEyeServerHandle = IntPtr.Zero;

    internal static BEServer.BESV_GAME_DATA battlEyeServerInitData = null;

    internal static BEServer.BESV_BE_DATA battlEyeServerRunData = null;

    private static uint _bytesSent;

    private static uint _bytesReceived;

    private static uint _packetsSent;

    private static uint _packetsReceived;

    private static SteamServerInfo _currentServerInfo;

    private static CSteamID _server;

    private static CSteamID _client;

    private static CSteamID _user;

    private static byte[] _clientHash;

    private static string _clientName;

    internal static List<SteamPlayer> _clients = new List<SteamPlayer>();

    public static List<SteamPending> pending = new List<SteamPending>();

    private static bool _isServer;

    private static bool _isClient;

    private static bool _isPro;

    private static bool _isConnected;

    internal static bool isWaitingForWorkshopResponse;

    internal static bool isWaitingForAuthenticationResponse;

    internal static double sentAuthenticationRequestTime;

    private static List<PublishedFileId_t> waitingForExpectedWorkshopItems;

    internal static ENPCHoliday authorityHoliday;

    private static CachedWorkshopResponse currentServerWorkshopResponse;

    private static List<ulong> _serverWorkshopFileIDs = new List<ulong>();

    internal static List<ServerRequiredWorkshopFile> serverRequiredWorkshopFiles = new List<ServerRequiredWorkshopFile>();

    public static bool isLoadingUGC;

    public static bool isLoadingInventory;

    private static int nextPlayerChannelId = 2;

    public static ESteamConnectionFailureInfo _connectionFailureInfo;

    internal static string _connectionFailureReason;

    internal static uint _connectionFailureDuration;

    private static List<SteamChannel> _receivers = new List<SteamChannel>();

    internal static byte[] buffer = new byte[Block.BUFFER_SIZE];

    internal static List<SDG.Framework.Modules.Module> critMods = new List<SDG.Framework.Modules.Module>();

    private static StringBuilder modBuilder = new StringBuilder();

    private static int nextBattlEyePlayerId = 1;

    public static LoginSpawningHandler onLoginSpawning;

    internal static bool isWaitingForConnectResponse;

    private static float sentConnectRequestTime;

    internal static readonly NetLength MAX_SKINS_LENGTH = new NetLength(127u);

    internal static IClientTransport clientTransport;

    private static IServerTransport serverTransport;

    private static int countShutdownTimer = -1;

    private static string shutdownMessage = string.Empty;

    private static float lastTimerMessage;

    internal static bool didServerShutdownTimerReachZero;

    private static bool isServerConnectedToSteam;

    internal static BuiltinAutoShutdown autoShutdownManager = null;

    private static IDedicatedWorkshopUpdateMonitor dswUpdateMonitor = null;

    private static bool isDedicatedUGCInstalled;

    private const int STEAM_KEYVALUE_MAX_VALUE_LENGTH = 127;

    [Obsolete]
    public static ServerWritingPacketHandler onServerWritingPacket;

    internal static List<WorkshopRequestLog> workshopRequests = new List<WorkshopRequestLog>();

    internal static List<CachedWorkshopResponse> cachedWorkshopResponses = new List<CachedWorkshopResponse>();

    private static List<CSteamID> netIgnoredSteamIDs = new List<CSteamID>();

    private static CommandLineFlag _constNetEvents = new CommandLineFlag(defaultValue: false, "-ConstNetEvents");

    [Obsolete]
    public static ServerReadingPacketHandler onServerReadingPacket;

    private List<SteamPlayer> clientsWithBadConnecion = new List<SteamPlayer>();

    public static ServerConnected onServerConnected;

    public static ServerDisconnected onServerDisconnected;

    public static ServerHosted onServerHosted;

    public static ServerShutdown onServerShutdown;

    private static Callback<P2PSessionConnectFail_t> p2pSessionConnectFail;

    [Obsolete("onCheckValidWithExplanation takes priority if bound")]
    public static CheckValid onCheckValid;

    public static CheckValidWithExplanation onCheckValidWithExplanation;

    [Obsolete]
    public static CheckBanStatusHandler onCheckBanStatus;

    public static CheckBanStatusWithHWIDHandler onCheckBanStatusWithHWID;

    [Obsolete("V2 provides list of HWIDs to ban")]
    public static RequestBanPlayerHandler onBanPlayerRequested;

    public static RequestBanPlayerHandlerV2 onBanPlayerRequestedV2;

    public static RequestUnbanPlayerHandler onUnbanPlayerRequested;

    private static Callback<ValidateAuthTicketResponse_t> validateAuthTicketResponse;

    private static Callback<GSClientGroupStatus_t> clientGroupStatus;

    private static CommandLineInt clMaxPlayersLimit = new CommandLineInt("-MaxPlayersLimit");

    private static byte _maxPlayers;

    public static byte queueSize;

    internal static byte _queuePosition;

    public static QueuePositionUpdated onQueuePositionUpdated;

    private static string _serverName;

    public static uint ip;

    public static string bindAddress;

    public static ushort port;

    internal static byte[] _serverPasswordHash;

    private static string _serverPassword;

    public static string map;

    public static bool isPvP;

    public static bool isWhitelisted;

    public static bool hideAdmins;

    public static bool hasCheats;

    public static bool filterName;

    public static EGameMode mode;

    public static bool isGold;

    public static GameMode gameMode;

    public static ECameraMode cameraMode;

    private static StatusData _statusData;

    private static PreferenceData _preferenceData;

    private static ConfigData _configData;

    internal static ModeConfigData _modeConfigData;

    private int clientsKickedForTransportConnectionFailureCount;

    private static uint STEAM_FAVORITE_FLAG_FAVORITE = 1u;

    internal static uint STEAM_FAVORITE_FLAG_HISTORY = 2u;

    private static List<CachedFavorite> cachedFavorites = new List<CachedFavorite>();

    public static ClientConnected onClientConnected;

    public static ClientDisconnected onClientDisconnected;

    public static EnemyConnected onEnemyConnected;

    public static EnemyDisconnected onEnemyDisconnected;

    private static Callback<PersonaStateChange_t> personaStateChange;

    private static Callback<GameServerChangeRequested_t> gameServerChangeRequested;

    private static Callback<GameRichPresenceJoinRequested_t> gameRichPresenceJoinRequested;

    private static HAuthTicket ticketHandle = HAuthTicket.Invalid;

    private static float lastPingRequestTime;

    private static float lastQueueNotificationTime;

    internal static float timeLastPingRequestWasSentToServer;

    public static readonly float EPSILON = 0.01f;

    public static readonly float UPDATE_TIME = 0.08f;

    public static readonly float UPDATE_DELAY = 0.1f;

    public static readonly float UPDATE_DISTANCE = 0.01f;

    public static readonly uint UPDATES = 1u;

    public static readonly float LERP = 3f;

    internal const float INTERP_SPEED = 10f;

    private static float[] pings;

    private static float _ping;

    private static Provider steam;

    private static bool _isInitialized;

    private static uint timeOffset;

    private static uint _time;

    private static uint initialBackendRealtimeSeconds;

    private static float initialLocalRealtime;

    private static DateTime unixEpochDateTime = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public static BackendRealtimeAvailableHandler onBackendRealtimeAvailable;

    private static SteamAPIWarningMessageHook_t apiWarningMessageHook;

    private static int debugUpdates;

    public static int debugUPS;

    private static float debugLastUpdate;

    private static int debugTicks;

    public static int debugTPS;

    private static float debugLastTick;

    private static Dictionary<string, Texture2D> downloadedIconCache = new Dictionary<string, Texture2D>();

    private static Dictionary<string, PendingIconRequest> pendingCachableIconRequests = new Dictionary<string, PendingIconRequest>();

    internal static CommandLineFlag allowWebRequests = new CommandLineFlag(defaultValue: true, "-NoWebRequests");

    private static bool wasQuitGameCalled;

    private static CommandLineFlag shouldCheckForGoldUpgrade = new CommandLineFlag(defaultValue: true, "-NoGoldUpgrade");

    public static string APP_VERSION { get; protected set; }

    public static uint APP_VERSION_PACKED { get; protected set; }

    public static string language
    {
        get
        {
            return privateLanguage;
        }
        private set
        {
            privateLanguage = value;
            languageIsEnglish = value == "English";
        }
    }

    public static string path => _path;

    public static string localizationRoot { get; private set; }

    public static List<string> streamerNames { get; private set; }

    public static uint bytesSent => _bytesSent;

    public static uint bytesReceived => _bytesReceived;

    public static uint packetsSent => _packetsSent;

    public static uint packetsReceived => _packetsReceived;

    public static SteamServerInfo currentServerInfo => _currentServerInfo;

    public static CSteamID server => _server;

    public static CSteamID client => _client;

    public static CSteamID user => _user;

    public static byte[] clientHash => _clientHash;

    public static string clientName => _clientName;

    public static List<SteamPlayer> clients => _clients;

    [Obsolete]
    public static List<SteamPlayer> players => clients;

    public static bool isServer => _isServer;

    public static bool isClient => _isClient;

    public static bool isPro => _isPro;

    public static bool isConnected => _isConnected;

    public static bool isLoading => isLoadingUGC;

    [Obsolete]
    public static int channels => 0;

    public static ESteamConnectionFailureInfo connectionFailureInfo
    {
        get
        {
            return _connectionFailureInfo;
        }
        set
        {
            _connectionFailureInfo = value;
        }
    }

    public static string connectionFailureReason
    {
        get
        {
            return _connectionFailureReason;
        }
        set
        {
            _connectionFailureReason = value;
        }
    }

    public static uint connectionFailureDuration => _connectionFailureDuration;

    public static List<SteamChannel> receivers => _receivers;

    public static bool hasRoomForNewConnection
    {
        get
        {
            if (clients.Count >= maxPlayers)
            {
                return pending.Count < queueSize;
            }
            return true;
        }
    }

    internal static bool IsBattlEyeEnabled
    {
        get
        {
            if (configData != null && configData.Server.BattlEye_Secure)
            {
                return !Dedicator.offlineOnly;
            }
            return false;
        }
    }

    public static bool useConstNetEvents => _constNetEvents;

    public static byte maxPlayers
    {
        get
        {
            return _maxPlayers;
        }
        set
        {
            _maxPlayers = value;
            if (clMaxPlayersLimit.hasValue && maxPlayers > clMaxPlayersLimit.value)
            {
                _maxPlayers = (byte)clMaxPlayersLimit.value;
                UnturnedLog.info("Clamped max players down from {0} to {1}", value, clMaxPlayersLimit.value);
            }
            if (isServer)
            {
                SteamGameServer.SetMaxPlayerCount(maxPlayers);
            }
        }
    }

    public static byte queuePosition => _queuePosition;

    public static string serverName
    {
        get
        {
            return _serverName;
        }
        set
        {
            _serverName = value;
            if (Dedicator.commandWindow != null)
            {
                Dedicator.commandWindow.title = serverName;
            }
            if (isServer)
            {
                SteamGameServer.SetServerName(serverName);
            }
        }
    }

    public static string serverID
    {
        get
        {
            return Dedicator.serverID;
        }
        set
        {
            Dedicator.serverID = value;
        }
    }

    public static byte[] serverPasswordHash => _serverPasswordHash;

    public static string serverPassword
    {
        get
        {
            return _serverPassword;
        }
        set
        {
            _serverPassword = value;
            _serverPasswordHash = Hash.SHA1(serverPassword);
            if (isServer)
            {
                SteamGameServer.SetPasswordProtected(serverPassword != "");
            }
        }
    }

    public static StatusData statusData => _statusData;

    public static PreferenceData preferenceData => _preferenceData;

    public static ConfigData configData => _configData;

    public static ModeConfigData modeConfigData => _modeConfigData;

    public static bool isCurrentServerFavorited => GetServerIsFavorited(currentServerInfo.ip, currentServerInfo.queryPort);

    public static float timeLastPacketWasReceivedFromServer { get; internal set; }

    public static float ping => _ping;

    public static IProvider provider { get; protected set; }

    public static bool isInitialized => _isInitialized;

    public static uint time
    {
        get
        {
            return _time + (uint)(Time.realtimeSinceStartup - (float)timeOffset);
        }
        set
        {
            _time = value;
            timeOffset = (uint)Time.realtimeSinceStartup;
        }
    }

    public static uint backendRealtimeSeconds
    {
        get
        {
            return initialBackendRealtimeSeconds + (uint)(Time.realtimeSinceStartup - initialLocalRealtime);
        }
        private set
        {
            initialBackendRealtimeSeconds = value;
            initialLocalRealtime = Time.realtimeSinceStartup;
            onBackendRealtimeAvailable?.Invoke();
        }
    }

    public static DateTime backendRealtimeDate => unixEpochDateTime.AddSeconds(backendRealtimeSeconds);

    public static bool isBackendRealtimeAvailable => initialBackendRealtimeSeconds != 0;

    public static bool isApplicationQuitting { get; private set; }

    public static event BattlEyeKickCallback onBattlEyeKick;

    public static event CommenceShutdownHandler onCommenceShutdown;

    public static event RejectingPlayerCallback onRejectingPlayer;

    private IEnumerator CaptureScreenshot()
    {
        bool enableScreenshotSupersampling = OptionsSettings.enableScreenshotSupersampling;
        int max = (enableScreenshotSupersampling ? 4 : 16);
        int sizeMultiplier = Mathf.Clamp(OptionsSettings.screenshotSizeMultiplier, 1, max);
        if (sizeMultiplier > 1 || enableScreenshotSupersampling)
        {
            UnturnedPostProcess.instance.DisableAntiAliasingForScreenshot = true;
        }
        string path = PathEx.Join(UnturnedPaths.RootDirectory, "Screenshots");
        Directory.CreateDirectory(path);
        bool flag = ((Level.isEditor && EditorUI.window != null) ? EditorUI.window.isEnabled : (!(Player.player != null) || PlayerUI.window == null || PlayerUI.window.isEnabled));
        string text = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
        if (!flag)
        {
            text += "_NoUI";
        }
        string filePath = Path.Combine(path, text + ".png");
        int finalWidth = Screen.width * sizeMultiplier;
        int finalHeight = Screen.height * sizeMultiplier;
        UnturnedLog.info($"Capturing {finalWidth}x{finalHeight} screenshot (Size Multiplier: {sizeMultiplier} Use Supersampling: {enableScreenshotSupersampling} HUD Visible: {flag})");
        if (enableScreenshotSupersampling)
        {
            yield return new WaitForEndOfFrame();
            int superSize = sizeMultiplier * 2;
            Texture2D supersampledTexture = ScreenCapture.CaptureScreenshotAsTexture(superSize);
            UnturnedPostProcess.instance.DisableAntiAliasingForScreenshot = false;
            if (supersampledTexture == null)
            {
                UnturnedLog.error("CaptureScreenshotAsTexture returned null");
                isCapturingScreenshot = false;
                yield break;
            }
            yield return null;
            supersampledTexture.filterMode = FilterMode.Bilinear;
            RenderTexture downsampleRenderTexture = RenderTexture.GetTemporary(finalWidth, finalHeight, 0, supersampledTexture.graphicsFormat);
            Graphics.Blit(supersampledTexture, downsampleRenderTexture, screenshotBlitMaterial);
            yield return null;
            Texture2D downsampledTexture = new Texture2D(finalWidth, finalHeight, supersampledTexture.format, mipChain: false, linear: false);
            RenderTexture.active = downsampleRenderTexture;
            downsampledTexture.ReadPixels(new Rect(0f, 0f, finalWidth, finalHeight), 0, 0, recalculateMipMaps: false);
            RenderTexture.active = null;
            RenderTexture.ReleaseTemporary(downsampleRenderTexture);
            UnityEngine.Object.Destroy(supersampledTexture);
            yield return null;
            byte[] downsampledBytes = downsampledTexture.EncodeToPNG();
            UnityEngine.Object.Destroy(downsampledTexture);
            yield return null;
            File.WriteAllBytes(filePath, downsampledBytes);
            yield return null;
        }
        else
        {
            ScreenCapture.CaptureScreenshot(filePath, sizeMultiplier);
            yield return null;
            UnturnedPostProcess.instance.DisableAntiAliasingForScreenshot = false;
            float timePassed = 0f;
            while (true)
            {
                timePassed += Time.deltaTime;
                if (File.Exists(filePath))
                {
                    break;
                }
                if (timePassed < 10f)
                {
                    yield return null;
                    continue;
                }
                UnturnedLog.error($"Screenshot file is not available after {timePassed}s ({filePath})");
                isCapturingScreenshot = false;
                yield break;
            }
        }
        UnturnedLog.info("Captured screenshot: " + filePath);
        ScreenshotHandle hScreenshot = SteamScreenshots.AddScreenshotToLibrary(filePath, null, finalWidth, finalHeight);
        if (Level.info != null)
        {
            string localizedName = Level.info.getLocalizedName();
            SteamScreenshots.SetLocation(hScreenshot, localizedName);
            UnturnedLog.info("Tagged location \"" + localizedName + "\" in screenshot");
        }
        Camera instance = MainCamera.instance;
        if (instance != null)
        {
            Vector3 position = instance.transform.position;
            foreach (SteamPlayer client in clients)
            {
                if (client.player == null || client.player.channel.isOwner)
                {
                    continue;
                }
                Vector3 vector = client.player.transform.position + Vector3.up;
                if (!((vector - position).sqrMagnitude > 4096f))
                {
                    Vector3 vector2 = instance.WorldToViewportPoint(vector);
                    if (!(vector2.x < 0f) && !(vector2.x > 1f) && !(vector2.y < 0f) && !(vector2.y > 1f) && !(vector2.z < 0f))
                    {
                        SteamScreenshots.TagUser(hScreenshot, client.playerID.steamID);
                        UnturnedLog.info("Tagged player \"" + client.GetLocalDisplayName() + "\" in screenshot");
                    }
                }
            }
        }
        isCapturingScreenshot = false;
    }

    public static void RequestScreenshot()
    {
        if (!isCapturingScreenshot)
        {
            isCapturingScreenshot = true;
            steam.StartCoroutine(steam.CaptureScreenshot());
        }
    }

    private static void OnSteamScreenshotRequested(ScreenshotRequested_t callback)
    {
        UnturnedLog.info("Steam overlay screenshot requested");
        RequestScreenshot();
    }

    internal static void battlEyeClientPrintMessage(string message)
    {
        UnturnedLog.info("BattlEye client message: {0}", message);
    }

    internal static void battlEyeClientRequestRestart(int reason)
    {
        switch (reason)
        {
        case 0:
            _connectionFailureInfo = ESteamConnectionFailureInfo.BATTLEYE_BROKEN;
            break;
        case 1:
            _connectionFailureInfo = ESteamConnectionFailureInfo.BATTLEYE_UPDATE;
            break;
        default:
            _connectionFailureInfo = ESteamConnectionFailureInfo.BATTLEYE_UNKNOWN;
            break;
        }
        battlEyeHasRequiredRestart = true;
        UnturnedLog.info("BattlEye client requested restart with reason: " + reason);
    }

    internal static void battlEyeClientSendPacket(IntPtr packetHandle, int length)
    {
        NetMessages.SendMessageToServer(EServerMessage.BattlEye, ENetReliability.Unreliable, delegate(NetPakWriter writer)
        {
            writer.WriteBits((uint)length, battlEyeBufferSize.bitCount);
            if (!writer.WriteBytes(packetHandle, length))
            {
                UnturnedLog.error("Unable to write BattlEye packet ({0} bytes)", length);
            }
        });
    }

    private static void battlEyeServerPrintMessage(string message)
    {
        for (int i = 0; i < clients.Count; i++)
        {
            SteamPlayer steamPlayer = clients[i];
            if (steamPlayer != null && !(steamPlayer.player == null) && steamPlayer.player.wantsBattlEyeLogs)
            {
                steamPlayer.player.sendTerminalRelay(message);
            }
        }
        if (CommandWindow.shouldLogAnticheat)
        {
            CommandWindow.Log("BattlEye Server: " + message);
            return;
        }
        UnturnedLog.info("BattlEye Print: {0}", message);
    }

    private static void broadcastBattlEyeKick(SteamPlayer client, string reason)
    {
        try
        {
            Provider.onBattlEyeKick?.Invoke(client, reason);
        }
        catch (Exception e)
        {
            UnturnedLog.warn("Plugin raised an exception from onBattlEyeKick:");
            UnturnedLog.exception(e);
        }
    }

    private static void battlEyeServerKickPlayer(int playerID, string reason)
    {
        foreach (SteamPlayer client in clients)
        {
            if (client.battlEyeId != playerID)
            {
                continue;
            }
            if (!client.playerID.BypassIntegrityChecks)
            {
                broadcastBattlEyeKick(client, reason);
                UnturnedLog.info("BattlEye Kick {0} Reason: {1}", client.playerID.steamID, reason);
                if (reason.Length == 18 && reason.StartsWith("Global Ban #"))
                {
                    ChatManager.say(client.playerID.playerName + " got banned by BattlEye", Color.yellow);
                }
                kick(client.playerID.steamID, "BattlEye: " + reason);
                SteamBlacklist.ban(client.playerID.steamID, client.getIPv4AddressOrZero(), client.playerID.GetHwids(), CSteamID.Nil, "(Temporary) BattlEye: " + reason, 60u);
            }
            break;
        }
    }

    private static void battlEyeServerSendPacket(int playerID, IntPtr packetHandle, int length)
    {
        for (int i = 0; i < clients.Count; i++)
        {
            if (clients[i].battlEyeId == playerID)
            {
                NetMessages.SendMessageToClient(EClientMessage.BattlEye, ENetReliability.Unreliable, clients[i].transportConnection, delegate(NetPakWriter writer)
                {
                    writer.WriteBits((uint)length, battlEyeBufferSize.bitCount);
                    writer.WriteBytes(packetHandle, length);
                });
                break;
            }
        }
    }

    public static void updateRichPresence()
    {
        if (!Dedicator.IsDedicatedServer)
        {
            updateSteamRichPresence();
        }
    }

    private static void updateSteamRichPresence()
    {
        if (Level.info != null)
        {
            if (Level.isEditor)
            {
                provider.communityService.setStatus(localization.format("Rich_Presence_Editing", Level.info.getLocalizedName()));
                SteamFriends.SetRichPresence("steam_display", "#Status_EditingLevel");
                SteamFriends.SetRichPresence("steam_player_group", string.Empty);
            }
            else
            {
                provider.communityService.setStatus(localization.format("Rich_Presence_Playing", Level.info.getLocalizedName()));
                if (isConnected && !isServer && server.m_SteamID != 0)
                {
                    SteamFriends.SetRichPresence("steam_display", "#Status_PlayingMultiplayer");
                    SteamFriends.SetRichPresence("steam_player_group", server.ToString());
                }
                else
                {
                    SteamFriends.SetRichPresence("steam_display", "#Status_PlayingSingleplayer");
                    SteamFriends.SetRichPresence("steam_player_group", string.Empty);
                }
            }
            SteamFriends.SetRichPresence("level_name", Level.info.getLocalizedName());
        }
        else if (Lobbies.inLobby)
        {
            provider.communityService.setStatus(localization.format("Rich_Presence_Lobby"));
            SteamFriends.SetRichPresence("steam_display", "#Status_WaitingInLobby");
            SteamFriends.SetRichPresence("steam_player_group", Lobbies.currentLobby.ToString());
        }
        else
        {
            provider.communityService.setStatus(localization.format("Rich_Presence_Menu"));
            SteamFriends.SetRichPresence("steam_display", "#Status_AtMainMenu");
            SteamFriends.SetRichPresence("steam_player_group", string.Empty);
        }
    }

    public static PooledTransportConnectionList GatherClientConnections()
    {
        PooledTransportConnectionList pooledTransportConnectionList = TransportConnectionListPool.Get();
        foreach (SteamPlayer client in _clients)
        {
            pooledTransportConnectionList.Add(client.transportConnection);
        }
        return pooledTransportConnectionList;
    }

    [Obsolete("Replaced by GatherClientConnections")]
    public static IEnumerable<ITransportConnection> EnumerateClients()
    {
        return GatherClientConnections();
    }

    public static PooledTransportConnectionList GatherClientConnectionsMatchingPredicate(Predicate<SteamPlayer> predicate)
    {
        PooledTransportConnectionList pooledTransportConnectionList = TransportConnectionListPool.Get();
        foreach (SteamPlayer client in _clients)
        {
            if (predicate(client))
            {
                pooledTransportConnectionList.Add(client.transportConnection);
            }
        }
        return pooledTransportConnectionList;
    }

    [Obsolete("Replaced by GatherClientConnectionsMatchingPredicate")]
    public static IEnumerable<ITransportConnection> EnumerateClients_Predicate(Predicate<SteamPlayer> predicate)
    {
        return GatherClientConnectionsMatchingPredicate(predicate);
    }

    public static PooledTransportConnectionList GatherClientConnectionsWithinSphere(Vector3 position, float radius)
    {
        PooledTransportConnectionList pooledTransportConnectionList = TransportConnectionListPool.Get();
        float num = radius * radius;
        foreach (SteamPlayer client in _clients)
        {
            if (client.player != null && (client.player.transform.position - position).sqrMagnitude < num)
            {
                pooledTransportConnectionList.Add(client.transportConnection);
            }
        }
        return pooledTransportConnectionList;
    }

    [Obsolete("Replaced by GatherClientConnectionsWithinSphere")]
    public static IEnumerable<ITransportConnection> EnumerateClients_WithinSphere(Vector3 position, float radius)
    {
        return GatherClientConnectionsWithinSphere(position, radius);
    }

    public static PooledTransportConnectionList GatherRemoteClientConnectionsWithinSphere(Vector3 position, float radius)
    {
        PooledTransportConnectionList pooledTransportConnectionList = TransportConnectionListPool.Get();
        float num = radius * radius;
        foreach (SteamPlayer client in _clients)
        {
            if (!client.IsLocalPlayer && client.player != null && (client.player.transform.position - position).sqrMagnitude < num)
            {
                pooledTransportConnectionList.Add(client.transportConnection);
            }
        }
        return pooledTransportConnectionList;
    }

    [Obsolete("Replaced by GatherRemoteClientConnectionsWithinSphere")]
    public static IEnumerable<ITransportConnection> EnumerateClients_RemoteWithinSphere(Vector3 position, float radius)
    {
        return GatherRemoteClientConnectionsWithinSphere(position, radius);
    }

    public static PooledTransportConnectionList GatherRemoteClientConnections()
    {
        PooledTransportConnectionList pooledTransportConnectionList = TransportConnectionListPool.Get();
        foreach (SteamPlayer client in _clients)
        {
            if (!client.IsLocalPlayer)
            {
                pooledTransportConnectionList.Add(client.transportConnection);
            }
        }
        return pooledTransportConnectionList;
    }

    [Obsolete("Replaced by GatherRemoteClientConnections")]
    public static IEnumerable<ITransportConnection> EnumerateClients_Remote()
    {
        return GatherRemoteClientConnections();
    }

    public static PooledTransportConnectionList GatherRemoteClientConnectionsMatchingPredicate(Predicate<SteamPlayer> predicate)
    {
        PooledTransportConnectionList pooledTransportConnectionList = TransportConnectionListPool.Get();
        foreach (SteamPlayer client in _clients)
        {
            if (!client.IsLocalPlayer && predicate(client))
            {
                pooledTransportConnectionList.Add(client.transportConnection);
            }
        }
        return pooledTransportConnectionList;
    }

    [Obsolete("Replaced by GatherRemoteClientsMatchingPredicate")]
    public static IEnumerable<ITransportConnection> EnumerateClients_RemotePredicate(Predicate<SteamPlayer> predicate)
    {
        return GatherRemoteClientConnectionsMatchingPredicate(predicate);
    }

    private static bool doServerItemsMatchAdvertisement(List<PublishedFileId_t> pendingWorkshopItems)
    {
        if (waitingForExpectedWorkshopItems == null)
        {
            return true;
        }
        if (waitingForExpectedWorkshopItems.Count < pendingWorkshopItems.Count)
        {
            return false;
        }
        foreach (PublishedFileId_t pendingWorkshopItem in pendingWorkshopItems)
        {
            if (!waitingForExpectedWorkshopItems.Contains(pendingWorkshopItem))
            {
                return false;
            }
        }
        return true;
    }

    internal static void receiveWorkshopResponse(CachedWorkshopResponse response)
    {
        authorityHoliday = response.holiday;
        currentServerWorkshopResponse = response;
        isWaitingForWorkshopResponse = false;
        List<PublishedFileId_t> list = new List<PublishedFileId_t>(response.requiredFiles.Count);
        foreach (ServerRequiredWorkshopFile requiredFile in response.requiredFiles)
        {
            if (requiredFile.fileId != 0L)
            {
                list.Add(new PublishedFileId_t(requiredFile.fileId));
            }
        }
        provider.workshopService.resetServerInvalidItems();
        if (list.Count < 1)
        {
            UnturnedLog.info("Server specified no workshop items, launching");
            launch();
        }
        else if (currentServerInfo.isWorkshop && doServerItemsMatchAdvertisement(list))
        {
            UnturnedLog.info("Server specified {0} workshop item(s), querying details", list.Count);
            provider.workshopService.queryServerWorkshopItems(list, response.ip);
        }
        else
        {
            _connectionFailureInfo = ESteamConnectionFailureInfo.WORKSHOP_ADVERTISEMENT_MISMATCH;
            RequestDisconnect("workshop advertisement mismatch");
        }
    }

    public static List<ulong> getServerWorkshopFileIDs()
    {
        return _serverWorkshopFileIDs;
    }

    public static void registerServerUsingWorkshopFileId(ulong id)
    {
        registerServerUsingWorkshopFileId(id, 0u);
    }

    internal static void registerServerUsingWorkshopFileId(ulong id, uint timestamp)
    {
        if (!_serverWorkshopFileIDs.Contains(id))
        {
            _serverWorkshopFileIDs.Add(id);
            ServerRequiredWorkshopFile serverRequiredWorkshopFile = default(ServerRequiredWorkshopFile);
            serverRequiredWorkshopFile.fileId = id;
            serverRequiredWorkshopFile.timestamp = DateTimeEx.FromUtcUnixTimeSeconds(timestamp);
            ServerRequiredWorkshopFile item = serverRequiredWorkshopFile;
            UnturnedLog.info($"Workshop file {id} requiring timestamp {item.timestamp.ToLocalTime()}");
            serverRequiredWorkshopFiles.Add(item);
        }
    }

    private static int allocPlayerChannelId()
    {
        for (int i = 0; i < 255; i++)
        {
            int num = nextPlayerChannelId;
            nextPlayerChannelId++;
            if (nextPlayerChannelId > 255)
            {
                nextPlayerChannelId = 2;
            }
            if (findChannelComponent(num) == null)
            {
                return num;
            }
        }
        CommandWindow.LogErrorFormat("Fatal error! Ran out of player RPC channel IDs");
        shutdown(1, "Fatal error! Ran out of player RPC channel IDs");
        return 2;
    }

    private static int allocBattlEyePlayerId()
    {
        int result = nextBattlEyePlayerId;
        nextBattlEyePlayerId++;
        return result;
    }

    public static void resetConnectionFailure()
    {
        _connectionFailureInfo = ESteamConnectionFailureInfo.NONE;
        _connectionFailureReason = "";
        _connectionFailureDuration = 0u;
    }

    [Conditional("LOG_NETCHANNEL")]
    private static void LogNetChannel(string format, params object[] args)
    {
        UnturnedLog.info(format, args);
    }

    public static void openChannel(SteamChannel receiver)
    {
        receivers.Add(receiver);
    }

    public static void closeChannel(SteamChannel receiver)
    {
        receivers.RemoveFast(receiver);
    }

    internal static SteamChannel findChannelComponent(int id)
    {
        for (int num = receivers.Count - 1; num >= 0; num--)
        {
            SteamChannel steamChannel = receivers[num];
            if (steamChannel == null)
            {
                receivers.RemoveAtFast(num);
            }
            else if (steamChannel.id == id)
            {
                return steamChannel;
            }
        }
        return null;
    }

    public static SteamPending findPendingPlayer(ITransportConnection transportConnection)
    {
        if (transportConnection == null)
        {
            return null;
        }
        foreach (SteamPending item in pending)
        {
            if (transportConnection.Equals(item.transportConnection))
            {
                return item;
            }
        }
        return null;
    }

    internal static SteamPending findPendingPlayerBySteamId(CSteamID steamId)
    {
        foreach (SteamPending item in pending)
        {
            if (item.playerID.steamID == steamId)
            {
                return item;
            }
        }
        return null;
    }

    public static SteamPlayer findPlayer(ITransportConnection transportConnection)
    {
        if (transportConnection == null)
        {
            return null;
        }
        foreach (SteamPlayer client in clients)
        {
            if (transportConnection.Equals(client.transportConnection))
            {
                return client;
            }
        }
        return null;
    }

    public static ITransportConnection findTransportConnection(CSteamID steamId)
    {
        foreach (SteamPlayer client in clients)
        {
            if (client.playerID.steamID == steamId)
            {
                return client.transportConnection;
            }
        }
        foreach (SteamPending item in pending)
        {
            if (item.playerID.steamID == steamId)
            {
                return item.transportConnection;
            }
        }
        return null;
    }

    public static CSteamID findTransportConnectionSteamId(ITransportConnection transportConnection)
    {
        return findPlayer(transportConnection)?.playerID.steamID ?? findPendingPlayer(transportConnection)?.playerID.steamID ?? CSteamID.Nil;
    }

    internal static NetId ClaimNetIdBlockForNewPlayer()
    {
        return NetIdRegistry.ClaimBlock(16u);
    }

    internal static SteamPlayer addPlayer(ITransportConnection transportConnection, NetId netId, SteamPlayerID playerID, Vector3 point, byte angle, bool isPro, bool isAdmin, int channel, byte face, byte hair, byte beard, Color skin, Color color, Color markerColor, bool hand, int shirtItem, int pantsItem, int hatItem, int backpackItem, int vestItem, int maskItem, int glassesItem, int[] skinItems, string[] skinTags, string[] skinDynamicProps, EPlayerSkillset skillset, string language, CSteamID lobbyID, EClientPlatform clientPlatform)
    {
        if (!Dedicator.IsDedicatedServer && playerID.steamID != client)
        {
            SteamFriends.SetPlayedWith(playerID.steamID);
        }
        if (playerID.steamID == client)
        {
            if (Level.placeholderAudioListener != null)
            {
                UnityEngine.Object.Destroy(Level.placeholderAudioListener);
                Level.placeholderAudioListener = null;
            }
            string value = skillset.ToString();
            int num = 0;
            int num2 = 0;
            if (shirtItem != 0)
            {
                num++;
                if (provider.economyService.getInventoryMythicID(shirtItem) != 0)
                {
                    num2++;
                }
            }
            if (pantsItem != 0)
            {
                num++;
                if (provider.economyService.getInventoryMythicID(pantsItem) != 0)
                {
                    num2++;
                }
            }
            if (hatItem != 0)
            {
                num++;
                if (provider.economyService.getInventoryMythicID(hatItem) != 0)
                {
                    num2++;
                }
            }
            if (backpackItem != 0)
            {
                num++;
                if (provider.economyService.getInventoryMythicID(backpackItem) != 0)
                {
                    num2++;
                }
            }
            if (vestItem != 0)
            {
                num++;
                if (provider.economyService.getInventoryMythicID(vestItem) != 0)
                {
                    num2++;
                }
            }
            if (maskItem != 0)
            {
                num++;
                if (provider.economyService.getInventoryMythicID(maskItem) != 0)
                {
                    num2++;
                }
            }
            if (glassesItem != 0)
            {
                num++;
                if (provider.economyService.getInventoryMythicID(glassesItem) != 0)
                {
                    num2++;
                }
            }
            int num3 = skinItems.Length;
            for (int i = 0; i < skinItems.Length; i++)
            {
                if (provider.economyService.getInventoryMythicID(skinItems[i]) != 0)
                {
                    num2++;
                }
            }
            new Dictionary<string, object>
            {
                { "Ability", value },
                { "Cosmetics", num },
                { "Mythics", num2 },
                { "Skins", num3 }
            };
        }
        Transform transform = null;
        try
        {
            transform = gameMode.getPlayerGameObject(playerID).transform;
            transform.position = point;
            transform.rotation = Quaternion.Euler(0f, angle * 2, 0f);
        }
        catch (Exception e)
        {
            UnturnedLog.error("Exception thrown when getting player from game mode:");
            UnturnedLog.exception(e);
        }
        SteamPlayer steamPlayer = null;
        try
        {
            steamPlayer = new SteamPlayer(transportConnection, netId, playerID, transform, isPro, isAdmin, channel, face, hair, beard, skin, color, markerColor, hand, shirtItem, pantsItem, hatItem, backpackItem, vestItem, maskItem, glassesItem, skinItems, skinTags, skinDynamicProps, skillset, language, lobbyID, clientPlatform);
            clients.Add(steamPlayer);
        }
        catch (Exception e2)
        {
            UnturnedLog.error("Exception thrown when adding player:");
            UnturnedLog.exception(e2);
        }
        updateRichPresence();
        broadcastEnemyConnected(steamPlayer);
        return steamPlayer;
    }

    internal static void removePlayer(byte index)
    {
        if (index < 0 || index >= clients.Count)
        {
            UnturnedLog.error("Failed to find player: " + index);
            return;
        }
        SteamPlayer steamPlayer = clients[index];
        if (battlEyeServerHandle != IntPtr.Zero && battlEyeServerRunData != null && battlEyeServerRunData.pfnChangePlayerStatus != null)
        {
            battlEyeServerRunData.pfnChangePlayerStatus(steamPlayer.battlEyeId, -1);
        }
        if (Dedicator.IsDedicatedServer)
        {
            steamPlayer.transportConnection.CloseConnection();
        }
        broadcastEnemyDisconnected(steamPlayer);
        steamPlayer.player.ReleaseNetIdBlock();
        if (steamPlayer.model != null)
        {
            UnityEngine.Object.Destroy(steamPlayer.model.gameObject);
        }
        NetIdRegistry.Release(steamPlayer.GetNetId());
        clients.RemoveAt(index);
        verifyNextPlayerInQueue();
        updateRichPresence();
    }

    private static void replicateRemovePlayer(CSteamID skipSteamID, byte removalIndex)
    {
        NetMessages.SendMessageToClients(EClientMessage.PlayerDisconnected, ENetReliability.Reliable, GatherRemoteClientConnectionsMatchingPredicate((SteamPlayer potentialRecipient) => potentialRecipient.playerID.steamID != skipSteamID), delegate(NetPakWriter writer)
        {
            writer.WriteUInt8(removalIndex);
        });
    }

    internal static void verifyNextPlayerInQueue()
    {
        if (pending.Count >= 1 && clients.Count < maxPlayers)
        {
            SteamPending steamPending = pending[0];
            if (!steamPending.hasSentVerifyPacket)
            {
                steamPending.sendVerifyPacket();
            }
        }
    }

    [Obsolete]
    private static bool isUnreliable(ESteamPacket type)
    {
        if (type == ESteamPacket.UPDATE_UNRELIABLE_BUFFER || (uint)(type - 3) <= 1u)
        {
            return true;
        }
        return false;
    }

    [Obsolete]
    public static bool isChunk(ESteamPacket packet)
    {
        if ((uint)(packet - 2) <= 1u)
        {
            return true;
        }
        return false;
    }

    [Obsolete]
    private static bool isUpdate(ESteamPacket packet)
    {
        if ((uint)packet <= 4u)
        {
            return true;
        }
        return false;
    }

    internal static void resetChannels()
    {
        _bytesSent = 0u;
        _bytesReceived = 0u;
        _packetsSent = 0u;
        _packetsReceived = 0u;
        _clients.Clear();
        pending.Clear();
        NetIdRegistry.Clear();
        NetInvocationDeferralRegistry.Clear();
        ClientAssetIntegrity.Clear();
        ItemManager.ClearNetworkStuff();
        BarricadeManager.ClearNetworkStuff();
        StructureManager.ClearNetworkStuff();
    }

    private static void loadPlayerSpawn(SteamPlayerID playerID, out Vector3 point, out byte angle, out EPlayerStance initialStance)
    {
        point = Vector3.zero;
        angle = 0;
        initialStance = EPlayerStance.STAND;
        bool needsNewSpawnpoint = false;
        if (PlayerSavedata.fileExists(playerID, "/Player/Player.dat") && Level.info.type == ELevelType.SURVIVAL)
        {
            Block block = PlayerSavedata.readBlock(playerID, "/Player/Player.dat", 1);
            point = block.readSingleVector3() + new Vector3(0f, 0.01f, 0f);
            angle = block.readByte();
            if (!point.IsFinite())
            {
                needsNewSpawnpoint = true;
                UnturnedLog.info("Reset {0} spawn position ({1}) because it was NaN or infinity", playerID, point);
            }
            else if (point.y > Level.HEIGHT)
            {
                UnturnedLog.info("Clamped {0} spawn position ({1}) because it was above the world height limit ({2})", playerID, point, Level.HEIGHT);
                point.y = Level.HEIGHT - 10f;
            }
            else if (!PlayerStance.getStanceForPosition(point, ref initialStance))
            {
                UnturnedLog.info("Reset {0} spawn position ({1}) because it was obstructed", playerID, point);
                needsNewSpawnpoint = true;
            }
        }
        else
        {
            needsNewSpawnpoint = true;
        }
        try
        {
            if (onLoginSpawning != null)
            {
                float yaw = angle * 2;
                onLoginSpawning(playerID, ref point, ref yaw, ref initialStance, ref needsNewSpawnpoint);
                angle = (byte)(yaw / 2f);
            }
        }
        catch (Exception e)
        {
            UnturnedLog.warn("Plugin raised an exception from onLoginSpawning:");
            UnturnedLog.exception(e);
        }
        if (needsNewSpawnpoint)
        {
            PlayerSpawnpoint spawn = LevelPlayers.getSpawn(isAlt: false);
            point = spawn.point + new Vector3(0f, 0.5f, 0f);
            angle = (byte)(spawn.angle / 2f);
        }
    }

    private static void onLevelLoaded(int level)
    {
        if (level != 2)
        {
            return;
        }
        isLoadingUGC = false;
        if (!isConnected)
        {
            return;
        }
        if (isServer)
        {
            if (isClient)
            {
                SteamPlayerID steamPlayerID = new SteamPlayerID(client, Characters.selected, clientName, Characters.active.name, Characters.active.nick, Characters.active.group);
                loadPlayerSpawn(steamPlayerID, out var point, out var angle, out var initialStance);
                int inventoryItem = provider.economyService.getInventoryItem(Characters.active.packageShirt);
                int inventoryItem2 = provider.economyService.getInventoryItem(Characters.active.packagePants);
                int inventoryItem3 = provider.economyService.getInventoryItem(Characters.active.packageHat);
                int inventoryItem4 = provider.economyService.getInventoryItem(Characters.active.packageBackpack);
                int inventoryItem5 = provider.economyService.getInventoryItem(Characters.active.packageVest);
                int inventoryItem6 = provider.economyService.getInventoryItem(Characters.active.packageMask);
                int inventoryItem7 = provider.economyService.getInventoryItem(Characters.active.packageGlasses);
                int[] array = new int[Characters.packageSkins.Count];
                for (int i = 0; i < array.Length; i++)
                {
                    array[i] = provider.economyService.getInventoryItem(Characters.packageSkins[i]);
                }
                string[] array2 = new string[Characters.packageSkins.Count];
                for (int j = 0; j < array2.Length; j++)
                {
                    array2[j] = provider.economyService.getInventoryTags(Characters.packageSkins[j]);
                }
                string[] array3 = new string[Characters.packageSkins.Count];
                for (int k = 0; k < array3.Length; k++)
                {
                    array3[k] = provider.economyService.getInventoryDynamicProps(Characters.packageSkins[k]);
                }
                TransportConnection_Loopback transportConnection_Loopback = TransportConnection_Loopback.Create();
                SteamPlayer steamPlayer = addPlayer(netId: ClaimNetIdBlockForNewPlayer(), transportConnection: transportConnection_Loopback, playerID: steamPlayerID, point: point, angle: angle, isPro: isPro, isAdmin: true, channel: allocPlayerChannelId(), face: Characters.active.face, hair: Characters.active.hair, beard: Characters.active.beard, skin: Characters.active.skin, color: Characters.active.color, markerColor: Characters.active.markerColor, hand: Characters.active.hand, shirtItem: inventoryItem, pantsItem: inventoryItem2, hatItem: inventoryItem3, backpackItem: inventoryItem4, vestItem: inventoryItem5, maskItem: inventoryItem6, glassesItem: inventoryItem7, skinItems: array, skinTags: array2, skinDynamicProps: array3, skillset: Characters.active.skillset, language: language, lobbyID: Lobbies.currentLobby, clientPlatform: EClientPlatform.Windows);
                steamPlayer.player.stance.initialStance = initialStance;
                steamPlayer.player.InitializePlayer();
                steamPlayer.player.SendInitialPlayerState(steamPlayer);
                Lobbies.leaveLobby();
                updateRichPresence();
                try
                {
                    onServerConnected?.Invoke(steamPlayerID.steamID);
                    return;
                }
                catch (Exception e)
                {
                    UnturnedLog.warn("Plugin raised an exception from onServerConnected:");
                    UnturnedLog.exception(e);
                    return;
                }
            }
            return;
        }
        EClientPlatform clientPlatform = EClientPlatform.Linux;
        critMods.Clear();
        modBuilder.Length = 0;
        ModuleHook.getRequiredModules(critMods);
        for (int l = 0; l < critMods.Count; l++)
        {
            modBuilder.Append(critMods[l].config.Name);
            modBuilder.Append(",");
            modBuilder.Append(critMods[l].config.Version_Internal);
            if (l < critMods.Count - 1)
            {
                modBuilder.Append(";");
            }
        }
        UnturnedLog.info("Ready to connect");
        isWaitingForConnectResponse = true;
        sentConnectRequestTime = Time.realtimeSinceStartup;
        NetMessages.SendMessageToServer(EServerMessage.ReadyToConnect, ENetReliability.Reliable, delegate(NetPakWriter writer)
        {
            writer.WriteUInt8(Characters.selected);
            writer.WriteString(clientName);
            writer.WriteString(Characters.active.name);
            writer.WriteBytes(_serverPasswordHash, 20);
            writer.WriteBytes(Level.hash, 20);
            writer.WriteBytes(ReadWrite.readData(), 20);
            writer.WriteEnum(clientPlatform);
            writer.WriteUInt32(APP_VERSION_PACKED);
            writer.WriteBit(isPro);
            writer.WriteUInt16(MathfEx.ClampToUShort(currentServerInfo.ping));
            writer.WriteString(Characters.active.nick);
            writer.WriteSteamID(Characters.active.group);
            writer.WriteUInt8(Characters.active.face);
            writer.WriteUInt8(Characters.active.hair);
            writer.WriteUInt8(Characters.active.beard);
            writer.WriteColor32RGB(Characters.active.skin);
            writer.WriteColor32RGB(Characters.active.color);
            writer.WriteColor32RGB(Characters.active.markerColor);
            writer.WriteBit(Characters.active.hand);
            writer.WriteUInt64(Characters.active.packageShirt);
            writer.WriteUInt64(Characters.active.packagePants);
            writer.WriteUInt64(Characters.active.packageHat);
            writer.WriteUInt64(Characters.active.packageBackpack);
            writer.WriteUInt64(Characters.active.packageVest);
            writer.WriteUInt64(Characters.active.packageMask);
            writer.WriteUInt64(Characters.active.packageGlasses);
            writer.WriteList(Characters.packageSkins, (SystemNetPakWriterEx.WriteListItem<ulong>)writer.WriteUInt64, MAX_SKINS_LENGTH);
            writer.WriteEnum(Characters.active.skillset);
            writer.WriteString(modBuilder.ToString());
            writer.WriteString(language);
            writer.WriteSteamID(Lobbies.currentLobby);
            writer.WriteUInt32(Level.packedVersion);
            byte[][] hwids = LocalHwid.GetHwids();
            writer.WriteUInt8((byte)hwids.Length);
            byte[][] array4 = hwids;
            foreach (byte[] bytes in array4)
            {
                writer.WriteBytes(bytes, 20);
            }
            writer.WriteBytes(TempSteamworksEconomy.econInfoHash, 20);
            writer.WriteSteamID(user);
        });
    }

    public static void connect(SteamServerInfo info, string password, List<PublishedFileId_t> expectedWorkshopItems)
    {
        if (isConnected)
        {
            return;
        }
        _currentServerInfo = info;
        _isConnected = true;
        map = info.map;
        isPvP = info.isPvP;
        isWhitelisted = false;
        mode = info.mode;
        cameraMode = info.cameraMode;
        maxPlayers = (byte)info.maxPlayers;
        _queuePosition = 0;
        resetChannels();
        Lobbies.LinkLobby(info.ip, info.queryPort);
        _server = info.steamID;
        _serverPassword = password;
        _serverPasswordHash = Hash.SHA1(password);
        _isClient = true;
        timeLastPacketWasReceivedFromServer = Time.realtimeSinceStartup;
        pings = new float[4];
        lag((float)info.ping / 1000f);
        isLoadingUGC = true;
        LoadingUI.updateScene();
        isWaitingForConnectResponse = false;
        isWaitingForWorkshopResponse = true;
        waitingForExpectedWorkshopItems = expectedWorkshopItems;
        isWaitingForAuthenticationResponse = false;
        List<SteamItemInstanceID_t> list = new List<SteamItemInstanceID_t>();
        if (Characters.active.packageShirt != 0L)
        {
            list.Add((SteamItemInstanceID_t)Characters.active.packageShirt);
        }
        if (Characters.active.packagePants != 0L)
        {
            list.Add((SteamItemInstanceID_t)Characters.active.packagePants);
        }
        if (Characters.active.packageHat != 0L)
        {
            list.Add((SteamItemInstanceID_t)Characters.active.packageHat);
        }
        if (Characters.active.packageBackpack != 0L)
        {
            list.Add((SteamItemInstanceID_t)Characters.active.packageBackpack);
        }
        if (Characters.active.packageVest != 0L)
        {
            list.Add((SteamItemInstanceID_t)Characters.active.packageVest);
        }
        if (Characters.active.packageMask != 0L)
        {
            list.Add((SteamItemInstanceID_t)Characters.active.packageMask);
        }
        if (Characters.active.packageGlasses != 0L)
        {
            list.Add((SteamItemInstanceID_t)Characters.active.packageGlasses);
        }
        for (int i = 0; i < Characters.packageSkins.Count; i++)
        {
            ulong num = Characters.packageSkins[i];
            if (num != 0L)
            {
                list.Add((SteamItemInstanceID_t)num);
            }
        }
        if (list.Count > 0)
        {
            SteamInventory.GetItemsByID(out provider.economyService.wearingResult, list.ToArray(), (uint)list.Count);
        }
        Level.loading();
        clientTransport = NetTransportFactory.CreateClientTransport(currentServerInfo.networkTransport);
        UnturnedLog.info("Initializing {0}", clientTransport.GetType().Name);
        clientTransport.Initialize(onClientTransportReady, onClientTransportFailure);
    }

    private static void onClientTransportReady()
    {
        CachedWorkshopResponse cachedWorkshopResponse = null;
        foreach (CachedWorkshopResponse cachedWorkshopResponse2 in cachedWorkshopResponses)
        {
            if (cachedWorkshopResponse2.server == server && Time.realtimeSinceStartup - cachedWorkshopResponse2.realTime < 60f)
            {
                cachedWorkshopResponse = cachedWorkshopResponse2;
                break;
            }
        }
        if (cachedWorkshopResponse != null)
        {
            receiveWorkshopResponse(cachedWorkshopResponse);
            return;
        }
        NetMessages.SendMessageToServer(EServerMessage.GetWorkshopFiles, ENetReliability.Reliable, delegate(NetPakWriter writer)
        {
            writer.AlignToByte();
            for (int i = 0; i < 240; i++)
            {
                writer.WriteBits(0u, 32);
            }
            writer.WriteString("Hello!");
        });
    }

    private static void onClientTransportFailure(string message)
    {
        _connectionFailureInfo = ESteamConnectionFailureInfo.CUSTOM;
        _connectionFailureReason = message;
        RequestDisconnect("Client transport failure: \"" + message + "\"");
    }

    private static bool CompareClientAndServerWorkshopFileTimestamps()
    {
        if (provider.workshopService.serverPendingIDs == null)
        {
            return true;
        }
        foreach (PublishedFileId_t serverPendingID in provider.workshopService.serverPendingIDs)
        {
            if (!currentServerWorkshopResponse.FindRequiredFile(serverPendingID.m_PublishedFileId, out var details))
            {
                UnturnedLog.error($"Server workshop files response missing details for file: {serverPendingID}");
                continue;
            }
            if (details.timestamp.Year < 2000)
            {
                UnturnedLog.info($"Skipping timestamp comparison for server workshop file {serverPendingID} because timestamp is invalid ({details.timestamp.ToLocalTime()})");
                continue;
            }
            if (!SteamUGC.GetItemInstallInfo(serverPendingID, out var _, out var _, 1024u, out var punTimeStamp))
            {
                UnturnedLog.info($"Skipping timestamp comparison for server workshop file {serverPendingID} because item install info is missing");
                continue;
            }
            DateTime dateTime = DateTimeEx.FromUtcUnixTimeSeconds(punTimeStamp);
            if (dateTime == details.timestamp)
            {
                UnturnedLog.info($"Workshop file {serverPendingID} timestamp matches between client and server ({dateTime})");
                continue;
            }
            CachedUGCDetails cachedDetails;
            bool cachedDetails2 = TempSteamworksWorkshop.getCachedDetails(serverPendingID, out cachedDetails);
            string text = ((!cachedDetails2) ? $"Unknown File ID {serverPendingID}" : cachedDetails.GetTitle());
            _connectionFailureInfo = ESteamConnectionFailureInfo.CUSTOM;
            string text2 = ((!(details.timestamp > dateTime)) ? ("Server is running an older version of the \"" + text + "\" workshop file.") : ("Server is running a newer version of the \"" + text + "\" workshop file."));
            if (cachedDetails2)
            {
                DateTime dateTime2 = DateTimeEx.FromUtcUnixTimeSeconds(cachedDetails.updateTimestamp);
                if (dateTime == dateTime2)
                {
                    text2 += "\nYour installed copy of the file matches the most recent version on Steam.";
                    text2 += $"\nLocal and Steam timestamp: {dateTime.ToLocalTime()} Server timestamp: {details.timestamp.ToLocalTime()}";
                }
                else if (details.timestamp == dateTime2)
                {
                    text2 += "\nThe server's installed copy of the file matches the most recent version on Steam.";
                    text2 += $"\nLocal timestamp: {dateTime.ToLocalTime()} Server and Steam timestamp: {details.timestamp.ToLocalTime()}";
                }
                else
                {
                    text2 += $"\nLocal timestamp: {dateTime.ToLocalTime()} Server timestamp: {details.timestamp.ToLocalTime()} Steam timestamp: {dateTime2}";
                }
            }
            else
            {
                text2 += $"\nLocal timestamp: {dateTime.ToLocalTime()} Server timestamp: {details.timestamp.ToLocalTime()}";
            }
            _connectionFailureReason = text2;
            RequestDisconnect($"Loaded workshop file timestamp mismatch (File ID: {serverPendingID} Local timestamp: {dateTime.ToLocalTime()} Server timestamp: {details.timestamp.ToLocalTime()})");
            return false;
        }
        return true;
    }

    public static void launch()
    {
        LevelInfo level = Level.getLevel(map);
        if (level == null)
        {
            _connectionFailureInfo = ESteamConnectionFailureInfo.MAP;
            RequestDisconnect("could not find level \"" + map + "\"");
        }
        else if (CompareClientAndServerWorkshopFileTimestamps())
        {
            Assets.ApplyServerAssetMapping(level, provider.workshopService.serverPendingIDs);
            UnturnedLog.info("Loading server level ({0})", map);
            Level.load(level, hasAuthority: false);
            loadGameMode();
        }
    }

    private static void loadGameMode()
    {
        LevelAsset asset = Level.getAsset();
        if (asset == null)
        {
            gameMode = new SurvivalGameMode();
            return;
        }
        Type type = asset.defaultGameMode.type;
        if (type == null)
        {
            gameMode = new SurvivalGameMode();
            return;
        }
        gameMode = Activator.CreateInstance(type) as GameMode;
        if (gameMode == null)
        {
            gameMode = new SurvivalGameMode();
        }
    }

    private static void unloadGameMode()
    {
        gameMode = null;
    }

    public static void singleplayer(EGameMode singleplayerMode, bool singleplayerCheats)
    {
        _isConnected = true;
        resetChannels();
        Dedicator.serverVisibility = ESteamServerVisibility.LAN;
        Dedicator.serverID = "Singleplayer_" + Characters.selected;
        Commander.init();
        maxPlayers = 1;
        queueSize = 8;
        serverName = "Singleplayer #" + (Characters.selected + 1);
        serverPassword = "Singleplayer";
        ip = 0u;
        port = 25000;
        timeLastPacketWasReceivedFromServer = Time.realtimeSinceStartup;
        pings = new float[4];
        isPvP = true;
        isWhitelisted = false;
        hideAdmins = false;
        hasCheats = singleplayerCheats;
        filterName = false;
        mode = singleplayerMode;
        isGold = false;
        gameMode = null;
        cameraMode = ECameraMode.BOTH;
        if (singleplayerMode != EGameMode.TUTORIAL)
        {
            PlayerInventory.skillsets = PlayerInventory.SKILLSETS_CLIENT;
        }
        lag(0f);
        SteamWhitelist.load();
        SteamBlacklist.load();
        SteamAdminlist.load();
        _currentServerInfo = new SteamServerInfo(serverName, mode, newVACSecure: false, newBattlEyeEnabled: false, newPro: false);
        _configData = ConfigData.CreateDefault(singleplayer: true);
        if (ServerSavedata.fileExists("/Config.json"))
        {
            try
            {
                ServerSavedata.populateJSON("/Config.json", _configData);
            }
            catch (Exception e)
            {
                UnturnedLog.error("Exception while parsing singleplayer config:");
                UnturnedLog.exception(e);
            }
        }
        _modeConfigData = _configData.getModeConfig(mode);
        if (_modeConfigData == null)
        {
            _modeConfigData = new ModeConfigData(mode);
        }
        authorityHoliday = (_modeConfigData.Gameplay.Allow_Holidays ? HolidayUtil.BackendGetActiveHoliday() : ENPCHoliday.NONE);
        time = SteamUtils.GetServerRealTime();
        Level.load(Level.getLevel(map), hasAuthority: true);
        loadGameMode();
        applyLevelModeConfigOverrides();
        _server = user;
        _client = _server;
        _clientHash = Hash.SHA1(client);
        timeLastPacketWasReceivedFromServer = Time.realtimeSinceStartup;
        _isServer = true;
        _isClient = true;
        broadcastServerHosted();
    }

    public static void host()
    {
        _isConnected = true;
        resetChannels();
        openGameServer();
        _isServer = true;
        broadcastServerHosted();
    }

    private static void broadcastCommenceShutdown()
    {
        try
        {
            Provider.onCommenceShutdown?.Invoke();
        }
        catch (Exception e)
        {
            UnturnedLog.warn("Plugin raised an exception from onCommenceShutdown:");
            UnturnedLog.exception(e);
        }
    }

    public static void shutdown()
    {
        shutdown(0);
    }

    public static void shutdown(int timer)
    {
        shutdown(timer, string.Empty);
    }

    public static void shutdown(int timer, string explanation)
    {
        countShutdownTimer = timer;
        lastTimerMessage = Time.realtimeSinceStartup;
        shutdownMessage = explanation;
    }

    public static void RequestDisconnect(string reason)
    {
        UnturnedLog.info("Disconnecting: " + reason);
        disconnect();
    }

    public static void disconnect()
    {
        if (!Dedicator.IsDedicatedServer && Player.player != null && Player.player.channel != null && Player.player.channel.owner != null)
        {
            Player.player.channel.owner.commitModifiedDynamicProps();
        }
        if (isServer)
        {
            if (IsBattlEyeEnabled && battlEyeServerHandle != IntPtr.Zero)
            {
                if (battlEyeServerRunData != null && battlEyeServerRunData.pfnExit != null)
                {
                    UnturnedLog.info("Shutting down BattlEye server");
                    bool flag = battlEyeServerRunData.pfnExit();
                    UnturnedLog.info("BattlEye server shutdown result: {0}", flag);
                }
                BEServer.dlclose(battlEyeServerHandle);
                battlEyeServerHandle = IntPtr.Zero;
            }
            if (serverTransport != null)
            {
                serverTransport.TearDown();
            }
            if (Dedicator.IsDedicatedServer)
            {
                closeGameServer();
            }
            else
            {
                broadcastServerShutdown();
            }
            if (isClient)
            {
                _client = user;
                _clientHash = Hash.SHA1(client);
            }
            _isServer = false;
            _isClient = false;
        }
        else if (isClient)
        {
            if (battlEyeClientHandle != IntPtr.Zero)
            {
                if (battlEyeClientRunData != null && battlEyeClientRunData.pfnExit != null)
                {
                    UnturnedLog.info("Shutting down BattlEye client");
                    bool flag2 = battlEyeClientRunData.pfnExit();
                    UnturnedLog.info("BattlEye client shutdown result: {0}", flag2);
                }
                BEClient.dlclose(battlEyeClientHandle);
                battlEyeClientHandle = IntPtr.Zero;
            }
            NetMessages.SendMessageToServer(EServerMessage.GracefullyDisconnect, ENetReliability.Reliable, delegate
            {
            });
            clientTransport.TearDown();
            SteamFriends.SetRichPresence("connect", "");
            Lobbies.leaveLobby();
            closeTicket();
            SteamUser.AdvertiseGame(CSteamID.Nil, 0u, 0);
            _server = default(CSteamID);
            _isServer = false;
            _isClient = false;
        }
        onClientDisconnected?.Invoke();
        if (!isApplicationQuitting)
        {
            Level.exit();
        }
        Assets.ClearServerAssetMapping();
        unloadGameMode();
        _isConnected = false;
        isWaitingForConnectResponse = false;
        isWaitingForWorkshopResponse = false;
        isLoadingUGC = false;
        isWaitingForAuthenticationResponse = false;
        isLoadingInventory = true;
        UnturnedLog.info("Disconnected");
    }

    [Obsolete]
    public static void sendGUIDTable(SteamPending player)
    {
        accept(player);
    }

    private static bool initializeBattlEyeServer()
    {
        string text = ReadWrite.PATH + "/BattlEye/BEServer_x64.so";
        if (!File.Exists(text))
        {
            text = ReadWrite.PATH + "/BattlEye/BEServer.so";
        }
        if (!File.Exists(text))
        {
            UnturnedLog.error("Missing BattlEye server library! (" + text + ")");
            return false;
        }
        UnturnedLog.info("Loading BattlEye server library from: " + text);
        try
        {
            battlEyeServerHandle = BEServer.dlopen(text, 2);
            if (battlEyeServerHandle != IntPtr.Zero)
            {
                if (Marshal.GetDelegateForFunctionPointer(BEServer.dlsym(battlEyeServerHandle, "Init"), typeof(BEServer.BEServerInitFn)) is BEServer.BEServerInitFn bEServerInitFn)
                {
                    battlEyeServerInitData = new BEServer.BESV_GAME_DATA();
                    battlEyeServerInitData.pstrGameVersion = APP_NAME + " " + APP_VERSION;
                    battlEyeServerInitData.pfnPrintMessage = battlEyeServerPrintMessage;
                    battlEyeServerInitData.pfnKickPlayer = battlEyeServerKickPlayer;
                    battlEyeServerInitData.pfnSendPacket = battlEyeServerSendPacket;
                    battlEyeServerRunData = new BEServer.BESV_BE_DATA();
                    if (bEServerInitFn(0, battlEyeServerInitData, battlEyeServerRunData))
                    {
                        return true;
                    }
                    BEServer.dlclose(battlEyeServerHandle);
                    battlEyeServerHandle = IntPtr.Zero;
                    UnturnedLog.error("Failed to call BattlEye server init!");
                    return false;
                }
                BEServer.dlclose(battlEyeServerHandle);
                battlEyeServerHandle = IntPtr.Zero;
                UnturnedLog.error("Failed to get BattlEye server init delegate!");
                return false;
            }
            UnturnedLog.error("Failed to load BattlEye server library!");
            return false;
        }
        catch (Exception e)
        {
            UnturnedLog.error("Unhandled exception when loading BattlEye server library!");
            UnturnedLog.exception(e);
            return false;
        }
    }

    private static void handleServerReady()
    {
        if (!isServerConnectedToSteam)
        {
            isServerConnectedToSteam = true;
            CommandWindow.Log("Steam servers ready!");
            initializeDedicatedUGC();
        }
    }

    private static void initializeDedicatedUGC()
    {
        WorkshopDownloadConfig orLoad = WorkshopDownloadConfig.getOrLoad();
        DedicatedUGC.initialize();
        if ((bool)Assets.shouldLoadAnyAssets)
        {
            foreach (ulong file_ID in orLoad.File_IDs)
            {
                DedicatedUGC.registerItemInstallation(file_ID);
            }
        }
        DedicatedUGC.installed += onDedicatedUGCInstalled;
        DedicatedUGC.beginInstallingItems(Dedicator.offlineOnly);
    }

    public static string getModeTagAbbreviation(EGameMode gm)
    {
        return gm switch
        {
            EGameMode.EASY => "EZY", 
            EGameMode.HARD => "HRD", 
            EGameMode.NORMAL => "NRM", 
            _ => null, 
        };
    }

    public static string getCameraModeTagAbbreviation(ECameraMode cm)
    {
        return cm switch
        {
            ECameraMode.FIRST => "1Pp", 
            ECameraMode.BOTH => "2Pp", 
            ECameraMode.THIRD => "3Pp", 
            ECameraMode.VEHICLE => "4Pp", 
            _ => null, 
        };
    }

    public static string GetMonetizationTagAbbreviation(EServerMonetizationTag monetization)
    {
        return monetization switch
        {
            EServerMonetizationTag.None => "MTXn", 
            EServerMonetizationTag.NonGameplay => "MTXy", 
            EServerMonetizationTag.Monetized => "MTXg", 
            _ => null, 
        };
    }

    private static void maybeLogCuratedMapFallback(string attemptedMap)
    {
        if (statusData == null || statusData.Maps == null || statusData.Maps.Curated_Map_Links == null)
        {
            return;
        }
        foreach (CuratedMapLink curated_Map_Link in statusData.Maps.Curated_Map_Links)
        {
            if (curated_Map_Link.Name.Equals(attemptedMap, StringComparison.InvariantCultureIgnoreCase))
            {
                CommandWindow.LogWarningFormat("Attempting to load curated map '{0}'? Include its workshop file ID ({1}) in the WorkshopDownloadConfig.json File_IDs array.", curated_Map_Link.Name, curated_Map_Link.Workshop_File_Id);
                break;
            }
        }
    }

    private static void onDedicatedUGCInstalled()
    {
        if (isDedicatedUGCInstalled)
        {
            return;
        }
        isDedicatedUGCInstalled = true;
        apiWarningMessageHook = onAPIWarningMessage;
        SteamGameServerUtils.SetWarningMessageHook(apiWarningMessageHook);
        time = SteamGameServerUtils.GetServerRealTime();
        LevelInfo level = Level.getLevel(map);
        if (level == null)
        {
            string text = map;
            maybeLogCuratedMapFallback(text);
            map = "PEI";
            CommandWindow.LogError(localization.format("Map_Missing", text, map));
            level = Level.getLevel(map);
            if (level == null)
            {
                CommandWindow.LogError("Fatal error: unable to load fallback map");
            }
        }
        Level.load(level, hasAuthority: true);
        loadGameMode();
        applyLevelModeConfigOverrides();
        dswUpdateMonitor = DedicatedWorkshopUpdateMonitorFactory.createForLevel(level);
        SteamGameServer.SetMaxPlayerCount(maxPlayers);
        SteamGameServer.SetServerName(serverName);
        SteamGameServer.SetPasswordProtected(serverPassword != "");
        SteamGameServer.SetMapName(map);
        if (Dedicator.IsDedicatedServer)
        {
            if (!ReadWrite.folderExists("/Bundles/Workshop/Content", usePath: true))
            {
                ReadWrite.createFolder("/Bundles/Workshop/Content", usePath: true);
            }
            string text2 = "/Bundles/Workshop/Content";
            string[] folders = ReadWrite.getFolders(text2);
            for (int i = 0; i < folders.Length; i++)
            {
                string text3 = ReadWrite.folderName(folders[i]);
                if (ulong.TryParse(text3, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
                {
                    registerServerUsingWorkshopFileId(result);
                    CommandWindow.Log("Recommended to add workshop item " + result + " to WorkshopDownloadConfig.json and remove it from " + text2);
                }
                else
                {
                    CommandWindow.LogWarning("Invalid workshop item '" + text3 + "' in " + text2);
                }
            }
            string text4 = ServerSavedata.directory + "/" + serverID + "/Workshop/Content";
            if (!ReadWrite.folderExists(text4, usePath: true))
            {
                ReadWrite.createFolder(text4, usePath: true);
            }
            string[] folders2 = ReadWrite.getFolders(text4);
            for (int j = 0; j < folders2.Length; j++)
            {
                string text5 = ReadWrite.folderName(folders2[j]);
                if (ulong.TryParse(text5, NumberStyles.Any, CultureInfo.InvariantCulture, out var result2))
                {
                    registerServerUsingWorkshopFileId(result2);
                    CommandWindow.Log("Recommended to add workshop item " + result2 + " to WorkshopDownloadConfig.json and remove it from " + text4);
                }
                else
                {
                    CommandWindow.LogWarning("Invalid workshop item '" + text5 + "' in " + text4);
                }
            }
            if (ulong.TryParse(new DirectoryInfo(Level.info.path).Parent.Name, NumberStyles.Any, CultureInfo.InvariantCulture, out var result3))
            {
                registerServerUsingWorkshopFileId(result3);
            }
            SteamGameServer.SetGameData(string.Concat(string.Concat(string.Concat(string.Concat(string.Concat(((serverPassword != "") ? "PASS" : "SSAP") + ",", configData.Server.VAC_Secure ? "VAC_ON" : "VAC_OFF"), ",GAME_VERSION_"), VersionUtils.binaryToHexadecimal(APP_VERSION_PACKED)), ",MAP_VERSION_"), VersionUtils.binaryToHexadecimal(Level.packedVersion)));
            SteamGameServer.SetKeyValue("GameVersion", APP_VERSION);
            int num = 128;
            string text6 = (isPvP ? "PVP" : "PVE") + "," + (hasCheats ? "CHy" : "CHn") + "," + getModeTagAbbreviation(mode) + "," + getCameraModeTagAbbreviation(cameraMode) + "," + ((getServerWorkshopFileIDs().Count > 0) ? "WSy" : "WSn") + "," + (isGold ? "GLD" : "F2P");
            text6 = text6 + "," + (IsBattlEyeEnabled ? "BEy" : "BEn");
            string monetizationTagAbbreviation = GetMonetizationTagAbbreviation(configData.Browser.Monetization);
            if (!string.IsNullOrEmpty(monetizationTagAbbreviation))
            {
                text6 = text6 + "," + monetizationTagAbbreviation;
            }
            if (!string.IsNullOrEmpty(configData.Browser.Thumbnail))
            {
                text6 = text6 + ",<tn>" + configData.Browser.Thumbnail + "</tn>";
            }
            text6 += $",<net>{NetTransportFactory.GetTag(serverTransport)}</net>";
            string pluginFrameworkTag = SteamPluginAdvertising.Get().PluginFrameworkTag;
            if (!string.IsNullOrEmpty(pluginFrameworkTag))
            {
                text6 += $",<pf>{pluginFrameworkTag}</pf>";
            }
            if (text6.Length > num)
            {
                CommandWindow.LogWarning("Server browser thumbnail URL is " + (text6.Length - num) + " characters over budget!");
                CommandWindow.LogWarning("Server will not list properly until this URL is adjusted!");
            }
            SteamGameServer.SetGameTags(text6);
            int num2 = 64;
            if (configData.Browser.Desc_Server_List.Length > num2)
            {
                CommandWindow.LogWarning("Server browser description is " + (configData.Browser.Desc_Server_List.Length - num2) + " characters over budget!");
            }
            SteamGameServer.SetGameDescription(configData.Browser.Desc_Server_List);
            SteamGameServer.SetKeyValue("Browser_Icon", configData.Browser.Icon);
            SteamGameServer.SetKeyValue("Browser_Desc_Hint", configData.Browser.Desc_Hint);
            AdvertiseFullDescription(configData.Browser.Desc_Full);
            if (getServerWorkshopFileIDs().Count > 0)
            {
                string text7 = string.Empty;
                for (int k = 0; k < getServerWorkshopFileIDs().Count; k++)
                {
                    if (text7.Length > 0)
                    {
                        text7 += ",";
                    }
                    text7 += getServerWorkshopFileIDs()[k];
                }
                int num3 = (text7.Length - 1) / 127 + 1;
                int num4 = 0;
                SteamGameServer.SetKeyValue("Mod_Count", num3.ToString());
                for (int l = 0; l < text7.Length; l += 127)
                {
                    int num5 = 127;
                    if (l + num5 > text7.Length)
                    {
                        num5 = text7.Length - l;
                    }
                    string pValue = text7.Substring(l, num5);
                    SteamGameServer.SetKeyValue("Mod_" + num4, pValue);
                    num4++;
                }
            }
            if (configData.Browser.Links != null && configData.Browser.Links.Length != 0)
            {
                SteamGameServer.SetKeyValue("Custom_Links_Count", configData.Browser.Links.Length.ToString());
                for (int m = 0; m < configData.Browser.Links.Length; m++)
                {
                    BrowserConfigData.Link link = configData.Browser.Links[m];
                    if (!ConvertEx.TryEncodeUtf8StringAsBase64(link.Message, out var output))
                    {
                        UnturnedLog.error("Unable to encode lobby link message as Base64: \"" + link.Message + "\"");
                        continue;
                    }
                    if (!ConvertEx.TryEncodeUtf8StringAsBase64(link.Url, out var output2))
                    {
                        UnturnedLog.error("Unable to encode lobby link url as Base64: \"" + link.Url + "\"");
                        continue;
                    }
                    SteamGameServer.SetKeyValue("Custom_Link_Message_" + m, output);
                    SteamGameServer.SetKeyValue("Custom_Link_Url_" + m, output2);
                }
            }
            AdvertiseConfig();
            SteamPluginAdvertising.Get().NotifyGameServerReady();
        }
        _server = SteamGameServer.GetSteamID();
        _client = _server;
        _clientHash = Hash.SHA1(client);
        if (Dedicator.IsDedicatedServer)
        {
            _clientName = localization.format("Console");
            autoShutdownManager = steam.gameObject.AddComponent<BuiltinAutoShutdown>();
            SteamGameServer.GetPublicIP().TryGetIPv4Address(out var address);
            EHostBanFlags eHostBanFlags = HostBansManager.Get().MatchBasicDetails(address, port, serverName, _server.m_SteamID);
            eHostBanFlags |= HostBansManager.Get().MatchExtendedDetails(configData.Browser.Desc_Server_List, configData.Browser.Thumbnail);
            if ((eHostBanFlags & EHostBanFlags.RecommendHostCheckWarningsList) != 0)
            {
                CommandWindow.LogWarning("It appears this server has received a warning.");
                CommandWindow.LogWarning("Checking the Unturned Server Standing page is recommended:");
                CommandWindow.LogWarning("https://smartlydressedgames.com/UnturnedHostBans/index.html");
            }
            if (eHostBanFlags.HasFlag(EHostBanFlags.Blocked))
            {
                shutdown();
            }
        }
        timeLastPacketWasReceivedFromServer = Time.realtimeSinceStartup;
    }

    private static void AdvertiseFullDescription(string message)
    {
        if (string.IsNullOrEmpty(message))
        {
            return;
        }
        if (!ConvertEx.TryEncodeUtf8StringAsBase64(message, out var output))
        {
            UnturnedLog.error("Unable to encode server browser description to Base64");
            return;
        }
        if (string.IsNullOrEmpty(output))
        {
            UnturnedLog.error("Base64 server browser description was empty");
            return;
        }
        int num = (output.Length - 1) / 127 + 1;
        int num2 = 0;
        SteamGameServer.SetKeyValue("Browser_Desc_Full_Count", num.ToString());
        for (int i = 0; i < output.Length; i += 127)
        {
            int num3 = 127;
            if (i + num3 > output.Length)
            {
                num3 = output.Length - i;
            }
            string pValue = output.Substring(i, num3);
            SteamGameServer.SetKeyValue("Browser_Desc_Full_Line_" + num2, pValue);
            num2++;
        }
    }

    private static void AdvertiseConfig()
    {
        ModeConfigData modeConfig = ConfigData.CreateDefault(singleplayer: false).getModeConfig(mode);
        if (modeConfig == null)
        {
            CommandWindow.LogError("Unable to compare default for advertise config");
            return;
        }
        int num = 0;
        FieldInfo[] fields = modeConfigData.GetType().GetFields();
        foreach (FieldInfo fieldInfo in fields)
        {
            object value = fieldInfo.GetValue(modeConfigData);
            object value2 = fieldInfo.GetValue(modeConfig);
            FieldInfo[] fields2 = value.GetType().GetFields();
            foreach (FieldInfo fieldInfo2 in fields2)
            {
                object value3 = fieldInfo2.GetValue(value);
                object value4 = fieldInfo2.GetValue(value2);
                string text = null;
                Type fieldType = fieldInfo2.FieldType;
                if (fieldType == typeof(bool))
                {
                    bool flag = (bool)value3;
                    bool flag2 = (bool)value4;
                    if (flag != flag2)
                    {
                        text = fieldInfo.Name + "." + fieldInfo2.Name + "=" + (flag ? "T" : "F");
                    }
                }
                else if (fieldType == typeof(float))
                {
                    float a = (float)value3;
                    float b = (float)value4;
                    if (!MathfEx.IsNearlyEqual(a, b, 0.0001f))
                    {
                        text = fieldInfo.Name + "." + fieldInfo2.Name + "=" + a.ToString(CultureInfo.InvariantCulture);
                    }
                }
                else if (fieldType == typeof(uint))
                {
                    uint num2 = (uint)value3;
                    uint num3 = (uint)value4;
                    if (num2 != num3)
                    {
                        text = fieldInfo.Name + "." + fieldInfo2.Name + "=" + num2.ToString(CultureInfo.InvariantCulture);
                    }
                }
                else
                {
                    CommandWindow.LogErrorFormat("Unable to advertise config type: {0}", fieldType);
                }
                if (!string.IsNullOrEmpty(text))
                {
                    string pKey = "Cfg_" + num.ToString(CultureInfo.InvariantCulture);
                    num++;
                    SteamGameServer.SetKeyValue(pKey, text);
                }
            }
        }
        SteamGameServer.SetKeyValue("Cfg_Count", num.ToString(CultureInfo.InvariantCulture));
    }

    [Obsolete]
    public static void send(CSteamID steamID, ESteamPacket type, byte[] packet, int size, int channel)
    {
        ITransportConnection transportConnection = findTransportConnection(steamID);
        if (transportConnection != null)
        {
            sendToClient(transportConnection, type, packet, size);
        }
    }

    [Obsolete]
    private static bool remapSteamPacketType(ref ESteamPacket type)
    {
        switch (type)
        {
        case ESteamPacket.KICKED:
            type = ESteamPacket.UPDATE_RELIABLE_BUFFER;
            return true;
        case ESteamPacket.CONNECTED:
            type = ESteamPacket.UPDATE_UNRELIABLE_BUFFER;
            return true;
        default:
            return false;
        }
    }

    [Obsolete]
    public static void sendToClient(ITransportConnection transportConnection, ESteamPacket type, byte[] packet, int size)
    {
        if (size < 1)
        {
            throw new ArgumentOutOfRangeException("size");
        }
        if (transportConnection == null)
        {
            throw new ArgumentNullException("transportConnection");
        }
        if (isConnected)
        {
            if (!isServer)
            {
                throw new NotSupportedException("Sending to client while not running as server");
            }
            if (remapSteamPacketType(ref type))
            {
                packet[0] = (byte)type;
            }
            _bytesSent += (uint)size;
            _packetsSent++;
            ENetReliability reliability = (isUnreliable(type) ? ENetReliability.Unreliable : ENetReliability.Reliable);
            transportConnection.Send(packet, size, reliability);
        }
    }

    public static bool shouldNetIgnoreSteamId(CSteamID id)
    {
        return netIgnoredSteamIDs.Contains(id);
    }

    public static void refuseGarbageConnection(CSteamID remoteId, string reason)
    {
        string[] obj = new string[5] { "Refusing connections from ", null, null, null, null };
        CSteamID cSteamID = remoteId;
        obj[1] = cSteamID.ToString();
        obj[2] = " (";
        obj[3] = reason;
        obj[4] = ")";
        UnturnedLog.info(string.Concat(obj));
        netIgnoredSteamIDs.Add(remoteId);
    }

    public static void refuseGarbageConnection(ITransportConnection transportConnection, string reason)
    {
        if (transportConnection == null)
        {
            throw new ArgumentNullException("transportConnection");
        }
        transportConnection.CloseConnection();
        CSteamID cSteamID = findTransportConnectionSteamId(transportConnection);
        if (cSteamID != CSteamID.Nil)
        {
            refuseGarbageConnection(cSteamID, reason);
        }
    }

    public static bool hasNetBufferChanged(byte[] original, byte[] copy, int offset, int size)
    {
        for (int num = offset + size - 1; num >= offset; num--)
        {
            if (copy[num] != original[num])
            {
                return true;
            }
        }
        return false;
    }

    internal static bool getChannelHeader(byte[] packet, int size, int offset, out int channel)
    {
        int num = offset + 2;
        if (num + 1 > size)
        {
            channel = -1;
            return false;
        }
        channel = packet[num];
        return true;
    }

    internal static bool canClientVersionJoinServer(uint version)
    {
        return version == APP_VERSION_PACKED;
    }

    internal static void legacyReceiveClient(byte[] packet, int offset, int size)
    {
        CSteamID steamID = server;
        _bytesReceived += (uint)size;
        _packetsReceived++;
        if (getChannelHeader(packet, size, offset, out var channel))
        {
            SteamChannel steamChannel = findChannelComponent(channel);
            if (steamChannel != null)
            {
                steamChannel.receive(steamID, packet, offset, size);
            }
        }
    }

    private static void listenServer()
    {
        long size;
        ITransportConnection transportConnection;
        while (serverTransport.Receive(buffer, out size, out transportConnection))
        {
            NetMessages.ReceiveMessageFromClient(transportConnection, buffer, 0, (int)size);
        }
    }

    private static void listenClient()
    {
        long size;
        while (clientTransport.Receive(buffer, out size))
        {
            NetMessages.ReceiveMessageFromServer(buffer, 0, (int)size);
        }
    }

    private void SendPingRequestToAllClients()
    {
        float realtimeSinceStartup = Time.realtimeSinceStartup;
        foreach (SteamPlayer client in clients)
        {
            if (realtimeSinceStartup - client.timeLastPingRequestWasSentToClient > 1f || client.timeLastPingRequestWasSentToClient < 0f)
            {
                client.timeLastPingRequestWasSentToClient = realtimeSinceStartup;
                NetMessages.SendMessageToClient(EClientMessage.PingRequest, ENetReliability.Unreliable, client.transportConnection, delegate
                {
                });
            }
        }
    }

    private void NotifyClientsInQueueOfPosition()
    {
        int queuePosition;
        for (queuePosition = 0; queuePosition < pending.Count; queuePosition++)
        {
            if (pending[queuePosition].lastNotifiedQueuePosition != queuePosition)
            {
                pending[queuePosition].lastNotifiedQueuePosition = queuePosition;
                NetMessages.SendMessageToClient(EClientMessage.QueuePositionChanged, ENetReliability.Reliable, pending[queuePosition].transportConnection, delegate(NetPakWriter writer)
                {
                    writer.WriteUInt8(MathfEx.ClampToByte(queuePosition));
                });
            }
        }
    }

    private void KickClientsWithBadConnection()
    {
        clientsWithBadConnecion.Clear();
        float realtimeSinceStartup = Time.realtimeSinceStartup;
        float num = 0f;
        foreach (SteamPlayer client in clients)
        {
            float num2 = realtimeSinceStartup - client.timeLastPacketWasReceivedFromClient;
            num += num2;
            if (num2 > configData.Server.Timeout_Game_Seconds)
            {
                if (CommandWindow.shouldLogJoinLeave)
                {
                    SteamPlayerID playerID = client.playerID;
                    CommandWindow.Log(localization.format("Dismiss_Timeout", playerID.steamID, playerID.playerName, playerID.characterName));
                }
                UnturnedLog.info($"Kicking {client.transportConnection} after {num2}s without message");
                clientsWithBadConnecion.Add(client);
            }
            else
            {
                if (!(realtimeSinceStartup - client.joined > configData.Server.Timeout_Game_Seconds))
                {
                    continue;
                }
                int num3 = Mathf.FloorToInt(client.ping * 1000f);
                if (num3 > configData.Server.Max_Ping_Milliseconds)
                {
                    if (CommandWindow.shouldLogJoinLeave)
                    {
                        SteamPlayerID playerID2 = client.playerID;
                        CommandWindow.Log(localization.format("Dismiss_Ping", num3, configData.Server.Max_Ping_Milliseconds, playerID2.steamID, playerID2.playerName, playerID2.characterName));
                    }
                    UnturnedLog.info($"Kicking {client.transportConnection} because their ping ({num3}ms) exceeds limit ({configData.Server.Max_Ping_Milliseconds}ms)");
                    clientsWithBadConnecion.Add(client);
                }
            }
        }
        if (clientsWithBadConnecion.Count > 1)
        {
            float num4 = num / (float)clientsWithBadConnecion.Count;
            UnturnedLog.info($"Kicking {clientsWithBadConnecion.Count} clients with bad connection this frame. Maybe something blocked the main thread on the server? Average time since last message: {num4}s");
        }
        foreach (SteamPlayer item in clientsWithBadConnecion)
        {
            try
            {
                dismiss(item.playerID.steamID);
            }
            catch (Exception e)
            {
                UnturnedLog.exception(e, "Caught exception while kicking client for bad connection:");
            }
        }
    }

    private void KickClientsBlockingUpQueue()
    {
        if (pending.Count < 1)
        {
            return;
        }
        float clampedTimeoutQueueSeconds = configData.Server.GetClampedTimeoutQueueSeconds();
        SteamPending steamPending = pending[0];
        if (steamPending.hasSentVerifyPacket && steamPending.realtimeSinceSentVerifyPacket > clampedTimeoutQueueSeconds)
        {
            UnturnedLog.info("Front of queue player timed out: {0} ({1})", steamPending.playerID.steamID, steamPending.GetQueueStateDebugString());
            ESteamRejection rejection;
            if (!steamPending.hasAuthentication && steamPending.hasProof && steamPending.hasGroup)
            {
                rejection = ESteamRejection.LATE_PENDING_STEAM_AUTH;
                UnturnedLog.info($"Server was only waiting for Steam authentication response for front of queue player, but {steamPending.realtimeSinceSentVerifyPacket}s passed so we will give the next player a chance instead.");
            }
            else if (steamPending.hasAuthentication && !steamPending.hasProof && steamPending.hasGroup)
            {
                rejection = ESteamRejection.LATE_PENDING_STEAM_ECON;
                UnturnedLog.info($"Server was only waiting for Steam economy/inventory details response for front of queue player, but {steamPending.realtimeSinceSentVerifyPacket}s passed so we will give the next player a chance instead.");
            }
            else if (steamPending.hasAuthentication && steamPending.hasProof && !steamPending.hasGroup)
            {
                rejection = ESteamRejection.LATE_PENDING_STEAM_GROUPS;
                UnturnedLog.info($"Server was only waiting for Steam group/clan details response for front of queue player, but {steamPending.realtimeSinceSentVerifyPacket}s passed so we will give the next player a chance instead.");
            }
            else
            {
                rejection = ESteamRejection.LATE_PENDING;
                UnturnedLog.info($"Server was waiting for multiple responses about front of queue player, but {steamPending.realtimeSinceSentVerifyPacket}s passed so we will give the next player a chance instead.");
            }
            reject(steamPending.playerID.steamID, rejection);
        }
        else
        {
            if (pending.Count <= 1)
            {
                return;
            }
            float realtimeSinceStartup = Time.realtimeSinceStartup;
            for (int num = pending.Count - 1; num > 0; num--)
            {
                float num2 = realtimeSinceStartup - pending[num].lastReceivedPingRequestRealtime;
                if (num2 > configData.Server.Timeout_Queue_Seconds)
                {
                    SteamPending steamPending2 = pending[num];
                    UnturnedLog.info($"Kicking queued player {steamPending2.transportConnection} after {num2}s without message");
                    reject(steamPending2.playerID.steamID, ESteamRejection.LATE_PENDING);
                    break;
                }
            }
        }
    }

    private static void listen()
    {
        if (!isConnected)
        {
            return;
        }
        if (isServer)
        {
            if (!Dedicator.IsDedicatedServer || !Level.isLoaded)
            {
                return;
            }
            TransportConnectionListPool.ReleaseAll();
            listenServer();
            if (Dedicator.IsDedicatedServer)
            {
                if (Time.realtimeSinceStartup - lastPingRequestTime > PING_REQUEST_INTERVAL)
                {
                    lastPingRequestTime = Time.realtimeSinceStartup;
                    steam.SendPingRequestToAllClients();
                }
                if (Time.realtimeSinceStartup - lastQueueNotificationTime > 6f)
                {
                    lastQueueNotificationTime = Time.realtimeSinceStartup;
                    steam.NotifyClientsInQueueOfPosition();
                }
                steam.KickClientsWithBadConnection();
                steam.KickClientsBlockingUpQueue();
                if (steam.clientsKickedForTransportConnectionFailureCount > 1)
                {
                    UnturnedLog.info($"Removed {steam.clientsKickedForTransportConnectionFailureCount} clients due to transport failure this frame");
                }
                steam.clientsKickedForTransportConnectionFailureCount = 0;
            }
            if (dswUpdateMonitor != null)
            {
                dswUpdateMonitor.tick(Time.deltaTime);
            }
            return;
        }
        listenClient();
        if (!isConnected)
        {
            return;
        }
        if (Time.realtimeSinceStartup - lastPingRequestTime > PING_REQUEST_INTERVAL && (Time.realtimeSinceStartup - timeLastPingRequestWasSentToServer > 1f || timeLastPingRequestWasSentToServer < 0f))
        {
            lastPingRequestTime = Time.realtimeSinceStartup;
            timeLastPingRequestWasSentToServer = Time.realtimeSinceStartup;
            NetMessages.SendMessageToServer(EServerMessage.PingRequest, ENetReliability.Unreliable, delegate
            {
            });
        }
        if (isLoadingUGC)
        {
            if (isWaitingForWorkshopResponse)
            {
                float num = Time.realtimeSinceStartup - timeLastPacketWasReceivedFromServer;
                if (num > (float)CLIENT_TIMEOUT)
                {
                    _connectionFailureInfo = ESteamConnectionFailureInfo.TIMED_OUT;
                    RequestDisconnect($"Server did not reply to workshop details request ({num}s elapsed)");
                }
            }
            else
            {
                timeLastPacketWasReceivedFromServer = Time.realtimeSinceStartup;
            }
        }
        else if (Level.isLoading)
        {
            float num2 = Time.realtimeSinceStartup - timeLastPacketWasReceivedFromServer;
            if (isWaitingForConnectResponse && num2 > 10f)
            {
                _connectionFailureInfo = ESteamConnectionFailureInfo.TIMED_OUT;
                RequestDisconnect($"Server did not reply to connection request ({num2}s elapsed)");
                return;
            }
            if (isWaitingForAuthenticationResponse)
            {
                double num3 = Time.realtimeSinceStartupAsDouble - sentAuthenticationRequestTime;
                if (num3 > 30.0)
                {
                    _connectionFailureInfo = ESteamConnectionFailureInfo.TIMED_OUT_LOGIN;
                    RequestDisconnect($"Server did not reply to authentication request ({num3}s elapsed)");
                    return;
                }
            }
            timeLastPacketWasReceivedFromServer = Time.realtimeSinceStartup;
        }
        else
        {
            float num4 = Time.realtimeSinceStartup - timeLastPacketWasReceivedFromServer;
            if (num4 > (float)CLIENT_TIMEOUT)
            {
                _connectionFailureInfo = ESteamConnectionFailureInfo.TIMED_OUT;
                RequestDisconnect($"it has been {num4}s without a message from the server");
            }
            else if (battlEyeHasRequiredRestart)
            {
                battlEyeHasRequiredRestart = false;
                RequestDisconnect("BattlEye required restart");
            }
            else
            {
                ClientAssetIntegrity.SendRequests();
            }
        }
    }

    private static void broadcastServerDisconnected(CSteamID steamID)
    {
        try
        {
            onServerDisconnected?.Invoke(steamID);
        }
        catch (Exception e)
        {
            UnturnedLog.warn("Plugin raised an exception from onServerDisconnected:");
            UnturnedLog.exception(e);
        }
    }

    private static void broadcastServerHosted()
    {
        try
        {
            onServerHosted?.Invoke();
        }
        catch (Exception e)
        {
            UnturnedLog.warn("Plugin raised an exception from onServerHosted:");
            UnturnedLog.exception(e);
        }
    }

    private static void broadcastServerShutdown()
    {
        try
        {
            onServerShutdown?.Invoke();
        }
        catch (Exception e)
        {
            UnturnedLog.warn("Plugin raised an exception from onServerShutdown:");
            UnturnedLog.exception(e);
        }
    }

    private static void onP2PSessionConnectFail(P2PSessionConnectFail_t callback)
    {
        UnturnedLog.info($"Removing player {callback.m_steamIDRemote} due to P2P connect failure (Error: {callback.m_eP2PSessionError})");
        dismiss(callback.m_steamIDRemote);
    }

    internal static void checkBanStatus(SteamPlayerID playerID, uint remoteIP, out bool isBanned, out string banReason, out uint banRemainingDuration)
    {
        isBanned = false;
        banReason = string.Empty;
        banRemainingDuration = 0u;
        if (SteamBlacklist.checkBanned(playerID.steamID, remoteIP, playerID.GetHwids(), out var blacklistID))
        {
            isBanned = true;
            banReason = blacklistID.reason;
            banRemainingDuration = blacklistID.getTime();
        }
        try
        {
            if (onCheckBanStatusWithHWID != null)
            {
                onCheckBanStatusWithHWID(playerID, remoteIP, ref isBanned, ref banReason, ref banRemainingDuration);
            }
            else if (onCheckBanStatus != null)
            {
                onCheckBanStatus(playerID.steamID, remoteIP, ref isBanned, ref banReason, ref banRemainingDuration);
            }
        }
        catch (Exception e)
        {
            UnturnedLog.warn("Plugin raised an exception from onCheckBanStatus:");
            UnturnedLog.exception(e);
        }
    }

    [Obsolete("Now accepts list of HWIDs to ban")]
    public static bool requestBanPlayer(CSteamID instigator, CSteamID playerToBan, uint ipToBan, string reason, uint duration)
    {
        return requestBanPlayer(instigator, playerToBan, ipToBan, null, reason, duration);
    }

    public static bool requestBanPlayer(CSteamID instigator, CSteamID playerToBan, uint ipToBan, IEnumerable<byte[]> hwidsToBan, string reason, uint duration)
    {
        bool shouldVanillaBan = true;
        try
        {
            onBanPlayerRequested?.Invoke(instigator, playerToBan, ipToBan, ref reason, ref duration, ref shouldVanillaBan);
        }
        catch (Exception e)
        {
            UnturnedLog.exception(e, "Plugin raised an exception from onBanPlayerRequested:");
        }
        try
        {
            onBanPlayerRequestedV2?.Invoke(instigator, playerToBan, ipToBan, hwidsToBan, ref reason, ref duration, ref shouldVanillaBan);
        }
        catch (Exception e2)
        {
            UnturnedLog.exception(e2, "Plugin raised an exception from onBanPlayerRequestedV2:");
        }
        if (shouldVanillaBan)
        {
            SteamBlacklist.ban(playerToBan, ipToBan, hwidsToBan, instigator, reason, duration);
        }
        return true;
    }

    public static bool requestUnbanPlayer(CSteamID instigator, CSteamID playerToUnban)
    {
        bool shouldVanillaUnban = true;
        try
        {
            onUnbanPlayerRequested?.Invoke(instigator, playerToUnban, ref shouldVanillaUnban);
        }
        catch (Exception e)
        {
            UnturnedLog.warn("Plugin raised an exception from onUnbanPlayerRequested:");
            UnturnedLog.exception(e);
        }
        if (shouldVanillaUnban)
        {
            return SteamBlacklist.unban(playerToUnban);
        }
        return true;
    }

    private static void handleValidateAuthTicketResponse(ValidateAuthTicketResponse_t callback)
    {
        if (callback.m_eAuthSessionResponse == EAuthSessionResponse.k_EAuthSessionResponseOK)
        {
            SteamPending steamPending = null;
            for (int i = 0; i < pending.Count; i++)
            {
                if (pending[i].playerID.steamID == callback.m_SteamID)
                {
                    steamPending = pending[i];
                    break;
                }
            }
            if (steamPending == null)
            {
                for (int j = 0; j < clients.Count; j++)
                {
                    if (clients[j].playerID.steamID == callback.m_SteamID)
                    {
                        return;
                    }
                }
                reject(callback.m_SteamID, ESteamRejection.NOT_PENDING);
                return;
            }
            bool isValid = true;
            string explanation = string.Empty;
            try
            {
                if (onCheckValidWithExplanation != null)
                {
                    onCheckValidWithExplanation(callback, ref isValid, ref explanation);
                }
                else if (onCheckValid != null)
                {
                    onCheckValid(callback, ref isValid);
                }
            }
            catch (Exception e)
            {
                UnturnedLog.warn("Plugin raised an exception from onCheckValidWithExplanation or onCheckValid:");
                UnturnedLog.exception(e);
            }
            if (!isValid)
            {
                reject(callback.m_SteamID, ESteamRejection.PLUGIN, explanation);
                return;
            }
            bool flag = SteamGameServer.UserHasLicenseForApp(steamPending.playerID.steamID, PRO_ID) != EUserHasLicenseForAppResult.k_EUserHasLicenseResultDoesNotHaveLicense;
            if (isGold && !flag)
            {
                reject(steamPending.playerID.steamID, ESteamRejection.PRO_SERVER);
                return;
            }
            if ((steamPending.playerID.characterID >= Customization.FREE_CHARACTERS && !flag) || steamPending.playerID.characterID >= Customization.FREE_CHARACTERS + Customization.PRO_CHARACTERS)
            {
                reject(steamPending.playerID.steamID, ESteamRejection.PRO_CHARACTER);
                return;
            }
            if (!flag && steamPending.isPro)
            {
                reject(steamPending.playerID.steamID, ESteamRejection.PRO_DESYNC);
                return;
            }
            if (steamPending.face >= Customization.FACES_FREE + Customization.FACES_PRO || (!flag && steamPending.face >= Customization.FACES_FREE))
            {
                reject(steamPending.playerID.steamID, ESteamRejection.PRO_APPEARANCE);
                return;
            }
            if (steamPending.hair >= Customization.HAIRS_FREE + Customization.HAIRS_PRO || (!flag && steamPending.hair >= Customization.HAIRS_FREE))
            {
                reject(steamPending.playerID.steamID, ESteamRejection.PRO_APPEARANCE);
                return;
            }
            if (steamPending.beard >= Customization.BEARDS_FREE + Customization.BEARDS_PRO || (!flag && steamPending.beard >= Customization.BEARDS_FREE))
            {
                reject(steamPending.playerID.steamID, ESteamRejection.PRO_APPEARANCE);
                return;
            }
            if (!flag)
            {
                if (!Customization.checkSkin(steamPending.skin))
                {
                    reject(steamPending.playerID.steamID, ESteamRejection.PRO_APPEARANCE);
                    return;
                }
                if (!Customization.checkColor(steamPending.color))
                {
                    reject(steamPending.playerID.steamID, ESteamRejection.PRO_APPEARANCE);
                    return;
                }
            }
            steamPending.assignedPro = flag;
            steamPending.assignedAdmin = SteamAdminlist.checkAdmin(steamPending.playerID.steamID);
            steamPending.hasAuthentication = true;
            if (steamPending.canAcceptYet)
            {
                accept(steamPending);
            }
        }
        else if (callback.m_eAuthSessionResponse == EAuthSessionResponse.k_EAuthSessionResponseUserNotConnectedToSteam)
        {
            reject(callback.m_SteamID, ESteamRejection.AUTH_NO_STEAM);
        }
        else if (callback.m_eAuthSessionResponse == EAuthSessionResponse.k_EAuthSessionResponseNoLicenseOrExpired)
        {
            reject(callback.m_SteamID, ESteamRejection.AUTH_LICENSE_EXPIRED);
        }
        else if (callback.m_eAuthSessionResponse == EAuthSessionResponse.k_EAuthSessionResponseVACBanned)
        {
            reject(callback.m_SteamID, ESteamRejection.AUTH_VAC_BAN);
        }
        else if (callback.m_eAuthSessionResponse == EAuthSessionResponse.k_EAuthSessionResponseLoggedInElseWhere)
        {
            reject(callback.m_SteamID, ESteamRejection.AUTH_ELSEWHERE);
        }
        else if (callback.m_eAuthSessionResponse == EAuthSessionResponse.k_EAuthSessionResponseVACCheckTimedOut)
        {
            reject(callback.m_SteamID, ESteamRejection.AUTH_TIMED_OUT);
        }
        else if (callback.m_eAuthSessionResponse == EAuthSessionResponse.k_EAuthSessionResponseAuthTicketCanceled)
        {
            if (CommandWindow.shouldLogJoinLeave)
            {
                CSteamID steamID = callback.m_SteamID;
                CommandWindow.Log("Player finished session: " + steamID.ToString());
            }
            else
            {
                CSteamID steamID = callback.m_SteamID;
                UnturnedLog.info("Player finished session: " + steamID.ToString());
            }
            dismiss(callback.m_SteamID);
        }
        else if (callback.m_eAuthSessionResponse == EAuthSessionResponse.k_EAuthSessionResponseAuthTicketInvalidAlreadyUsed)
        {
            reject(callback.m_SteamID, ESteamRejection.AUTH_USED);
        }
        else if (callback.m_eAuthSessionResponse == EAuthSessionResponse.k_EAuthSessionResponseAuthTicketInvalid)
        {
            reject(callback.m_SteamID, ESteamRejection.AUTH_NO_USER);
        }
        else if (callback.m_eAuthSessionResponse == EAuthSessionResponse.k_EAuthSessionResponsePublisherIssuedBan)
        {
            reject(callback.m_SteamID, ESteamRejection.AUTH_PUB_BAN);
        }
        else
        {
            if (CommandWindow.shouldLogJoinLeave)
            {
                CSteamID steamID = callback.m_SteamID;
                CommandWindow.Log("Kicking player " + steamID.ToString() + " for unknown session response " + callback.m_eAuthSessionResponse);
            }
            else
            {
                CSteamID steamID = callback.m_SteamID;
                UnturnedLog.info("Kicking player " + steamID.ToString() + " for unknown session response " + callback.m_eAuthSessionResponse);
            }
            dismiss(callback.m_SteamID);
        }
    }

    private static void onValidateAuthTicketResponse(ValidateAuthTicketResponse_t callback)
    {
        handleValidateAuthTicketResponse(callback);
    }

    private static void handleClientGroupStatus(GSClientGroupStatus_t callback)
    {
        SteamPending steamPending = null;
        for (int i = 0; i < pending.Count; i++)
        {
            if (pending[i].playerID.steamID == callback.m_SteamIDUser)
            {
                steamPending = pending[i];
                break;
            }
        }
        if (steamPending == null)
        {
            reject(callback.m_SteamIDUser, ESteamRejection.NOT_PENDING);
            return;
        }
        if (!callback.m_bMember && !callback.m_bOfficer)
        {
            steamPending.playerID.group = CSteamID.Nil;
        }
        steamPending.hasGroup = true;
        if (steamPending.canAcceptYet)
        {
            accept(steamPending);
        }
    }

    private static void onClientGroupStatus(GSClientGroupStatus_t callback)
    {
        handleClientGroupStatus(callback);
    }

    public static ushort GetServerConnectionPort()
    {
        return (ushort)(port + 1);
    }

    public static void resetConfig()
    {
        _modeConfigData = new ModeConfigData(mode);
        switch (mode)
        {
        case EGameMode.EASY:
            configData.Easy = modeConfigData;
            break;
        case EGameMode.NORMAL:
            configData.Normal = modeConfigData;
            break;
        case EGameMode.HARD:
            configData.Hard = modeConfigData;
            break;
        }
        ServerSavedata.serializeJSON("/Config.json", configData);
    }

    private static void applyLevelConfigOverride(FieldInfo field, object targetObject, object defaultTargetObject, KeyValuePair<string, object> levelOverride)
    {
        object value = field.GetValue(targetObject);
        object value2 = field.GetValue(defaultTargetObject);
        Type fieldType = field.FieldType;
        bool flag2;
        if (fieldType == typeof(bool))
        {
            bool num = (bool)value;
            bool flag = (bool)value2;
            flag2 = num == flag;
            if (flag2)
            {
                field.SetValue(targetObject, Convert.ToBoolean(levelOverride.Value));
            }
        }
        else if (fieldType == typeof(float))
        {
            float a = (float)value;
            float b = (float)value2;
            flag2 = MathfEx.IsNearlyEqual(a, b, 0.0001f);
            if (flag2)
            {
                field.SetValue(targetObject, Convert.ToSingle(levelOverride.Value));
            }
        }
        else
        {
            if (!(fieldType == typeof(uint)))
            {
                CommandWindow.LogErrorFormat("Unable to handle level mode config override type: {0} ({1})", fieldType, levelOverride.Key);
                return;
            }
            uint num2 = (uint)value;
            uint num3 = (uint)value2;
            flag2 = num2 == num3;
            if (flag2)
            {
                field.SetValue(targetObject, Convert.ToUInt32(levelOverride.Value));
            }
        }
        if (!flag2)
        {
            CommandWindow.LogFormat("Skipping level config override {0} because server value ({1}) is not the default ({2})", levelOverride.Key, value, value2);
        }
        else
        {
            CommandWindow.LogFormat("Level overrides config {0}: {1} (Default: {2})", levelOverride.Key, levelOverride.Value, value2);
        }
    }

    public static void applyLevelModeConfigOverrides()
    {
        if (Level.info == null || Level.info.configData == null)
        {
            return;
        }
        ModeConfigData modeConfig = ConfigData.CreateDefault(!Dedicator.IsDedicatedServer).getModeConfig(mode);
        if (modeConfig == null)
        {
            CommandWindow.LogError("Unable to compare default for level mode config overrides");
            return;
        }
        foreach (KeyValuePair<string, object> mode_Config_Override in Level.info.configData.Mode_Config_Overrides)
        {
            if (string.IsNullOrEmpty(mode_Config_Override.Key))
            {
                CommandWindow.LogError("Level mode config overrides contains an empty key");
                break;
            }
            if (mode_Config_Override.Value == null)
            {
                CommandWindow.LogError("Level mode config overrides contains a null value");
                break;
            }
            Type type = typeof(ModeConfigData);
            object value = modeConfigData;
            object obj = modeConfig;
            string[] array = mode_Config_Override.Key.Split('.');
            for (int i = 0; i < array.Length; i++)
            {
                string text = array[i];
                FieldInfo field = type.GetField(text);
                if (field == null)
                {
                    CommandWindow.LogError("Failed to find mode config for level override: " + text);
                    break;
                }
                if (i == array.Length - 1)
                {
                    try
                    {
                        applyLevelConfigOverride(field, value, obj, mode_Config_Override);
                    }
                    catch (Exception e)
                    {
                        CommandWindow.LogError("Exception when applying level config override: " + mode_Config_Override.Key);
                        UnturnedLog.exception(e);
                        break;
                    }
                }
                else
                {
                    type = field.FieldType;
                    value = field.GetValue(value);
                    obj = field.GetValue(obj);
                }
            }
        }
    }

    public static void accept(SteamPending player)
    {
        accept(player.playerID, player.assignedPro, player.assignedAdmin, player.face, player.hair, player.beard, player.skin, player.color, player.markerColor, player.hand, player.shirtItem, player.pantsItem, player.hatItem, player.backpackItem, player.vestItem, player.maskItem, player.glassesItem, player.skinItems, player.skinTags, player.skinDynamicProps, player.skillset, player.language, player.lobbyID, player.clientPlatform);
    }

    private static void WriteConnectedMessage(NetPakWriter writer, SteamPlayer aboutPlayer, SteamPlayer forPlayer)
    {
        writer.WriteNetId(aboutPlayer.GetNetId());
        writer.WriteSteamID(aboutPlayer.playerID.steamID);
        writer.WriteUInt8(aboutPlayer.playerID.characterID);
        writer.WriteString(aboutPlayer.playerID.playerName);
        writer.WriteString(aboutPlayer.playerID.characterName);
        writer.WriteClampedVector3(aboutPlayer.model.transform.position);
        byte value = (byte)(aboutPlayer.model.transform.rotation.eulerAngles.y / 2f);
        writer.WriteUInt8(value);
        writer.WriteBit(aboutPlayer.isPro);
        bool value2 = aboutPlayer.isAdmin;
        if (forPlayer != aboutPlayer && hideAdmins)
        {
            value2 = false;
        }
        writer.WriteBit(value2);
        writer.WriteUInt8((byte)aboutPlayer.channel);
        writer.WriteSteamID(aboutPlayer.playerID.group);
        writer.WriteString(aboutPlayer.playerID.nickName);
        writer.WriteUInt8(aboutPlayer.face);
        writer.WriteUInt8(aboutPlayer.hair);
        writer.WriteUInt8(aboutPlayer.beard);
        writer.WriteColor32RGB(aboutPlayer.skin);
        writer.WriteColor32RGB(aboutPlayer.color);
        writer.WriteColor32RGB(aboutPlayer.markerColor);
        writer.WriteBit(aboutPlayer.hand);
        writer.WriteInt32(aboutPlayer.shirtItem);
        writer.WriteInt32(aboutPlayer.pantsItem);
        writer.WriteInt32(aboutPlayer.hatItem);
        writer.WriteInt32(aboutPlayer.backpackItem);
        writer.WriteInt32(aboutPlayer.vestItem);
        writer.WriteInt32(aboutPlayer.maskItem);
        writer.WriteInt32(aboutPlayer.glassesItem);
        int[] skinItems = aboutPlayer.skinItems;
        writer.WriteUInt8((byte)skinItems.Length);
        int[] array = skinItems;
        foreach (int value3 in array)
        {
            writer.WriteInt32(value3);
        }
        string[] skinTags = aboutPlayer.skinTags;
        writer.WriteUInt8((byte)skinTags.Length);
        string[] array2 = skinTags;
        foreach (string value4 in array2)
        {
            writer.WriteString(value4);
        }
        string[] skinDynamicProps = aboutPlayer.skinDynamicProps;
        writer.WriteUInt8((byte)skinDynamicProps.Length);
        array2 = skinDynamicProps;
        foreach (string value5 in array2)
        {
            writer.WriteString(value5);
        }
        writer.WriteEnum(aboutPlayer.skillset);
        writer.WriteString(aboutPlayer.language);
    }

    private static void SendInitialGlobalState(SteamPlayer client)
    {
        LightingManager.SendInitialGlobalState(client);
        VehicleManager.SendInitialGlobalState(client);
        AnimalManager.SendInitialGlobalState(client.transportConnection);
        LevelManager.SendInitialGlobalState(client);
        ZombieManager.SendInitialGlobalState(client);
    }

    [Obsolete("This should not have been public in the first place")]
    public static void accept(SteamPlayerID playerID, bool isPro, bool isAdmin, byte face, byte hair, byte beard, Color skin, Color color, Color markerColor, bool hand, int shirtItem, int pantsItem, int hatItem, int backpackItem, int vestItem, int maskItem, int glassesItem, int[] skinItems, string[] skinTags, string[] skinDynamicProps, EPlayerSkillset skillset, string language, CSteamID lobbyID)
    {
        accept(playerID, isPro, isAdmin, face, hair, beard, skin, color, markerColor, hand, shirtItem, pantsItem, hatItem, backpackItem, vestItem, maskItem, glassesItem, skinItems, skinTags, skinDynamicProps, skillset, language, lobbyID, EClientPlatform.Windows);
    }

    internal static void accept(SteamPlayerID playerID, bool isPro, bool isAdmin, byte face, byte hair, byte beard, Color skin, Color color, Color markerColor, bool hand, int shirtItem, int pantsItem, int hatItem, int backpackItem, int vestItem, int maskItem, int glassesItem, int[] skinItems, string[] skinTags, string[] skinDynamicProps, EPlayerSkillset skillset, string language, CSteamID lobbyID, EClientPlatform clientPlatform)
    {
        ITransportConnection transportConnection = null;
        bool flag = false;
        int num = 0;
        for (int i = 0; i < pending.Count; i++)
        {
            if (pending[i].playerID == playerID)
            {
                if (pending[i].inventoryResult != SteamInventoryResult_t.Invalid)
                {
                    SteamGameServerInventory.DestroyResult(pending[i].inventoryResult);
                    pending[i].inventoryResult = SteamInventoryResult_t.Invalid;
                }
                transportConnection = pending[i].transportConnection;
                flag = true;
                num = i;
                pending.RemoveAt(i);
                break;
            }
        }
        if (!flag)
        {
            UnturnedLog.info($"Ignoring call to accept {playerID} because they are not in the queue");
            return;
        }
        UnturnedLog.info($"Accepting queued player {playerID}");
        string characterName = playerID.characterName;
        uint uScore = (isPro ? 1u : 0u);
        SteamGameServer.BUpdateUserData(playerID.steamID, characterName, uScore);
        loadPlayerSpawn(playerID, out var point, out var angle, out var initialStance);
        int channel = allocPlayerChannelId();
        NetId netId = ClaimNetIdBlockForNewPlayer();
        SteamPlayer newClient = addPlayer(transportConnection, netId, playerID, point, angle, isPro, isAdmin, channel, face, hair, beard, skin, color, markerColor, hand, shirtItem, pantsItem, hatItem, backpackItem, vestItem, maskItem, glassesItem, skinItems, skinTags, skinDynamicProps, skillset, language, lobbyID, clientPlatform);
        newClient.battlEyeId = allocBattlEyePlayerId();
        PlayerStance component = newClient.player.GetComponent<PlayerStance>();
        if (component != null)
        {
            component.initialStance = initialStance;
        }
        else
        {
            UnturnedLog.warn("Was unable to get PlayerStance for new connection!");
        }
        foreach (SteamPlayer aboutClient in _clients)
        {
            NetMessages.SendMessageToClient(EClientMessage.PlayerConnected, ENetReliability.Reliable, newClient.transportConnection, delegate(NetPakWriter writer)
            {
                WriteConnectedMessage(writer, aboutClient, newClient);
            });
        }
        SteamGameServer.GetPublicIP().TryGetIPv4Address(out var ipForClient);
        NetMessages.SendMessageToClient(EClientMessage.Accepted, ENetReliability.Reliable, transportConnection, delegate(NetPakWriter writer)
        {
            writer.WriteUInt32(ipForClient);
            writer.WriteUInt16(port);
            writer.WriteUInt8((byte)modeConfigData.Gameplay.Repair_Level_Max);
            writer.WriteBit(modeConfigData.Gameplay.Hitmarkers);
            writer.WriteBit(modeConfigData.Gameplay.Crosshair);
            writer.WriteBit(modeConfigData.Gameplay.Ballistics);
            writer.WriteBit(modeConfigData.Gameplay.Chart);
            writer.WriteBit(modeConfigData.Gameplay.Satellite);
            writer.WriteBit(modeConfigData.Gameplay.Compass);
            writer.WriteBit(modeConfigData.Gameplay.Group_Map);
            writer.WriteBit(modeConfigData.Gameplay.Group_HUD);
            writer.WriteBit(modeConfigData.Gameplay.Group_Player_List);
            writer.WriteBit(modeConfigData.Gameplay.Allow_Static_Groups);
            writer.WriteBit(modeConfigData.Gameplay.Allow_Dynamic_Groups);
            writer.WriteBit(modeConfigData.Gameplay.Allow_Shoulder_Camera);
            writer.WriteBit(modeConfigData.Gameplay.Can_Suicide);
            writer.WriteBit(modeConfigData.Gameplay.Friendly_Fire);
            writer.WriteBit(modeConfigData.Gameplay.Bypass_Buildable_Mobility);
            writer.WriteUInt16((ushort)modeConfigData.Gameplay.Timer_Exit);
            writer.WriteUInt16((ushort)modeConfigData.Gameplay.Timer_Respawn);
            writer.WriteUInt16((ushort)modeConfigData.Gameplay.Timer_Home);
            writer.WriteUInt16((ushort)modeConfigData.Gameplay.Max_Group_Members);
            writer.WriteBit(modeConfigData.Barricades.Allow_Item_Placement_On_Vehicle);
            writer.WriteBit(modeConfigData.Barricades.Allow_Trap_Placement_On_Vehicle);
            writer.WriteFloat(modeConfigData.Barricades.Max_Item_Distance_From_Hull);
            writer.WriteFloat(modeConfigData.Barricades.Max_Trap_Distance_From_Hull);
            writer.WriteFloat(modeConfigData.Gameplay.AirStrafing_Acceleration_Multiplier);
            writer.WriteFloat(modeConfigData.Gameplay.AirStrafing_Deceleration_Multiplier);
            writer.WriteFloat(modeConfigData.Gameplay.ThirdPerson_RecoilMultiplier);
            writer.WriteFloat(modeConfigData.Gameplay.ThirdPerson_SpreadMultiplier);
        });
        if (battlEyeServerHandle != IntPtr.Zero && battlEyeServerRunData != null && battlEyeServerRunData.pfnAddPlayer != null && battlEyeServerRunData.pfnReceivedPlayerGUID != null)
        {
            uint iPv4AddressOrZero = newClient.getIPv4AddressOrZero();
            transportConnection.TryGetPort(out var num2);
            uint ulAddress = ((iPv4AddressOrZero & 0xFF) << 24) | ((iPv4AddressOrZero & 0xFF00) << 8) | ((iPv4AddressOrZero & 0xFF0000) >> 8) | ((iPv4AddressOrZero & 0xFF000000u) >> 24);
            ushort usPort = (ushort)((uint)((num2 & 0xFF) << 8) | ((uint)(num2 & 0xFF00) >> 8));
            battlEyeServerRunData.pfnAddPlayer(newClient.battlEyeId, ulAddress, usPort, playerID.playerName);
            GCHandle gCHandle = GCHandle.Alloc(playerID.steamID.m_SteamID, GCHandleType.Pinned);
            IntPtr pvGUID = gCHandle.AddrOfPinnedObject();
            battlEyeServerRunData.pfnReceivedPlayerGUID(newClient.battlEyeId, pvGUID, 8);
            gCHandle.Free();
        }
        NetMessages.SendMessageToClients(EClientMessage.PlayerConnected, ENetReliability.Reliable, GatherRemoteClientConnectionsMatchingPredicate((SteamPlayer potentialRecipient) => potentialRecipient != newClient), delegate(NetPakWriter writer)
        {
            WriteConnectedMessage(writer, newClient, null);
        });
        SendInitialGlobalState(newClient);
        newClient.player.InitializePlayer();
        foreach (SteamPlayer client in _clients)
        {
            client.player.SendInitialPlayerState(newClient);
        }
        newClient.player.SendInitialPlayerState(GatherRemoteClientConnectionsMatchingPredicate((SteamPlayer potentialRecipient) => potentialRecipient != newClient));
        try
        {
            onServerConnected?.Invoke(playerID.steamID);
        }
        catch (Exception e)
        {
            UnturnedLog.warn("Plugin raised an exception from onServerConnected:");
            UnturnedLog.exception(e);
        }
        if (CommandWindow.shouldLogJoinLeave)
        {
            CommandWindow.Log(localization.format("PlayerConnectedText", playerID.steamID, playerID.playerName, playerID.characterName));
        }
        else
        {
            UnturnedLog.info(localization.format("PlayerConnectedText", playerID.steamID, playerID.playerName, playerID.characterName));
        }
        if (num == 0)
        {
            verifyNextPlayerInQueue();
        }
    }

    private static void broadcastRejectingPlayer(CSteamID steamID, ESteamRejection rejection, string explanation)
    {
        try
        {
            Provider.onRejectingPlayer?.Invoke(steamID, rejection, explanation);
        }
        catch (Exception e)
        {
            UnturnedLog.warn("Plugin raised an exception from onRejectingPlayer:");
            UnturnedLog.exception(e);
        }
    }

    public static void reject(CSteamID steamID, ESteamRejection rejection)
    {
        reject(steamID, rejection, string.Empty);
    }

    public static void reject(CSteamID steamID, ESteamRejection rejection, string explanation)
    {
        ITransportConnection transportConnection = findTransportConnection(steamID);
        if (transportConnection != null)
        {
            reject(transportConnection, rejection, explanation);
        }
    }

    public static void reject(ITransportConnection transportConnection, ESteamRejection rejection)
    {
        reject(transportConnection, rejection, string.Empty);
    }

    public static void reject(ITransportConnection transportConnection, ESteamRejection rejection, string explanation)
    {
        if (transportConnection == null)
        {
            throw new ArgumentNullException("transportConnection");
        }
        CSteamID cSteamID = findTransportConnectionSteamId(transportConnection);
        if (cSteamID != CSteamID.Nil)
        {
            broadcastRejectingPlayer(cSteamID, rejection, explanation);
        }
        for (int i = 0; i < pending.Count; i++)
        {
            if (transportConnection.Equals(pending[i].transportConnection))
            {
                if (rejection == ESteamRejection.AUTH_VAC_BAN)
                {
                    ChatManager.say(pending[i].playerID.playerName + " was banned by VAC", Color.yellow);
                }
                else if (rejection == ESteamRejection.AUTH_PUB_BAN)
                {
                    ChatManager.say(pending[i].playerID.playerName + " was banned by BattlEye", Color.yellow);
                }
                if (pending[i].inventoryResult != SteamInventoryResult_t.Invalid)
                {
                    SteamGameServerInventory.DestroyResult(pending[i].inventoryResult);
                    pending[i].inventoryResult = SteamInventoryResult_t.Invalid;
                }
                pending.RemoveAt(i);
                if (i == 0)
                {
                    verifyNextPlayerInQueue();
                }
                break;
            }
        }
        SteamGameServer.EndAuthSession(cSteamID);
        NetMessages.SendMessageToClient(EClientMessage.Rejected, ENetReliability.Reliable, transportConnection, delegate(NetPakWriter writer)
        {
            writer.WriteEnum(rejection);
            writer.WriteString(explanation);
        });
        transportConnection.CloseConnection();
    }

    [Obsolete]
    internal static void notifyClientPending(ITransportConnection transportConnection)
    {
    }

    private static bool findClientForKickBanDismiss(CSteamID steamID, out SteamPlayer foundClient, out byte foundIndex)
    {
        for (byte b = 0; b < clients.Count; b = (byte)(b + 1))
        {
            SteamPlayer steamPlayer = clients[b];
            if (steamPlayer.playerID.steamID == steamID)
            {
                foundClient = steamPlayer;
                foundIndex = b;
                return true;
            }
        }
        foundClient = null;
        foundIndex = 0;
        return false;
    }

    private static void validateDisconnectedMaintainedIndex(CSteamID steamID, byte index)
    {
        if (index >= clients.Count || clients[index].playerID.steamID != steamID)
        {
            UnturnedLog.error("Clients array was modified during onServerDisconnected!");
        }
    }

    private static void notifyKickedInternal(ITransportConnection transportConnection, string reason)
    {
        NetMessages.SendMessageToClient(EClientMessage.Kicked, ENetReliability.Reliable, transportConnection, delegate(NetPakWriter writer)
        {
            writer.WriteString(reason);
        });
    }

    public static void kick(CSteamID steamID, string reason)
    {
        if (findClientForKickBanDismiss(steamID, out var foundClient, out var foundIndex))
        {
            UnturnedLog.info($"Kicking player {steamID} because \"{reason}\"");
            notifyKickedInternal(foundClient.transportConnection, reason);
            broadcastServerDisconnected(steamID);
            validateDisconnectedMaintainedIndex(steamID, foundIndex);
            SteamGameServer.EndAuthSession(steamID);
            removePlayer(foundIndex);
            replicateRemovePlayer(steamID, foundIndex);
        }
    }

    internal static void notifyBannedInternal(ITransportConnection transportConnection, string reason, uint duration)
    {
        NetMessages.SendMessageToClient(EClientMessage.Banned, ENetReliability.Reliable, transportConnection, delegate(NetPakWriter writer)
        {
            writer.WriteString(reason);
            writer.WriteUInt32(duration);
        });
    }

    public static void ban(CSteamID steamID, string reason, uint duration)
    {
        if (findClientForKickBanDismiss(steamID, out var foundClient, out var foundIndex))
        {
            UnturnedLog.info($"Banning player {steamID} for {TimeSpan.FromSeconds(duration)} because \"{reason}\"");
            notifyBannedInternal(foundClient.transportConnection, reason, duration);
            broadcastServerDisconnected(steamID);
            validateDisconnectedMaintainedIndex(steamID, foundIndex);
            SteamGameServer.EndAuthSession(steamID);
            removePlayer(foundIndex);
            replicateRemovePlayer(steamID, foundIndex);
        }
    }

    public static void dismiss(CSteamID steamID)
    {
        if (findClientForKickBanDismiss(steamID, out var foundClient, out var foundIndex))
        {
            broadcastServerDisconnected(steamID);
            validateDisconnectedMaintainedIndex(steamID, foundIndex);
            SteamGameServer.EndAuthSession(steamID);
            if (CommandWindow.shouldLogJoinLeave)
            {
                CommandWindow.Log(localization.format("PlayerDisconnectedText", steamID, foundClient.playerID.playerName, foundClient.playerID.characterName));
            }
            else
            {
                UnturnedLog.info(localization.format("PlayerDisconnectedText", steamID, foundClient.playerID.playerName, foundClient.playerID.characterName));
            }
            removePlayer(foundIndex);
            replicateRemovePlayer(steamID, foundIndex);
        }
    }

    private static void OnServerTransportConnectionFailure(ITransportConnection transportConnection, string debugString, bool isError)
    {
        SteamPending steamPending = findPendingPlayer(transportConnection);
        if (steamPending != null)
        {
            if (isError)
            {
                steam.clientsKickedForTransportConnectionFailureCount++;
                UnturnedLog.info($"Removing player in queue {transportConnection} due to transport failure ({debugString}) queue state: \"{steamPending.GetQueueStateDebugString()}\"");
            }
            else
            {
                UnturnedLog.info($"Removing player in queue {transportConnection} because they disconnected ({debugString}) queue state: \"{steamPending.GetQueueStateDebugString()}\"");
            }
            reject(transportConnection, ESteamRejection.LATE_PENDING);
            return;
        }
        SteamPlayer steamPlayer = findPlayer(transportConnection);
        if (steamPlayer != null)
        {
            if (isError)
            {
                steam.clientsKickedForTransportConnectionFailureCount++;
                UnturnedLog.info($"Removing player {transportConnection} due to transport failure ({debugString})");
            }
            else
            {
                UnturnedLog.info($"Removing player {transportConnection} because they disconnected ({debugString})");
            }
            dismiss(steamPlayer.playerID.steamID);
        }
    }

    internal static bool verifyTicket(CSteamID steamID, byte[] ticket)
    {
        return SteamGameServer.BeginAuthSession(ticket, ticket.Length, steamID) == EBeginAuthSessionResult.k_EBeginAuthSessionResultOK;
    }

    private static void openGameServer()
    {
        if (isServer || isClient)
        {
            UnturnedLog.error("Failed to open game server: session already in progress.");
            return;
        }
        ESecurityMode eSecurityMode = ESecurityMode.LAN;
        switch (Dedicator.serverVisibility)
        {
        case ESteamServerVisibility.Internet:
            eSecurityMode = (configData.Server.VAC_Secure ? ESecurityMode.SECURE : ESecurityMode.INSECURE);
            break;
        case ESteamServerVisibility.LAN:
            eSecurityMode = ESecurityMode.LAN;
            break;
        }
        if (eSecurityMode == ESecurityMode.INSECURE)
        {
            CommandWindow.LogWarning(localization.format("InsecureWarningText"));
        }
        try
        {
            if (Process.GetCurrentProcess().MainModule.FileName.EndsWith(".x86"))
            {
                CommandWindow.LogWarning("Consider switching to the 64-bit Linux build: https://github.com/SmartlyDressedGames/Unturned-3.x-Community/issues/697");
            }
        }
        catch
        {
        }
        if (IsBattlEyeEnabled && eSecurityMode == ESecurityMode.SECURE && !initializeBattlEyeServer())
        {
            QuitGame("BattlEye server init failed");
            return;
        }
        bool flag = !Dedicator.offlineOnly;
        if (flag)
        {
            provider.multiplayerService.serverMultiplayerService.ready += handleServerReady;
        }
        try
        {
            provider.multiplayerService.serverMultiplayerService.open(ip, port, eSecurityMode);
        }
        catch (Exception ex)
        {
            QuitGame("server init failed (" + ex.Message + ")");
            return;
        }
        serverTransport = NetTransportFactory.CreateServerTransport();
        UnturnedLog.info("Initializing {0}", serverTransport.GetType().Name);
        serverTransport.Initialize(OnServerTransportConnectionFailure);
        backendRealtimeSeconds = SteamGameServerUtils.GetServerRealTime();
        authorityHoliday = (_modeConfigData.Gameplay.Allow_Holidays ? HolidayUtil.BackendGetActiveHoliday() : ENPCHoliday.NONE);
        if (flag)
        {
            CommandWindow.Log("Waiting for Steam servers...");
        }
        else
        {
            initializeDedicatedUGC();
        }
    }

    private static void closeGameServer()
    {
        if (!isServer)
        {
            UnturnedLog.error("Failed to close game server: no session in progress.");
            return;
        }
        broadcastServerShutdown();
        _isServer = false;
        provider.multiplayerService.serverMultiplayerService.close();
    }

    public static bool GetServerIsFavorited(uint ip, ushort queryPort)
    {
        foreach (CachedFavorite cachedFavorite in cachedFavorites)
        {
            if (cachedFavorite.matchesServer(ip, queryPort))
            {
                return cachedFavorite.isFavorited;
            }
        }
        for (int i = 0; i < SteamMatchmaking.GetFavoriteGameCount(); i++)
        {
            SteamMatchmaking.GetFavoriteGame(i, out var _, out var pnIP, out var _, out var pnQueryPort, out var punFlags, out var _);
            if ((punFlags | STEAM_FAVORITE_FLAG_FAVORITE) == punFlags && pnIP == ip && pnQueryPort == queryPort)
            {
                return true;
            }
        }
        return false;
    }

    public static void SetServerIsFavorited(uint ip, ushort connectionPort, ushort queryPort, bool newFavorited)
    {
        bool flag = false;
        foreach (CachedFavorite cachedFavorite2 in cachedFavorites)
        {
            if (cachedFavorite2.matchesServer(ip, queryPort))
            {
                cachedFavorite2.isFavorited = newFavorited;
                flag = true;
                break;
            }
        }
        if (!flag)
        {
            CachedFavorite cachedFavorite = new CachedFavorite();
            cachedFavorite.ip = ip;
            cachedFavorite.queryPort = port;
            cachedFavorite.isFavorited = newFavorited;
            cachedFavorites.Add(cachedFavorite);
        }
        if (newFavorited)
        {
            SteamMatchmaking.AddFavoriteGame(APP_ID, ip, connectionPort, queryPort, STEAM_FAVORITE_FLAG_FAVORITE, SteamUtils.GetServerRealTime());
        }
        else
        {
            SteamMatchmaking.RemoveFavoriteGame(APP_ID, ip, connectionPort, queryPort, STEAM_FAVORITE_FLAG_FAVORITE);
        }
    }

    public static void openURL(string url)
    {
        if (SteamUtils.IsOverlayEnabled())
        {
            SteamFriends.ActivateGameOverlayToWebPage(url);
        }
        else
        {
            Process.Start(url);
        }
    }

    public static void toggleCurrentServerFavorited()
    {
        if (!isServer)
        {
            SetServerIsFavorited(currentServerInfo.ip, currentServerInfo.connectionPort, currentServerInfo.queryPort, !isCurrentServerFavorited);
        }
    }

    private static void broadcastEnemyConnected(SteamPlayer player)
    {
        try
        {
            onEnemyConnected?.Invoke(player);
        }
        catch (Exception e)
        {
            UnturnedLog.warn("Exception during onEnemyConnected:");
            UnturnedLog.exception(e);
        }
    }

    private static void broadcastEnemyDisconnected(SteamPlayer player)
    {
        try
        {
            onEnemyDisconnected?.Invoke(player);
        }
        catch (Exception e)
        {
            UnturnedLog.warn("Exception during onEnemyDisconnected:");
            UnturnedLog.exception(e);
        }
    }

    private static void onPersonaStateChange(PersonaStateChange_t callback)
    {
        if (callback.m_nChangeFlags == EPersonaChange.k_EPersonaChangeName && callback.m_ulSteamID == client.m_SteamID)
        {
            _clientName = SteamFriends.GetPersonaName();
        }
    }

    private static void onGameServerChangeRequested(GameServerChangeRequested_t callback)
    {
        if (!isConnected)
        {
            UnturnedLog.info("onGameServerChangeRequested {0} {1}", callback.m_rgchServer, callback.m_rgchPassword);
            SteamConnectionInfo steamConnectionInfo = new SteamConnectionInfo(callback.m_rgchServer, callback.m_rgchPassword);
            UnturnedLog.info("External connect IP: {0} Port: {1} Password: '{2}'", Parser.getIPFromUInt32(steamConnectionInfo.ip), steamConnectionInfo.port, steamConnectionInfo.password);
            MenuPlayConnectUI.connect(steamConnectionInfo, shouldAutoJoin: false);
        }
    }

    private static void onGameRichPresenceJoinRequested(GameRichPresenceJoinRequested_t callback)
    {
        if (!isConnected)
        {
            UnturnedLog.info("onGameRichPresenceJoinRequested {0}", callback.m_rgchConnect);
            if (CommandLine.TryGetSteamConnect(callback.m_rgchConnect, out var newIP, out var queryPort, out var pass))
            {
                SteamConnectionInfo steamConnectionInfo = new SteamConnectionInfo(newIP, queryPort, pass);
                UnturnedLog.info("Rich presence connect IP: {0} Port: {1} Password: '{2}'", Parser.getIPFromUInt32(steamConnectionInfo.ip), steamConnectionInfo.port, steamConnectionInfo.password);
                MenuPlayConnectUI.connect(steamConnectionInfo, shouldAutoJoin: false);
            }
        }
    }

    internal static void lag(float value)
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

    internal static byte[] openTicket()
    {
        if (ticketHandle != HAuthTicket.Invalid)
        {
            return null;
        }
        byte[] array = new byte[1024];
        ticketHandle = SteamUser.GetAuthSessionTicket(array, array.Length, out var pcbTicket);
        if (pcbTicket == 0)
        {
            return null;
        }
        byte[] array2 = new byte[pcbTicket];
        Buffer.BlockCopy(array, 0, array2, 0, (int)pcbTicket);
        return array2;
    }

    private static void closeTicket()
    {
        if (!(ticketHandle == HAuthTicket.Invalid))
        {
            SteamUser.CancelAuthTicket(ticketHandle);
            ticketHandle = HAuthTicket.Invalid;
            UnturnedLog.info("Cancelled auth ticket");
        }
    }

    private IEnumerator QuitAfterDelay(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        QuitGame("server shutdown");
    }

    private static void onAPIWarningMessage(int severity, StringBuilder warning)
    {
        CommandWindow.LogWarning("Steam API Warning Message:");
        CommandWindow.LogWarning("Severity: " + severity);
        CommandWindow.LogWarning("Warning: " + warning);
    }

    private void updateDebug()
    {
        debugUpdates++;
        if (Time.realtimeSinceStartup - debugLastUpdate > 1f)
        {
            debugUPS = (int)((float)debugUpdates / (Time.realtimeSinceStartup - debugLastUpdate));
            debugLastUpdate = Time.realtimeSinceStartup;
            debugUpdates = 0;
        }
    }

    private void tickDebug()
    {
        debugTicks++;
        if (Time.realtimeSinceStartup - debugLastTick > 1f)
        {
            debugTPS = (int)((float)debugTicks / (Time.realtimeSinceStartup - debugLastTick));
            debugLastTick = Time.realtimeSinceStartup;
            debugTicks = 0;
        }
    }

    private IEnumerator downloadIcon(PendingIconRequest iconQueryParams)
    {
        UnityWebRequest request = UnityWebRequestTexture.GetTexture(iconQueryParams.url, nonReadable: true);
        request.timeout = 15;
        yield return request.SendWebRequest();
        Texture2D value = null;
        bool flag = false;
        if (request.result != UnityWebRequest.Result.Success)
        {
            UnturnedLog.warn($"{request.result} downloading \"{iconQueryParams.url}\" for icon query: \"{request.error}\"");
        }
        else
        {
            Texture2D content = DownloadHandlerTexture.GetContent(request);
            content.hideFlags = HideFlags.HideAndDontSave;
            content.filterMode = FilterMode.Trilinear;
            if (iconQueryParams.shouldCache)
            {
                if (downloadedIconCache.TryGetValue(iconQueryParams.url, out value))
                {
                    UnityEngine.Object.Destroy(content);
                }
                else
                {
                    downloadedIconCache.Add(iconQueryParams.url, content);
                    value = content;
                }
                flag = false;
            }
            else
            {
                value = content;
                flag = true;
            }
        }
        if (iconQueryParams.callback == null)
        {
            if (flag && value != null)
            {
                UnityEngine.Object.Destroy(value);
            }
        }
        else
        {
            try
            {
                iconQueryParams.callback(value, flag);
            }
            catch (Exception e)
            {
                UnturnedLog.exception(e, "Caught exception during texture downloaded callback:");
            }
        }
        if (iconQueryParams.shouldCache)
        {
            pendingCachableIconRequests.Remove(iconQueryParams.url);
        }
    }

    public static void destroyCachedIcon(string url)
    {
        if (downloadedIconCache.TryGetValue(url, out var value))
        {
            UnityEngine.Object.Destroy(value);
            downloadedIconCache.Remove(url);
        }
    }

    public static void refreshIcon(IconQueryParams iconQueryParams)
    {
        if (iconQueryParams.callback == null)
        {
            return;
        }
        if (string.IsNullOrEmpty(iconQueryParams.url) || !allowWebRequests)
        {
            iconQueryParams.callback(null, responsibleForDestroy: false);
            return;
        }
        iconQueryParams.url = iconQueryParams.url.Trim();
        if (string.IsNullOrEmpty(iconQueryParams.url))
        {
            iconQueryParams.callback(null, responsibleForDestroy: false);
            return;
        }
        if (iconQueryParams.shouldCache)
        {
            if (downloadedIconCache.TryGetValue(iconQueryParams.url, out var value))
            {
                iconQueryParams.callback(value, responsibleForDestroy: false);
                return;
            }
            if (pendingCachableIconRequests.TryGetValue(iconQueryParams.url, out var value2))
            {
                PendingIconRequest pendingIconRequest = value2;
                pendingIconRequest.callback = (IconQueryCallback)Delegate.Combine(pendingIconRequest.callback, iconQueryParams.callback);
                return;
            }
        }
        PendingIconRequest pendingIconRequest2 = new PendingIconRequest();
        pendingIconRequest2.url = iconQueryParams.url;
        pendingIconRequest2.callback = iconQueryParams.callback;
        pendingIconRequest2.shouldCache = iconQueryParams.shouldCache;
        if (iconQueryParams.shouldCache)
        {
            pendingCachableIconRequests.Add(iconQueryParams.url, pendingIconRequest2);
        }
        steam.StartCoroutine(steam.downloadIcon(pendingIconRequest2));
    }

    private void Update()
    {
        if (!isInitialized)
        {
            return;
        }
        if (Time.unscaledDeltaTime > 1.5f)
        {
            UnturnedLog.info("Long delay between Updates: {0}s", Time.unscaledDeltaTime);
        }
        if (battlEyeClientHandle != IntPtr.Zero && battlEyeClientRunData != null && battlEyeClientRunData.pfnRun != null)
        {
            battlEyeClientRunData.pfnRun();
        }
        if (battlEyeServerHandle != IntPtr.Zero && battlEyeServerRunData != null && battlEyeServerRunData.pfnRun != null)
        {
            battlEyeServerRunData.pfnRun();
        }
        if (isConnected)
        {
            listen();
        }
        updateDebug();
        provider.update();
        if (countShutdownTimer > 0)
        {
            if (Time.realtimeSinceStartup - lastTimerMessage > 1f)
            {
                lastTimerMessage = Time.realtimeSinceStartup;
                countShutdownTimer--;
                if (countShutdownTimer == 300 || countShutdownTimer == 60 || countShutdownTimer == 30 || countShutdownTimer == 15 || countShutdownTimer == 3 || countShutdownTimer == 2 || countShutdownTimer == 1)
                {
                    ChatManager.say(localization.format("Shutdown", countShutdownTimer), ChatManager.welcomeColor);
                }
            }
        }
        else
        {
            if (countShutdownTimer != 0)
            {
                return;
            }
            didServerShutdownTimerReachZero = true;
            countShutdownTimer = -1;
            broadcastCommenceShutdown();
            bool flag = _clients.Count > 0;
            if (_clients.Count > 0)
            {
                NetMessages.SendMessageToClients(EClientMessage.Shutdown, ENetReliability.Reliable, GatherRemoteClientConnections(), delegate(NetPakWriter writer)
                {
                    writer.WriteString(shutdownMessage);
                });
            }
            foreach (SteamPlayer client in _clients)
            {
                SteamGameServer.EndAuthSession(client.playerID.steamID);
            }
            float seconds = (flag ? 1f : 0f);
            StartCoroutine(QuitAfterDelay(seconds));
        }
    }

    private void FixedUpdate()
    {
        if (isInitialized)
        {
            tickDebug();
        }
    }

    public static void initAutoSubscribeMaps()
    {
        if (statusData == null || statusData.Maps == null)
        {
            return;
        }
        foreach (AutoSubscribeMap item in statusData.Maps.Auto_Subscribe)
        {
            if (!LocalNews.hasAutoSubscribedToWorkshopItem(item.Workshop_File_Id) && new DateTimeRange(item.Start, item.End).isNowWithinRange())
            {
                LocalNews.markAutoSubscribedToWorkshopItem(item.Workshop_File_Id);
                provider.workshopService.setSubscribed(item.Workshop_File_Id, subscribe: true);
            }
        }
        ConvenientSavedata.SaveIfDirty();
    }

    private void WriteSteamAppIdFileAndEnvironmentVariables()
    {
        string text = APP_ID.m_AppId.ToString(CultureInfo.InvariantCulture);
        UnturnedLog.info("Unturned overriding Steam AppId with \"" + text + "\"");
        try
        {
            Environment.SetEnvironmentVariable("SteamOverlayGameId", text, EnvironmentVariableTarget.Process);
            Environment.SetEnvironmentVariable("SteamGameId", text, EnvironmentVariableTarget.Process);
            Environment.SetEnvironmentVariable("SteamAppId", text, EnvironmentVariableTarget.Process);
        }
        catch (Exception e)
        {
            UnturnedLog.exception(e, "Caught exception writing Steam environment variables:");
        }
        string text2 = PathEx.Join(UnityPaths.GameDirectory, "steam_appid.txt");
        try
        {
            using FileStream stream = new FileStream(text2, FileMode.OpenOrCreate, FileAccess.Write, FileShare.ReadWrite);
            using StreamWriter streamWriter = new StreamWriter(stream, Encoding.ASCII);
            streamWriter.Write(text);
        }
        catch (Exception e2)
        {
            UnturnedLog.exception(e2, "Caught exception writing steam_appid.txt file:");
        }
    }

    public static StatusData LoadStatusData()
    {
        if (ReadWrite.fileExists("/Status.json", useCloud: false, usePath: true))
        {
            try
            {
                return ReadWrite.deserializeJSON<StatusData>("/Status.json", useCloud: false, usePath: true);
            }
            catch (Exception e)
            {
                UnturnedLog.exception(e, "Unable to parse Status.json! consider validating with a JSON linter");
            }
        }
        return null;
    }

    private void LoadPreferences()
    {
        string text = PathEx.Join(UnturnedPaths.RootDirectory, "Preferences.json");
        if (ReadWrite.fileExists(text, useCloud: false, usePath: false))
        {
            try
            {
                _preferenceData = ReadWrite.deserializeJSON<PreferenceData>(text, useCloud: false, usePath: false);
            }
            catch (Exception e)
            {
                UnturnedLog.exception(e, "Unable to parse Preferences.json! consider validating with a JSON linter");
                _preferenceData = null;
            }
            if (preferenceData == null)
            {
                _preferenceData = new PreferenceData();
            }
        }
        else
        {
            _preferenceData = new PreferenceData();
        }
        _preferenceData.Viewmodel.Clamp();
        SleekCustomization.defaultTextContrast = _preferenceData.Graphics.Default_Text_Contrast;
        SleekCustomization.inconspicuousTextContrast = _preferenceData.Graphics.Inconspicuous_Text_Contrast;
        SleekCustomization.colorfulTextContrast = _preferenceData.Graphics.Colorful_Text_Contrast;
        try
        {
            ReadWrite.serializeJSON(text, useCloud: false, usePath: false, preferenceData);
        }
        catch (Exception e2)
        {
            UnturnedLog.exception(e2, "Caught exception re-serializing Preferences.json:");
        }
    }

    public void awake()
    {
        _statusData = LoadStatusData();
        if (statusData == null)
        {
            _statusData = new StatusData();
        }
        HolidayUtil.scheduleHolidays(statusData.Holidays);
        APP_VERSION = statusData.Game.FormatApplicationVersion();
        APP_VERSION_PACKED = Parser.getUInt32FromIP(APP_VERSION);
        if (isInitialized)
        {
            UnityEngine.Object.Destroy(base.gameObject);
            return;
        }
        _isInitialized = true;
        UnityEngine.Object.DontDestroyOnLoad(base.gameObject);
        steam = this;
        Level.onLevelLoaded = (LevelLoaded)Delegate.Combine(Level.onLevelLoaded, new LevelLoaded(onLevelLoaded));
        Application.quitting += onApplicationQuitting;
        Application.wantsToQuit += onApplicationWantsToQuit;
        if (Dedicator.IsDedicatedServer)
        {
            try
            {
                WriteSteamAppIdFileAndEnvironmentVariables();
                provider = new SDG.SteamworksProvider.SteamworksProvider(new SteamworksAppInfo(APP_ID.m_AppId, APP_NAME, APP_VERSION, newIsDedicated: true));
                provider.intialize();
            }
            catch (Exception ex)
            {
                QuitGame("Steam init exception (" + ex.Message + ")");
                return;
            }
            if (!CommandLine.tryGetLanguage(out var local, out _path))
            {
                _path = ReadWrite.PATH + "/Localization/";
                local = "English";
            }
            language = local;
            localizationRoot = path + language;
            localization = Localization.read("/Server/ServerConsole.dat");
            p2pSessionConnectFail = Callback<P2PSessionConnectFail_t>.CreateGameServer(onP2PSessionConnectFail);
            validateAuthTicketResponse = Callback<ValidateAuthTicketResponse_t>.CreateGameServer(onValidateAuthTicketResponse);
            clientGroupStatus = Callback<GSClientGroupStatus_t>.CreateGameServer(onClientGroupStatus);
            _isPro = true;
            CommandWindow.Log("Game version: " + APP_VERSION + " Engine version: " + Application.unityVersion);
            maxPlayers = 8;
            queueSize = 8;
            serverName = "Unturned";
            serverPassword = "";
            ip = 0u;
            port = 27015;
            map = "PEI";
            isPvP = true;
            isWhitelisted = false;
            hideAdmins = false;
            hasCheats = false;
            filterName = false;
            mode = EGameMode.NORMAL;
            isGold = false;
            gameMode = null;
            cameraMode = ECameraMode.FIRST;
            Commander.init();
            SteamWhitelist.load();
            SteamBlacklist.load();
            SteamAdminlist.load();
            string[] commands = CommandLine.getCommands();
            UnturnedLog.info($"Executing {commands.Length} potential game command(s) from the command-line:");
            for (int i = 0; i < commands.Length; i++)
            {
                if (!Commander.execute(CSteamID.Nil, commands[i]))
                {
                    UnturnedLog.info("Did not match \"" + commands[i] + "\" with any commands");
                }
            }
            if (ServerSavedata.fileExists("/Server/Commands.dat"))
            {
                FileStream fileStream = null;
                StreamReader streamReader = null;
                try
                {
                    fileStream = new FileStream(ReadWrite.PATH + "/Servers/" + serverID + "/Server/Commands.dat", FileMode.Open, FileAccess.Read, FileShare.Read);
                    streamReader = new StreamReader(fileStream);
                    string text;
                    while ((text = streamReader.ReadLine()) != null)
                    {
                        if (!string.IsNullOrWhiteSpace(text) && !Commander.execute(CSteamID.Nil, text))
                        {
                            UnturnedLog.error("Unknown entry in Commands.dat: '{0}'", text);
                        }
                    }
                }
                finally
                {
                    fileStream?.Close();
                    streamReader?.Close();
                }
            }
            else
            {
                Data data = new Data();
                ServerSavedata.writeData("/Server/Commands.dat", data);
            }
            if (!ServerSavedata.folderExists("/Bundles"))
            {
                ServerSavedata.createFolder("/Bundles");
            }
            if (!ServerSavedata.folderExists("/Maps"))
            {
                ServerSavedata.createFolder("/Maps");
            }
            if (!ServerSavedata.folderExists("/Workshop/Content"))
            {
                ServerSavedata.createFolder("/Workshop/Content");
            }
            if (!ServerSavedata.folderExists("/Workshop/Maps"))
            {
                ServerSavedata.createFolder("/Workshop/Maps");
            }
            _configData = ConfigData.CreateDefault(singleplayer: false);
            if (ServerSavedata.fileExists("/Config.json"))
            {
                try
                {
                    ServerSavedata.populateJSON("/Config.json", _configData);
                }
                catch (Exception e)
                {
                    UnturnedLog.error("Exception while parsing server config:");
                    UnturnedLog.exception(e);
                }
            }
            ServerSavedata.serializeJSON("/Config.json", configData);
            _modeConfigData = _configData.getModeConfig(mode);
            if (_modeConfigData == null)
            {
                _modeConfigData = new ModeConfigData(mode);
            }
            if (!Dedicator.offlineOnly)
            {
                HostBansManager.Get().Refresh();
            }
            LogSystemInfo();
            return;
        }
        try
        {
            WriteSteamAppIdFileAndEnvironmentVariables();
            provider = new SDG.SteamworksProvider.SteamworksProvider(new SteamworksAppInfo(APP_ID.m_AppId, APP_NAME, APP_VERSION, newIsDedicated: false));
            provider.intialize();
        }
        catch (Exception ex2)
        {
            QuitGame("Steam init exception (" + ex2.Message + ")");
            return;
        }
        backendRealtimeSeconds = SteamUtils.GetServerRealTime();
        apiWarningMessageHook = onAPIWarningMessage;
        SteamUtils.SetWarningMessageHook(apiWarningMessageHook);
        screenshotRequestedCallback = Callback<ScreenshotRequested_t>.Create(OnSteamScreenshotRequested);
        SteamScreenshots.HookScreenshots(bHook: true);
        time = SteamUtils.GetServerRealTime();
        personaStateChange = Callback<PersonaStateChange_t>.Create(onPersonaStateChange);
        gameServerChangeRequested = Callback<GameServerChangeRequested_t>.Create(onGameServerChangeRequested);
        gameRichPresenceJoinRequested = Callback<GameRichPresenceJoinRequested_t>.Create(onGameRichPresenceJoinRequested);
        _user = SteamUser.GetSteamID();
        _client = user;
        _clientHash = Hash.SHA1(client);
        _clientName = SteamFriends.GetPersonaName();
        provider.statisticsService.userStatisticsService.requestStatistics();
        provider.statisticsService.globalStatisticsService.requestStatistics();
        provider.workshopService.refreshUGC();
        provider.workshopService.refreshPublished();
        if ((bool)shouldCheckForGoldUpgrade)
        {
            _isPro = SteamApps.BIsSubscribedApp(PRO_ID);
        }
        UnturnedLog.info("Game version: " + APP_VERSION + " Engine version: " + Application.unityVersion);
        isLoadingInventory = true;
        provider.economyService.GrantPromoItems();
        SteamNetworkingSockets.InitAuthentication();
        if (SteamUser.BLoggedOn() && (bool)allowWebRequests)
        {
            HostBansManager.Get().Refresh();
        }
        LiveConfig.Refresh();
        ProfanityFilter.InitSteam();
        if (CommandLine.tryGetLanguage(out var local2, out _path))
        {
            language = local2;
            localizationRoot = path + language;
        }
        else
        {
            string steamUILanguage = SteamUtils.GetSteamUILanguage();
            language = steamUILanguage.Substring(0, 1).ToUpper() + steamUILanguage.Substring(1, steamUILanguage.Length - 1).ToLower();
            bool flag = false;
            foreach (SteamContent item in provider.workshopService.ugc)
            {
                if (item.type == ESteamUGCType.LOCALIZATION && ReadWrite.folderExists(item.path + "/" + language, usePath: false))
                {
                    _path = item.path + "/";
                    localizationRoot = path + language;
                    flag = true;
                    UnturnedLog.info("Found Steam language '{0}' in workshop item {1}", steamUILanguage, item.publishedFileID);
                    break;
                }
            }
            if (!flag && ReadWrite.folderExists("/Localization/" + language))
            {
                _path = ReadWrite.PATH + "/Localization/";
                localizationRoot = path + language;
                flag = true;
                UnturnedLog.info("Found Steam language '{0}' in root Localization directory", steamUILanguage);
            }
            if (!flag && ReadWrite.folderExists("/Sandbox/" + language))
            {
                _path = ReadWrite.PATH + "/Sandbox/";
                localizationRoot = path + language;
                flag = true;
                UnturnedLog.info("Found Steam language '{0}' in Sandbox directory", steamUILanguage);
            }
            if (!flag)
            {
                foreach (SteamContent item2 in provider.workshopService.ugc)
                {
                    bool num = ReadWrite.folderExists(item2.path + "/Editor", usePath: false);
                    bool flag2 = ReadWrite.folderExists(item2.path + "/Menu", usePath: false);
                    bool flag3 = ReadWrite.folderExists(item2.path + "/Player", usePath: false);
                    bool flag4 = ReadWrite.folderExists(item2.path + "/Server", usePath: false);
                    bool flag5 = ReadWrite.folderExists(item2.path + "/Shared", usePath: false);
                    if (num && flag2 && flag3 && flag4 && flag5)
                    {
                        _path = null;
                        localizationRoot = item2.path;
                        flag = true;
                        UnturnedLog.info("Found language files for unknown language in workshop item {0}", item2.publishedFileID);
                    }
                }
            }
            if (!flag)
            {
                _path = ReadWrite.PATH + "/Localization/";
                language = "English";
                localizationRoot = path + language;
            }
        }
        provider.economyService.loadTranslationEconInfo();
        localization = Localization.read("/Server/ServerConsole.dat");
        updateRichPresence();
        _configData = ConfigData.CreateDefault(singleplayer: true);
        _modeConfigData = configData.Normal;
        LoadPreferences();
        if (ReadWrite.fileExists("/StreamerNames.json", useCloud: false, usePath: true))
        {
            try
            {
                streamerNames = ReadWrite.deserializeJSON<List<string>>("/StreamerNames.json", useCloud: false, usePath: true);
            }
            catch (Exception e2)
            {
                UnturnedLog.exception(e2, "Unable to parse StreamerNames.json! consider validating with a JSON linter");
                streamerNames = null;
            }
            if (streamerNames == null)
            {
                streamerNames = new List<string>();
            }
        }
        else
        {
            streamerNames = new List<string>();
        }
        LogSystemInfo();
    }

    public void start()
    {
    }

    public void unityStart()
    {
        _ = Dedicator.IsDedicatedServer;
    }

    private void LogSystemInfo()
    {
        try
        {
            UnturnedLog.info("Platform: {0}", Application.platform);
            UnturnedLog.info("Operating System: " + SystemInfo.operatingSystem);
            UnturnedLog.info("System Memory: " + SystemInfo.systemMemorySize + "MB");
            UnturnedLog.info("Graphics Device Name: " + SystemInfo.graphicsDeviceName);
            UnturnedLog.info("Graphics Device Type: " + SystemInfo.graphicsDeviceType);
            UnturnedLog.info("Graphics Memory: " + SystemInfo.graphicsMemorySize + "MB");
            UnturnedLog.info("Graphics Multi-Threaded: " + SystemInfo.graphicsMultiThreaded);
            UnturnedLog.info("Render Threading Mode: " + SystemInfo.renderingThreadingMode);
            UnturnedLog.info("Supports Audio: " + SystemInfo.supportsAudio);
            UnturnedLog.info("Supports Instancing: " + SystemInfo.supportsInstancing);
            UnturnedLog.info("Supports Motion Vectors: " + SystemInfo.supportsMotionVectors);
            UnturnedLog.info("Supports Ray Tracing: " + SystemInfo.supportsRayTracing);
        }
        catch (Exception e)
        {
            UnturnedLog.exception(e, "Caught exception while logging system info:");
        }
    }

    private void onApplicationQuitting()
    {
        UnturnedLog.info("Application quitting");
        isApplicationQuitting = true;
        if (!Dedicator.IsDedicatedServer)
        {
            ConvenientSavedata.save();
        }
        if (isInitialized)
        {
            RequestDisconnect("application quitting");
            provider.shutdown();
            UnturnedLog.info("Finished quitting");
        }
    }

    public static void QuitGame(string reason)
    {
        UnturnedLog.info("Quit game: " + reason);
        wasQuitGameCalled = true;
        Application.Quit();
    }

    private bool onApplicationWantsToQuit()
    {
        if (wasQuitGameCalled)
        {
            return true;
        }
        if (Dedicator.IsDedicatedServer)
        {
            return true;
        }
        if (!isServer && isPvP && clients.Count > 1 && Player.player != null && !Player.player.movement.isSafe && !Player.player.life.isDead)
        {
            return false;
        }
        return true;
    }
}
