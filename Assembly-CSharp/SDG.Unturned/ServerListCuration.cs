using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Unturned.SystemEx;

namespace SDG.Unturned;

internal class ServerListCuration
{
    private int _denyMode = -1;

    /// <summary>
    /// Used to detect asset refresh.
    /// </summary>
    private int assetListChangeCounter = -1;

    /// <summary>
    /// If true, list needs to be sorted.
    /// </summary>
    private bool isListDirty;

    /// <summary>
    /// If true, MergeRules should be called before doing any filtering.
    /// </summary>
    private bool areMergedRulesDirty;

    /// <summary>
    /// If false, LoadWebUrls still needs to be called.
    /// </summary>
    private bool hasLoadedWebUrls;

    private int nextWebLinkId = -1;

    private List<ServerListCurationAsset> assets = new List<ServerListCurationAsset>();

    private List<ServerListCurationWebLink> webUrls = new List<ServerListCurationWebLink>();

    private List<ServerCurationItem> items = new List<ServerCurationItem>();

    private ServerCurationItemComparer comparer = new ServerCurationItemComparer();

    private List<ServerListCurationRule> mergedRules = new List<ServerListCurationRule>();

    private StringBuilder labelBuilder = new StringBuilder();

    internal ServerListCurationWebRequestHandler webRequestHandler;

    private static ServerListCuration instance = new ServerListCuration();

    public EServerListCurationDenyMode DenyMode
    {
        get
        {
            if (_denyMode < 0)
            {
                if (ConvenientSavedata.get().read("ServerListCurationDenyMode", out long value))
                {
                    _denyMode = Mathf.Clamp((int)value, 0, 1);
                }
                else
                {
                    _denyMode = 0;
                }
            }
            return (EServerListCurationDenyMode)_denyMode;
        }
        set
        {
            if (_denyMode != (int)value)
            {
                _denyMode = (int)value;
                ConvenientSavedata.get().write("ServerListCurationDenyMode", _denyMode);
            }
        }
    }

    public static ServerListCuration Get()
    {
        return instance;
    }

    public void RefreshIfDirty()
    {
        if (Assets.HasDefaultAssetMappingChanged(ref assetListChangeCounter))
        {
            MarkDirty();
            assets.Clear();
            Assets.FindAssetsByType_UseDefaultAssetMapping(assets);
        }
        if (!hasLoadedWebUrls)
        {
            hasLoadedWebUrls = true;
            LoadWebUrls();
            MarkDirty();
        }
        if (!isListDirty)
        {
            return;
        }
        UpdateOrRemoveExistingAssetItems();
        AddItemsForNewAssets();
        int num = 0;
        bool flag = false;
        IConvenientSavedata convenientSavedata = ConvenientSavedata.get();
        if (convenientSavedata.read("ServerCurationItems", out long value))
        {
            for (int i = 0; i < value; i++)
            {
                string key = $"ServerCurationItem_{i}";
                if (!convenientSavedata.read(key, out string value2) || string.IsNullOrEmpty(value2))
                {
                    continue;
                }
                if (value2.StartsWith("Asset:"))
                {
                    string text = value2.Substring(6);
                    if (Guid.TryParse(text, out var result))
                    {
                        ServerCurationItem serverCurationItem = FindItemByAssetGuid(result);
                        if (serverCurationItem != null)
                        {
                            flag |= serverCurationItem.SortOrder != num;
                            serverCurationItem.SortOrder = num;
                            num++;
                        }
                        else
                        {
                            UnturnedLog.warn("Missing asset for server list curation item: " + text);
                        }
                    }
                    else
                    {
                        UnturnedLog.warn("Failed to parse server list curation item \"" + value2 + "\"");
                    }
                }
                else
                {
                    if (!value2.StartsWith("Web:"))
                    {
                        continue;
                    }
                    if (int.TryParse(value2.Substring(4), out var result2))
                    {
                        ServerCurationItem_Web serverCurationItem_Web = FindWebItemByLinkId(result2);
                        if (serverCurationItem_Web != null)
                        {
                            flag |= serverCurationItem_Web.SortOrder != num;
                            serverCurationItem_Web.SortOrder = num;
                            num++;
                        }
                        else
                        {
                            UnturnedLog.warn($"Missing web link for server list curation item: {result2}");
                        }
                    }
                    else
                    {
                        UnturnedLog.warn("Failed to parse server list curation item \"" + value2 + "\"");
                    }
                }
            }
        }
        foreach (ServerCurationItem item in items)
        {
            if (item.SortOrder < 0)
            {
                flag = true;
                item.SortOrder = num;
                num++;
            }
        }
        items.Sort(comparer);
        if (flag)
        {
            SaveOrdering();
        }
    }

    /// <summary>
    /// Called earlier during startup to try and have web lists ready by the time server browser is opened.
    /// </summary>
    public void StartupLoadWebUrls()
    {
        if (!hasLoadedWebUrls)
        {
            hasLoadedWebUrls = true;
            LoadWebUrls();
            MarkDirty();
        }
    }

    public void MergeRulesIfDirty()
    {
        if (areMergedRulesDirty)
        {
            areMergedRulesDirty = false;
            MergeRules();
        }
    }

    public List<ServerCurationItem> GetItems()
    {
        return items;
    }

    public void MarkDirty()
    {
        isListDirty = true;
        areMergedRulesDirty = true;
    }

    public void AddUrl(string url)
    {
        foreach (ServerListCurationWebLink webUrl in webUrls)
        {
            if (webUrl.url == url)
            {
                return;
            }
        }
        MarkDirty();
        ServerListCurationWebLink serverListCurationWebLink = default(ServerListCurationWebLink);
        serverListCurationWebLink.id = GetIdForNewWebLink();
        serverListCurationWebLink.url = url;
        ServerListCurationWebLink serverListCurationWebLink2 = serverListCurationWebLink;
        webUrls.Add(serverListCurationWebLink2);
        ServerCurationItem_Web item = new ServerCurationItem_Web(this, serverListCurationWebLink2);
        items.Add(item);
        SaveWebUrls();
    }

    internal void RemoveUrl(ServerCurationItem_Web webItem)
    {
        bool flag = false;
        for (int num = webUrls.Count - 1; num >= 0; num--)
        {
            if (webUrls[num].id == webItem.linkId)
            {
                webUrls.RemoveAtFast(num);
                flag = true;
                break;
            }
        }
        items.Remove(webItem);
        if (flag)
        {
            MarkDirty();
            SaveWebUrls();
            SaveOrdering();
        }
    }

    public void MoveItem(ServerCurationItem item, int direction)
    {
        int num = items.IndexOf(item);
        if (num < 0)
        {
            UnturnedLog.error("Attempted to move curated server item that isn't in list (bug?)");
        }
        else if (direction < 0)
        {
            if (num > 0)
            {
                ServerCurationItem serverCurationItem = items[num - 1];
                int sortOrder = serverCurationItem.SortOrder;
                items[num] = serverCurationItem;
                serverCurationItem.SortOrder = item.SortOrder;
                items[num - 1] = item;
                item.SortOrder = sortOrder;
                MarkDirty();
                SaveOrdering();
            }
        }
        else if (direction > 0 && num < items.Count - 1)
        {
            ServerCurationItem serverCurationItem2 = items[num + 1];
            int sortOrder2 = serverCurationItem2.SortOrder;
            items[num] = serverCurationItem2;
            serverCurationItem2.SortOrder = item.SortOrder;
            items[num + 1] = item;
            item.SortOrder = sortOrder2;
            MarkDirty();
            SaveOrdering();
        }
    }

    public bool DoesInputMatchRule(in ServerListCurationInput input, ServerListCurationRule rule)
    {
        bool flag = false;
        switch (rule.ruleType)
        {
        case EServerListCurationRuleType.Name:
            if (input.hasName)
            {
                int num = 0;
                do
                {
                    flag = rule.regexes[num].IsMatch(input.name);
                    num++;
                }
                while (num < rule.regexes.Length && !flag);
            }
            break;
        case EServerListCurationRuleType.IPv4:
            if (input.hasAddress)
            {
                int num2 = 0;
                do
                {
                    IPv4Filter pv4Filter = rule.ipv4Filters[num2];
                    flag = pv4Filter.Matches(input.address, input.queryPort);
                    num2++;
                }
                while (num2 < rule.ipv4Filters.Length && !flag);
            }
            break;
        case EServerListCurationRuleType.ServerID:
            if (input.hasSteamId)
            {
                flag = Array.IndexOf(rule.steamIds, input.steamId) >= 0;
            }
            break;
        }
        if (rule.inverted)
        {
            flag = !flag;
        }
        return flag;
    }

    public void EvaluateMergedRules(in ServerListCurationInput input, ref ServerListCurationOutput output)
    {
        Evaluate(mergedRules, in input, ref output);
    }

    public void Evaluate(List<ServerListCurationRule> rules, in ServerListCurationInput input, ref ServerListCurationOutput output)
    {
        output.allowed = true;
        output.matchedAnyRules = false;
        output.allowOrDenyRule = null;
        if (output.matchedRules != null)
        {
            output.matchedRules.Clear();
        }
        output.labels = null;
        labelBuilder.Clear();
        foreach (ServerListCurationRule rule in rules)
        {
            if (!DoesInputMatchRule(in input, rule))
            {
                continue;
            }
            output.matchedAnyRules = true;
            if (output.matchedRules != null)
            {
                output.matchedRules.Add(rule);
            }
            if (!string.IsNullOrEmpty(rule.label))
            {
                if (labelBuilder.Length > 0)
                {
                    labelBuilder.Append(' ');
                }
                labelBuilder.Append(rule.label);
            }
            bool flag = false;
            switch (rule.action)
            {
            case EServerListCurationAction.Allow:
                flag = true;
                output.allowOrDenyRule = rule;
                break;
            case EServerListCurationAction.Deny:
                output.allowed = false;
                output.allowOrDenyRule = rule;
                flag = true;
                break;
            }
            if (flag)
            {
                break;
            }
        }
        if (labelBuilder.Length > 0)
        {
            output.labels = labelBuilder.ToString();
        }
    }

    private ServerCurationItem_Asset FindItemByAssetGuid(Guid guid)
    {
        foreach (ServerCurationItem item in items)
        {
            if (item is ServerCurationItem_Asset serverCurationItem_Asset && serverCurationItem_Asset.asset.GUID == guid)
            {
                return serverCurationItem_Asset;
            }
        }
        return null;
    }

    private ServerCurationItem_Web FindWebItemByLinkId(int id)
    {
        foreach (ServerCurationItem item in items)
        {
            if (item is ServerCurationItem_Web serverCurationItem_Web && serverCurationItem_Web.linkId == id)
            {
                return serverCurationItem_Web;
            }
        }
        return null;
    }

    private void SaveOrdering()
    {
        int num = 0;
        using (List<ServerCurationItem>.Enumerator enumerator = items.GetEnumerator())
        {
            while (enumerator.MoveNext() && enumerator.Current.SortOrder >= 0)
            {
                num++;
            }
        }
        IConvenientSavedata convenientSavedata = ConvenientSavedata.get();
        convenientSavedata.write("ServerCurationItems", num);
        for (int i = 0; i < num; i++)
        {
            string key = $"ServerCurationItem_{i}";
            string value = string.Empty;
            ServerCurationItem serverCurationItem = items[i];
            if (serverCurationItem is ServerCurationItem_Asset { asset: not null } serverCurationItem_Asset)
            {
                value = $"Asset:{serverCurationItem_Asset.asset.GUID:N}";
            }
            else if (serverCurationItem is ServerCurationItem_Web serverCurationItem_Web)
            {
                value = $"Web:{serverCurationItem_Web.linkId}";
            }
            convenientSavedata.write(key, value);
        }
    }

    private int GetIdForNewWebLink()
    {
        if (nextWebLinkId < 0)
        {
            if (ConvenientSavedata.get().read("ServerCurationNextWebLinkId", out long value))
            {
                nextWebLinkId = (int)value;
            }
        }
        else
        {
            nextWebLinkId = 1;
        }
        int result = nextWebLinkId;
        nextWebLinkId++;
        ConvenientSavedata.get().write("ServerCurationNextWebLinkId", nextWebLinkId);
        return result;
    }

    private void LoadWebUrls()
    {
        IConvenientSavedata convenientSavedata = ConvenientSavedata.get();
        if (!convenientSavedata.read("ServerCurationWebLinks", out long value))
        {
            return;
        }
        for (int i = 0; i < value; i++)
        {
            string key = $"ServerCurationWebId_{i}";
            string key2 = $"ServerCurationWebUrl_{i}";
            if (convenientSavedata.read(key, out long value2) && convenientSavedata.read(key2, out string value3) && !string.IsNullOrWhiteSpace(value3))
            {
                ServerListCurationWebLink serverListCurationWebLink = default(ServerListCurationWebLink);
                serverListCurationWebLink.id = (int)value2;
                serverListCurationWebLink.url = value3;
                ServerListCurationWebLink serverListCurationWebLink2 = serverListCurationWebLink;
                webUrls.Add(serverListCurationWebLink2);
                ServerCurationItem_Web item = new ServerCurationItem_Web(this, serverListCurationWebLink2);
                items.Add(item);
            }
        }
    }

    private void SaveWebUrls()
    {
        IConvenientSavedata convenientSavedata = ConvenientSavedata.get();
        convenientSavedata.write("ServerCurationWebLinks", webUrls.Count);
        for (int i = 0; i < webUrls.Count; i++)
        {
            ServerListCurationWebLink serverListCurationWebLink = webUrls[i];
            string key = $"ServerCurationWebId_{i}";
            convenientSavedata.write(key, serverListCurationWebLink.id);
            string key2 = $"ServerCurationWebUrl_{i}";
            convenientSavedata.write(key2, serverListCurationWebLink.url);
        }
    }

    private void MergeRules()
    {
        mergedRules.Clear();
        foreach (ServerCurationItem item in items)
        {
            if (item.IsActive)
            {
                List<ServerListCurationRule> rules = item.GetRules();
                if (rules != null && rules.Count > 0)
                {
                    mergedRules.AddRange(rules);
                }
            }
        }
    }

    private void UpdateOrRemoveExistingAssetItems()
    {
        for (int num = items.Count - 1; num >= 0; num--)
        {
            if (items[num] is ServerCurationItem_Asset serverCurationItem_Asset)
            {
                ServerListCurationAsset serverListCurationAsset = null;
                foreach (ServerListCurationAsset asset in assets)
                {
                    if (asset.GUID == serverCurationItem_Asset.asset.GUID)
                    {
                        serverListCurationAsset = asset;
                        break;
                    }
                }
                if (serverListCurationAsset != null)
                {
                    serverCurationItem_Asset.NotifyAssetChanged(serverListCurationAsset);
                }
                else
                {
                    items.RemoveAtFast(num);
                }
            }
        }
    }

    private void AddItemsForNewAssets()
    {
        foreach (ServerListCurationAsset asset in assets)
        {
            ServerCurationItem_Asset serverCurationItem_Asset = FindItemByAssetGuid(asset.GUID);
            if (serverCurationItem_Asset == null)
            {
                serverCurationItem_Asset = new ServerCurationItem_Asset(this, asset);
                items.Add(serverCurationItem_Asset);
            }
        }
    }

    private ServerListCuration()
    {
        GameObject gameObject = new GameObject("ServerListCuration");
        UnityEngine.Object.DontDestroyOnLoad(gameObject);
        gameObject.hideFlags = HideFlags.HideAndDontSave;
        webRequestHandler = gameObject.AddComponent<ServerListCurationWebRequestHandler>();
    }
}
