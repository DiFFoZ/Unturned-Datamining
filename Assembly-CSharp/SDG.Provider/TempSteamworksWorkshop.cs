using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using SDG.SteamworksProvider;
using SDG.Unturned;
using Steamworks;
using UnityEngine;

namespace SDG.Provider;

public class TempSteamworksWorkshop
{
    public delegate void PublishedAdded();

    public delegate void PublishedRemoved();

    /// <summary>
    /// Used when transitioning Unity versions breaks asset bundles. Replaced by AssetBundleVersion const values.
    /// </summary>
    [Obsolete]
    public static readonly byte COMPATIBILITY_VERSION = 3;

    /// <summary>
    /// Workshop item key-value tag storing the version number.
    /// </summary>
    public static readonly string COMPATIBILITY_VERSION_KVTAG = "compatibility_version";

    private SteamworksAppInfo appInfo;

    public PublishedAdded onPublishedAdded;

    public PublishedRemoved onPublishedRemoved;

    private PublishedFileId_t publishedFileID;

    private UGCQueryHandle_t ugcRequest;

    private uint ugcRequestPage;

    private bool shouldRequestAnotherPage;

    private string ugcName;

    private string ugcDescription;

    private string ugcPath;

    private string ugcPreview;

    private string ugcChange;

    private ESteamUGCType ugcType;

    private List<string> ugcTags;

    private string ugcAllowedIPs;

    private ESteamUGCVisibility ugcVisibility;

    private bool ugcVerified;

    public int totalNumberOfFilesToDownload;

    private float progressPerFileDownloaded;

    public List<PublishedFileId_t> downloaded;

    public List<PublishedFileId_t> installing;

    private List<SteamContent> _ugc;

    private List<SteamPublished> _published;

    /// <summary>
    /// Maps published file id to name, version, etc.
    /// </summary>
    private static Dictionary<ulong, CachedUGCDetails> cachedUGCDetails = new Dictionary<ulong, CachedUGCDetails>();

    private static readonly PublishedFileId_t FRANCE = new PublishedFileId_t(1975500516uL);

    private CallResult<CreateItemResult_t> createItemResult;

    private CallResult<SubmitItemUpdateResult_t> submitItemUpdateResult;

    private CallResult<SteamUGCQueryCompleted_t> queryCompleted;

    private float currentlyDownloadingFileEstimatedProgress;

    private int previousEstimatedDownloadProgress;

    private PublishedFileId_t currentlyDownloadingFileId;

    private UGCQueryHandle_t serverItemsQueryHandle;

    private CallResult<SteamUGCQueryCompleted_t> serverItemsQueryCompleted;

    /// <summary>
    /// File IDs the client knows the server is using. Fallback in-case the query fails.
    /// </summary>
    internal List<PublishedFileId_t> serverPendingIDs;

    /// <summary>
    /// IP of the currently connected server, or zero if unable to retrieve from network system.
    /// Used for testing download restrictions.
    /// </summary>
    protected uint serverDownloadIP;

    private Callback<DownloadItemResult_t> itemDownloaded;

    /// <summary>
    /// Callback when player subscribes to an item and it finishes downloading.
    /// Different than the game-managed DownloadItem calls.
    /// </summary>
    private Callback<ItemInstalled_t> itemInstalled;

    /// <summary>
    /// Workshop file ids we were locally subscribed to during startup.
    /// These items are queried for compatibility before registering.
    /// </summary>
    private PublishedFileId_t[] locallySubscribedFileIds;

    private UGCQueryHandle_t subscribedQueryHandle;

    private CallResult<SteamUGCQueryCompleted_t> subscribedQueryCompleted;

    /// <summary>
    /// If specified, player's workshop file subscriptions are not registered at startup.
    /// </summary>
    private static CommandLineFlag shouldIgnoreSubscribedItems = new CommandLineFlag(defaultValue: false, "-NoWorkshopSubscriptions");

    /// <summary>
    /// Map of subscriptions added/removed by the player through the in-game client API, as opposed to the web browser.
    /// </summary>
    private Dictionary<PublishedFileId_t, bool> ingameSubscriptions = new Dictionary<PublishedFileId_t, bool>();

    public bool canOpenWorkshop => true;

    public List<SteamContent> ugc => _ugc;

    public List<SteamPublished> published => _published;

    /// <summary>
    /// Number of items currently connected server was not authorized to download.
    /// </summary>
    public int serverInvalidItemsCount { get; protected set; }

    public void open(PublishedFileId_t id)
    {
        SteamFriends.ActivateGameOverlayToWebPage("http://steamcommunity.com/sharedfiles/filedetails/?id=" + id.m_PublishedFileId);
    }

    /// <summary>
    /// Get compatibility version from workshop query, or zero if unset.
    /// </summary>
    public static byte getCompatibilityVersion(UGCQueryHandle_t queryHandle, uint index)
    {
        uint num = (Dedicator.IsDedicatedServer ? SteamGameServerUGC.GetQueryUGCNumKeyValueTags(queryHandle, index) : SteamUGC.GetQueryUGCNumKeyValueTags(queryHandle, index));
        for (uint num2 = 0u; num2 < num; num2++)
        {
            if ((Dedicator.IsDedicatedServer ? SteamGameServerUGC.GetQueryUGCKeyValueTag(queryHandle, index, num2, out var pchKey, 255u, out var pchValue, 255u) : SteamUGC.GetQueryUGCKeyValueTag(queryHandle, index, num2, out pchKey, 255u, out pchValue, 255u)) && pchKey.Equals(COMPATIBILITY_VERSION_KVTAG, StringComparison.InvariantCultureIgnoreCase))
            {
                if (byte.TryParse(pchValue, NumberStyles.Any, CultureInfo.InvariantCulture, out var result))
                {
                    return result;
                }
                UnturnedLog.warn("Unable to parse workshop item compatibility version from '{0}'", pchValue);
                return 0;
            }
        }
        return 0;
    }

    private static void DumpDetails(in SteamUGCDetails_t details)
    {
        UnturnedLog.info("{0} \"{1}\"", details.m_nPublishedFileId, details.m_rgchTitle);
        UnturnedLog.info("\tBanned: {0}", details.m_bBanned);
        UnturnedLog.info("\tResult: {0}", details.m_eResult);
        UnturnedLog.info("\tVisibility: {0}", details.m_eVisibility);
    }

    /// <summary>
    /// Save the details from a workshop query for lookup later.
    /// Allows game to inspect the installed files before deciding if they are
    /// compatible, since maps and localization are not affected by unity upgrades.
    /// Previously the compatibility test occurred before downloading the content.
    /// </summary>
    public static bool cacheDetails(UGCQueryHandle_t queryHandle, uint index, out CachedUGCDetails cachedDetails)
    {
        cachedDetails = default(CachedUGCDetails);
        SteamUGCDetails_t pDetails;
        bool num = (Dedicator.IsDedicatedServer ? SteamGameServerUGC.GetQueryUGCResult(queryHandle, index, out pDetails) : SteamUGC.GetQueryUGCResult(queryHandle, index, out pDetails));
        if (num)
        {
            PublishedFileId_t nPublishedFileId = pDetails.m_nPublishedFileId;
            byte compatibilityVersion = getCompatibilityVersion(queryHandle, index);
            cachedDetails.fileId = nPublishedFileId;
            cachedDetails.name = pDetails.m_rgchTitle;
            cachedDetails.compatibilityVersion = compatibilityVersion;
            cachedDetails.isBannedOrPrivate = pDetails.m_bBanned || pDetails.m_eVisibility == ERemoteStoragePublishedFileVisibility.k_ERemoteStoragePublishedFileVisibilityPrivate || pDetails.m_eResult == EResult.k_EResultAccessDenied;
            cachedDetails.updateTimestamp = MathfEx.Max(pDetails.m_rtimeCreated, pDetails.m_rtimeUpdated);
            cachedUGCDetails[nPublishedFileId.m_PublishedFileId] = cachedDetails;
            if (!string.IsNullOrEmpty(cachedDetails.name))
            {
                AssetOrigin assetOrigin = Assets.FindWorkshopFileOrigin(nPublishedFileId.m_PublishedFileId);
                if (assetOrigin != null)
                {
                    assetOrigin.name = $"Workshop File \"{cachedDetails.name}\" ({cachedDetails.fileId})";
                    return num;
                }
            }
        }
        else
        {
            UnturnedLog.warn("Unable to get query UGC result for caching");
        }
        return num;
    }

    /// <summary>
    /// Get cached workshop item details.
    /// </summary>
    public static bool getCachedDetails(PublishedFileId_t fileId, out CachedUGCDetails cachedDetails)
    {
        return cachedUGCDetails.TryGetValue(fileId.m_PublishedFileId, out cachedDetails);
    }

    public static AssetOrigin FindOrAddOrigin(ulong fileId)
    {
        AssetOrigin assetOrigin = Assets.FindOrAddWorkshopFileOrigin(fileId, shouldOverrideIds: true);
        if (cachedUGCDetails.TryGetValue(fileId, out var value) && !string.IsNullOrEmpty(value.name))
        {
            assetOrigin.name = $"Workshop File \"{value.name}\" ({value.fileId})";
        }
        return assetOrigin;
    }

    public static bool isCompatible(PublishedFileId_t fileId, ESteamUGCType type, string dir, out string explanation)
    {
        if (!getCachedDetails(fileId, out var cachedDetails))
        {
            explanation = null;
            return true;
        }
        if (type switch
        {
            ESteamUGCType.MAP => Directory.Exists(dir + "/Bundles"), 
            ESteamUGCType.LOCALIZATION => false, 
            _ => true, 
        })
        {
            if (cachedDetails.compatibilityVersion < 2)
            {
                explanation = $"Workshop version of \"{cachedDetails.GetTitle()}\" has not yet been updated from Unity 5.5 and cannot be loaded.";
                return false;
            }
            if (cachedDetails.compatibilityVersion > 5)
            {
                explanation = $"Workshop version of \"{cachedDetails.GetTitle()}\" has been updated to an unknown future version of Unity and cannot be loaded.";
                return false;
            }
        }
        explanation = null;
        return true;
    }

    /// <summary>
    /// Should caller skip loading a given workshop file?
    ///
    /// Used to skip workshop version of map if the map is locally installed,
    /// e.g. Canyon Arena moved to workshop and auto-subscribed.
    /// </summary>
    public static bool shouldIgnoreFile(PublishedFileId_t fileId, out string explanation)
    {
        if (fileId == FRANCE && ReadWrite.fileExists("/Maps/France/Config.json", useCloud: false, usePath: true))
        {
            explanation = "non-Workshop version of France is still installed";
            return true;
        }
        explanation = null;
        return false;
    }

    private void onCreateItemResult(CreateItemResult_t callback, bool io)
    {
        if (callback.m_bUserNeedsToAcceptWorkshopLegalAgreement)
        {
            MenuUI.alert(MenuDashboardUI.localization.format("UGC_NeedsToAcceptWorkshopLegalAgreement"));
            return;
        }
        if (callback.m_eResult != EResult.k_EResultOK)
        {
            MenuUI.alert(MenuDashboardUI.localization.format("UGC_UnknownResult", callback.m_eResult));
            return;
        }
        if (io)
        {
            MenuUI.alert(MenuDashboardUI.localization.format("UGC_IOError"));
            return;
        }
        publishedFileID = callback.m_nPublishedFileId;
        updateUGC();
    }

    private void onSubmitItemUpdateResult(SubmitItemUpdateResult_t callback, bool io)
    {
        if (callback.m_bUserNeedsToAcceptWorkshopLegalAgreement)
        {
            MenuUI.alert(MenuDashboardUI.localization.format("UGC_NeedsToAcceptWorkshopLegalAgreement"));
            return;
        }
        if (callback.m_eResult != EResult.k_EResultOK)
        {
            MenuUI.alert(MenuDashboardUI.localization.format("UGC_UnknownResult", callback.m_eResult));
            return;
        }
        if (io)
        {
            MenuUI.alert(MenuDashboardUI.localization.format("UGC_IOError"));
            return;
        }
        MenuUI.alert(MenuDashboardUI.localization.format("UGC_Success"));
        SDG.Unturned.Provider.provider.workshopService.open(publishedFileID);
        refreshPublished();
    }

    private void onQueryCompleted(SteamUGCQueryCompleted_t callback, bool io)
    {
        if (!(callback.m_eResult != EResult.k_EResultOK || io) && callback.m_unNumResultsReturned >= 1)
        {
            for (uint num = 0u; num < callback.m_unNumResultsReturned; num++)
            {
                SteamUGC.GetQueryUGCResult(ugcRequest, num, out var pDetails);
                SteamPublished item = new SteamPublished(pDetails.m_rgchTitle, pDetails.m_nPublishedFileId);
                published.Add(item);
            }
            onPublishedAdded?.Invoke();
            cleanupUGCRequest();
            shouldRequestAnotherPage = true;
        }
    }

    public void update()
    {
        if (shouldRequestAnotherPage)
        {
            shouldRequestAnotherPage = false;
            ugcRequestPage++;
            ugcRequest = SteamUGC.CreateQueryUserUGCRequest(SDG.Unturned.Provider.client.GetAccountID(), EUserUGCList.k_EUserUGCList_Published, EUGCMatchingUGCType.k_EUGCMatchingUGCType_Items, EUserUGCListSortOrder.k_EUserUGCListSortOrder_CreationOrderAsc, SteamUtils.GetAppID(), SteamUtils.GetAppID(), ugcRequestPage);
            SteamAPICall_t hAPICall = SteamUGC.SendQueryUGCRequest(ugcRequest);
            queryCompleted.Set(hAPICall);
        }
        if (currentlyDownloadingFileId != PublishedFileId_t.Invalid && SteamUGC.GetItemDownloadInfo(currentlyDownloadingFileId, out var punBytesDownloaded, out var punBytesTotal) && punBytesTotal != 0)
        {
            currentlyDownloadingFileEstimatedProgress = (float)((double)punBytesDownloaded / (double)punBytesTotal);
            if (punBytesDownloaded >= punBytesTotal)
            {
                currentlyDownloadingFileId = PublishedFileId_t.Invalid;
            }
            UpdateEstimatedDownloadProgress();
        }
    }

    private void UpdateEstimatedDownloadProgress()
    {
        float num = progressPerFileDownloaded * (float)(totalNumberOfFilesToDownload - installing.Count) + progressPerFileDownloaded * currentlyDownloadingFileEstimatedProgress;
        int num2 = Mathf.RoundToInt(num * 100f);
        if (previousEstimatedDownloadProgress != num2)
        {
            previousEstimatedDownloadProgress = num2;
            LoadingUI.NotifyDownloadProgress(num);
        }
    }

    private void OnFinishedDownloadingItems()
    {
        if (Assets.ShouldWaitForNewAssetsToFinishLoading)
        {
            UnturnedLog.info("Client UGC waiting for assets to finish loading...");
            Assets.OnNewAssetsFinishedLoading = (System.Action)Delegate.Combine(Assets.OnNewAssetsFinishedLoading, new System.Action(OnNewAssetsFinishedLoading));
        }
        else
        {
            OnNewAssetsFinishedLoading();
        }
    }

    private void OnNewAssetsFinishedLoading()
    {
        Assets.OnNewAssetsFinishedLoading = (System.Action)Delegate.Remove(Assets.OnNewAssetsFinishedLoading, new System.Action(OnNewAssetsFinishedLoading));
        SDG.Unturned.Provider.launch();
    }

    public void downloadNextItem()
    {
        if (installing.Count == 0)
        {
            LoadingUI.SetIsDownloading(isDownloading: false);
            OnFinishedDownloadingItems();
            return;
        }
        PublishedFileId_t publishedFileId_t = installing[0];
        string downloadFileName;
        if (getCachedDetails(publishedFileId_t, out var cachedDetails))
        {
            downloadFileName = cachedDetails.GetTitle();
        }
        else
        {
            PublishedFileId_t publishedFileId_t2 = publishedFileId_t;
            downloadFileName = "Unknown ID " + publishedFileId_t2.ToString();
        }
        LoadingUI.SetDownloadFileName(downloadFileName);
        currentlyDownloadingFileId = publishedFileId_t;
        currentlyDownloadingFileEstimatedProgress = 0f;
        SteamUGC.DownloadItem(publishedFileId_t, bHighPriority: true);
    }

    /// <summary>
    /// Helper for downloadServerItems.
    /// Called for each workshop item we want to download for the server.
    /// </summary>
    private void enqueueServerItemDownloadOrInstallFromCache(PublishedFileId_t fileId)
    {
        bool flag = isInstalledItemAlreadyRegistered(fileId);
        ulong punSizeOnDisk;
        string pchFolder;
        uint punTimeStamp;
        if (shouldIgnoreFile(fileId, out var explanation))
        {
            UnturnedLog.info("Ignoring server download {0} because '{1}'", fileId, explanation);
        }
        else if (SteamUGC.GetItemInstallInfo(fileId, out punSizeOnDisk, out pchFolder, 1024u, out punTimeStamp) && ReadWrite.folderExists(pchFolder, usePath: false))
        {
            CachedUGCDetails cachedDetails;
            if ((SteamUGC.GetItemState(fileId) & 8) == 8)
            {
                if (flag)
                {
                    UnturnedLog.info($"Server workshop file {fileId} is already loaded, but was flagged as needing update");
                    return;
                }
                UnturnedLog.info("Server workshop item {0} found in cache, but was flagged as needing update", fileId);
                installing.Add(fileId);
            }
            else if (getCachedDetails(fileId, out cachedDetails) && cachedDetails.updateTimestamp > punTimeStamp)
            {
                if (flag)
                {
                    UnturnedLog.info("Server workshop file {0} is already loaded, but remote ({1}) is newer than local ({2})", fileId, DateTimeEx.FromUtcUnixTimeSeconds(cachedDetails.updateTimestamp).ToLocalTime(), DateTimeEx.FromUtcUnixTimeSeconds(punTimeStamp).ToLocalTime());
                }
                else
                {
                    UnturnedLog.info("Server workshop item {0} found in cache, but remote ({1}) is newer than local ({2})", fileId, DateTimeEx.FromUtcUnixTimeSeconds(cachedDetails.updateTimestamp).ToLocalTime(), DateTimeEx.FromUtcUnixTimeSeconds(punTimeStamp).ToLocalTime());
                    installing.Add(fileId);
                }
            }
            else if (!flag)
            {
                PublishedFileId_t publishedFileId_t = fileId;
                UnturnedLog.info("Installing cached server workshop item: " + publishedFileId_t.ToString());
                installItemDownloadedFromServer(fileId, pchFolder);
            }
        }
        else
        {
            installing.Add(fileId);
        }
    }

    /// <summary>
    /// Called once we know which items the server is allowed to use (queryServerItems),
    /// or the query has failed in which case we proceed with all items it told us.
    /// </summary>
    private void downloadServerItems(List<PublishedFileId_t> itemIDs)
    {
        installing = new List<PublishedFileId_t>();
        Assets.loadingStats.Reset();
        foreach (PublishedFileId_t itemID in itemIDs)
        {
            enqueueServerItemDownloadOrInstallFromCache(itemID);
        }
        if (installing.Count < 1)
        {
            UnturnedLog.info("Server has {0} valid workshop item(s), but we already have them downloaded", itemIDs.Count);
            OnFinishedDownloadingItems();
            return;
        }
        UnturnedLog.info("Server has {0} valid workshop item(s), of which {1} need to be downloaded", itemIDs.Count, installing.Count);
        totalNumberOfFilesToDownload = installing.Count;
        progressPerFileDownloaded = 1f / (float)totalNumberOfFilesToDownload;
        previousEstimatedDownloadProgress = 0;
        LoadingUI.SetIsDownloading(isDownloading: true);
        LoadingUI.NotifyDownloadProgress(0f);
        downloadNextItem();
    }

    /// <summary>
    /// Is currently connected server allowed to auto-download the workshop item?
    /// Requested by mod authors so that they can whitelist/blacklist access.
    /// </summary>
    private bool testDownloadRestrictions(UGCQueryHandle_t queryHandle, uint resultIndex, uint ip, string itemDisplayText)
    {
        if (ip == 0)
        {
            return true;
        }
        EWorkshopDownloadRestrictionResult restrictionResult = WorkshopDownloadRestrictions.getRestrictionResult(queryHandle, resultIndex, ip);
        switch (restrictionResult)
        {
        case EWorkshopDownloadRestrictionResult.NoRestrictions:
            return true;
        case EWorkshopDownloadRestrictionResult.NotWhitelisted:
            UnturnedLog.warn("Server is not authorized in the IP whitelist for " + itemDisplayText);
            return false;
        case EWorkshopDownloadRestrictionResult.Blacklisted:
            UnturnedLog.warn("Server is blocked in IP blacklist from downloading " + itemDisplayText);
            return false;
        case EWorkshopDownloadRestrictionResult.Allowed:
            UnturnedLog.info("Server is authorized to download " + itemDisplayText);
            return true;
        case EWorkshopDownloadRestrictionResult.Banned:
            UnturnedLog.warn("Workshop file is banned " + itemDisplayText);
            return false;
        case EWorkshopDownloadRestrictionResult.PrivateVisibility:
            UnturnedLog.warn("Workshop file is private " + itemDisplayText);
            return false;
        default:
            UnturnedLog.warn("Unknown restriction result '{0}' for '{1}'", restrictionResult, itemDisplayText);
            return true;
        }
    }

    /// <summary>
    /// Successfully queried details of the items current server is using.
    /// Ensure server has permission to use these items, then proceed with downloading.
    /// Also caches item titles for use on the loading screen.
    /// </summary>
    private void handleServerItemsQuerySuccess(SteamUGCQueryCompleted_t callback)
    {
        string iPFromUInt = Parser.getIPFromUInt32(serverDownloadIP);
        UnturnedLog.info("Server's allowed IP for Workshop downloads: " + iPFromUInt);
        serverPendingIDs = new List<PublishedFileId_t>((int)callback.m_unNumResultsReturned);
        for (uint num = 0u; num < callback.m_unNumResultsReturned; num++)
        {
            cacheDetails(callback.m_handle, num, out var cachedDetails);
            if (testDownloadRestrictions(callback.m_handle, num, serverDownloadIP, cachedDetails.GetTitle()))
            {
                serverPendingIDs.Add(cachedDetails.fileId);
                continue;
            }
            int num2 = serverInvalidItemsCount + 1;
            serverInvalidItemsCount = num2;
        }
        downloadServerItems(serverPendingIDs);
    }

    /// <summary>
    /// IO or bad result occurred when querying items the current server is using.
    /// We do not know the file details, but we proceed with downloading them all.
    /// </summary>
    private void handleServerItemsQueryFailed()
    {
        downloadServerItems(serverPendingIDs);
    }

    private void onServerItemsQueryCompleted(SteamUGCQueryCompleted_t callback, bool ioFailure)
    {
        if (!(callback.m_handle != serverItemsQueryHandle))
        {
            if (ioFailure)
            {
                UnturnedLog.error("IO error querying workshop for server items!");
                handleServerItemsQueryFailed();
            }
            else if (callback.m_eResult == EResult.k_EResultOK)
            {
                handleServerItemsQuerySuccess(callback);
            }
            else
            {
                UnturnedLog.error("Error querying workshop for server items: " + callback.m_eResult);
                handleServerItemsQueryFailed();
            }
            SteamUGC.ReleaseQueryUGCRequest(serverItemsQueryHandle);
            serverItemsQueryHandle = UGCQueryHandle_t.Invalid;
        }
    }

    /// <summary>
    /// Called prior to downloading, and after a connection failure.
    /// </summary>
    public void resetServerInvalidItems()
    {
        serverPendingIDs = null;
        serverInvalidItemsCount = 0;
    }

    /// <summary>
    /// Client now knows the published file IDs the server is using, but
    /// queries the workshop for additional information before installing.
    /// </summary>
    public void queryServerWorkshopItems(List<PublishedFileId_t> fileIDs, uint serverIP)
    {
        serverPendingIDs = fileIDs;
        serverDownloadIP = serverIP;
        serverItemsQueryHandle = SteamUGC.CreateQueryUGCDetailsRequest(fileIDs.ToArray(), (uint)fileIDs.Count);
        SteamUGC.SetReturnKeyValueTags(serverItemsQueryHandle, bReturnKeyValueTags: true);
        SteamUGC.SetAllowCachedResponse(serverItemsQueryHandle, 60u);
        SteamAPICall_t hAPICall = SteamUGC.SendQueryUGCRequest(serverItemsQueryHandle);
        serverItemsQueryCompleted.Set(hAPICall);
    }

    private void installItemDownloadedFromServer(PublishedFileId_t fileId, string path)
    {
        if (WorkshopTool.detectUGCMetaType(path, usePath: false, out var outType))
        {
            ugc.Add(new SteamContent(fileId, path, outType));
            LoadFileIfAssetStartupAlreadyRan(fileId, path, outType);
        }
        else
        {
            PublishedFileId_t publishedFileId_t = fileId;
            UnturnedLog.warn("Unable to determine UGC type for downloaded item: " + publishedFileId_t.ToString());
        }
    }

    private void onItemDownloaded(DownloadItemResult_t callback)
    {
        if (installing == null || installing.Count == 0 || callback.m_unAppID.m_AppId != appInfo.id)
        {
            return;
        }
        PublishedFileId_t nPublishedFileId = callback.m_nPublishedFileId;
        UnturnedLog.info("Workshop item downloaded: " + nPublishedFileId.ToString());
        if (callback.m_nPublishedFileId == currentlyDownloadingFileId)
        {
            currentlyDownloadingFileId = PublishedFileId_t.Invalid;
            currentlyDownloadingFileEstimatedProgress = 0f;
        }
        installing.Remove(callback.m_nPublishedFileId);
        UpdateEstimatedDownloadProgress();
        if (callback.m_eResult == EResult.k_EResultOK)
        {
            string explanation;
            ulong punSizeOnDisk;
            string pchFolder;
            uint punTimeStamp;
            if (isInstalledItemAlreadyRegistered(callback.m_nPublishedFileId))
            {
                UnturnedLog.warn("Already registered newly downloaded workshop item '{0}', so ignoring this callback", callback.m_nPublishedFileId);
            }
            else if (shouldIgnoreFile(callback.m_nPublishedFileId, out explanation))
            {
                UnturnedLog.info("Ignoring newly downloaded workshop item {0} because '{1}'", callback.m_nPublishedFileId, explanation);
            }
            else if (SteamUGC.GetItemInstallInfo(callback.m_nPublishedFileId, out punSizeOnDisk, out pchFolder, 1024u, out punTimeStamp))
            {
                if (ReadWrite.folderExists(pchFolder, usePath: false))
                {
                    installItemDownloadedFromServer(callback.m_nPublishedFileId, pchFolder);
                }
                else
                {
                    UnturnedLog.warn("Finished downloading workshop item {0}, but unable to find the files on disk ({1})", callback.m_nPublishedFileId, pchFolder);
                }
            }
            else
            {
                UnturnedLog.warn("Finished downloading workshop item {0}, but unable get install info", callback.m_nPublishedFileId);
            }
        }
        else
        {
            UnturnedLog.warn("Download workshop item {0} failed, result: {1}", callback.m_nPublishedFileId, callback.m_eResult);
        }
        downloadNextItem();
    }

    private void onItemInstalled(ItemInstalled_t callback)
    {
        if (callback.m_unAppID.m_AppId == appInfo.id)
        {
            PublishedFileId_t nPublishedFileId = callback.m_nPublishedFileId;
            UnturnedLog.info("Workshop item installed: " + nPublishedFileId.ToString());
            string explanation;
            string path;
            ESteamUGCType outType;
            if (isInstalledItemAlreadyRegistered(callback.m_nPublishedFileId))
            {
                UnturnedLog.warn("Already registered newly installed workshop item '{0}', so ignoring this callback", callback.m_nPublishedFileId);
            }
            else if (shouldIgnoreFile(callback.m_nPublishedFileId, out explanation))
            {
                UnturnedLog.info("Ignoring newly installed workshop item because '{0}'", explanation);
            }
            else if (!getInstalledItemPath(callback.m_nPublishedFileId, out path))
            {
                UnturnedLog.warn("Unable to determine newly installed workshop item '{0}' file path", callback.m_nPublishedFileId);
            }
            else if (!WorkshopTool.detectUGCMetaType(path, usePath: false, out outType))
            {
                UnturnedLog.warn("Unable to determine newly installed workshop item '{0}' type", callback.m_nPublishedFileId);
            }
            else
            {
                ugc.Add(new SteamContent(callback.m_nPublishedFileId, path, outType));
                LoadFileIfAssetStartupAlreadyRan(callback.m_nPublishedFileId, path, outType);
            }
        }
    }

    private void LoadFileIfAssetStartupAlreadyRan(PublishedFileId_t fileId, string path, ESteamUGCType type)
    {
        if (!((type == ESteamUGCType.MAP) ? Assets.hasLoadedMaps : Assets.hasLoadedUgc))
        {
            UnturnedLog.info($"Workshop file {fileId} not requesting load because asset refresh is in progress");
            return;
        }
        switch (type)
        {
        case ESteamUGCType.MAP:
            WorkshopTool.loadMapBundlesAndContent(path, fileId.m_PublishedFileId);
            Level.broadcastLevelsRefreshed();
            break;
        default:
            Assets.RequestAddSearchLocation(path, FindOrAddOrigin(fileId.m_PublishedFileId));
            break;
        case ESteamUGCType.LOCALIZATION:
            break;
        }
    }

    private void cleanupUGCRequest()
    {
        if (!(ugcRequest == UGCQueryHandle_t.Invalid))
        {
            SteamUGC.ReleaseQueryUGCRequest(ugcRequest);
            ugcRequest = UGCQueryHandle_t.Invalid;
        }
    }

    public void prepareUGC(string name, string description, string path, string preview, string change, ESteamUGCType type, List<string> tags, string allowedIPs, ESteamUGCVisibility visibility)
    {
        bool verified = File.Exists(path + "/Skin.kvt");
        prepareUGC(name, description, path, preview, change, type, tags, allowedIPs, visibility, verified);
    }

    public void prepareUGC(string name, string description, string path, string preview, string change, ESteamUGCType type, List<string> tags, string allowedIPs, ESteamUGCVisibility visibility, bool verified)
    {
        ugcName = name;
        ugcDescription = description;
        ugcPath = path;
        ugcPreview = preview;
        ugcChange = change;
        ugcType = type;
        ugcTags = tags;
        ugcAllowedIPs = allowedIPs;
        ugcVisibility = visibility;
        ugcVerified = verified;
    }

    public void prepareUGC(PublishedFileId_t id)
    {
        publishedFileID = id;
    }

    public void createUGC(bool ugcFor)
    {
        SteamAPICall_t hAPICall = SteamUGC.CreateItem(SteamUtils.GetAppID(), ugcFor ? EWorkshopFileType.k_EWorkshopFileTypeMicrotransaction : EWorkshopFileType.k_EWorkshopFileTypeFirst);
        createItemResult.Set(hAPICall);
    }

    public void updateUGC()
    {
        UGCUpdateHandle_t uGCUpdateHandle_t = SteamUGC.StartItemUpdate(SteamUtils.GetAppID(), publishedFileID);
        if (ugcType == ESteamUGCType.MAP)
        {
            ReadWrite.writeBytes(ugcPath + "/Map.meta", useCloud: false, usePath: false, new byte[1]);
        }
        else if (ugcType == ESteamUGCType.LOCALIZATION)
        {
            ReadWrite.writeBytes(ugcPath + "/Localization.meta", useCloud: false, usePath: false, new byte[1]);
        }
        else if (ugcType == ESteamUGCType.OBJECT)
        {
            ReadWrite.writeBytes(ugcPath + "/Object.meta", useCloud: false, usePath: false, new byte[1]);
        }
        else if (ugcType == ESteamUGCType.ITEM)
        {
            ReadWrite.writeBytes(ugcPath + "/Item.meta", useCloud: false, usePath: false, new byte[1]);
        }
        else if (ugcType == ESteamUGCType.VEHICLE)
        {
            ReadWrite.writeBytes(ugcPath + "/Vehicle.meta", useCloud: false, usePath: false, new byte[1]);
        }
        else if (ugcType == ESteamUGCType.SKIN)
        {
            ReadWrite.writeBytes(ugcPath + "/Skin.meta", useCloud: false, usePath: false, new byte[1]);
        }
        SteamUGC.SetItemContent(uGCUpdateHandle_t, ugcPath);
        if (ugcDescription.Length > 0)
        {
            SteamUGC.SetItemDescription(uGCUpdateHandle_t, ugcDescription);
        }
        if (ugcPreview.Length > 0)
        {
            SteamUGC.SetItemPreview(uGCUpdateHandle_t, ugcPreview);
        }
        List<string> list = new List<string>();
        if (ugcTags != null)
        {
            foreach (string ugcTag in ugcTags)
            {
                if (!string.IsNullOrWhiteSpace(ugcTag))
                {
                    list.Add(ugcTag);
                }
            }
        }
        if (ugcVerified)
        {
            list.Add("Verified");
        }
        SteamUGC.SetItemTags(uGCUpdateHandle_t, list.ToArray());
        if (ugcName.Length > 0)
        {
            SteamUGC.SetItemTitle(uGCUpdateHandle_t, ugcName);
        }
        SteamUGC.RemoveItemKeyValueTags(uGCUpdateHandle_t, WorkshopDownloadRestrictions.IP_RESTRICTIONS_KVTAG);
        if (!string.IsNullOrEmpty(ugcAllowedIPs))
        {
            SteamUGC.AddItemKeyValueTag(uGCUpdateHandle_t, WorkshopDownloadRestrictions.IP_RESTRICTIONS_KVTAG, ugcAllowedIPs);
        }
        SteamUGC.RemoveItemKeyValueTags(uGCUpdateHandle_t, COMPATIBILITY_VERSION_KVTAG);
        SteamUGC.AddItemKeyValueTag(uGCUpdateHandle_t, COMPATIBILITY_VERSION_KVTAG, 5.ToString());
        if (ugcVisibility == ESteamUGCVisibility.PUBLIC)
        {
            SteamUGC.SetItemVisibility(uGCUpdateHandle_t, ERemoteStoragePublishedFileVisibility.k_ERemoteStoragePublishedFileVisibilityPublic);
        }
        else if (ugcVisibility == ESteamUGCVisibility.FRIENDS_ONLY)
        {
            SteamUGC.SetItemVisibility(uGCUpdateHandle_t, ERemoteStoragePublishedFileVisibility.k_ERemoteStoragePublishedFileVisibilityFriendsOnly);
        }
        else if (ugcVisibility == ESteamUGCVisibility.PRIVATE)
        {
            SteamUGC.SetItemVisibility(uGCUpdateHandle_t, ERemoteStoragePublishedFileVisibility.k_ERemoteStoragePublishedFileVisibilityPrivate);
        }
        else if (ugcVisibility == ESteamUGCVisibility.UNLISTED)
        {
            SteamUGC.SetItemVisibility(uGCUpdateHandle_t, ERemoteStoragePublishedFileVisibility.k_ERemoteStoragePublishedFileVisibilityUnlisted);
        }
        SteamAPICall_t hAPICall = SteamUGC.SubmitItemUpdate(uGCUpdateHandle_t, ugcChange);
        submitItemUpdateResult.Set(hAPICall);
    }

    private bool isInstalledItemAlreadyRegistered(PublishedFileId_t fileId)
    {
        foreach (SteamContent item in ugc)
        {
            if (item.publishedFileID == fileId)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Get path to an already-installed workshop item.
    /// </summary>
    /// <returns>True if the path was found.</returns>
    private bool getInstalledItemPath(PublishedFileId_t fileId, out string path)
    {
        EItemState itemState = (EItemState)SteamUGC.GetItemState(fileId);
        if ((itemState & EItemState.k_EItemStateInstalled) != EItemState.k_EItemStateInstalled)
        {
            UnturnedLog.warn("Installed item {0} state flags missing k_EItemStateInstalled: {1}", fileId, itemState);
        }
        if (SteamUGC.GetItemInstallInfo(fileId, out var _, out path, 1024u, out var _))
        {
            return ReadWrite.folderExists(path, usePath: false);
        }
        return false;
    }

    /// <summary>
    /// Used during startup to register subscribed workshop items.
    /// Given a workshop item file id, if its files exist on disk then register it.
    /// </summary>
    private void registerInstalledItem(PublishedFileId_t fileId)
    {
        if (isInstalledItemAlreadyRegistered(fileId))
        {
            return;
        }
        if (shouldIgnoreFile(fileId, out var explanation))
        {
            UnturnedLog.info("Ignoring subscribed item {0} because '{1}'", fileId, explanation);
            return;
        }
        if (!getInstalledItemPath(fileId, out var path))
        {
            UnturnedLog.warn("Unable to register installed item during startup: {0}\nPath:{1}", fileId, path);
            return;
        }
        if (!WorkshopTool.detectUGCMetaType(path, usePath: false, out var outType))
        {
            PublishedFileId_t publishedFileId_t = fileId;
            UnturnedLog.warn("Unable to determine UGC type for installed item: " + publishedFileId_t.ToString());
            return;
        }
        if (!isCompatible(fileId, outType, path, out var explanation2))
        {
            Assets.reportError(explanation2);
            return;
        }
        ugc.Add(new SteamContent(fileId, path, outType));
        if (LocalWorkshopSettings.get().getEnabled(fileId))
        {
            LoadFileIfAssetStartupAlreadyRan(fileId, path, outType);
        }
    }

    /// <summary>
    /// Called when subscribed items callback was successful to register all compatible files.
    /// </summary>
    private void handleSubscribedItemsCallbackSuccess(SteamUGCQueryCompleted_t callback)
    {
        UnturnedLog.info($"Received details for {callback.m_unNumResultsReturned} subscribed workshop file(s)");
        for (uint num = 0u; num < callback.m_unNumResultsReturned; num++)
        {
            if (cacheDetails(callback.m_handle, num, out var cachedDetails))
            {
                UnturnedLog.info($"Subscribed workshop file {num + 1} of {callback.m_unNumResultsReturned}: \"{cachedDetails.name}\" ({cachedDetails.fileId})");
                registerInstalledItem(cachedDetails.fileId);
            }
        }
    }

    /// <summary>
    /// Called when subscribed items callback did not execute as expected,
    /// maybe because steam's servers are offline. In this case we can't check
    /// compatibility so we register all the locally subscribed items as compatible.
    /// </summary>
    private void handleSubscribedItemsCallbackFailed()
    {
        UnturnedLog.info("Registering {0} locally subscribed item(s)", locallySubscribedFileIds.Length);
        PublishedFileId_t[] array = locallySubscribedFileIds;
        foreach (PublishedFileId_t fileId in array)
        {
            registerInstalledItem(fileId);
        }
    }

    /// <summary>
    /// Register any localization-type workshop content before waiting for the steam callbacks.
    /// Important so that localizations are available for loading screens and whatnot during startup.
    /// Any items we register now will be skipped later.
    /// </summary>
    private void registerLocalizations()
    {
        PublishedFileId_t[] array = locallySubscribedFileIds;
        foreach (PublishedFileId_t fileId in array)
        {
            if (getInstalledItemPath(fileId, out var path) && WorkshopTool.detectUGCMetaType(path, usePath: false, out var outType) && outType == ESteamUGCType.LOCALIZATION)
            {
                registerInstalledItem(fileId);
            }
        }
    }

    private void onSubscribedQueryCompleted(SteamUGCQueryCompleted_t callback, bool ioFailure)
    {
        if (callback.m_handle != subscribedQueryHandle)
        {
            return;
        }
        if (!ioFailure)
        {
            if (callback.m_eResult == EResult.k_EResultOK)
            {
                handleSubscribedItemsCallbackSuccess(callback);
            }
            else
            {
                UnturnedLog.error("Encountered an error when querying workshop for subscribed items: " + callback.m_eResult);
                handleSubscribedItemsCallbackFailed();
            }
        }
        else
        {
            UnturnedLog.error("Encountered an IO error when querying workshop for subscribed items!");
            handleSubscribedItemsCallbackFailed();
        }
        SteamUGC.ReleaseQueryUGCRequest(subscribedQueryHandle);
        subscribedQueryHandle = UGCQueryHandle_t.Invalid;
    }

    public void refreshUGC()
    {
        _ugc = new List<SteamContent>();
        uint numSubscribedItems = SteamUGC.GetNumSubscribedItems();
        if (numSubscribedItems < 1)
        {
            UnturnedLog.info("Found zero workshop file subscriptions");
            return;
        }
        if ((bool)shouldIgnoreSubscribedItems)
        {
            UnturnedLog.info("Ignoring all workshop file subscriptions");
            return;
        }
        locallySubscribedFileIds = new PublishedFileId_t[numSubscribedItems];
        SteamUGC.GetSubscribedItems(locallySubscribedFileIds, numSubscribedItems);
        UnturnedLog.info("Subscribed workshop file ID(s): " + string.Join(", ", locallySubscribedFileIds));
        registerLocalizations();
        UnturnedLog.info("Querying details for subscribed workshop files...");
        subscribedQueryHandle = SteamUGC.CreateQueryUGCDetailsRequest(locallySubscribedFileIds, numSubscribedItems);
        SteamUGC.SetReturnKeyValueTags(subscribedQueryHandle, bReturnKeyValueTags: true);
        SteamAPICall_t hAPICall = SteamUGC.SendQueryUGCRequest(subscribedQueryHandle);
        subscribedQueryCompleted.Set(hAPICall);
    }

    public void refreshPublished()
    {
        onPublishedRemoved?.Invoke();
        cleanupUGCRequest();
        _published = new List<SteamPublished>();
        ugcRequestPage = 1u;
        shouldRequestAnotherPage = false;
        ugcRequest = SteamUGC.CreateQueryUserUGCRequest(SDG.Unturned.Provider.client.GetAccountID(), EUserUGCList.k_EUserUGCList_Published, EUGCMatchingUGCType.k_EUGCMatchingUGCType_Items, EUserUGCListSortOrder.k_EUserUGCListSortOrder_CreationOrderAsc, SteamUtils.GetAppID(), SteamUtils.GetAppID(), ugcRequestPage);
        SteamAPICall_t hAPICall = SteamUGC.SendQueryUGCRequest(ugcRequest);
        queryCompleted.Set(hAPICall);
    }

    public bool getSubscribed(ulong fileId)
    {
        if (ugc == null)
        {
            return false;
        }
        PublishedFileId_t publishedFileId_t = new PublishedFileId_t(fileId);
        if (ingameSubscriptions.TryGetValue(publishedFileId_t, out var value))
        {
            return value;
        }
        foreach (SteamContent item in ugc)
        {
            if (item.publishedFileID == publishedFileId_t)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Called by us when we subscribe to an item from in-game.
    /// If item already exists on-disk steam doesn't always call onItemInstalled, so we do our own check and potentially load.
    /// </summary>
    private void gameSubscribed(PublishedFileId_t fileId)
    {
        if (!isInstalledItemAlreadyRegistered(fileId))
        {
            EItemState itemState = (EItemState)SteamUGC.GetItemState(fileId);
            if ((itemState & EItemState.k_EItemStateInstalled) == EItemState.k_EItemStateInstalled && (itemState & EItemState.k_EItemStateDownloading) != EItemState.k_EItemStateDownloading && (itemState & EItemState.k_EItemStateDownloadPending) != EItemState.k_EItemStateDownloading)
            {
                UnturnedLog.info("Triggering a fake onItemInstalled callback for {0} because game subscribed to a pre-installed item", fileId);
                ItemInstalled_t callback = default(ItemInstalled_t);
                callback.m_unAppID.m_AppId = appInfo.id;
                callback.m_nPublishedFileId = fileId;
                onItemInstalled(callback);
            }
        }
    }

    public void setSubscribed(ulong fileId, bool subscribe)
    {
        PublishedFileId_t publishedFileId_t = new PublishedFileId_t(fileId);
        if (subscribe)
        {
            SteamUGC.SubscribeItem(publishedFileId_t);
            UnturnedLog.info("Game subscribed to " + fileId);
            gameSubscribed(publishedFileId_t);
        }
        else
        {
            SteamUGC.UnsubscribeItem(publishedFileId_t);
            UnturnedLog.info("Game un-subscribed from " + fileId);
        }
        ingameSubscriptions[publishedFileId_t] = subscribe;
    }

    public TempSteamworksWorkshop(SteamworksAppInfo newAppInfo)
    {
        appInfo = newAppInfo;
        downloaded = new List<PublishedFileId_t>();
        if (!appInfo.isDedicated)
        {
            createItemResult = CallResult<CreateItemResult_t>.Create(onCreateItemResult);
            submitItemUpdateResult = CallResult<SubmitItemUpdateResult_t>.Create(onSubmitItemUpdateResult);
            queryCompleted = CallResult<SteamUGCQueryCompleted_t>.Create(onQueryCompleted);
            itemDownloaded = Callback<DownloadItemResult_t>.Create(onItemDownloaded);
            itemInstalled = Callback<ItemInstalled_t>.Create(onItemInstalled);
            subscribedQueryCompleted = CallResult<SteamUGCQueryCompleted_t>.Create(onSubscribedQueryCompleted);
            serverItemsQueryCompleted = CallResult<SteamUGCQueryCompleted_t>.Create(onServerItemsQueryCompleted);
        }
    }
}
