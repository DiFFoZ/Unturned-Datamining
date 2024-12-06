using System;
using System.Collections.Generic;
using System.IO;
using SDG.Framework.Utilities;
using SDG.Provider;
using Steamworks;

namespace SDG.Unturned;

public static class DedicatedUGC
{
    /// <summary>
    /// Request for details about the pending items.
    /// </summary>
    private static UGCQueryHandle_t queryHandle;

    /// <summary>
    /// File IDs of all the items we have enqueued for query.
    /// </summary>
    private static HashSet<ulong> itemsQueried;

    /// <summary>
    /// Built from user-specified workshop item IDs, and then expanded as the query results
    /// arrive with details about any dependent or child items.
    /// </summary>
    private static Queue<PublishedFileId_t> itemsToQuery;

    /// <summary>
    /// File IDs requested by the latest query submitted.
    /// </summary>
    private static PublishedFileId_t[] itemsPendingQuery;

    /// <summary>
    /// Number of times we've tried re-submitted failed queries.
    /// </summary>
    private static uint queryRetryCount;

    /// <summary>
    /// Built as the valid list of items arrive.
    /// </summary>
    private static Queue<PublishedFileId_t> itemsToDownload;

    /// <summary>
    /// ID of the latest item we requested for download so that we can test if the callback is for us.
    /// </summary>
    private static PublishedFileId_t currentDownload;

    private static CallResult<SteamUGCQueryCompleted_t> queryCompleted;

    private static Callback<DownloadItemResult_t> itemDownloaded;

    private static bool linkedSpawns;

    private static bool initializedValidation;

    public static List<SteamContent> ugc { get; private set; }

    private static uint maxQueryRetries => WorkshopDownloadConfig.get().Max_Query_Retries;

    /// <summary>
    /// Broadcasts once all workshop assets are finished installing.
    /// </summary>
    public static event DedicatedUGCInstalledHandler installed;

    public static void registerItemInstallation(ulong id)
    {
        enqueueItemToQuery(new PublishedFileId_t(id));
    }

    /// <summary>
    /// Called once the server is done registering items it wants to install.
    /// </summary>
    /// <param name="onlyFromCache">True when running in offline-only mode.</param>
    public static void beginInstallingItems(bool onlyFromCache)
    {
        CommandWindow.Log(itemsToQuery.Count + " workshop item(s) requested");
        if (itemsToQuery.Count == 0)
        {
            OnFinishedDownloadingItems();
            return;
        }
        Assets.loadingStats.Reset();
        if (onlyFromCache)
        {
            installItemsToQueryFromCache();
        }
        else
        {
            submitQuery();
        }
    }

    /// <summary>
    /// Enqueue an item if we have not queried it yet. This guards against querying an item
    /// that is in two separate collections leading to duplicates.
    /// </summary>
    private static bool enqueueItemToQuery(PublishedFileId_t item)
    {
        if (itemsQueried.Contains(item.m_PublishedFileId))
        {
            return false;
        }
        itemsToQuery.Enqueue(item);
        itemsQueried.Add(item.m_PublishedFileId);
        return true;
    }

    private static void enqueueItemToDownload(PublishedFileId_t item)
    {
        if (itemsToDownload.Contains(item))
        {
            UnturnedLog.warn("Tried to enqueue {0} for download more than once", item);
        }
        else
        {
            itemsToDownload.Enqueue(item);
        }
    }

    /// <returns>True if item was installed from cache.</returns>
    private static bool installFromCache(PublishedFileId_t fileId)
    {
        if (SteamGameServerUGC.GetItemInstallInfo(fileId, out var _, out var pchFolder, 1024u, out var punTimeStamp) && ReadWrite.folderExists(pchFolder, usePath: false))
        {
            if ((SteamGameServerUGC.GetItemState(fileId) & 8) == 8)
            {
                CommandWindow.LogFormat("Workshop item {0} found in cache, but was flagged as needing update", fileId);
            }
            else
            {
                if (!TempSteamworksWorkshop.getCachedDetails(fileId, out var cachedDetails) || cachedDetails.updateTimestamp <= punTimeStamp)
                {
                    PublishedFileId_t publishedFileId_t = fileId;
                    CommandWindow.Log("Workshop item found in cache: " + publishedFileId_t.ToString());
                    installDownloadedItem(fileId, pchFolder);
                    return true;
                }
                CommandWindow.LogFormat("Workshop item {0} found in cache, but remote ({1}) is newer than local ({2})", fileId, DateTimeEx.FromUtcUnixTimeSeconds(cachedDetails.updateTimestamp).ToLocalTime(), DateTimeEx.FromUtcUnixTimeSeconds(punTimeStamp).ToLocalTime());
            }
        }
        return false;
    }

    private static void installNextItem()
    {
        if (itemsToDownload.Count == 0)
        {
            OnFinishedDownloadingItems();
            return;
        }
        PublishedFileId_t fileId = itemsToDownload.Dequeue();
        bool flag = false;
        if (WorkshopDownloadConfig.get().Use_Cached_Downloads)
        {
            flag = installFromCache(fileId);
        }
        if (flag)
        {
            installNextItem();
            return;
        }
        currentDownload = fileId;
        PublishedFileId_t publishedFileId_t = currentDownload;
        CommandWindow.Log("Downloading workshop item: " + publishedFileId_t.ToString());
        if (!SteamGameServerUGC.DownloadItem(currentDownload, bHighPriority: true))
        {
            CommandWindow.Log("Unable to download item!");
            installNextItem();
        }
    }

    /// <summary>
    /// Used in offline-only mode.
    /// </summary>
    private static void installItemsToQueryFromCache()
    {
        CommandWindow.Log("Only installing cached workshop files (no query / download)");
        while (itemsToQuery.Count > 0)
        {
            PublishedFileId_t publishedFileId_t = itemsToQuery.Dequeue();
            if (!installFromCache(publishedFileId_t))
            {
                CommandWindow.LogFormat("Unable to find workshop item in cache: {0}", publishedFileId_t);
            }
        }
        OnFinishedDownloadingItems();
    }

    /// <summary>
    /// Prepare a query that will request metadata for all the workshop items we want to install.
    /// This allows us to check if the items are allowed to be auto-downloaded to this server, and to
    /// detect any child or dependent items.
    ///
    /// Waits for onQueryCompleted.
    /// </summary>
    private static void submitQuery()
    {
        CommandWindow.Log("Submitting workshop query for " + itemsToQuery.Count + " item(s)...");
        itemsPendingQuery = itemsToQuery.ToArray();
        itemsToQuery.Clear();
        submitQueryHelper(itemsPendingQuery);
    }

    /// <summary>
    /// Re-submit previous query after a query failure.
    /// </summary>
    private static void resubmitQuery()
    {
        queryRetryCount++;
        CommandWindow.LogFormat("Re-submitting ({0} of {1}) workshop query for {2} item(s)...", queryRetryCount, maxQueryRetries, itemsPendingQuery.Length);
        submitQueryHelper(itemsPendingQuery);
    }

    private static void submitQueryHelper(PublishedFileId_t[] fileIDs)
    {
        queryHandle = SteamGameServerUGC.CreateQueryUGCDetailsRequest(fileIDs, (uint)fileIDs.Length);
        SteamGameServerUGC.SetReturnKeyValueTags(queryHandle, bReturnKeyValueTags: true);
        SteamGameServerUGC.SetReturnChildren(queryHandle, bReturnChildren: true);
        uint query_Cache_Max_Age_Seconds = WorkshopDownloadConfig.get().Query_Cache_Max_Age_Seconds;
        if (query_Cache_Max_Age_Seconds != 0)
        {
            SteamGameServerUGC.SetAllowCachedResponse(queryHandle, query_Cache_Max_Age_Seconds);
        }
        SteamAPICall_t hAPICall = SteamGameServerUGC.SendQueryUGCRequest(queryHandle);
        queryCompleted.Set(hAPICall);
    }

    private static bool testDownloadRestrictions(UGCQueryHandle_t queryHandle, uint resultIndex, uint ip, string itemDisplayText)
    {
        EWorkshopDownloadRestrictionResult restrictionResult = WorkshopDownloadRestrictions.getRestrictionResult(queryHandle, resultIndex, ip);
        switch (restrictionResult)
        {
        case EWorkshopDownloadRestrictionResult.NoRestrictions:
            return true;
        case EWorkshopDownloadRestrictionResult.NotWhitelisted:
            CommandWindow.LogWarning("Not authorized in the IP whitelist for " + itemDisplayText);
            return false;
        case EWorkshopDownloadRestrictionResult.Blacklisted:
            CommandWindow.LogWarning("Blocked in IP blacklist from downloading " + itemDisplayText);
            return false;
        case EWorkshopDownloadRestrictionResult.Allowed:
            CommandWindow.Log("Authorized to download " + itemDisplayText);
            return true;
        case EWorkshopDownloadRestrictionResult.Banned:
            CommandWindow.LogWarning("Workshop file is banned " + itemDisplayText);
            return false;
        case EWorkshopDownloadRestrictionResult.PrivateVisibility:
            CommandWindow.LogWarning("Workshop file is private " + itemDisplayText);
            return false;
        default:
            CommandWindow.LogWarningFormat("Unknown restriction result '{0}' for '{1}'", restrictionResult, itemDisplayText);
            return false;
        }
    }

    private static void OnNextFrameResubmitQuery()
    {
        TimeUtility.updated -= OnNextFrameResubmitQuery;
        resubmitQuery();
    }

    private static void OnNextFrameSubmitQuery()
    {
        TimeUtility.updated -= OnNextFrameSubmitQuery;
        submitQuery();
    }

    private static void onQueryCompleted(SteamUGCQueryCompleted_t callback, bool ioFailure)
    {
        if (callback.m_handle != queryHandle)
        {
            return;
        }
        bool flag;
        if (!ioFailure)
        {
            if (callback.m_eResult == EResult.k_EResultOK)
            {
                flag = false;
                CommandWindow.Log("Workshop query yielded " + callback.m_unNumResultsReturned + " result(s)");
                SteamGameServer.GetPublicIP().TryGetIPv4Address(out var address);
                string text = Parser.getIPFromUInt32(address);
                if (Logs.ShouldRedactLogs)
                {
                    text = Logs.RedactionReplacement;
                }
                CommandWindow.Log("This server's allowed IP for Workshop downloads: " + text);
                for (uint num = 0u; num < callback.m_unNumResultsReturned; num++)
                {
                    if (!SteamGameServerUGC.GetQueryUGCResult(queryHandle, num, out var pDetails))
                    {
                        CommandWindow.LogWarning($"Workshop query unable to get details for result index {num}");
                        continue;
                    }
                    PublishedFileId_t nPublishedFileId = pDetails.m_nPublishedFileId;
                    string text2 = nPublishedFileId.ToString() + " '" + pDetails.m_rgchTitle + "'";
                    if (pDetails.m_eResult != EResult.k_EResultOK)
                    {
                        CommandWindow.LogWarning($"Error {pDetails.m_eResult} querying workshop file {text2}");
                    }
                    else
                    {
                        if (!testDownloadRestrictions(queryHandle, num, address, text2))
                        {
                            continue;
                        }
                        TempSteamworksWorkshop.cacheDetails(queryHandle, num, out var _);
                        if (pDetails.m_eFileType != EWorkshopFileType.k_EWorkshopFileTypeCollection)
                        {
                            CommandWindow.Log(text2 + " queued for download");
                            enqueueItemToDownload(pDetails.m_nPublishedFileId);
                        }
                        uint unNumChildren = pDetails.m_unNumChildren;
                        if (unNumChildren == 0)
                        {
                            continue;
                        }
                        if (WorkshopDownloadConfig.get().Ignore_Children_File_IDs.Contains(pDetails.m_nPublishedFileId.m_PublishedFileId))
                        {
                            CommandWindow.LogFormat("Ignoring {0} children of {1}", unNumChildren, text2);
                            continue;
                        }
                        CommandWindow.Log(text2 + " has " + unNumChildren + " children");
                        PublishedFileId_t[] array = new PublishedFileId_t[unNumChildren];
                        if (SteamGameServerUGC.GetQueryUGCChildren(queryHandle, num, array, unNumChildren))
                        {
                            PublishedFileId_t[] array2 = array;
                            foreach (PublishedFileId_t publishedFileId_t in array2)
                            {
                                CommandWindow.LogFormat(enqueueItemToQuery(publishedFileId_t) ? "\t{0}" : "\t{0} (already queued)", publishedFileId_t);
                            }
                        }
                    }
                }
            }
            else
            {
                flag = true;
                CommandWindow.LogError("Encountered an error when querying workshop: " + callback.m_eResult);
            }
        }
        else
        {
            flag = true;
            CommandWindow.LogError("Encountered an IO error when querying workshop!");
        }
        SteamGameServerUGC.ReleaseQueryUGCRequest(queryHandle);
        queryHandle = UGCQueryHandle_t.Invalid;
        if (flag)
        {
            if (queryRetryCount < maxQueryRetries)
            {
                TimeUtility.updated += OnNextFrameResubmitQuery;
                return;
            }
            CommandWindow.LogWarning("Reached maximum workshop query retry count!");
            Provider.QuitGame("reached maximum workshop query retry count");
        }
        else if (itemsToQuery.Count > 0)
        {
            TimeUtility.updated += OnNextFrameSubmitQuery;
        }
        else
        {
            CommandWindow.Log(itemsToDownload.Count + " workshop item(s) to download...");
            installNextItem();
        }
    }

    private static void installDownloadedItem(PublishedFileId_t fileId, string path)
    {
        if (WorkshopTool.detectUGCMetaType(path, usePath: false, out var outType))
        {
            CommandWindow.LogFormat("Installing workshop item: {0}", fileId);
            if (!TempSteamworksWorkshop.isCompatible(fileId, outType, path, out var explanation))
            {
                CommandWindow.LogWarning(explanation);
            }
            if (TempSteamworksWorkshop.shouldIgnoreFile(fileId, out var _))
            {
                CommandWindow.LogFormat("Ignoring downloaded workshop item {0} because '{1}'");
            }
            else
            {
                ugc.Add(new SteamContent(fileId, path, outType));
                switch (outType)
                {
                case ESteamUGCType.MAP:
                    WorkshopTool.loadMapBundlesAndContent(path, fileId.m_PublishedFileId);
                    break;
                default:
                    Assets.RequestAddSearchLocation(path, TempSteamworksWorkshop.FindOrAddOrigin(fileId.m_PublishedFileId));
                    break;
                case ESteamUGCType.LOCALIZATION:
                    break;
                }
                CommandWindow.LogFormat("Installed workshop item: {0}", fileId);
            }
            uint timestamp = 0u;
            if (TempSteamworksWorkshop.getCachedDetails(fileId, out var cachedDetails))
            {
                timestamp = cachedDetails.updateTimestamp;
            }
            Provider.registerServerUsingWorkshopFileId(fileId.m_PublishedFileId, timestamp);
        }
        else
        {
            PublishedFileId_t publishedFileId_t = fileId;
            CommandWindow.LogWarning("Unable to determine UGC type for downloaded item: " + publishedFileId_t.ToString());
        }
    }

    private static void onItemDownloaded(DownloadItemResult_t callback)
    {
        if (callback.m_nPublishedFileId != currentDownload)
        {
            return;
        }
        if (callback.m_eResult == EResult.k_EResultOK)
        {
            CommandWindow.Log("Successfully downloaded workshop item: " + callback.m_nPublishedFileId.m_PublishedFileId);
            if (SteamGameServerUGC.GetItemInstallInfo(callback.m_nPublishedFileId, out var _, out var pchFolder, 1024u, out var _))
            {
                if (ReadWrite.folderExists(pchFolder, usePath: false))
                {
                    installDownloadedItem(callback.m_nPublishedFileId, pchFolder);
                }
                else
                {
                    CommandWindow.LogWarningFormat("Finished downloading workshop item {0}, but unable to find the files on disk ({1})", callback.m_nPublishedFileId, pchFolder);
                }
            }
            else
            {
                CommandWindow.LogWarningFormat("Finished downloading workshop item {0}, but unable to get install info", callback.m_nPublishedFileId);
            }
        }
        else
        {
            CommandWindow.LogWarningFormat("Download workshop item {0} failed, result: {1}", callback.m_nPublishedFileId, callback.m_eResult);
        }
        installNextItem();
    }

    public static void initialize()
    {
        if (!Dedicator.IsDedicatedServer)
        {
            throw new NotSupportedException("DedicatedUGC should only be used on dedicated server!");
        }
        ugc = new List<SteamContent>();
        itemsQueried = new HashSet<ulong>();
        itemsToQuery = new Queue<PublishedFileId_t>();
        itemsToDownload = new Queue<PublishedFileId_t>();
        queryCompleted = CallResult<SteamUGCQueryCompleted_t>.Create(onQueryCompleted);
        itemDownloaded = Callback<DownloadItemResult_t>.CreateGameServer(onItemDownloaded);
        string text = ReadWrite.PATH + ServerSavedata.directory + "/" + Provider.serverID + "/Workshop/Steam";
        if (!Directory.Exists(text))
        {
            Directory.CreateDirectory(text);
        }
        CommandWindow.Log("Workshop install folder: " + text);
        SteamGameServerUGC.BInitWorkshopForGameServer((DepotId_t)Provider.APP_ID.m_AppId, text);
    }

    private static void OnFinishedDownloadingItems()
    {
        if (Assets.ShouldWaitForNewAssetsToFinishLoading)
        {
            UnturnedLog.info("Server UGC waiting for assets to finish loading...");
            Assets.OnNewAssetsFinishedLoading = (System.Action)Delegate.Combine(Assets.OnNewAssetsFinishedLoading, new System.Action(OnNewAssetsFinishedLoading));
        }
        else
        {
            OnNewAssetsFinishedLoading();
        }
    }

    private static void OnNewAssetsFinishedLoading()
    {
        Assets.OnNewAssetsFinishedLoading = (System.Action)Delegate.Remove(Assets.OnNewAssetsFinishedLoading, new System.Action(OnNewAssetsFinishedLoading));
        if (!linkedSpawns)
        {
            linkedSpawns = true;
            Assets.linkSpawns();
        }
        if (!initializedValidation)
        {
            initializedValidation = true;
            Assets.initializeMasterBundleValidation();
        }
        DedicatedUGC.installed?.Invoke();
    }
}
