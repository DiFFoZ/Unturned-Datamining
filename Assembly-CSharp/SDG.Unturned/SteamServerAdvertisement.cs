using SDG.HostBans;
using Steamworks;
using UnityEngine;
using Unturned.SystemEx;

namespace SDG.Unturned;

/// <summary>
/// Information about a game server retrieved through Steam's "A2S" query system.
/// Available when joining using the Steam server list API (in-game server browser)
/// or querying the Server's A2S port directly (connect by IP menu), but not when
/// joining by Steam ID.
/// </summary>
public class SteamServerAdvertisement
{
    public enum EPluginFramework
    {
        None,
        Rocket,
        OpenMod,
        Unknown
    }

    public enum EInfoSource
    {
        /// <summary>
        /// Join server by IP.
        /// </summary>
        DirectConnect,
        InternetServerList,
        FavoriteServerList,
        FriendServerList,
        HistoryServerList,
        LanServerList
    }

    private CSteamID _steamID;

    private uint _ip;

    public ushort queryPort;

    public ushort connectionPort;

    private string _name;

    private string _map;

    private bool _isPvP;

    private bool _hasCheats;

    private bool _isWorkshop;

    private EGameMode _mode;

    private ECameraMode _cameraMode;

    private int _ping;

    internal int sortingPing;

    private int _players;

    private int _maxPlayers;

    private bool _isPassworded;

    private bool _isPro;

    internal float utilityScore;

    internal EInfoSource infoSource;

    private static AnimationCurve pingCurve = new AnimationCurve(new Keyframe(50f, 1f), new Keyframe(100f, 0.6f), new Keyframe(300f, 0.3f), new Keyframe(900f, 0.1f));

    private static AnimationCurve fullnessCurve = new AnimationCurve(new Keyframe(0f, 0.1f), new Keyframe(0.5f, 0.8f), new Keyframe(0.75f, 1f), new Keyframe(1f, 0.8f));

    private static AnimationCurve playerCountCurve = new AnimationCurve(new Keyframe(2f, 0.1f), new Keyframe(18f, 0.8f), new Keyframe(64f, 1f));

    internal EHostBanFlags hostBanFlags;

    internal string serverCurationLabels;

    internal bool isDeniedByServerCurationRule;

    /// <summary>
    /// If set, this server was denied by a server curation list.
    /// </summary>
    internal ServerListCurationRule deniedByRule;

    public CSteamID steamID => _steamID;

    public uint ip => _ip;

    public string name => _name;

    public string map => _map;

    public bool isPvP => _isPvP;

    public bool hasCheats => _hasCheats;

    public bool isWorkshop => _isWorkshop;

    public EGameMode mode => _mode;

    public ECameraMode cameraMode => _cameraMode;

    public EServerMonetizationTag monetization { get; private set; }

    public int ping => _ping;

    public int players => _players;

    public int maxPlayers => _maxPlayers;

    public bool isPassworded => _isPassworded;

    public bool IsVACSecure { get; private set; }

    public bool IsBattlEyeSecure { get; private set; }

    public bool isPro => _isPro;

    /// <summary>
    /// ID of network transport implementation to use.
    /// </summary>
    public string networkTransport { get; protected set; }

    /// <summary>
    /// Known plugin systems.
    /// </summary>
    public EPluginFramework pluginFramework { get; protected set; }

    public string thumbnailURL { get; protected set; }

    public string descText { get; protected set; }

    /// <summary>
    /// Active player count divided by max player count.
    /// </summary>
    internal float NormalizedPlayerCount
    {
        get
        {
            if (_maxPlayers <= 0)
            {
                return 0f;
            }
            return Mathf.Clamp01((float)_players / (float)_maxPlayers);
        }
    }

    /// <summary>
    /// Nelson 2024-08-20: This score is intended to prioritize low ping without making it the be-all end-all. The
    /// old default of sorting by ping could put near-empty servers at the top of the list, and encouraged using
    /// anycast caching to make the server appear as low-ping as possible.
    /// </summary>
    private float PingUtilityScore => pingCurve.Evaluate(sortingPing);

    /// <summary>
    /// Nelson 2024-08-20: This score is intended to prioritize servers around 75% capacity. My thought process is
    /// that near-empty and near-full servers are already easy to find, but typically if you want to play online you
    /// want a server with space for you and your friends. Unfortunately, servers with plenty of players but an even
    /// higher max players make a 50% score plenty good.
    /// </summary>
    private float FullnessUtilityScore
    {
        get
        {
            int num = Mathf.Clamp(_maxPlayers, 1, 100);
            float time = Mathf.Clamp01((float)_players / (float)num);
            return fullnessCurve.Evaluate(time);
        }
    }

    /// <summary>
    /// Nelson 2024-08-20: This score is intended to balance out the downside of the fullness score decreasing for
    /// servers with very high max player counts, and over-scoring servers with low max players.
    /// </summary>
    private float PlayerCountUtilityScore => playerCountCurve.Evaluate(_players);

    /// <summary>
    /// Probably just checks whether IP is link-local, but may as well use Steam's utility function.
    /// </summary>
    public bool IsAddressUsingSteamFakeIP()
    {
        return SteamNetworkingUtils.IsFakeIPv4(ip);
    }

    /// <summary>
    /// Called before inserting to server list.
    /// </summary>
    internal void CalculateUtilityScore()
    {
        utilityScore = PingUtilityScore * FullnessUtilityScore * PlayerCountUtilityScore;
    }

    /// <summary>
    /// Parses value between two keys <stuff>thing</stuff> would parse thing
    /// </summary>
    protected string parseTagValue(string tags, string startKey, string endKey)
    {
        int num = tags.IndexOf(startKey);
        if (num == -1)
        {
            return null;
        }
        num += startKey.Length;
        int num2 = tags.IndexOf(endKey, num);
        if (num2 == -1)
        {
            return null;
        }
        if (num2 == num)
        {
            return null;
        }
        return tags.Substring(num, num2 - num);
    }

    protected bool hasTagKey(string tags, string key, int thumbnailIndex)
    {
        int num = tags.IndexOf(key);
        if (num == -1)
        {
            return false;
        }
        if (thumbnailIndex == -1)
        {
            return true;
        }
        return num < thumbnailIndex;
    }

    internal void SetServerListHostBanFlags(EHostBanFlags hostBanFlags)
    {
        this.hostBanFlags = hostBanFlags;
        if (hostBanFlags.HasFlag(EHostBanFlags.QueryPingWarning))
        {
            sortingPing += LiveConfig.Get().queryPingWarningOffsetMs;
        }
    }

    public SteamServerAdvertisement(gameserveritem_t data, EInfoSource infoSource)
    {
        _steamID = data.m_steamID;
        _ip = data.m_NetAdr.GetIP();
        queryPort = data.m_NetAdr.GetQueryPort();
        connectionPort = (ushort)(queryPort + 1);
        _name = data.GetServerName();
        ProfanityFilter.ApplyFilter(OptionsSettings.filter, ref _name);
        _map = data.GetMap();
        string gameTags = data.GetGameTags();
        if (gameTags.Length > 0)
        {
            int thumbnailIndex = gameTags.IndexOf("<tn>");
            _isPvP = hasTagKey(gameTags, "PVP", thumbnailIndex);
            _hasCheats = hasTagKey(gameTags, "CHy", thumbnailIndex);
            _isWorkshop = hasTagKey(gameTags, "WSy", thumbnailIndex);
            if (hasTagKey(gameTags, Provider.getModeTagAbbreviation(EGameMode.EASY), thumbnailIndex))
            {
                _mode = EGameMode.EASY;
            }
            else if (hasTagKey(gameTags, Provider.getModeTagAbbreviation(EGameMode.HARD), thumbnailIndex))
            {
                _mode = EGameMode.HARD;
            }
            else
            {
                _mode = EGameMode.NORMAL;
            }
            if (hasTagKey(gameTags, Provider.getCameraModeTagAbbreviation(ECameraMode.FIRST), thumbnailIndex))
            {
                _cameraMode = ECameraMode.FIRST;
            }
            else if (hasTagKey(gameTags, Provider.getCameraModeTagAbbreviation(ECameraMode.THIRD), thumbnailIndex))
            {
                _cameraMode = ECameraMode.THIRD;
            }
            else if (hasTagKey(gameTags, Provider.getCameraModeTagAbbreviation(ECameraMode.BOTH), thumbnailIndex))
            {
                _cameraMode = ECameraMode.BOTH;
            }
            else
            {
                _cameraMode = ECameraMode.VEHICLE;
            }
            if (hasTagKey(gameTags, Provider.GetMonetizationTagAbbreviation(EServerMonetizationTag.None), thumbnailIndex))
            {
                monetization = EServerMonetizationTag.None;
            }
            else if (hasTagKey(gameTags, Provider.GetMonetizationTagAbbreviation(EServerMonetizationTag.NonGameplay), thumbnailIndex))
            {
                monetization = EServerMonetizationTag.NonGameplay;
            }
            else if (hasTagKey(gameTags, Provider.GetMonetizationTagAbbreviation(EServerMonetizationTag.Monetized), thumbnailIndex))
            {
                monetization = EServerMonetizationTag.Monetized;
            }
            else
            {
                monetization = EServerMonetizationTag.Unspecified;
            }
            _isPro = hasTagKey(gameTags, "GLD", thumbnailIndex);
            IsBattlEyeSecure = hasTagKey(gameTags, "BEy", thumbnailIndex);
            networkTransport = parseTagValue(gameTags, "<net>", "</net>");
            if (string.IsNullOrEmpty(networkTransport))
            {
                UnturnedLog.warn("Unable to parse net transport tag for server \"{0}\" from \"{1}\"", name, gameTags);
            }
            string text = parseTagValue(gameTags, "<pf>", "</pf>");
            if (string.IsNullOrEmpty(text))
            {
                if (data.m_nBotPlayers == 1)
                {
                    pluginFramework = EPluginFramework.Rocket;
                }
                else
                {
                    pluginFramework = EPluginFramework.None;
                }
            }
            else if (text.Equals("rm"))
            {
                pluginFramework = EPluginFramework.Rocket;
            }
            else if (text.Equals("om"))
            {
                pluginFramework = EPluginFramework.OpenMod;
            }
            else
            {
                pluginFramework = EPluginFramework.Unknown;
            }
            thumbnailURL = parseTagValue(gameTags, "<tn>", "</tn>");
            string message = data.GetGameDescription();
            if (!RichTextUtil.IsTextValidForServerListShortDescription(message))
            {
                message = null;
            }
            else
            {
                ProfanityFilter.ApplyFilter(OptionsSettings.filter, ref message);
            }
            if (message.ContainsNewLine() || message.ContainsChar('\t'))
            {
                message = null;
                UnturnedLog.warn("Control characters not allowed in server \"" + name + "\" description");
            }
            descText = message;
        }
        else
        {
            _isPvP = true;
            _hasCheats = false;
            _mode = EGameMode.NORMAL;
            _cameraMode = ECameraMode.FIRST;
            monetization = EServerMonetizationTag.Unspecified;
            _isPro = true;
            IsBattlEyeSecure = false;
            networkTransport = null;
            pluginFramework = EPluginFramework.None;
            thumbnailURL = null;
            descText = null;
        }
        _ping = data.m_nPing;
        sortingPing = _ping;
        _maxPlayers = data.m_nMaxPlayers;
        if (data.m_nPlayers < 0 || data.m_nBotPlayers < 0 || data.m_nPlayers > 255 || data.m_nBotPlayers > 255)
        {
            _players = 0;
        }
        else
        {
            _players = Mathf.Max(0, data.m_nPlayers - data.m_nBotPlayers);
        }
        _isPassworded = data.m_bPassword;
        IsVACSecure = data.m_bSecure;
        this.infoSource = infoSource;
    }

    public SteamServerAdvertisement(string newName, EGameMode newMode, bool newVACSecure, bool newBattlEyeEnabled, bool newPro)
    {
        _name = newName;
        ProfanityFilter.ApplyFilter(OptionsSettings.filter, ref _name);
        _mode = newMode;
        IsVACSecure = newVACSecure;
        IsBattlEyeSecure = newBattlEyeEnabled;
        _isPro = newPro;
    }

    public SteamServerAdvertisement(CSteamID steamId)
    {
        _steamID = steamId;
    }

    public override string ToString()
    {
        return "Name: " + name + " Map: " + map + " PvP: " + isPvP + " Mode: " + mode.ToString() + " Ping: " + ping + " Players: " + players + "/" + maxPlayers + " Passworded: " + isPassworded;
    }
}
