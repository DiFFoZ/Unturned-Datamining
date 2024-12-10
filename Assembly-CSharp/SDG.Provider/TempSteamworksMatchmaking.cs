using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using SDG.HostBans;
using SDG.Unturned;
using Steamworks;
using UnityEngine;
using Unturned.SystemEx;

namespace SDG.Provider;

public class TempSteamworksMatchmaking
{
    public delegate void MasterServerAdded(int insert, SteamServerAdvertisement server);

    public delegate void MasterServerRemoved();

    public delegate void MasterServerResorted();

    public delegate void MasterServerRefreshed(EMatchMakingServerResponse response);

    public delegate void MasterServerQueryRefreshed(SteamServerAdvertisement server);

    public delegate void AttemptUpdated(int attempt);

    public delegate void TimedOut();

    public delegate void PlayersQueryRefreshed(string name, int score, float time);

    public delegate void RulesQueryRefreshed(Dictionary<string, string> rulesMap);

    public MasterServerAdded onMasterServerAdded;

    public MasterServerRemoved onMasterServerRemoved;

    public MasterServerResorted onMasterServerResorted;

    public MasterServerRefreshed onMasterServerRefreshed;

    public MasterServerQueryRefreshed onMasterServerQueryRefreshed;

    public PlayersQueryRefreshed onPlayersQueryRefreshed;

    public RulesQueryRefreshed onRulesQueryRefreshed;

    private SteamConnectionInfo connectionInfo;

    private ESteamServerList _currentList;

    private string currentNameFilter;

    private Regex currentNameRegex;

    private bool isCurrentNameFilterSet;

    private int currentMaxPingFilter;

    private EPlugins currentPluginsFilter;

    private EServerListCurationDenyMode curationDenyMode;

    private List<SteamServerAdvertisement> _serverList = new List<SteamServerAdvertisement>();

    private List<MatchMakingKeyValuePair_t> filters;

    private ISteamMatchmakingPingResponse serverPingResponse;

    private ISteamMatchmakingServerListResponse serverListResponse;

    private ISteamMatchmakingPlayersResponse playersResponse;

    private ISteamMatchmakingRulesResponse rulesResponse;

    private HServerQuery playersQuery = HServerQuery.Invalid;

    private HServerQuery rulesQuery = HServerQuery.Invalid;

    private Dictionary<string, string> rulesMap;

    private HServerQuery serverQuery = HServerQuery.Invalid;

    private int serverQueryAttempts;

    /// <summary>
    /// Reset after starting connection attempt, so set to true afterwards to auto join the server.
    /// </summary>
    public bool autoJoinServerQuery;

    public MenuPlayServerInfoUI.EServerInfoOpenContext serverQueryContext;

    private HServerListRequest serverListRequest = HServerListRequest.Invalid;

    private int serverListRefreshIndex = -1;

    private IComparer<SteamServerAdvertisement> _serverInfoComparer = new ServerListComparer_UtilityScore();

    public ESteamServerList currentList => _currentList;

    public List<SteamServerAdvertisement> serverList => _serverList;

    /// <summary>
    /// Used to show a warning when a lot of servers are blocked by curation list.
    /// </summary>
    public int CuratorBlockedServerCount { get; private set; }

    public bool isAttemptingServerQuery { get; private set; }

    public IComparer<SteamServerAdvertisement> serverInfoComparer => _serverInfoComparer;

    public event AttemptUpdated onAttemptUpdated;

    public event TimedOut onTimedOut;

    public void sortMasterServer(IComparer<SteamServerAdvertisement> newServerInfoComparer)
    {
        _serverInfoComparer = newServerInfoComparer;
        serverList.Sort(serverInfoComparer);
        onMasterServerResorted?.Invoke();
    }

    private void cleanupServerQuery()
    {
        if (!(serverQuery == HServerQuery.Invalid))
        {
            SteamMatchmakingServers.CancelServerQuery(serverQuery);
            serverQuery = HServerQuery.Invalid;
        }
    }

    private void cleanupPlayersQuery()
    {
        if (!(playersQuery == HServerQuery.Invalid))
        {
            SteamMatchmakingServers.CancelServerQuery(playersQuery);
            playersQuery = HServerQuery.Invalid;
        }
    }

    private void cleanupRulesQuery()
    {
        if (!(rulesQuery == HServerQuery.Invalid))
        {
            SteamMatchmakingServers.CancelServerQuery(rulesQuery);
            rulesQuery = HServerQuery.Invalid;
        }
    }

    private void cleanupServerListRequest()
    {
        if (!(serverListRequest == HServerListRequest.Invalid))
        {
            SteamMatchmakingServers.ReleaseRequest(serverListRequest);
            serverListRequest = HServerListRequest.Invalid;
            serverListRefreshIndex = -1;
        }
    }

    public void connect(SteamConnectionInfo info)
    {
        if (!SDG.Unturned.Provider.isConnected)
        {
            connectionInfo = info;
            serverQueryAttempts = 0;
            isAttemptingServerQuery = true;
            autoJoinServerQuery = false;
            serverQueryContext = MenuPlayServerInfoUI.EServerInfoOpenContext.CONNECT;
            attemptServerQuery();
        }
    }

    public void cancel()
    {
        if (isAttemptingServerQuery)
        {
            serverQueryAttempts = 99;
            onPingFailedToRespond();
        }
    }

    private void attemptServerQuery()
    {
        cleanupServerQuery();
        serverQuery = SteamMatchmakingServers.PingServer(connectionInfo.ip, connectionInfo.port, serverPingResponse);
        serverQueryAttempts++;
        this.onAttemptUpdated?.Invoke(serverQueryAttempts);
    }

    public void cancelRequest()
    {
        if (serverListRequest != HServerListRequest.Invalid)
        {
            SteamMatchmakingServers.CancelQuery(serverListRequest);
            cleanupServerListRequest();
        }
    }

    public void refreshMasterServer(ServerListFilters inputFilters)
    {
        _currentList = inputFilters.listSource;
        currentPluginsFilter = inputFilters.plugins;
        currentNameFilter = inputFilters.serverName;
        isCurrentNameFilterSet = !string.IsNullOrEmpty(currentNameFilter);
        curationDenyMode = ServerListCuration.Get().DenyMode;
        currentNameRegex = null;
        string text = "regex:";
        if (isCurrentNameFilterSet && currentNameFilter.StartsWith(text))
        {
            string text2 = currentNameFilter.Substring(text.Length);
            try
            {
                currentNameRegex = new Regex(text2);
            }
            catch (Exception e)
            {
                UnturnedLog.exception(e, "Caught exception parsing server name regex \"" + text2 + "\":");
                isCurrentNameFilterSet = false;
            }
        }
        currentMaxPingFilter = inputFilters.maxPing;
        _serverList.Clear();
        CuratorBlockedServerCount = 0;
        onMasterServerRemoved?.Invoke();
        cleanupServerListRequest();
        if (inputFilters.listSource == ESteamServerList.LAN)
        {
            serverListRequest = SteamMatchmakingServers.RequestLANServerList(SDG.Unturned.Provider.APP_ID, serverListResponse);
            return;
        }
        filters = new List<MatchMakingKeyValuePair_t>();
        MatchMakingKeyValuePair_t item = default(MatchMakingKeyValuePair_t);
        item.m_szKey = "gamedir";
        item.m_szValue = "unturned";
        filters.Add(item);
        if (inputFilters.mapNames != null && inputFilters.mapNames.Count > 0)
        {
            List<LevelInfo> list = new List<LevelInfo>();
            inputFilters.GetLevels(list);
            if (list.Count > 0)
            {
                if (list.Count > 1)
                {
                    int num = list.Count * 3;
                    filters.Add(new MatchMakingKeyValuePair_t
                    {
                        m_szKey = "or",
                        m_szValue = num.ToString()
                    });
                }
                foreach (LevelInfo item10 in list)
                {
                    filters.Add(new MatchMakingKeyValuePair_t
                    {
                        m_szKey = "and",
                        m_szValue = "2"
                    });
                    MatchMakingKeyValuePair_t item2 = default(MatchMakingKeyValuePair_t);
                    item2.m_szKey = "map";
                    item2.m_szValue = item10.name.ToLower();
                    filters.Add(item2);
                    MatchMakingKeyValuePair_t item3 = default(MatchMakingKeyValuePair_t);
                    item3.m_szKey = "gamedataand";
                    item3.m_szValue = "MAP_VERSION_" + VersionUtils.binaryToHexadecimal(item10.configData.PackedVersion);
                    filters.Add(item3);
                }
            }
        }
        if (inputFilters.attendance == EAttendance.Empty)
        {
            MatchMakingKeyValuePair_t item4 = default(MatchMakingKeyValuePair_t);
            item4.m_szKey = "noplayers";
            item4.m_szValue = "1";
            filters.Add(item4);
        }
        else if (inputFilters.attendance == EAttendance.HasPlayers)
        {
            MatchMakingKeyValuePair_t item5 = default(MatchMakingKeyValuePair_t);
            item5.m_szKey = "hasplayers";
            item5.m_szValue = "1";
            filters.Add(item5);
        }
        if (inputFilters.notFull)
        {
            MatchMakingKeyValuePair_t item6 = default(MatchMakingKeyValuePair_t);
            item6.m_szKey = "notfull";
            item6.m_szValue = "1";
            filters.Add(item6);
        }
        MatchMakingKeyValuePair_t item7 = default(MatchMakingKeyValuePair_t);
        item7.m_szKey = "gamedataand";
        if (inputFilters.password == EPassword.YES)
        {
            item7.m_szValue = "PASS";
        }
        else if (inputFilters.password == EPassword.NO)
        {
            item7.m_szValue = "SSAP";
        }
        if (inputFilters.vacProtection == EVACProtectionFilter.Secure)
        {
            item7.m_szValue += ",";
            item7.m_szValue += "VAC_ON";
            MatchMakingKeyValuePair_t item8 = default(MatchMakingKeyValuePair_t);
            item8.m_szKey = "secure";
            item8.m_szValue = "1";
            filters.Add(item8);
        }
        else if (inputFilters.vacProtection == EVACProtectionFilter.Insecure)
        {
            item7.m_szValue += ",";
            item7.m_szValue += "VAC_OFF";
        }
        item7.m_szValue += ",GAME_VERSION_";
        item7.m_szValue += VersionUtils.binaryToHexadecimal(SDG.Unturned.Provider.APP_VERSION_PACKED);
        if (!string.IsNullOrEmpty(item7.m_szValue))
        {
            filters.Add(item7);
        }
        MatchMakingKeyValuePair_t item9 = default(MatchMakingKeyValuePair_t);
        item9.m_szKey = "gametagsand";
        if (inputFilters.workshop == EWorkshop.YES)
        {
            item9.m_szValue = "WSy";
        }
        else if (inputFilters.workshop == EWorkshop.NO)
        {
            item9.m_szValue = "WSn";
        }
        if (inputFilters.combat == ECombat.PVP)
        {
            item9.m_szValue += ",PVP";
        }
        else if (inputFilters.combat == ECombat.PVE)
        {
            item9.m_szValue += ",PVE";
        }
        if (inputFilters.cheats == ECheats.YES)
        {
            item9.m_szValue += ",CHy";
        }
        else if (inputFilters.cheats == ECheats.NO)
        {
            item9.m_szValue += ",CHn";
        }
        if (inputFilters.camera != ECameraMode.ANY)
        {
            ref string szValue = ref item9.m_szValue;
            szValue = szValue + "," + SDG.Unturned.Provider.getCameraModeTagAbbreviation(inputFilters.camera);
        }
        if (inputFilters.monetization == EServerMonetizationTag.None)
        {
            ref string szValue2 = ref item9.m_szValue;
            szValue2 = szValue2 + "," + SDG.Unturned.Provider.GetMonetizationTagAbbreviation(inputFilters.monetization);
        }
        else
        {
            bool flag = inputFilters.monetization == EServerMonetizationTag.NonGameplay;
            if (!LiveConfig.Get().shouldServersWithoutMonetizationTagBeVisibleInInternetServerList)
            {
                flag |= isCurrentNameFilterSet;
            }
            if (flag)
            {
                filters.Add(new MatchMakingKeyValuePair_t
                {
                    m_szKey = "or",
                    m_szValue = "2"
                });
                filters.Add(new MatchMakingKeyValuePair_t
                {
                    m_szKey = "gametagsand",
                    m_szValue = SDG.Unturned.Provider.GetMonetizationTagAbbreviation(EServerMonetizationTag.None)
                });
                filters.Add(new MatchMakingKeyValuePair_t
                {
                    m_szKey = "gametagsand",
                    m_szValue = SDG.Unturned.Provider.GetMonetizationTagAbbreviation(EServerMonetizationTag.NonGameplay)
                });
            }
        }
        if (inputFilters.gold == EServerListGoldFilter.RequiresGold)
        {
            item9.m_szValue += ",GLD";
        }
        else if (inputFilters.gold == EServerListGoldFilter.DoesNotRequireGold)
        {
            item9.m_szValue += ",F2P";
        }
        if (inputFilters.battlEyeProtection == EBattlEyeProtectionFilter.Secure)
        {
            item9.m_szValue += ",BEy";
        }
        else if (inputFilters.battlEyeProtection == EBattlEyeProtectionFilter.Insecure)
        {
            item9.m_szValue += ",BEn";
        }
        if (!string.IsNullOrEmpty(item9.m_szValue))
        {
            filters.Add(item9);
        }
        StringBuilder stringBuilder = new StringBuilder(128);
        stringBuilder.Append("Refreshing server list with filters:");
        foreach (MatchMakingKeyValuePair_t filter in filters)
        {
            stringBuilder.Append(' ');
            stringBuilder.Append(filter.m_szKey);
            stringBuilder.Append('=');
            stringBuilder.Append(filter.m_szValue);
        }
        UnturnedLog.info(stringBuilder.ToString());
        if (inputFilters.listSource == ESteamServerList.HISTORY)
        {
            serverListRequest = SteamMatchmakingServers.RequestHistoryServerList(SDG.Unturned.Provider.APP_ID, filters.ToArray(), (uint)filters.Count, serverListResponse);
        }
        else if (inputFilters.listSource == ESteamServerList.FAVORITES)
        {
            serverListRequest = SteamMatchmakingServers.RequestFavoritesServerList(SDG.Unturned.Provider.APP_ID, filters.ToArray(), (uint)filters.Count, serverListResponse);
        }
        else if (inputFilters.listSource == ESteamServerList.INTERNET)
        {
            ServerListCuration serverListCuration = ServerListCuration.Get();
            serverListCuration.RefreshIfDirty();
            serverListCuration.MergeRulesIfDirty();
            serverListCuration.ResetBlockedServerCounts();
            serverListRequest = SteamMatchmakingServers.RequestInternetServerList(SDG.Unturned.Provider.APP_ID, filters.ToArray(), (uint)filters.Count, serverListResponse);
        }
        else if (inputFilters.listSource == ESteamServerList.FRIENDS)
        {
            serverListRequest = SteamMatchmakingServers.RequestFriendsServerList(SDG.Unturned.Provider.APP_ID, filters.ToArray(), (uint)filters.Count, serverListResponse);
        }
    }

    public void refreshPlayers(uint ip, ushort port)
    {
        cleanupPlayersQuery();
        playersQuery = SteamMatchmakingServers.PlayerDetails(ip, port, playersResponse);
    }

    public void refreshRules(uint ip, ushort port)
    {
        cleanupRulesQuery();
        rulesMap = new Dictionary<string, string>();
        rulesQuery = SteamMatchmakingServers.ServerRules(ip, port, rulesResponse);
    }

    private void onPingResponded(gameserveritem_t data)
    {
        isAttemptingServerQuery = false;
        cleanupServerQuery();
        if (data.m_nAppID == SDG.Unturned.Provider.APP_ID.m_AppId)
        {
            SteamServerAdvertisement steamServerAdvertisement = new SteamServerAdvertisement(data, SteamServerAdvertisement.EInfoSource.DirectConnect);
            if (!steamServerAdvertisement.isPro || SDG.Unturned.Provider.isPro)
            {
                bool flag = false;
                if (autoJoinServerQuery && (!steamServerAdvertisement.isPassworded || !string.IsNullOrEmpty(connectionInfo.password)))
                {
                    flag = true;
                }
                if (flag)
                {
                    SDG.Unturned.Provider.connect(new ServerConnectParameters(new IPv4Address(steamServerAdvertisement.ip), steamServerAdvertisement.queryPort, steamServerAdvertisement.connectionPort, connectionInfo.password), steamServerAdvertisement, null);
                }
                else
                {
                    ServerListCurationInput input = new ServerListCurationInput(steamServerAdvertisement);
                    ServerListCurationOutput output = default(ServerListCurationOutput);
                    ServerListCuration.Get().EvaluateMergedRules(in input, ref output);
                    steamServerAdvertisement.serverCurationLabels = output.labels;
                    MenuUI.closeAll();
                    MenuUI.closeAlert();
                    MenuPlayServerInfoUI.open(steamServerAdvertisement, connectionInfo.password, serverQueryContext);
                }
            }
            else
            {
                SDG.Unturned.Provider._connectionFailureInfo = ESteamConnectionFailureInfo.PRO_SERVER;
            }
        }
        else
        {
            SDG.Unturned.Provider._connectionFailureInfo = ESteamConnectionFailureInfo.TIMED_OUT;
        }
        this.onTimedOut?.Invoke();
    }

    private void onPingFailedToRespond()
    {
        if (serverQueryAttempts < 10)
        {
            attemptServerQuery();
            return;
        }
        isAttemptingServerQuery = false;
        cleanupServerQuery();
        SDG.Unturned.Provider._connectionFailureInfo = ESteamConnectionFailureInfo.TIMED_OUT;
        this.onTimedOut?.Invoke();
    }

    private void onServerListResponded(HServerListRequest request, int index)
    {
        if (request != serverListRequest)
        {
            return;
        }
        gameserveritem_t serverDetails = SteamMatchmakingServers.GetServerDetails(request, index);
        if (_currentList == ESteamServerList.INTERNET && !serverDetails.m_steamID.BPersistentGameServerAccount())
        {
            return;
        }
        if (serverDetails.m_nAppID != SDG.Unturned.Provider.APP_ID.m_AppId)
        {
            UnturnedLog.info($"Ignoring server \"{serverDetails.GetServerName()}\" because it has a different AppID ({serverDetails.m_nAppID})");
            return;
        }
        IPv4Address ip = new IPv4Address(serverDetails.m_NetAdr.GetIP());
        EHostBanFlags eHostBanFlags = HostBansManager.Get().MatchBasicDetails(ip, serverDetails.m_NetAdr.GetQueryPort(), serverDetails.GetServerName(), serverDetails.m_steamID.m_SteamID);
        if (eHostBanFlags.HasFlag(EHostBanFlags.HiddenFromAllServerLists) || eHostBanFlags.HasFlag(EHostBanFlags.Blocked) || (_currentList == ESteamServerList.INTERNET && eHostBanFlags.HasFlag(EHostBanFlags.HiddenFromInternetServerList)))
        {
            return;
        }
        SteamServerAdvertisement steamServerAdvertisement = new SteamServerAdvertisement(serverDetails, _currentList switch
        {
            ESteamServerList.FRIENDS => SteamServerAdvertisement.EInfoSource.FriendServerList, 
            ESteamServerList.FAVORITES => SteamServerAdvertisement.EInfoSource.FavoriteServerList, 
            ESteamServerList.HISTORY => SteamServerAdvertisement.EInfoSource.HistoryServerList, 
            ESteamServerList.LAN => SteamServerAdvertisement.EInfoSource.LanServerList, 
            _ => SteamServerAdvertisement.EInfoSource.InternetServerList, 
        });
        eHostBanFlags |= HostBansManager.Get().MatchExtendedDetails(steamServerAdvertisement.descText, steamServerAdvertisement.thumbnailURL);
        if (eHostBanFlags.HasFlag(EHostBanFlags.HiddenFromAllServerLists) || eHostBanFlags.HasFlag(EHostBanFlags.Blocked) || (_currentList == ESteamServerList.INTERNET && eHostBanFlags.HasFlag(EHostBanFlags.HiddenFromInternetServerList)))
        {
            return;
        }
        steamServerAdvertisement.SetServerListHostBanFlags(eHostBanFlags);
        string serverCurationLabels = null;
        if (_currentList == ESteamServerList.INTERNET)
        {
            ServerListCuration serverListCuration = ServerListCuration.Get();
            ServerListCurationInput input = new ServerListCurationInput(steamServerAdvertisement);
            ServerListCurationOutput output = default(ServerListCurationOutput);
            serverListCuration.EvaluateMergedRules(in input, ref output);
            serverCurationLabels = output.labels;
            if (!output.allowed)
            {
                int curatorBlockedServerCount = CuratorBlockedServerCount + 1;
                CuratorBlockedServerCount = curatorBlockedServerCount;
                if (output.allowOrDenyRule != null)
                {
                    output.allowOrDenyRule.latestBlockedServerCount++;
                    output.allowOrDenyRule.owner.latestBlockedServerCount++;
                }
                if (curationDenyMode == EServerListCurationDenyMode.Hide)
                {
                    return;
                }
                steamServerAdvertisement.isDeniedByServerCurationRule = true;
                steamServerAdvertisement.deniedByRule = output.allowOrDenyRule;
            }
        }
        steamServerAdvertisement.serverCurationLabels = serverCurationLabels;
        if (index == serverListRefreshIndex)
        {
            onMasterServerQueryRefreshed?.Invoke(steamServerAdvertisement);
            return;
        }
        if (currentPluginsFilter == EPlugins.NO)
        {
            if (steamServerAdvertisement.pluginFramework != 0)
            {
                return;
            }
        }
        else if (currentPluginsFilter == EPlugins.YES && steamServerAdvertisement.pluginFramework == SteamServerAdvertisement.EPluginFramework.None)
        {
            return;
        }
        if (steamServerAdvertisement.maxPlayers < CommandMaxPlayers.MIN_NUMBER)
        {
            return;
        }
        if (isCurrentNameFilterSet)
        {
            if (currentNameRegex != null)
            {
                if (!currentNameRegex.IsMatch(steamServerAdvertisement.name))
                {
                    return;
                }
            }
            else if (steamServerAdvertisement.name.IndexOf(currentNameFilter, StringComparison.OrdinalIgnoreCase) == -1)
            {
                return;
            }
        }
        else if (Mathf.Max(steamServerAdvertisement.players, steamServerAdvertisement.maxPlayers) > CommandMaxPlayers.MAX_NUMBER)
        {
            return;
        }
        if (currentMaxPingFilter <= 0 || steamServerAdvertisement.ping <= currentMaxPingFilter)
        {
            steamServerAdvertisement.CalculateUtilityScore();
            int num = serverList.BinarySearch(steamServerAdvertisement, serverInfoComparer);
            if (num < 0)
            {
                num = ~num;
            }
            serverList.Insert(num, steamServerAdvertisement);
            onMasterServerAdded?.Invoke(num, steamServerAdvertisement);
        }
    }

    private void onServerListFailedToRespond(HServerListRequest request, int index)
    {
    }

    private void onRefreshComplete(HServerListRequest request, EMatchMakingServerResponse response)
    {
        if (!(request == serverListRequest))
        {
            return;
        }
        onMasterServerRefreshed?.Invoke(response);
        if (response == EMatchMakingServerResponse.eNoServersListedOnMasterServer || serverList.Count == 0)
        {
            UnturnedLog.info("No servers found on the master server.");
            return;
        }
        switch (response)
        {
        case EMatchMakingServerResponse.eServerFailedToRespond:
            UnturnedLog.error("Failed to connect to the master server.");
            break;
        case EMatchMakingServerResponse.eServerResponded:
            UnturnedLog.info("Successfully refreshed the master server.");
            break;
        }
    }

    private void onAddPlayerToList(string name, int score, float time)
    {
        onPlayersQueryRefreshed?.Invoke(name, score, time);
    }

    private void onPlayersFailedToRespond()
    {
        UnturnedLog.warn("Server players query failed to respond");
    }

    private void onPlayersRefreshComplete()
    {
    }

    private void onRulesResponded(string key, string value)
    {
        if (rulesMap != null)
        {
            rulesMap.Add(key, value);
        }
    }

    private void onRulesFailedToRespond()
    {
        UnturnedLog.warn("Server rules query failed to respond");
    }

    private void onRulesRefreshComplete()
    {
        onRulesQueryRefreshed?.Invoke(rulesMap);
    }

    public TempSteamworksMatchmaking()
    {
        serverPingResponse = new ISteamMatchmakingPingResponse(onPingResponded, onPingFailedToRespond);
        serverListResponse = new ISteamMatchmakingServerListResponse(onServerListResponded, onServerListFailedToRespond, onRefreshComplete);
        playersResponse = new ISteamMatchmakingPlayersResponse(onAddPlayerToList, onPlayersFailedToRespond, onPlayersRefreshComplete);
        rulesResponse = new ISteamMatchmakingRulesResponse(onRulesResponded, onRulesFailedToRespond, onRulesRefreshComplete);
    }
}
