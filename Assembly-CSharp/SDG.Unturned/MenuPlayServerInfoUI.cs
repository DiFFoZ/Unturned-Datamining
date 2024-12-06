using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using SDG.HostBans;
using SDG.Provider;
using Steamworks;
using UnityEngine;
using Unturned.SystemEx;

namespace SDG.Unturned;

public class MenuPlayServerInfoUI
{
    public enum EServerInfoOpenContext
    {
        CONNECT,
        SERVERS,
        BOOKMARKS
    }

    private class ServerInfoViewWorkshopButton : SleekWrapper
    {
        public PublishedFileId_t fileId;

        public ServerInfoViewWorkshopButton(PublishedFileId_t fileId, string name)
        {
            this.fileId = fileId;
            base.SizeOffset_X = 20f;
            base.SizeOffset_Y = 20f;
            ISleekButton sleekButton = Glazier.Get().CreateButton();
            sleekButton.SizeScale_X = 1f;
            sleekButton.SizeScale_Y = 1f;
            sleekButton.OnClicked += onClickedButton;
            sleekButton.TooltipText = MenuWorkshopSubscriptionsUI.localization.format("View_Tooltip", name);
            AddChild(sleekButton);
            ISleekSprite sleekSprite = Glazier.Get().CreateSprite();
            sleekSprite.PositionOffset_X = 5f;
            sleekSprite.PositionOffset_Y = 5f;
            sleekSprite.SizeOffset_X = 10f;
            sleekSprite.SizeOffset_Y = 10f;
            sleekSprite.Sprite = MenuDashboardUI.icons.load<Sprite>("External_Link_Sprite");
            sleekSprite.DrawMethod = ESleekSpriteType.Regular;
            sleekButton.AddChild(sleekSprite);
        }

        private void onClickedButton(ISleekElement button)
        {
            PublishedFileId_t publishedFileId_t = fileId;
            string url = "http://steamcommunity.com/sharedfiles/filedetails/?id=" + publishedFileId_t.ToString();
            Provider.provider.browserService.open(url);
        }
    }

    internal static Local localization;

    private static SleekFullscreenBox container;

    public static bool active;

    private static ISleekElement infoContainer;

    private static ISleekElement playersContainer;

    private static ISleekElement detailsContainer;

    private static ISleekElement mapContainer;

    private static ISleekElement buttonsContainer;

    private static ISleekBox titleBox;

    private static SleekWebImage titleIconImage;

    private static ISleekLabel titleNameLabel;

    private static ISleekLabel titleDescriptionLabel;

    private static ISleekLabel titleCurationLabelsLabel;

    private static ISleekBox playerCountBox;

    private static ISleekScrollView playersScrollBox;

    private static ISleekBox detailsBox;

    private static ISleekScrollView detailsScrollBox;

    private static ISleekButton hostBanWarningButton;

    private static ISleekButton notLoggedInWarningButton;

    private static ISleekElement linksFrame;

    private static ISleekBox serverTitle;

    private static ISleekBox serverBox;

    private static ISleekLabel serverWorkshopLabel;

    private static ISleekLabel serverCombatLabel;

    private static ISleekLabel serverPerspectiveLabel;

    private static ISleekLabel serverSecurityLabel;

    private static ISleekLabel serverModeLabel;

    private static ISleekLabel serverCheatsLabel;

    private static ISleekLabel serverMonetizationLabel;

    private static ISleekLabel serverPingLabel;

    private static ISleekBox ugcTitle;

    private static ISleekBox ugcBox;

    private static ISleekBox configTitle;

    private static ISleekBox configBox;

    private static ISleekBox rocketTitle;

    private static ISleekBox rocketBox;

    private static ISleekBox mapNameBox;

    private static ISleekBox mapPreviewBox;

    private static ISleekImage mapPreviewImage;

    private static ISleekBox mapDescriptionBox;

    private static ISleekBox serverDescriptionBox;

    private static ISleekButton joinButton;

    private static ISleekBox joinDisabledBox;

    private static ISleekButton favoriteButton;

    private static SleekButtonIcon bookmarkButton;

    private static ISleekButton refreshButton;

    private static ISleekButton cancelButton;

    private static SteamServerAdvertisement serverInfo;

    private static string serverPassword;

    private static bool serverFavorited;

    /// <summary>
    /// Null if not bookmarked.
    /// </summary>
    private static ServerBookmarkDetails bookmarkDetails;

    /// <summary>
    /// DNS entry to use if adding a bookmark for this server.
    /// </summary>
    private static string serverBookmarkHost;

    private static List<PublishedFileId_t> expectedWorkshopItems;

    private static List<string> linkUrls;

    private static int playersOffset;

    private static int playerCount;

    private static EHostBanFlags banFlags;

    private static UGCQueryHandle_t detailsHandle;

    private static CallResult<SteamUGCQueryCompleted_t> ugcQueryCompleted;

    private static MenuServerPasswordUI passwordUI;

    public static EServerInfoOpenContext openContext { get; private set; }

    private static void onUGCQueryCompleted(SteamUGCQueryCompleted_t callback, bool io)
    {
        if (callback.m_eResult != EResult.k_EResultOK || io)
        {
            return;
        }
        for (uint num = 0u; num < callback.m_unNumResultsReturned; num++)
        {
            if (TempSteamworksWorkshop.cacheDetails(callback.m_handle, num, out var cachedDetails))
            {
                string title = cachedDetails.GetTitle();
                ISleekLabel sleekLabel = Glazier.Get().CreateLabel();
                sleekLabel.PositionOffset_X = 5f;
                sleekLabel.PositionOffset_Y = (int)(num * 20);
                sleekLabel.SizeOffset_Y = 30f;
                sleekLabel.SizeScale_X = 1f;
                sleekLabel.TextAlignment = TextAnchor.MiddleLeft;
                sleekLabel.Text = title;
                sleekLabel.TextColor = (cachedDetails.isBannedOrPrivate ? ESleekTint.BAD : ESleekTint.FONT);
                ugcBox.AddChild(sleekLabel);
                ServerInfoViewWorkshopButton serverInfoViewWorkshopButton = new ServerInfoViewWorkshopButton(cachedDetails.fileId, title);
                serverInfoViewWorkshopButton.PositionOffset_X = -45f;
                serverInfoViewWorkshopButton.PositionOffset_Y = sleekLabel.PositionOffset_Y + 5f;
                serverInfoViewWorkshopButton.PositionScale_X = 1f;
                ugcBox.AddChild(serverInfoViewWorkshopButton);
                SleekWorkshopSubscriptionButton sleekWorkshopSubscriptionButton = new SleekWorkshopSubscriptionButton();
                sleekWorkshopSubscriptionButton.PositionOffset_X = -25f;
                sleekWorkshopSubscriptionButton.PositionOffset_Y = sleekLabel.PositionOffset_Y + 5f;
                sleekWorkshopSubscriptionButton.PositionScale_X = 1f;
                sleekWorkshopSubscriptionButton.SizeOffset_X = 20f;
                sleekWorkshopSubscriptionButton.SizeOffset_Y = 20f;
                sleekWorkshopSubscriptionButton.subscribeText = localization.format("Subscribe");
                sleekWorkshopSubscriptionButton.unsubscribeText = localization.format("Unsubscribe");
                sleekWorkshopSubscriptionButton.subscribeTooltip = localization.format("Subscribe_Tooltip", title);
                sleekWorkshopSubscriptionButton.unsubscribeTooltip = localization.format("Unsubscribe_Tooltip", title);
                sleekWorkshopSubscriptionButton.fileId = cachedDetails.fileId;
                sleekWorkshopSubscriptionButton.synchronizeText();
                ugcBox.AddChild(sleekWorkshopSubscriptionButton);
            }
        }
        ugcBox.SizeOffset_Y = (int)(callback.m_unNumResultsReturned * 20 + 10);
        ugcTitle.IsVisible = true;
        ugcBox.IsVisible = true;
        updateDetails();
    }

    public static string GetClipboardData()
    {
        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.AppendLine("Name: " + serverInfo.name);
        stringBuilder.AppendLine("Description: " + serverInfo.descText);
        stringBuilder.AppendLine("Thumbnail: " + serverInfo.thumbnailURL);
        stringBuilder.AppendLine("Address: " + Parser.getIPFromUInt32(serverInfo.ip));
        stringBuilder.AppendLine($"Connection Port: {serverInfo.connectionPort}");
        stringBuilder.AppendLine($"Query Port: {serverInfo.queryPort}");
        stringBuilder.AppendLine($"SteamId: {serverInfo.steamID} ({serverInfo.steamID.GetEAccountType()})");
        stringBuilder.AppendLine($"Ping: {serverInfo.ping}ms");
        if (expectedWorkshopItems == null)
        {
            stringBuilder.AppendLine("Workshop files unknown");
        }
        else
        {
            stringBuilder.AppendLine($"{expectedWorkshopItems.Count} workshop file(s):");
            for (int i = 0; i < expectedWorkshopItems.Count; i++)
            {
                stringBuilder.AppendLine($"{i}: {expectedWorkshopItems[i]}");
            }
        }
        return stringBuilder.ToString();
    }

    public static void OpenWithoutRefresh()
    {
        if (!active)
        {
            active = true;
            container.AnimateIntoView();
        }
    }

    public static void open(SteamServerAdvertisement newServerInfo, string newServerPassword, EServerInfoOpenContext newOpenContext)
    {
        if (active)
        {
            return;
        }
        active = true;
        openContext = newOpenContext;
        serverInfo = newServerInfo;
        serverPassword = newServerPassword;
        expectedWorkshopItems = null;
        linkUrls = null;
        IPv4Address pv4Address = new IPv4Address(serverInfo.ip);
        serverBookmarkHost = null;
        bool flag = !serverInfo.steamID.BPersistentGameServerAccount() && pv4Address.IsWideAreaNetwork;
        flag &= serverInfo.infoSource != SteamServerAdvertisement.EInfoSource.LanServerList;
        flag &= !LiveConfig.Get().shouldAllowJoiningInternetServersWithoutGslt;
        if (flag)
        {
            UnturnedLog.info($"{serverInfo.name} is not logged in ({serverInfo.steamID}) and IP ({pv4Address}) is WAN");
        }
        notLoggedInWarningButton.IsVisible = flag;
        banFlags = HostBansManager.Get().MatchBasicDetails(pv4Address, serverInfo.queryPort, serverInfo.name, serverInfo.steamID.m_SteamID);
        if (banFlags == EHostBanFlags.None)
        {
            banFlags = HostBansManager.Get().MatchExtendedDetails(serverInfo.descText, serverInfo.thumbnailURL);
        }
        UnturnedLog.info($"{serverInfo.name} host ban flags: {banFlags}");
        bool flag2 = banFlags.HasFlag(EHostBanFlags.Blocked);
        hostBanWarningButton.IsVisible = false;
        hostBanWarningButton.Text = string.Empty;
        if (banFlags.HasFlag(EHostBanFlags.MonetizationWarning))
        {
            hostBanWarningButton.IsVisible = true;
            hostBanWarningButton.Text += localization.format("HostBan_MonetizationWarning");
        }
        if (banFlags.HasFlag(EHostBanFlags.WorkshopWarning))
        {
            if (hostBanWarningButton.IsVisible)
            {
                hostBanWarningButton.Text += "\n";
            }
            else
            {
                hostBanWarningButton.IsVisible = true;
            }
            hostBanWarningButton.Text += localization.format("HostBan_WorkshopWarning");
        }
        if (banFlags.HasFlag(EHostBanFlags.IncorrectMonetizationTagWarning))
        {
            if (hostBanWarningButton.IsVisible)
            {
                hostBanWarningButton.Text += "\n";
            }
            else
            {
                hostBanWarningButton.IsVisible = true;
            }
            hostBanWarningButton.Text += localization.format("HostBan_IncorrectMonetizationTagWarning");
        }
        if (banFlags.HasFlag(EHostBanFlags.HostingProvider))
        {
            if (hostBanWarningButton.IsVisible)
            {
                hostBanWarningButton.Text += "\n";
            }
            else
            {
                hostBanWarningButton.IsVisible = true;
            }
            hostBanWarningButton.Text += localization.format("HostBan_HostingProvider");
        }
        if (banFlags.HasFlag(EHostBanFlags.Apology))
        {
            if (hostBanWarningButton.IsVisible)
            {
                hostBanWarningButton.Text += "\n";
            }
            else
            {
                hostBanWarningButton.IsVisible = true;
            }
            hostBanWarningButton.Text += localization.format("HostBan_Apology");
            hostBanWarningButton.TextColor = ESleekTint.FONT;
            hostBanWarningButton.IsRaycastTarget = false;
        }
        else
        {
            hostBanWarningButton.TextColor = ESleekTint.BAD;
            hostBanWarningButton.IsRaycastTarget = true;
        }
        if (flag)
        {
            joinButton.IsVisible = false;
            joinDisabledBox.IsVisible = true;
            joinDisabledBox.Text = localization.format("NotLoggedInBlock_Label");
            joinDisabledBox.TooltipText = localization.format("NotLoggedInBlock_Tooltip");
        }
        else if (flag2)
        {
            joinButton.IsVisible = false;
            joinDisabledBox.IsVisible = true;
            joinDisabledBox.Text = localization.format("ServerBlacklisted_Label");
            joinDisabledBox.TooltipText = localization.format("ServerBlacklisted_Tooltip");
        }
        else
        {
            joinButton.IsVisible = true;
            joinDisabledBox.IsVisible = false;
        }
        reset();
        serverFavorited = Provider.GetServerIsFavorited(serverInfo.ip, serverInfo.queryPort);
        updateFavorite();
        bookmarkDetails = ServerBookmarksManager.FindBookmarkDetails(serverInfo);
        UpdateBookmarkButton();
        updatePlayers();
        Provider.provider.matchmakingService.refreshPlayers(serverInfo.ip, serverInfo.queryPort);
        Provider.provider.matchmakingService.refreshPlayers(serverInfo.ip, serverInfo.queryPort);
        updateRules();
        Provider.provider.matchmakingService.refreshRules(serverInfo.ip, serverInfo.queryPort);
        updateServerInfo();
        UpdateVisibleButtons();
        container.AnimateIntoView();
    }

    public static void close()
    {
        if (active)
        {
            active = false;
            container.AnimateOutOfView(0f, 1f);
        }
    }

    private static void onClickedJoinButton(ISleekElement button)
    {
        IPv4Address address = new IPv4Address(serverInfo.ip);
        if (serverInfo.isPassworded && string.IsNullOrEmpty(serverPassword))
        {
            MenuServerPasswordUI.open(serverInfo, expectedWorkshopItems);
            close();
        }
        else
        {
            Provider.connect(new ServerConnectParameters(address, serverInfo.queryPort, serverInfo.connectionPort, serverPassword), serverInfo, expectedWorkshopItems);
        }
    }

    private static void onClickedFavoriteButton(ISleekElement button)
    {
        serverFavorited = !serverFavorited;
        Provider.SetServerIsFavorited(serverInfo.ip, serverInfo.connectionPort, serverInfo.queryPort, serverFavorited);
        updateFavorite();
    }

    private static void OnClickedBookmarkButton(ISleekElement button)
    {
        if (bookmarkDetails != null)
        {
            bookmarkDetails.isBookmarked = !bookmarkDetails.isBookmarked;
            if (bookmarkDetails.isBookmarked)
            {
                ServerBookmarksManager.AddBookmark(bookmarkDetails);
            }
            else
            {
                ServerBookmarksManager.RemoveBookmark(serverInfo.steamID);
            }
        }
        else
        {
            bookmarkDetails = ServerBookmarksManager.AddBookmark(serverInfo, serverBookmarkHost);
        }
        UpdateBookmarkButton();
    }

    private static void onClickedRefreshButton(ISleekElement button)
    {
        updatePlayers();
        Provider.provider.matchmakingService.refreshPlayers(serverInfo.ip, serverInfo.queryPort);
    }

    private static void onClickedCancelButton(ISleekElement button)
    {
        switch (openContext)
        {
        case EServerInfoOpenContext.CONNECT:
            MenuPlayConnectUI.open();
            break;
        case EServerInfoOpenContext.SERVERS:
            MenuPlayUI.serverListUI.open(shouldRefresh: false);
            break;
        case EServerInfoOpenContext.BOOKMARKS:
            MenuPlayUI.serverBookmarksUI.open();
            break;
        }
        close();
    }

    private static void onMasterServerQueryRefreshed(SteamServerAdvertisement server)
    {
        serverInfo = server;
        updateServerInfo();
    }

    private static void reset()
    {
        titleDescriptionLabel.Text = string.Empty;
        titleIconImage.Clear();
        serverDescriptionBox.Text = string.Empty;
        titleCurationLabelsLabel.Text = string.Empty;
    }

    private static void updateServerInfo()
    {
        titleNameLabel.TextColor = (serverInfo.isPro ? new SleekColor(Palette.PRO) : new SleekColor(ESleekTint.FONT));
        titleNameLabel.Text = serverInfo.name;
        titleCurationLabelsLabel.Text = serverInfo.serverCurationLabels;
        int num = 0;
        serverWorkshopLabel.Text = localization.format("Workshop", localization.format(serverInfo.isWorkshop ? "Yes" : "No"));
        num += 20;
        serverCombatLabel.Text = localization.format("Combat", localization.format(serverInfo.isPvP ? "PvP" : "PvE"));
        num += 20;
        string text = serverInfo.cameraMode switch
        {
            ECameraMode.FIRST => localization.format("First"), 
            ECameraMode.THIRD => localization.format("Third"), 
            ECameraMode.BOTH => localization.format("Both"), 
            ECameraMode.VEHICLE => localization.format("Vehicle"), 
            _ => string.Empty, 
        };
        serverPerspectiveLabel.Text = localization.format("Perspective", text);
        serverPerspectiveLabel.IsVisible = !string.IsNullOrEmpty(text);
        num += (serverPerspectiveLabel.IsVisible ? 20 : 0);
        string text2 = ((!serverInfo.IsVACSecure) ? localization.format("VAC_Insecure") : localization.format("VAC_Secure"));
        text2 = ((!serverInfo.IsBattlEyeSecure) ? (text2 + " + " + localization.format("BattlEye_Insecure")) : (text2 + " + " + localization.format("BattlEye_Secure")));
        serverSecurityLabel.PositionOffset_Y = num;
        serverSecurityLabel.Text = localization.format("Security", text2);
        num += 20;
        string arg = serverInfo.mode switch
        {
            EGameMode.EASY => localization.format("Easy"), 
            EGameMode.NORMAL => localization.format("Normal"), 
            EGameMode.HARD => localization.format("Hard"), 
            _ => string.Empty, 
        };
        serverModeLabel.PositionOffset_Y = num;
        serverModeLabel.Text = localization.format("Mode", arg);
        num += 20;
        serverCheatsLabel.PositionOffset_Y = num;
        serverCheatsLabel.Text = localization.format("Cheats", localization.format(serverInfo.hasCheats ? "Yes" : "No"));
        num += 20;
        if (serverInfo.monetization != 0)
        {
            serverMonetizationLabel.IsVisible = true;
            serverMonetizationLabel.PositionOffset_Y = num;
            switch (serverInfo.monetization)
            {
            case EServerMonetizationTag.None:
                serverMonetizationLabel.Text = localization.format("Monetization_None");
                break;
            case EServerMonetizationTag.NonGameplay:
                serverMonetizationLabel.Text = localization.format("Monetization_NonGameplay");
                break;
            case EServerMonetizationTag.Monetized:
                serverMonetizationLabel.Text = localization.format("Monetization_Monetized");
                break;
            default:
                serverMonetizationLabel.Text = "unknown: " + serverInfo.monetization;
                break;
            }
            num += 20;
        }
        else
        {
            serverMonetizationLabel.IsVisible = false;
        }
        serverPingLabel.Text = localization.format("QueryPing", serverInfo.ping);
        serverPingLabel.PositionOffset_Y = num;
        num += 20;
        if (banFlags.HasFlag(EHostBanFlags.QueryPingWarning))
        {
            serverPingLabel.Text += " - ";
            serverPingLabel.Text += localization.format("HostBan_QueryPingWarning");
            serverPingLabel.TextColor = ESleekTint.BAD;
        }
        else
        {
            serverPingLabel.TextColor = ESleekTint.FONT;
        }
        serverBox.SizeOffset_Y = num + 10;
        updateDetails();
        LevelInfo level = Level.getLevel(serverInfo.map);
        if (level != null)
        {
            Local local = level.getLocalization();
            if (local != null)
            {
                string desc = local.format("Description");
                desc = ItemTool.filterRarityRichText(desc);
                RichTextUtil.replaceNewlineMarkup(ref desc);
                mapDescriptionBox.Text = desc;
            }
            if (local != null && local.has("Name"))
            {
                mapNameBox.Text = localization.format("Map", local.format("Name"));
            }
            else
            {
                mapNameBox.Text = localization.format("Map", serverInfo.map);
            }
            string previewImageFilePath = level.GetPreviewImageFilePath();
            if (!string.IsNullOrEmpty(previewImageFilePath))
            {
                mapPreviewImage.SetTextureAndShouldDestroy(ReadWrite.readTextureFromFile(previewImageFilePath), shouldDestroyTexture: true);
            }
        }
        else
        {
            mapDescriptionBox.Text = string.Empty;
            mapNameBox.Text = serverInfo.map;
            mapPreviewImage.SetTextureAndShouldDestroy(null, shouldDestroyTexture: true);
        }
    }

    private static void updateFavorite()
    {
        favoriteButton.IsVisible = !serverInfo.IsAddressUsingSteamFakeIP();
        if (serverFavorited)
        {
            favoriteButton.Text = localization.format("Favorite_Off_Button");
        }
        else
        {
            favoriteButton.Text = localization.format("Favorite_On_Button");
        }
    }

    private static void UpdateBookmarkButton()
    {
        bookmarkButton.IsVisible = serverInfo.steamID.BPersistentGameServerAccount() && !string.IsNullOrEmpty(serverBookmarkHost);
        if (bookmarkDetails != null && bookmarkDetails.isBookmarked)
        {
            bookmarkButton.text = localization.format("Bookmark_Off_Button");
            bookmarkButton.icon = MenuPlayUI.serverListUI.icons.load<Texture2D>("Bookmark_Remove");
        }
        else
        {
            bookmarkButton.text = localization.format("Bookmark_On_Button");
            bookmarkButton.icon = MenuPlayUI.serverListUI.icons.load<Texture2D>("Bookmark_Add");
        }
    }

    private static void updatePlayers()
    {
        playersScrollBox.RemoveAllChildren();
        playersOffset = 0;
        playerCount = 0;
        playerCountBox.Text = localization.format("Players", playerCount, serverInfo.maxPlayers);
    }

    private static void onPlayersQueryRefreshed(string name, int score, float time)
    {
        TimeSpan timeSpan = TimeSpan.FromSeconds(time);
        string text = string.Empty;
        if (timeSpan.Days > 0)
        {
            text = text + " " + timeSpan.Days + "d";
        }
        if (timeSpan.Hours > 0)
        {
            text = text + " " + timeSpan.Hours + "h";
        }
        if (timeSpan.Minutes > 0)
        {
            text = text + " " + timeSpan.Minutes + "m";
        }
        if (timeSpan.Seconds > 0)
        {
            text = text + " " + timeSpan.Seconds + "s";
        }
        ISleekBox sleekBox = Glazier.Get().CreateBox();
        sleekBox.PositionOffset_Y = playersOffset;
        sleekBox.SizeOffset_Y = 30f;
        sleekBox.SizeScale_X = 1f;
        playersScrollBox.AddChild(sleekBox);
        ISleekLabel sleekLabel = Glazier.Get().CreateLabel();
        sleekLabel.PositionOffset_X = 5f;
        sleekLabel.SizeOffset_X = -10f;
        sleekLabel.SizeScale_X = 1f;
        sleekLabel.SizeScale_Y = 1f;
        sleekLabel.TextAlignment = TextAnchor.MiddleLeft;
        sleekLabel.Text = name;
        sleekBox.AddChild(sleekLabel);
        ISleekLabel sleekLabel2 = Glazier.Get().CreateLabel();
        sleekLabel2.PositionOffset_X = -5f;
        sleekLabel2.SizeOffset_X = -10f;
        sleekLabel2.SizeScale_X = 1f;
        sleekLabel2.SizeScale_Y = 1f;
        sleekLabel2.TextAlignment = TextAnchor.MiddleRight;
        sleekLabel2.Text = text;
        sleekBox.AddChild(sleekLabel2);
        playersOffset += 40;
        playersScrollBox.ContentSizeOffset = new Vector2(0f, playersOffset - 10);
        playerCount++;
        playerCountBox.Text = localization.format("Players", playerCount, serverInfo.maxPlayers);
    }

    private static void updateRules()
    {
        linksFrame.RemoveAllChildren();
        linksFrame.IsVisible = false;
        ugcTitle.IsVisible = false;
        ugcBox.RemoveAllChildren();
        ugcBox.IsVisible = false;
        configTitle.IsVisible = false;
        configBox.RemoveAllChildren();
        configBox.IsVisible = false;
        rocketTitle.IsVisible = false;
        rocketBox.RemoveAllChildren();
        rocketBox.IsVisible = false;
        updateDetails();
    }

    private static void onRulesQueryRefreshed(Dictionary<string, string> rulesMap)
    {
        if (rulesMap == null)
        {
            return;
        }
        if (rulesMap.TryGetValue("Browser_Icon", out var value) && !string.IsNullOrEmpty(value))
        {
            titleIconImage.Refresh(value);
        }
        if (rulesMap.TryGetValue("Browser_Desc_Hint", out var value2) && !string.IsNullOrEmpty(value2))
        {
            ProfanityFilter.ApplyFilter(OptionsSettings.filter, ref value2);
            titleDescriptionLabel.Text = value2;
        }
        if (rulesMap.TryGetValue("BookmarkHost", out var value3))
        {
            serverBookmarkHost = value3;
            if (bookmarkDetails != null && !string.IsNullOrEmpty(serverBookmarkHost))
            {
                bookmarkDetails.host = serverBookmarkHost;
                ServerBookmarksManager.MarkDirty();
            }
            UpdateBookmarkButton();
            UpdateVisibleButtons();
        }
        if (rulesMap.TryGetValue("Browser_Desc_Full_Count", out var value4) && int.TryParse(value4, NumberStyles.Any, CultureInfo.InvariantCulture, out var result) && result > 0)
        {
            string text = string.Empty;
            for (int i = 0; i < result; i++)
            {
                if (rulesMap.TryGetValue("Browser_Desc_Full_Line_" + i, out var value5))
                {
                    text += value5;
                }
            }
            if (ConvertEx.TryDecodeBase64AsUtf8String(text, out var output))
            {
                if (!string.IsNullOrEmpty(output))
                {
                    ProfanityFilter.ApplyFilter(OptionsSettings.filter, ref output);
                    RichTextUtil.replaceNewlineMarkup(ref output);
                    serverDescriptionBox.Text = output;
                }
            }
            else
            {
                UnturnedLog.error("Unable to convert server browser Base64 string: \"" + text + "\"");
            }
        }
        linkUrls = new List<string>();
        if (rulesMap.TryGetValue("Custom_Links_Count", out var value6) && int.TryParse(value6, NumberStyles.Any, CultureInfo.InvariantCulture, out var result2) && result2 > 0)
        {
            int num = 0;
            for (int j = 0; j < result2; j++)
            {
                if (!rulesMap.TryGetValue("Custom_Link_Message_" + j, out var value7))
                {
                    UnturnedLog.warn("Skipping link index {0} because message is missing", j);
                    continue;
                }
                if (string.IsNullOrEmpty(value7))
                {
                    UnturnedLog.warn("Skipping link index {0} because message is empty", j);
                    continue;
                }
                if (!rulesMap.TryGetValue("Custom_Link_Url_" + j, out var value8))
                {
                    UnturnedLog.warn("Skipping link index {0} because url is missing", j);
                    continue;
                }
                if (string.IsNullOrEmpty(value8))
                {
                    UnturnedLog.warn("Skipping link index {0} because url is empty", j);
                    continue;
                }
                if (!ConvertEx.TryDecodeBase64AsUtf8String(value7, out var output2))
                {
                    UnturnedLog.warn("Skipping link index {0} because unable to decode message Base64: \"{1}\"", j, value7);
                    continue;
                }
                if (!ConvertEx.TryDecodeBase64AsUtf8String(value8, out var output3))
                {
                    UnturnedLog.warn("Skipping link index {0} because unable to decode url Base64: \"{1}\"", j, value8);
                    continue;
                }
                if (!WebUtils.ParseThirdPartyUrl(output3, out var result3))
                {
                    UnturnedLog.warn("Ignoring potentially unsafe link index {0} url {1}", j, output3);
                    continue;
                }
                ProfanityFilter.ApplyFilter(OptionsSettings.filter, ref output2);
                linkUrls.Add(result3);
                ISleekButton sleekButton = Glazier.Get().CreateButton();
                sleekButton.PositionOffset_Y += num;
                sleekButton.SizeScale_X = 1f;
                sleekButton.SizeOffset_Y = 30f;
                sleekButton.AllowRichText = true;
                sleekButton.Text = output2;
                sleekButton.TooltipText = output3;
                sleekButton.TextColor = ESleekTint.RICH_TEXT_DEFAULT;
                sleekButton.OnClicked += OnClickedLinkButton;
                linksFrame.AddChild(sleekButton);
                num += 30;
            }
            if (num > 0)
            {
                linksFrame.SizeOffset_Y = num;
                linksFrame.IsVisible = true;
            }
        }
        if (rulesMap.TryGetValue("rocketplugins", out var value9) && !string.IsNullOrEmpty(value9))
        {
            string[] array = value9.Split(',');
            rocketBox.SizeOffset_Y = array.Length * 20 + 10;
            for (int k = 0; k < array.Length; k++)
            {
                ISleekLabel sleekLabel = Glazier.Get().CreateLabel();
                sleekLabel.PositionOffset_X = 5f;
                sleekLabel.PositionOffset_Y = k * 20;
                sleekLabel.SizeOffset_Y = 30f;
                sleekLabel.SizeScale_X = 1f;
                sleekLabel.TextAlignment = TextAnchor.MiddleLeft;
                sleekLabel.Text = array[k];
                rocketBox.AddChild(sleekLabel);
            }
            if (serverInfo.pluginFramework == SteamServerAdvertisement.EPluginFramework.Rocket)
            {
                rocketTitle.Text = localization.format("Plugins_Rocket");
            }
            else if (serverInfo.pluginFramework == SteamServerAdvertisement.EPluginFramework.OpenMod)
            {
                rocketTitle.Text = localization.format("Plugins_OpenMod");
            }
            else
            {
                rocketTitle.Text = localization.format("Plugins_Unknown");
            }
            rocketTitle.IsVisible = true;
            rocketBox.IsVisible = true;
        }
        expectedWorkshopItems = new List<PublishedFileId_t>(0);
        if (rulesMap.TryGetValue("Mod_Count", out var value10) && int.TryParse(value10, NumberStyles.Any, CultureInfo.InvariantCulture, out var result4) && result4 > 0)
        {
            string text2 = string.Empty;
            for (int l = 0; l < result4; l++)
            {
                if (rulesMap.TryGetValue("Mod_" + l, out var value11))
                {
                    text2 += value11;
                }
            }
            string[] array2 = text2.Split(',');
            expectedWorkshopItems = new List<PublishedFileId_t>(array2.Length);
            for (int m = 0; m < array2.Length; m++)
            {
                if (ulong.TryParse(array2[m], NumberStyles.Any, CultureInfo.InvariantCulture, out var result5))
                {
                    expectedWorkshopItems.Add(new PublishedFileId_t(result5));
                }
            }
            detailsHandle = SteamUGC.CreateQueryUGCDetailsRequest(expectedWorkshopItems.ToArray(), (uint)expectedWorkshopItems.Count);
            SteamUGC.SetAllowCachedResponse(detailsHandle, 60u);
            SteamAPICall_t hAPICall = SteamUGC.SendQueryUGCRequest(detailsHandle);
            ugcQueryCompleted.Set(hAPICall);
        }
        if (rulesMap.TryGetValue("Cfg_Count", out var value12) && int.TryParse(value12, NumberStyles.Any, CultureInfo.InvariantCulture, out var result6) && result6 > 0)
        {
            int num2 = 0;
            for (int n = 0; n < result6; n++)
            {
                if (!rulesMap.TryGetValue("Cfg_" + n.ToString(CultureInfo.InvariantCulture), out var value13))
                {
                    continue;
                }
                int num3 = value13.IndexOf('.');
                int num4 = value13.IndexOf('=', num3 + 1);
                if (num3 >= 0 && num4 >= 0)
                {
                    string fieldName = value13.Substring(0, num3);
                    string fieldName2 = value13.Substring(num3 + 1, num4 - num3 - 1);
                    string text3 = value13.Substring(num4 + 1);
                    string text4 = null;
                    float result7;
                    int result8;
                    if (text3 == "T")
                    {
                        text4 = localization.format("Yes");
                    }
                    else if (text3 == "F")
                    {
                        text4 = localization.format("No");
                    }
                    else if (float.TryParse(text3, NumberStyles.Any, CultureInfo.InvariantCulture, out result7))
                    {
                        text4 = result7.ToString();
                    }
                    else if (int.TryParse(text3, NumberStyles.Any, CultureInfo.InvariantCulture, out result8))
                    {
                        text4 = result8.ToString();
                    }
                    if (string.IsNullOrEmpty(text4))
                    {
                        ISleekLabel sleekLabel2 = Glazier.Get().CreateLabel();
                        sleekLabel2.PositionOffset_X = 5f;
                        sleekLabel2.PositionOffset_Y = num2;
                        sleekLabel2.SizeOffset_X = -10f;
                        sleekLabel2.SizeOffset_Y = 30f;
                        sleekLabel2.SizeScale_X = 1f;
                        sleekLabel2.TextAlignment = TextAnchor.MiddleLeft;
                        sleekLabel2.TextColor = Color.red;
                        sleekLabel2.Text = value13;
                        configBox.AddChild(sleekLabel2);
                    }
                    else
                    {
                        ISleekLabel sleekLabel3 = Glazier.Get().CreateLabel();
                        sleekLabel3.PositionOffset_X = 5f;
                        sleekLabel3.PositionOffset_Y = num2;
                        sleekLabel3.SizeOffset_X = -5f;
                        sleekLabel3.SizeOffset_Y = 30f;
                        sleekLabel3.SizeScale_X = 0.25f;
                        sleekLabel3.TextAlignment = TextAnchor.MiddleRight;
                        sleekLabel3.Text = MenuPlayConfigUI.sanitizeName(fieldName);
                        sleekLabel3.TextColor = new SleekColor(ESleekTint.FONT, 0.5f);
                        configBox.AddChild(sleekLabel3);
                        ISleekLabel sleekLabel4 = Glazier.Get().CreateLabel();
                        sleekLabel4.PositionOffset_X = 5f;
                        sleekLabel4.PositionOffset_Y = num2;
                        sleekLabel4.PositionScale_X = 0.25f;
                        sleekLabel4.SizeOffset_X = -5f;
                        sleekLabel4.SizeOffset_Y = 30f;
                        sleekLabel4.SizeScale_X = 0.75f;
                        sleekLabel4.TextAlignment = TextAnchor.MiddleLeft;
                        sleekLabel4.Text = localization.format("Rule", MenuPlayConfigUI.sanitizeName(fieldName2), text4);
                        configBox.AddChild(sleekLabel4);
                    }
                    num2 += 20;
                }
            }
            configBox.SizeOffset_Y = num2 + 10;
            if (num2 > 0)
            {
                configTitle.IsVisible = true;
                configBox.IsVisible = true;
            }
        }
        if (rulesMap.TryGetValue("GameVersion", out var value14) && Parser.TryGetUInt32FromIP(value14, out var value15) && Provider.APP_VERSION_PACKED != value15)
        {
            joinButton.IsVisible = false;
            joinDisabledBox.IsVisible = true;
            if (value15 > Provider.APP_VERSION_PACKED)
            {
                joinDisabledBox.Text = localization.format("ServerNewerVersion_Label", value14);
                joinDisabledBox.TooltipText = localization.format("ServerNewerVersion_Tooltip");
            }
            else
            {
                joinDisabledBox.Text = localization.format("ServerOlderVersion_Label", value14);
                joinDisabledBox.TooltipText = localization.format("ServerOlderVersion_Tooltip");
            }
        }
        updateDetails();
    }

    private static void updateDetails()
    {
        float num = 0f;
        if (hostBanWarningButton.IsVisible)
        {
            hostBanWarningButton.PositionOffset_X = num;
            num += hostBanWarningButton.SizeOffset_Y + 10f;
        }
        if (notLoggedInWarningButton.IsVisible)
        {
            notLoggedInWarningButton.PositionOffset_X = num;
            num += notLoggedInWarningButton.SizeOffset_Y + 10f;
        }
        if (linksFrame.IsVisible)
        {
            linksFrame.PositionOffset_Y = num;
            num += linksFrame.SizeOffset_Y + 10f;
        }
        if (serverTitle.IsVisible)
        {
            serverTitle.PositionOffset_Y = num;
            num += 40f;
        }
        if (serverBox.IsVisible)
        {
            serverBox.PositionOffset_Y = num;
            num += serverBox.SizeOffset_Y + 10f;
        }
        if (ugcTitle.IsVisible)
        {
            ugcTitle.PositionOffset_Y = num;
            num += ugcTitle.SizeOffset_Y + 10f;
        }
        if (ugcBox.IsVisible)
        {
            ugcBox.PositionOffset_Y = num;
            num += ugcBox.SizeOffset_Y + 10f;
        }
        if (configTitle.IsVisible)
        {
            configTitle.PositionOffset_Y = num;
            num += configTitle.SizeOffset_Y + 10f;
        }
        if (configBox.IsVisible)
        {
            configBox.PositionOffset_Y = num;
            num += configBox.SizeOffset_Y + 10f;
        }
        if (rocketTitle.IsVisible)
        {
            rocketTitle.PositionOffset_Y = num;
            num += rocketTitle.SizeOffset_Y + 10f;
        }
        if (rocketBox.IsVisible)
        {
            rocketBox.PositionOffset_Y = num;
            num += rocketBox.SizeOffset_Y + 10f;
        }
        detailsScrollBox.ContentSizeOffset = new Vector2(0f, num - 10f);
    }

    /// <summary>
    /// Adjusts width and spacing of buttons along the bottom of the screen.
    /// Favorite and bookmark buttons can be hidden depending whether the necessary server details are set.
    /// </summary>
    private static void UpdateVisibleButtons()
    {
        int num = 3;
        if (favoriteButton.IsVisible)
        {
            num++;
        }
        if (bookmarkButton.IsVisible)
        {
            num++;
        }
        float num2 = 1f / (float)num;
        joinButton.SizeScale_X = num2;
        joinDisabledBox.SizeScale_X = num2;
        float num3 = num2;
        if (favoriteButton.IsVisible)
        {
            favoriteButton.PositionScale_X = num3;
            favoriteButton.SizeScale_X = num2;
            num3 += num2;
        }
        if (bookmarkButton.IsVisible)
        {
            bookmarkButton.PositionScale_X = num3;
            bookmarkButton.SizeScale_X = num2;
            num3 += num2;
        }
        refreshButton.PositionScale_X = num3;
        refreshButton.SizeScale_X = num2;
        cancelButton.PositionScale_X = 1f - num2;
        cancelButton.SizeScale_X = num2;
    }

    private static void OnClickedLinkButton(ISleekElement button)
    {
        int index = linksFrame.FindIndexOfChild(button);
        Provider.openURL(linkUrls[index]);
    }

    private static void OnClickedHostBanWarning(ISleekElement button)
    {
        Provider.openURL("https://docs.smartlydressedgames.com/en/stable/servers/server-hosting-rules.html");
    }

    private static void OnClickedNotLoggedInWarning(ISleekElement button)
    {
        Provider.openURL("https://docs.smartlydressedgames.com/en/stable/servers/game-server-login-tokens.html");
    }

    public void OnDestroy()
    {
        TempSteamworksMatchmaking matchmakingService = Provider.provider.matchmakingService;
        matchmakingService.onMasterServerQueryRefreshed = (TempSteamworksMatchmaking.MasterServerQueryRefreshed)Delegate.Remove(matchmakingService.onMasterServerQueryRefreshed, new TempSteamworksMatchmaking.MasterServerQueryRefreshed(onMasterServerQueryRefreshed));
        TempSteamworksMatchmaking matchmakingService2 = Provider.provider.matchmakingService;
        matchmakingService2.onPlayersQueryRefreshed = (TempSteamworksMatchmaking.PlayersQueryRefreshed)Delegate.Remove(matchmakingService2.onPlayersQueryRefreshed, new TempSteamworksMatchmaking.PlayersQueryRefreshed(onPlayersQueryRefreshed));
        TempSteamworksMatchmaking matchmakingService3 = Provider.provider.matchmakingService;
        matchmakingService3.onRulesQueryRefreshed = (TempSteamworksMatchmaking.RulesQueryRefreshed)Delegate.Remove(matchmakingService3.onRulesQueryRefreshed, new TempSteamworksMatchmaking.RulesQueryRefreshed(onRulesQueryRefreshed));
        if (ugcQueryCompleted != null)
        {
            ugcQueryCompleted.Dispose();
            ugcQueryCompleted = null;
        }
    }

    public MenuPlayServerInfoUI()
    {
        localization = Localization.read("/Menu/Play/MenuPlayServerInfo.dat");
        container = new SleekFullscreenBox();
        container.PositionOffset_X = 10f;
        container.PositionOffset_Y = 10f;
        container.PositionScale_Y = 1f;
        container.SizeOffset_X = -20f;
        container.SizeOffset_Y = -20f;
        container.SizeScale_X = 1f;
        container.SizeScale_Y = 1f;
        MenuUI.container.AddChild(container);
        active = false;
        infoContainer = Glazier.Get().CreateFrame();
        infoContainer.PositionOffset_Y = 94f;
        infoContainer.SizeOffset_Y = -154f;
        infoContainer.SizeScale_X = 1f;
        infoContainer.SizeScale_Y = 1f;
        container.AddChild(infoContainer);
        buttonsContainer = Glazier.Get().CreateFrame();
        buttonsContainer.PositionOffset_Y = -50f;
        buttonsContainer.PositionScale_Y = 1f;
        buttonsContainer.SizeOffset_Y = 50f;
        buttonsContainer.SizeScale_X = 1f;
        container.AddChild(buttonsContainer);
        playersContainer = Glazier.Get().CreateFrame();
        playersContainer.SizeOffset_X = 280f;
        playersContainer.SizeScale_Y = 1f;
        infoContainer.AddChild(playersContainer);
        detailsContainer = Glazier.Get().CreateFrame();
        detailsContainer.PositionOffset_X = 290f;
        detailsContainer.SizeOffset_X = 0f - detailsContainer.PositionOffset_X - 350f;
        detailsContainer.SizeScale_X = 1f;
        detailsContainer.SizeScale_Y = 1f;
        infoContainer.AddChild(detailsContainer);
        mapContainer = Glazier.Get().CreateFrame();
        mapContainer.PositionOffset_X = -340f;
        mapContainer.PositionScale_X = 1f;
        mapContainer.SizeOffset_X = 340f;
        mapContainer.SizeScale_Y = 1f;
        infoContainer.AddChild(mapContainer);
        titleBox = Glazier.Get().CreateBox();
        titleBox.SizeOffset_Y = 84f;
        titleBox.SizeScale_X = 1f;
        container.AddChild(titleBox);
        titleIconImage = new SleekWebImage();
        titleIconImage.PositionOffset_X = 10f;
        titleIconImage.PositionOffset_Y = 10f;
        titleIconImage.SizeOffset_X = 64f;
        titleIconImage.SizeOffset_Y = 64f;
        titleBox.AddChild(titleIconImage);
        float positionOffset_X = (playersContainer.SizeOffset_X - mapContainer.SizeOffset_X) / 2f;
        titleNameLabel = Glazier.Get().CreateLabel();
        titleNameLabel.PositionOffset_X = positionOffset_X;
        titleNameLabel.PositionOffset_Y = 5f;
        titleNameLabel.SizeOffset_Y = 40f;
        titleNameLabel.SizeScale_X = 1f;
        titleNameLabel.FontSize = ESleekFontSize.Large;
        titleBox.AddChild(titleNameLabel);
        titleDescriptionLabel = Glazier.Get().CreateLabel();
        titleDescriptionLabel.PositionOffset_X = positionOffset_X;
        titleDescriptionLabel.PositionOffset_Y = 35f;
        titleDescriptionLabel.SizeOffset_Y = 34f;
        titleDescriptionLabel.SizeScale_X = 1f;
        titleDescriptionLabel.AllowRichText = true;
        titleDescriptionLabel.TextColor = ESleekTint.RICH_TEXT_DEFAULT;
        titleDescriptionLabel.TextContrastContext = ETextContrastContext.InconspicuousBackdrop;
        titleBox.AddChild(titleDescriptionLabel);
        titleCurationLabelsLabel = Glazier.Get().CreateLabel();
        titleCurationLabelsLabel.PositionOffset_X = positionOffset_X;
        titleCurationLabelsLabel.PositionOffset_Y = 55f;
        titleCurationLabelsLabel.SizeOffset_Y = 34f;
        titleCurationLabelsLabel.SizeScale_X = 1f;
        titleCurationLabelsLabel.AllowRichText = true;
        titleCurationLabelsLabel.TextColor = ESleekTint.RICH_TEXT_DEFAULT;
        titleCurationLabelsLabel.TextContrastContext = ETextContrastContext.InconspicuousBackdrop;
        titleBox.AddChild(titleCurationLabelsLabel);
        playerCountBox = Glazier.Get().CreateBox();
        playerCountBox.SizeScale_X = 1f;
        playerCountBox.SizeOffset_Y = 30f;
        playersContainer.AddChild(playerCountBox);
        playersScrollBox = Glazier.Get().CreateScrollView();
        playersScrollBox.PositionOffset_Y = 40f;
        playersScrollBox.SizeScale_X = 1f;
        playersScrollBox.SizeOffset_Y = -40f;
        playersScrollBox.SizeScale_Y = 1f;
        playersScrollBox.ScaleContentToWidth = true;
        playersContainer.AddChild(playersScrollBox);
        detailsBox = Glazier.Get().CreateBox();
        detailsBox.SizeScale_X = 1f;
        detailsBox.SizeOffset_Y = 30f;
        detailsBox.Text = localization.format("Details");
        detailsContainer.AddChild(detailsBox);
        detailsScrollBox = Glazier.Get().CreateScrollView();
        detailsScrollBox.PositionOffset_Y = 40f;
        detailsScrollBox.SizeScale_X = 1f;
        detailsScrollBox.SizeOffset_Y = -40f;
        detailsScrollBox.SizeScale_Y = 1f;
        detailsScrollBox.ScaleContentToWidth = true;
        detailsContainer.AddChild(detailsScrollBox);
        hostBanWarningButton = Glazier.Get().CreateButton();
        hostBanWarningButton.SizeOffset_Y = 60f;
        hostBanWarningButton.SizeScale_X = 1f;
        hostBanWarningButton.IsVisible = false;
        hostBanWarningButton.OnClicked += OnClickedHostBanWarning;
        detailsScrollBox.AddChild(hostBanWarningButton);
        notLoggedInWarningButton = Glazier.Get().CreateButton();
        notLoggedInWarningButton.SizeOffset_Y = 60f;
        notLoggedInWarningButton.SizeScale_X = 1f;
        notLoggedInWarningButton.IsVisible = false;
        notLoggedInWarningButton.OnClicked += OnClickedNotLoggedInWarning;
        notLoggedInWarningButton.Text += localization.format("NotLoggedInMessage");
        notLoggedInWarningButton.TextColor = ESleekTint.BAD;
        detailsScrollBox.AddChild(notLoggedInWarningButton);
        notLoggedInWarningButton.IsVisible = false;
        linksFrame = Glazier.Get().CreateFrame();
        linksFrame.PositionOffset_Y = 40f;
        linksFrame.SizeScale_X = 1f;
        detailsScrollBox.AddChild(linksFrame);
        serverTitle = Glazier.Get().CreateBox();
        serverTitle.SizeOffset_Y = 30f;
        serverTitle.SizeScale_X = 1f;
        serverTitle.Text = localization.format("Server");
        detailsScrollBox.AddChild(serverTitle);
        serverBox = Glazier.Get().CreateBox();
        serverBox.PositionOffset_Y = 40f;
        serverBox.SizeScale_X = 1f;
        serverBox.SizeOffset_Y = 130f;
        detailsScrollBox.AddChild(serverBox);
        serverWorkshopLabel = Glazier.Get().CreateLabel();
        serverWorkshopLabel.PositionOffset_X = 5f;
        serverWorkshopLabel.PositionOffset_Y = 0f;
        serverWorkshopLabel.SizeOffset_Y = 30f;
        serverWorkshopLabel.SizeScale_X = 1f;
        serverWorkshopLabel.TextAlignment = TextAnchor.MiddleLeft;
        serverBox.AddChild(serverWorkshopLabel);
        serverCombatLabel = Glazier.Get().CreateLabel();
        serverCombatLabel.PositionOffset_X = 5f;
        serverCombatLabel.PositionOffset_Y = 20f;
        serverCombatLabel.SizeOffset_Y = 30f;
        serverCombatLabel.SizeScale_X = 1f;
        serverCombatLabel.TextAlignment = TextAnchor.MiddleLeft;
        serverBox.AddChild(serverCombatLabel);
        serverPerspectiveLabel = Glazier.Get().CreateLabel();
        serverPerspectiveLabel.PositionOffset_X = 5f;
        serverPerspectiveLabel.PositionOffset_Y = 40f;
        serverPerspectiveLabel.SizeOffset_Y = 30f;
        serverPerspectiveLabel.SizeScale_X = 1f;
        serverPerspectiveLabel.TextAlignment = TextAnchor.MiddleLeft;
        serverBox.AddChild(serverPerspectiveLabel);
        serverSecurityLabel = Glazier.Get().CreateLabel();
        serverSecurityLabel.PositionOffset_X = 5f;
        serverSecurityLabel.PositionOffset_Y = 60f;
        serverSecurityLabel.SizeOffset_Y = 30f;
        serverSecurityLabel.SizeScale_X = 1f;
        serverSecurityLabel.TextAlignment = TextAnchor.MiddleLeft;
        serverBox.AddChild(serverSecurityLabel);
        serverModeLabel = Glazier.Get().CreateLabel();
        serverModeLabel.PositionOffset_X = 5f;
        serverModeLabel.PositionOffset_Y = 80f;
        serverModeLabel.SizeOffset_Y = 30f;
        serverModeLabel.SizeScale_X = 1f;
        serverModeLabel.TextAlignment = TextAnchor.MiddleLeft;
        serverBox.AddChild(serverModeLabel);
        serverCheatsLabel = Glazier.Get().CreateLabel();
        serverCheatsLabel.PositionOffset_X = 5f;
        serverCheatsLabel.PositionOffset_Y = 100f;
        serverCheatsLabel.SizeOffset_Y = 30f;
        serverCheatsLabel.SizeScale_X = 1f;
        serverCheatsLabel.TextAlignment = TextAnchor.MiddleLeft;
        serverBox.AddChild(serverCheatsLabel);
        serverMonetizationLabel = Glazier.Get().CreateLabel();
        serverMonetizationLabel.PositionOffset_X = 5f;
        serverMonetizationLabel.PositionOffset_Y = 100f;
        serverMonetizationLabel.SizeOffset_Y = 30f;
        serverMonetizationLabel.SizeScale_X = 1f;
        serverMonetizationLabel.TextAlignment = TextAnchor.MiddleLeft;
        serverBox.AddChild(serverMonetizationLabel);
        serverPingLabel = Glazier.Get().CreateLabel();
        serverPingLabel.PositionOffset_X = 5f;
        serverPingLabel.PositionOffset_Y = 100f;
        serverPingLabel.SizeOffset_Y = 30f;
        serverPingLabel.SizeScale_X = 1f;
        serverPingLabel.TextAlignment = TextAnchor.MiddleLeft;
        serverBox.AddChild(serverPingLabel);
        ugcTitle = Glazier.Get().CreateBox();
        ugcTitle.SizeOffset_Y = 30f;
        ugcTitle.SizeScale_X = 1f;
        ugcTitle.Text = localization.format("UGC");
        detailsScrollBox.AddChild(ugcTitle);
        ugcTitle.IsVisible = false;
        ugcBox = Glazier.Get().CreateBox();
        ugcBox.SizeScale_X = 1f;
        detailsScrollBox.AddChild(ugcBox);
        ugcBox.IsVisible = false;
        configTitle = Glazier.Get().CreateBox();
        configTitle.SizeOffset_Y = 30f;
        configTitle.SizeScale_X = 1f;
        configTitle.Text = localization.format("Config");
        detailsScrollBox.AddChild(configTitle);
        configTitle.IsVisible = false;
        configBox = Glazier.Get().CreateBox();
        configBox.SizeScale_X = 1f;
        detailsScrollBox.AddChild(configBox);
        configBox.IsVisible = false;
        rocketTitle = Glazier.Get().CreateBox();
        rocketTitle.SizeOffset_Y = 30f;
        rocketTitle.SizeScale_X = 1f;
        detailsScrollBox.AddChild(rocketTitle);
        rocketTitle.IsVisible = false;
        rocketBox = Glazier.Get().CreateBox();
        rocketBox.SizeScale_X = 1f;
        detailsScrollBox.AddChild(rocketBox);
        rocketBox.IsVisible = false;
        mapNameBox = Glazier.Get().CreateBox();
        mapNameBox.SizeOffset_X = 340f;
        mapNameBox.SizeOffset_Y = 30f;
        mapContainer.AddChild(mapNameBox);
        mapPreviewBox = Glazier.Get().CreateBox();
        mapPreviewBox.PositionOffset_Y = 40f;
        mapPreviewBox.SizeOffset_X = 340f;
        mapPreviewBox.SizeOffset_Y = 200f;
        mapContainer.AddChild(mapPreviewBox);
        mapPreviewImage = Glazier.Get().CreateImage();
        mapPreviewImage.PositionOffset_X = 10f;
        mapPreviewImage.PositionOffset_Y = 10f;
        mapPreviewImage.SizeOffset_X = -20f;
        mapPreviewImage.SizeOffset_Y = -20f;
        mapPreviewImage.SizeScale_X = 1f;
        mapPreviewImage.SizeScale_Y = 1f;
        mapPreviewBox.AddChild(mapPreviewImage);
        mapDescriptionBox = Glazier.Get().CreateBox();
        mapDescriptionBox.PositionOffset_Y = 250f;
        mapDescriptionBox.SizeOffset_X = 340f;
        mapDescriptionBox.SizeOffset_Y = 140f;
        mapDescriptionBox.TextAlignment = TextAnchor.UpperCenter;
        mapDescriptionBox.AllowRichText = true;
        mapDescriptionBox.TextColor = ESleekTint.RICH_TEXT_DEFAULT;
        mapContainer.AddChild(mapDescriptionBox);
        serverDescriptionBox = Glazier.Get().CreateBox();
        serverDescriptionBox.PositionOffset_Y = 400f;
        serverDescriptionBox.SizeOffset_X = 340f;
        serverDescriptionBox.SizeOffset_Y = -400f;
        serverDescriptionBox.SizeScale_Y = 1f;
        serverDescriptionBox.TextAlignment = TextAnchor.UpperCenter;
        serverDescriptionBox.AllowRichText = true;
        serverDescriptionBox.TextColor = ESleekTint.RICH_TEXT_DEFAULT;
        serverDescriptionBox.TextContrastContext = ETextContrastContext.InconspicuousBackdrop;
        mapContainer.AddChild(serverDescriptionBox);
        joinButton = Glazier.Get().CreateButton();
        joinButton.SizeOffset_X = -5f;
        joinButton.SizeScale_X = 0.2f;
        joinButton.SizeScale_Y = 1f;
        joinButton.Text = localization.format("Join_Button");
        joinButton.TooltipText = localization.format("Join_Button_Tooltip");
        joinButton.OnClicked += onClickedJoinButton;
        joinButton.FontSize = ESleekFontSize.Medium;
        buttonsContainer.AddChild(joinButton);
        joinDisabledBox = Glazier.Get().CreateBox();
        joinDisabledBox.SizeOffset_X = -5f;
        joinDisabledBox.SizeScale_X = 0.2f;
        joinDisabledBox.SizeScale_Y = 1f;
        joinDisabledBox.TextColor = ESleekTint.BAD;
        buttonsContainer.AddChild(joinDisabledBox);
        joinDisabledBox.IsVisible = false;
        favoriteButton = Glazier.Get().CreateButton();
        favoriteButton.PositionOffset_X = 5f;
        favoriteButton.PositionScale_X = 0.2f;
        favoriteButton.SizeOffset_X = -10f;
        favoriteButton.SizeScale_X = 0.2f;
        favoriteButton.SizeScale_Y = 1f;
        favoriteButton.TooltipText = localization.format("Favorite_Button_Tooltip");
        favoriteButton.OnClicked += onClickedFavoriteButton;
        favoriteButton.FontSize = ESleekFontSize.Medium;
        buttonsContainer.AddChild(favoriteButton);
        bookmarkButton = new SleekButtonIcon(null, 40);
        bookmarkButton.PositionOffset_X = 5f;
        bookmarkButton.PositionScale_X = 0.4f;
        bookmarkButton.SizeOffset_X = -10f;
        bookmarkButton.SizeScale_X = 0.2f;
        bookmarkButton.SizeScale_Y = 1f;
        bookmarkButton.tooltip = localization.format("Bookmark_Button_Tooltip");
        bookmarkButton.onClickedButton += OnClickedBookmarkButton;
        bookmarkButton.fontSize = ESleekFontSize.Medium;
        buttonsContainer.AddChild(bookmarkButton);
        refreshButton = Glazier.Get().CreateButton();
        refreshButton.PositionOffset_X = 5f;
        refreshButton.PositionScale_X = 0.6f;
        refreshButton.SizeOffset_X = -10f;
        refreshButton.SizeScale_X = 0.2f;
        refreshButton.SizeScale_Y = 1f;
        refreshButton.Text = localization.format("Refresh_Button");
        refreshButton.TooltipText = localization.format("Refresh_Button_Tooltip");
        refreshButton.OnClicked += onClickedRefreshButton;
        refreshButton.FontSize = ESleekFontSize.Medium;
        buttonsContainer.AddChild(refreshButton);
        cancelButton = Glazier.Get().CreateButton();
        cancelButton.PositionOffset_X = 5f;
        cancelButton.PositionScale_X = 0.8f;
        cancelButton.SizeOffset_X = -5f;
        cancelButton.SizeScale_X = 0.2f;
        cancelButton.SizeScale_Y = 1f;
        cancelButton.Text = localization.format("Cancel_Button");
        cancelButton.TooltipText = localization.format("Cancel_Button_Tooltip");
        cancelButton.OnClicked += onClickedCancelButton;
        cancelButton.FontSize = ESleekFontSize.Medium;
        buttonsContainer.AddChild(cancelButton);
        TempSteamworksMatchmaking matchmakingService = Provider.provider.matchmakingService;
        matchmakingService.onMasterServerQueryRefreshed = (TempSteamworksMatchmaking.MasterServerQueryRefreshed)Delegate.Combine(matchmakingService.onMasterServerQueryRefreshed, new TempSteamworksMatchmaking.MasterServerQueryRefreshed(onMasterServerQueryRefreshed));
        TempSteamworksMatchmaking matchmakingService2 = Provider.provider.matchmakingService;
        matchmakingService2.onPlayersQueryRefreshed = (TempSteamworksMatchmaking.PlayersQueryRefreshed)Delegate.Combine(matchmakingService2.onPlayersQueryRefreshed, new TempSteamworksMatchmaking.PlayersQueryRefreshed(onPlayersQueryRefreshed));
        TempSteamworksMatchmaking matchmakingService3 = Provider.provider.matchmakingService;
        matchmakingService3.onRulesQueryRefreshed = (TempSteamworksMatchmaking.RulesQueryRefreshed)Delegate.Combine(matchmakingService3.onRulesQueryRefreshed, new TempSteamworksMatchmaking.RulesQueryRefreshed(onRulesQueryRefreshed));
        if (ugcQueryCompleted == null)
        {
            ugcQueryCompleted = CallResult<SteamUGCQueryCompleted_t>.Create(onUGCQueryCompleted);
        }
        passwordUI = new MenuServerPasswordUI();
    }
}
