using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using SDG.NetTransport;
using SDG.SteamworksProvider.Services.Store;
using Steamworks;
using UnityEngine;

namespace SDG.Unturned;

public class MenuDashboardUI
{
    public static Local localization;

    public static Bundle icons;

    private static SleekFullscreenBox container;

    public static bool active;

    private static SleekButtonIcon playButton;

    private static SleekButtonIcon survivorsButton;

    private static SleekButtonIcon configurationButton;

    private static SleekButtonIcon workshopButton;

    private static SleekButtonIcon exitButton;

    private static ISleekScrollView mainScrollView;

    private static ISleekButton proButton;

    private static ISleekLabel proLabel;

    private static ISleekLabel featureLabel;

    private static ISleekButton battlEyeButton;

    private static ISleekImage battlEyeIcon;

    private static ISleekLabel battlEyeHeaderLabel;

    private static ISleekLabel battlEyeBodyLabel;

    private static ISleekButton alertBox;

    private static ISleekImage alertImage;

    private static SleekWebImage alertWebImage;

    private static ISleekLabel alertHeaderLabel;

    private static ISleekLabel alertLinkLabel;

    private static ISleekLabel dismissAlertLabel;

    private static ISleekLabel alertBodyLabel;

    private static float mainHeaderOffset;

    private static bool hasCreatedItemStoreButton;

    private static NewsResponse newsResponse;

    /// <summary>
    /// Has a new announcement been posted by the developer?
    /// If so, it is given priority over the featured workshop item.
    /// </summary>
    private static ISleekElement newAnnouncement;

    private static ISleekBox workshopBox;

    private static ISleekElement itemStoreSaleNews;

    private static UGCQueryHandle_t popularWorkshopHandle = UGCQueryHandle_t.Invalid;

    private static UGCQueryHandle_t featuredWorkshopHandle = UGCQueryHandle_t.Invalid;

    /// <summary>
    /// Nelson 2024-04-23: A concerned player raised the issue that mature content could potentially be returned in
    /// popular item results. Steam excludes certain mature content by default, but just in case, we check for these
    /// words and hide if contained in title.
    /// </summary>
    private static string[] featuredWorkshopTitleBannedWords = new string[5] { "drug", "alcohol", "cigarette", "heroin", "cocaine" };

    private static CallResult<SteamUGCQueryCompleted_t> steamUGCQueryCompletedPopular;

    private static CallResult<SteamUGCQueryCompleted_t> steamUGCQueryCompletedFeatured;

    /// <summary>
    /// Ensures workshop files are not refreshed more than once per main menu load.
    /// </summary>
    private static bool hasBegunQueryingLiveConfigWorkshop;

    private MenuPauseUI pauseUI;

    private MenuCreditsUI creditsUI;

    private MenuTitleUI titleUI;

    private MenuPlayUI playUI;

    private MenuSurvivorsUI survivorsUI;

    private MenuConfigurationUI configUI;

    private MenuWorkshopUI workshopUI;

    private const string STEAM_CLAN_IMAGE = "https://clan.cloudflare.steamstatic.com/images/";

    public static void open()
    {
        if (!active)
        {
            active = true;
            container.AnimateIntoView();
        }
    }

    public static void close()
    {
        if (active)
        {
            active = false;
            container.AnimateOutOfView(0f, 1f);
        }
    }

    private static void onClickedPlayButton(ISleekElement button)
    {
        MenuPlayUI.open();
        close();
        MenuTitleUI.close();
    }

    private static void onClickedSurvivorsButton(ISleekElement button)
    {
        MenuSurvivorsUI.open();
        close();
        MenuTitleUI.close();
    }

    private static void onClickedConfigurationButton(ISleekElement button)
    {
        MenuConfigurationUI.open();
        close();
        MenuTitleUI.close();
    }

    private static void onClickedWorkshopButton(ISleekElement button)
    {
        MenuWorkshopUI.open();
        close();
        MenuTitleUI.close();
    }

    private static void onClickedExitButton(ISleekElement button)
    {
        MenuPauseUI.open();
        close();
        MenuTitleUI.close();
    }

    private static void onClickedProButton(ISleekElement button)
    {
        Provider.provider.storeService.open(new SteamworksStorePackageID(Provider.PRO_ID.m_AppId));
    }

    private static void onClickedAlertButton(ISleekElement button)
    {
        LiveConfigData liveConfigData = LiveConfig.Get();
        ConvenientSavedata.get().write("MainMenuAlertSeenId", liveConfigData.mainMenuAlert.id);
        if (alertBox != null)
        {
            alertBox.IsVisible = false;
        }
        if (!string.IsNullOrEmpty(liveConfigData.mainMenuAlert.link))
        {
            if (!Provider.provider.browserService.canOpenBrowser)
            {
                MenuUI.alert(MenuSurvivorsCharacterUI.localization.format("Overlay"));
            }
            else
            {
                Provider.provider.browserService.open(liveConfigData.mainMenuAlert.link);
            }
        }
    }

    private static void InsertSteamBbCode(ISleekElement parent, string contents, bool useLinkFiltering)
    {
        if (string.IsNullOrEmpty(contents))
        {
            return;
        }
        BbCodeTokenizer bbCodeTokenizer = new BbCodeTokenizer();
        List<BbCodeToken> tokens = bbCodeTokenizer.Tokenize(contents);
        if (bbCodeTokenizer.HasError)
        {
            UnturnedLog.warn("Error tokenizing Steam BBcode: \"" + bbCodeTokenizer.ErrorMessage + "\" Input: \"" + contents + "\"");
            ISleekLabel sleekLabel = Glazier.Get().CreateLabel();
            sleekLabel.Text = contents;
            sleekLabel.UseManualLayout = false;
            sleekLabel.TextAlignment = TextAnchor.UpperLeft;
            parent.AddChild(sleekLabel);
            return;
        }
        BbCodeWidgetConverter bbCodeWidgetConverter = new BbCodeWidgetConverter();
        List<BbCodeWidget> list = bbCodeWidgetConverter.Convert(tokens);
        if (bbCodeWidgetConverter.HasError)
        {
            UnturnedLog.warn("Error converting Steam BBcode to widgets: \"" + bbCodeWidgetConverter.ErrorMessage + "\" Input: \"" + contents + "\"");
            ISleekLabel sleekLabel2 = Glazier.Get().CreateLabel();
            sleekLabel2.Text = contents;
            sleekLabel2.UseManualLayout = false;
            sleekLabel2.TextAlignment = TextAnchor.UpperLeft;
            parent.AddChild(sleekLabel2);
            return;
        }
        foreach (BbCodeWidget item in list)
        {
            switch (item.widgetType)
            {
            case EBbCodeWidgetType.RichTextLabel:
            {
                ISleekLabel sleekLabel3 = Glazier.Get().CreateLabel();
                sleekLabel3.Text = item.widgetData;
                sleekLabel3.UseManualLayout = false;
                sleekLabel3.AllowRichText = true;
                sleekLabel3.TextAlignment = TextAnchor.UpperLeft;
                parent.AddChild(sleekLabel3);
                break;
            }
            case EBbCodeWidgetType.Image:
            {
                SleekWebImage sleekWebImage = new SleekWebImage();
                sleekWebImage.UseManualLayout = false;
                sleekWebImage.UseWidthLayoutOverride = true;
                sleekWebImage.UseHeightLayoutOverride = true;
                sleekWebImage.useImageDimensions = true;
                string url = item.widgetData.Replace("{STEAM_CLAN_IMAGE}", "https://clan.cloudflare.steamstatic.com/images/");
                sleekWebImage.Refresh(url, shouldCache: false);
                parent.AddChild(sleekWebImage);
                break;
            }
            case EBbCodeWidgetType.YouTubeButton:
            {
                SleekYouTubeVideoButton sleekYouTubeVideoButton = new SleekYouTubeVideoButton(icons);
                sleekYouTubeVideoButton.UseManualLayout = false;
                sleekYouTubeVideoButton.UseWidthLayoutOverride = true;
                sleekYouTubeVideoButton.UseHeightLayoutOverride = true;
                sleekYouTubeVideoButton.Refresh(item.widgetData);
                parent.AddChild(sleekYouTubeVideoButton);
                break;
            }
            case EBbCodeWidgetType.LinkButton:
            {
                int num = item.widgetData.IndexOf(',');
                string text;
                string text2;
                if (num < 0)
                {
                    text = item.widgetData;
                    text2 = text;
                }
                else
                {
                    text = item.widgetData.Substring(0, num);
                    text2 = item.widgetData.Substring(num + 1);
                }
                if (!useLinkFiltering || WebUtils.CanParseThirdPartyUrl(text))
                {
                    SleekWebLinkButton sleekWebLinkButton = new SleekWebLinkButton();
                    sleekWebLinkButton.Text = text2;
                    sleekWebLinkButton.Url = text;
                    sleekWebLinkButton.UseManualLayout = false;
                    sleekWebLinkButton.UseChildAutoLayout = ESleekChildLayout.Vertical;
                    sleekWebLinkButton.UseHeightLayoutOverride = true;
                    sleekWebLinkButton.ExpandChildren = true;
                    sleekWebLinkButton.SizeOffset_Y = 30f;
                    sleekWebLinkButton.useLinkFiltering = useLinkFiltering;
                    parent.AddChild(sleekWebLinkButton);
                }
                else
                {
                    UnturnedLog.warn("Ignoring potentially unsafe link in BBcode: {0}", text);
                }
                break;
            }
            }
        }
    }

    private static void ReviseNewsOrder()
    {
        bool flag = newAnnouncement != null;
        if (itemStoreSaleNews != null && !flag)
        {
            itemStoreSaleNews.SetAsFirstSibling();
        }
        if (workshopBox != null)
        {
            workshopBox.SetAsFirstSibling();
        }
        if (itemStoreSaleNews != null && flag)
        {
            itemStoreSaleNews.SetAsFirstSibling();
        }
        if (newAnnouncement != null)
        {
            newAnnouncement.SetAsFirstSibling();
        }
    }

    /// <summary>
    /// Called after newsResponse is updated from web request.
    /// </summary>
    private static void receiveNewsResponse()
    {
        for (int i = 0; i < newsResponse.AppNews.NewsItems.Length; i++)
        {
            NewsItem newsItem = newsResponse.AppNews.NewsItems[i];
            if (newsItem != null && !string.IsNullOrEmpty(newsItem.Title))
            {
                ISleekBox sleekBox = Glazier.Get().CreateBox();
                sleekBox.SizeScale_X = 1f;
                sleekBox.UseManualLayout = false;
                sleekBox.UseChildAutoLayout = ESleekChildLayout.Vertical;
                sleekBox.ChildAutoLayoutPadding = 5f;
                if (newsItem.Title.StartsWith("Community Blog"))
                {
                    ISleekSprite sleekSprite = Glazier.Get().CreateSprite(icons.load<Sprite>("CommunityBlogBackground"));
                    sleekSprite.IgnoreLayout = true;
                    sleekSprite.SizeScale_X = 1f;
                    sleekSprite.SizeScale_Y = 1f;
                    sleekSprite.TintColor = SleekColor.BackgroundIfLight(new Color(0f, 0f, 0f, 0.25f));
                    sleekSprite.DrawMethod = ESleekSpriteType.Tiled;
                    sleekBox.AddChild(sleekSprite);
                }
                ISleekLabel sleekLabel = Glazier.Get().CreateLabel();
                sleekLabel.Text = newsItem.Title;
                sleekLabel.UseManualLayout = false;
                sleekLabel.TextAlignment = TextAnchor.UpperLeft;
                sleekLabel.FontSize = ESleekFontSize.Large;
                sleekBox.AddChild(sleekLabel);
                DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(newsItem.Date).ToLocalTime();
                ISleekLabel sleekLabel2 = Glazier.Get().CreateLabel();
                sleekLabel2.Text = localization.format("News_Author", dateTime, newsItem.Author);
                sleekLabel2.UseManualLayout = false;
                sleekLabel2.TextAlignment = TextAnchor.UpperLeft;
                sleekLabel2.FontSize = ESleekFontSize.Tiny;
                sleekLabel2.TextColor = new SleekColor(ESleekTint.FONT, 0.5f);
                sleekBox.AddChild(sleekLabel2);
                try
                {
                    InsertSteamBbCode(sleekBox, newsItem.Contents, useLinkFiltering: false);
                }
                catch (Exception e)
                {
                    UnturnedLog.warn("Announcement description mis-formatted! Nelson messed up.");
                    UnturnedLog.exception(e);
                }
                SleekWebLinkButton sleekWebLinkButton = new SleekWebLinkButton();
                sleekWebLinkButton.Text = localization.format("News_Comments_Link");
                sleekWebLinkButton.Url = newsItem.URL;
                sleekWebLinkButton.UseManualLayout = false;
                sleekWebLinkButton.UseChildAutoLayout = ESleekChildLayout.Vertical;
                sleekWebLinkButton.UseHeightLayoutOverride = true;
                sleekWebLinkButton.ExpandChildren = true;
                sleekWebLinkButton.SizeOffset_Y = 30f;
                sleekBox.AddChild(sleekWebLinkButton);
                mainScrollView.AddChild(sleekBox);
                if (i == 0 && (!ConvenientSavedata.get().read("Newest_Announcement", out long value) || value != newsItem.Date))
                {
                    ConvenientSavedata.get().write("Newest_Announcement", newsItem.Date);
                    newAnnouncement = sleekBox;
                    ReviseNewsOrder();
                }
            }
        }
    }

    private static void OnUpdateDetected(string versionString, bool isRollback)
    {
        ISleekBox sleekBox = Glazier.Get().CreateBox();
        sleekBox.PositionOffset_X = 210f;
        sleekBox.PositionOffset_Y = mainHeaderOffset;
        sleekBox.SizeOffset_Y = 40f;
        sleekBox.SizeOffset_X = -210f;
        sleekBox.SizeScale_X = 1f;
        sleekBox.FontSize = ESleekFontSize.Medium;
        container.AddChild(sleekBox);
        string key = (isRollback ? "RollbackAvailable" : "UpdateAvailable");
        string s = localization.format(key, versionString);
        RichTextUtil.replaceNewlineMarkup(ref s);
        sleekBox.Text = s;
        mainHeaderOffset += sleekBox.SizeOffset_Y + 10f;
        mainScrollView.PositionOffset_Y += sleekBox.SizeOffset_Y + 10f;
        mainScrollView.SizeOffset_Y -= sleekBox.SizeOffset_Y + 10f;
    }

    /// <summary>
    /// Read News.txt file from Cloud directory to preview on main menu.
    /// </summary>
    private static bool readNewsPreview()
    {
        string path = Path.Combine(ReadWrite.PATH, "Cloud", "News.txt");
        if (!File.Exists(path))
        {
            return false;
        }
        string contents = File.ReadAllText(path);
        NewsItem newsItem = new NewsItem();
        newsItem.Author = "Preview";
        newsItem.Title = "Preview";
        newsItem.Contents = contents;
        newsItem.Date = DateTime.UtcNow.ToUnixTimeSeconds();
        newsResponse = new NewsResponse();
        newsResponse.AppNews = new AppNews();
        newsResponse.AppNews.NewsItems = new NewsItem[1] { newsItem };
        receiveNewsResponse();
        return true;
    }

    internal static void receiveSteamNews(string data)
    {
        newsResponse = JsonConvert.DeserializeObject<NewsResponse>(data);
        receiveNewsResponse();
    }

    private static void spawnFeaturedWorkshopArticle()
    {
        if (!SteamUGC.GetQueryUGCResult(featuredWorkshopHandle, 0u, out var pDetails))
        {
            UnturnedLog.warn("Unable to retrieve details for featured workshop article");
            return;
        }
        if (pDetails.m_eResult != EResult.k_EResultOK)
        {
            UnturnedLog.warn("Error retrieving details for featured workshop item: " + pDetails.m_eResult);
            return;
        }
        if (pDetails.m_bBanned)
        {
            UnturnedLog.warn("Ignoring featured workshop file because it was banned");
            return;
        }
        if (pDetails.m_eVisibility == ERemoteStoragePublishedFileVisibility.k_ERemoteStoragePublishedFileVisibilityPrivate)
        {
            UnturnedLog.warn("Ignoring featured workshop file because visibility is private");
            return;
        }
        if (string.IsNullOrWhiteSpace(pDetails.m_rgchTitle))
        {
            UnturnedLog.warn($"Ignoring featured workshop file {pDetails.m_nPublishedFileId} because title is empty");
            return;
        }
        if (ProfanityFilter.NaiveContainsHardcodedBannedWord(pDetails.m_rgchTitle))
        {
            UnturnedLog.warn($"Ignoring featured workshop file {pDetails.m_nPublishedFileId} because title contains banned string. May need moderator attention!");
            return;
        }
        string[] array = featuredWorkshopTitleBannedWords;
        foreach (string text in array)
        {
            if (pDetails.m_rgchTitle.Contains(text, StringComparison.InvariantCultureIgnoreCase))
            {
                UnturnedLog.warn($"Ignoring featured workshop file {pDetails.m_nPublishedFileId} because title contains inappropriate string \"{text}\"");
                return;
            }
        }
        workshopBox = Glazier.Get().CreateBox();
        workshopBox.SizeScale_X = 1f;
        workshopBox.UseManualLayout = false;
        workshopBox.UseChildAutoLayout = ESleekChildLayout.Vertical;
        workshopBox.ChildAutoLayoutPadding = 5f;
        mainScrollView.AddChild(workshopBox);
        ReviseNewsOrder();
        MainMenuWorkshopFeaturedLiveConfig featured = LiveConfig.Get().mainMenuWorkshop.featured;
        bool flag = featured.IsNowFeaturedTimeOrBypassed() && featured.IsFeatured(pDetails.m_nPublishedFileId.m_PublishedFileId);
        string key = ((!flag) ? "Featured_Workshop_Title" : (featured.type switch
        {
            EFeaturedWorkshopType.Curated => "Curated_Workshop_Title", 
            _ => "Highlighted_Workshop_Title", 
        }));
        string text2 = localization.format(key, pDetails.m_rgchTitle);
        ISleekElement sleekElement = Glazier.Get().CreateFrame();
        sleekElement.UseManualLayout = false;
        sleekElement.UseChildAutoLayout = ESleekChildLayout.Horizontal;
        workshopBox.AddChild(sleekElement);
        ISleekLabel sleekLabel = Glazier.Get().CreateLabel();
        sleekLabel.UseManualLayout = false;
        sleekLabel.Text = text2;
        sleekLabel.FontSize = ESleekFontSize.Large;
        sleekLabel.TextAlignment = TextAnchor.UpperLeft;
        sleekElement.AddChild(sleekLabel);
        if (flag && featured.status != 0)
        {
            bool flag2 = featured.status == EMapStatus.Updated;
            ISleekLabel sleekLabel2 = Glazier.Get().CreateLabel();
            sleekLabel2.UseManualLayout = false;
            sleekLabel2.TextAlignment = TextAnchor.UpperLeft;
            sleekLabel2.Text = Provider.localization.format(flag2 ? "Updated" : "New");
            sleekLabel2.TextColor = Color.green;
            sleekLabel2.TextContrastContext = ETextContrastContext.InconspicuousBackdrop;
            sleekElement.AddChild(sleekLabel2);
        }
        SleekDismissWorkshopArticleButton sleekDismissWorkshopArticleButton = new SleekDismissWorkshopArticleButton();
        sleekDismissWorkshopArticleButton.PositionOffset_X = -105f;
        sleekDismissWorkshopArticleButton.PositionOffset_Y = 5f;
        sleekDismissWorkshopArticleButton.PositionScale_X = 1f;
        sleekDismissWorkshopArticleButton.SizeOffset_X = 100f;
        sleekDismissWorkshopArticleButton.SizeOffset_Y = 30f;
        sleekDismissWorkshopArticleButton.internalButton.Text = localization.format("Featured_Workshop_Dismiss");
        sleekDismissWorkshopArticleButton.articleId = pDetails.m_nPublishedFileId.m_PublishedFileId;
        sleekDismissWorkshopArticleButton.targetContent = workshopBox;
        sleekDismissWorkshopArticleButton.IgnoreLayout = true;
        workshopBox.AddChild(sleekDismissWorkshopArticleButton);
        if (SteamUGC.GetQueryUGCPreviewURL(featuredWorkshopHandle, 0u, out var pchURL, 1024u))
        {
            SleekWebImage sleekWebImage = new SleekWebImage();
            sleekWebImage.UseManualLayout = false;
            sleekWebImage.useImageDimensions = true;
            sleekWebImage.UseWidthLayoutOverride = true;
            sleekWebImage.UseHeightLayoutOverride = true;
            sleekWebImage.maxImageDimensionsWidth = 960f;
            sleekWebImage.maxImageDimensionsHeight = 540f;
            workshopBox.AddChild(sleekWebImage);
            sleekWebImage.Refresh(pchURL, shouldCache: false);
        }
        SleekReadMoreButton sleekReadMoreButton = new SleekReadMoreButton();
        sleekReadMoreButton.UseManualLayout = false;
        sleekReadMoreButton.UseChildAutoLayout = ESleekChildLayout.Vertical;
        sleekReadMoreButton.UseHeightLayoutOverride = true;
        sleekReadMoreButton.ExpandChildren = true;
        sleekReadMoreButton.SizeOffset_Y = 30f;
        ISleekElement sleekElement2 = Glazier.Get().CreateFrame();
        sleekElement2.UseManualLayout = false;
        sleekElement2.IsVisible = false;
        sleekElement2.UseChildAutoLayout = ESleekChildLayout.Vertical;
        sleekReadMoreButton.targetContent = sleekElement2;
        workshopBox.AddChild(sleekReadMoreButton);
        workshopBox.AddChild(sleekElement2);
        if (flag && featured.autoExpandDescription)
        {
            sleekElement2.IsVisible = true;
        }
        PublishedFileId_t nPublishedFileId;
        try
        {
            string contents = pDetails.m_rgchDescription;
            if (flag && !string.IsNullOrEmpty(featured.overrideDescription))
            {
                contents = featured.overrideDescription;
            }
            InsertSteamBbCode(sleekElement2, contents, useLinkFiltering: true);
        }
        catch (Exception e)
        {
            nPublishedFileId = pDetails.m_nPublishedFileId;
            UnturnedLog.warn("Workshop description mis-formatted! If you made this item I suggest you adjust it to display in-game properly: " + nPublishedFileId.ToString());
            UnturnedLog.exception(e);
        }
        sleekReadMoreButton.onText = localization.format("ReadMore_Link_On");
        sleekReadMoreButton.offText = localization.format("ReadMore_Link_Off");
        sleekReadMoreButton.Refresh();
        ISleekElement sleekElement3 = Glazier.Get().CreateFrame();
        sleekElement3.UseManualLayout = false;
        sleekElement3.UseChildAutoLayout = ESleekChildLayout.Horizontal;
        sleekElement3.ExpandChildren = true;
        sleekElement3.SizeOffset_Y = 30f;
        sleekElement3.UseHeightLayoutOverride = true;
        workshopBox.AddChild(sleekElement3);
        SleekWebLinkButton sleekWebLinkButton = new SleekWebLinkButton();
        sleekWebLinkButton.UseManualLayout = false;
        sleekWebLinkButton.Text = localization.format("Featured_Workshop_Link");
        nPublishedFileId = pDetails.m_nPublishedFileId;
        sleekWebLinkButton.Url = "https://steamcommunity.com/sharedfiles/filedetails/?id=" + nPublishedFileId.ToString();
        sleekWebLinkButton.UseChildAutoLayout = ESleekChildLayout.Vertical;
        sleekWebLinkButton.ExpandChildren = true;
        sleekElement3.AddChild(sleekWebLinkButton);
        int[] associatedStockpileItems = featured.associatedStockpileItems;
        if (flag && associatedStockpileItems != null && associatedStockpileItems.Length != 0)
        {
            List<int> list = new List<int>(associatedStockpileItems.Length);
            int[] array2 = associatedStockpileItems;
            foreach (int num in array2)
            {
                if (num > 0 && !Provider.provider.economyService.isItemHiddenByCountryRestrictions(num))
                {
                    list.Add(num);
                }
            }
            int num2 = list.RandomOrDefault();
            if (num2 > 0)
            {
                string inventoryName = Provider.provider.economyService.getInventoryName(num2);
                if (string.IsNullOrEmpty(inventoryName))
                {
                    UnturnedLog.warn("Unknown itemdefid {0} specified in featured workshop stockpile items", num2);
                }
                else
                {
                    string text3 = localization.format("Featured_Workshop_Stockpile_Link", inventoryName);
                    SleekStockpileLinkButton sleekStockpileLinkButton = new SleekStockpileLinkButton();
                    sleekStockpileLinkButton.UseManualLayout = false;
                    sleekStockpileLinkButton.internalButton.Text = text3;
                    sleekStockpileLinkButton.itemdefid = num2;
                    sleekStockpileLinkButton.UseChildAutoLayout = ESleekChildLayout.Vertical;
                    sleekStockpileLinkButton.ExpandChildren = true;
                    sleekElement3.AddChild(sleekStockpileLinkButton);
                }
            }
        }
        if (flag && !string.IsNullOrEmpty(featured.linkURL))
        {
            SleekWebLinkButton sleekWebLinkButton2 = new SleekWebLinkButton();
            sleekWebLinkButton2.UseManualLayout = false;
            sleekWebLinkButton2.Text = featured.linkText;
            sleekWebLinkButton2.Url = featured.linkURL;
            sleekWebLinkButton2.UseChildAutoLayout = ESleekChildLayout.Vertical;
            sleekWebLinkButton2.ExpandChildren = true;
            sleekElement3.AddChild(sleekWebLinkButton2);
        }
        SleekWorkshopSubscriptionButton sleekWorkshopSubscriptionButton = new SleekWorkshopSubscriptionButton();
        sleekWorkshopSubscriptionButton.UseManualLayout = false;
        sleekWorkshopSubscriptionButton.subscribeText = localization.format("Featured_Workshop_Sub");
        sleekWorkshopSubscriptionButton.unsubscribeText = localization.format("Featured_Workshop_Unsub");
        sleekWorkshopSubscriptionButton.subscribeTooltip = localization.format("Subscribe_Tooltip", pDetails.m_rgchTitle);
        sleekWorkshopSubscriptionButton.unsubscribeTooltip = localization.format("Unsubscribe_Tooltip", pDetails.m_rgchTitle);
        sleekWorkshopSubscriptionButton.fileId = pDetails.m_nPublishedFileId;
        sleekWorkshopSubscriptionButton.synchronizeText();
        sleekWorkshopSubscriptionButton.UseChildAutoLayout = ESleekChildLayout.Vertical;
        sleekWorkshopSubscriptionButton.ExpandChildren = true;
        sleekElement3.AddChild(sleekWorkshopSubscriptionButton);
    }

    /// <summary>
    /// Helper for handlePopularItemResults.
    /// If player has not dismissed item at index then proceed with query and return true.
    /// </summary>
    private static bool featurePopularItem(uint index)
    {
        if (!SteamUGC.GetQueryUGCResult(popularWorkshopHandle, index, out var pDetails))
        {
            UnturnedLog.warn($"Unable to get popular workshop item details for index {index}");
            return false;
        }
        if (pDetails.m_eResult != EResult.k_EResultOK)
        {
            UnturnedLog.warn($"Error retrieving details for popular workshop file {pDetails.m_nPublishedFileId}: {pDetails.m_eResult}");
            return false;
        }
        if (pDetails.m_bBanned)
        {
            UnturnedLog.warn($"Ignoring popular workshop file {pDetails.m_nPublishedFileId} because it was banned");
            return false;
        }
        if (pDetails.m_eVisibility != 0)
        {
            UnturnedLog.warn($"Ignoring popular workshop file {pDetails.m_nPublishedFileId} because visibility is {pDetails.m_eVisibility}");
            return false;
        }
        if (LiveConfig.Get().mainMenuWorkshop.popular.IsHidden(pDetails.m_nPublishedFileId.m_PublishedFileId))
        {
            UnturnedLog.info($"Ignoring popular workshop file {pDetails.m_nPublishedFileId} because it's not eligible");
            return false;
        }
        if (LocalNews.wasWorkshopItemDismissed(pDetails.m_nPublishedFileId.m_PublishedFileId))
        {
            return false;
        }
        queryFeaturedItem(pDetails.m_nPublishedFileId);
        return true;
    }

    /// <summary>
    /// Successfully queried popular workshop items.
    /// Tries to decide on an item that player has not dismissed.
    /// </summary>
    private static void handlePopularItemResults(SteamUGCQueryCompleted_t callback)
    {
        UnturnedLog.info("Received popular workshop files");
        uint unNumResultsReturned = callback.m_unNumResultsReturned;
        if (unNumResultsReturned < 1)
        {
            UnturnedLog.warn("Popular workshop items response was empty");
            return;
        }
        MainMenuWorkshopPopularLiveConfig popular = LiveConfig.Get().mainMenuWorkshop.popular;
        int num = Mathf.Min((int)unNumResultsReturned, popular.carouselItems);
        if (num > 0)
        {
            List<uint> list = new List<uint>(num);
            for (uint num2 = 0u; num2 < num; num2++)
            {
                list.Add(num2);
            }
            while (list.Count > 0)
            {
                int index = UnityEngine.Random.Range(0, list.Count);
                if (featurePopularItem(list[index]))
                {
                    return;
                }
                list.RemoveAtFast(index);
            }
        }
        for (uint num3 = (uint)num; num3 < unNumResultsReturned; num3++)
        {
            if (featurePopularItem(num3))
            {
                return;
            }
        }
        UnturnedLog.info("None of {0} popular workshop item(s) were eligible");
    }

    private static void onPopularQueryCompleted(SteamUGCQueryCompleted_t callback, bool io)
    {
        if (io)
        {
            UnturnedLog.warn("IO error while querying popular workshop items");
        }
        else if (callback.m_eResult == EResult.k_EResultOK)
        {
            handlePopularItemResults(callback);
        }
        else
        {
            UnturnedLog.warn("Error while querying popular workshop items: " + callback.m_eResult);
        }
    }

    /// <summary>
    /// Response about the item we decided to display.
    /// </summary>
    private static void onFeaturedQueryCompleted(SteamUGCQueryCompleted_t callback, bool io)
    {
        if (io)
        {
            UnturnedLog.warn("IO error while querying featured workshop item");
            return;
        }
        if (callback.m_eResult == EResult.k_EResultOK)
        {
            UnturnedLog.info("Received workshop file details for news feed");
            try
            {
                spawnFeaturedWorkshopArticle();
                return;
            }
            catch (Exception e)
            {
                UnturnedLog.warn("Workshop news article spawn failed!");
                UnturnedLog.exception(e);
                return;
            }
        }
        UnturnedLog.warn("Error while querying featured workshop item: " + callback.m_eResult);
    }

    private static void onSteamUGCQueryCompleted(SteamUGCQueryCompleted_t callback, bool io)
    {
        if (callback.m_handle == popularWorkshopHandle)
        {
            onPopularQueryCompleted(callback, io);
            SteamUGC.ReleaseQueryUGCRequest(popularWorkshopHandle);
            popularWorkshopHandle = UGCQueryHandle_t.Invalid;
        }
        else if (callback.m_handle == featuredWorkshopHandle)
        {
            onFeaturedQueryCompleted(callback, io);
            SteamUGC.ReleaseQueryUGCRequest(featuredWorkshopHandle);
            featuredWorkshopHandle = UGCQueryHandle_t.Invalid;
        }
    }

    protected static void queryFeaturedItem(PublishedFileId_t publishedFileID)
    {
        UnturnedLog.info("Requesting workshop file details for news feed ({0})", publishedFileID);
        if (featuredWorkshopHandle != UGCQueryHandle_t.Invalid)
        {
            SteamUGC.ReleaseQueryUGCRequest(featuredWorkshopHandle);
            featuredWorkshopHandle = UGCQueryHandle_t.Invalid;
        }
        featuredWorkshopHandle = SteamUGC.CreateQueryUGCDetailsRequest(new PublishedFileId_t[1] { publishedFileID }, 1u);
        SteamUGC.SetReturnLongDescription(featuredWorkshopHandle, bReturnLongDescription: true);
        SteamUGC.SetReturnChildren(featuredWorkshopHandle, bReturnChildren: true);
        SteamAPICall_t hAPICall = SteamUGC.SendQueryUGCRequest(featuredWorkshopHandle);
        steamUGCQueryCompletedFeatured.Set(hAPICall);
    }

    /// <summary>
    /// Submit query for recently trending popular workshop items.
    /// </summary>
    private static void queryPopularWorkshopItems()
    {
        MainMenuWorkshopPopularLiveConfig popular = LiveConfig.Get().mainMenuWorkshop.popular;
        uint num = popular.trendDays;
        if (num < 1 || popular.carouselItems < 1)
        {
            UnturnedLog.warn("Not requesting popular workshop files for news feed");
            return;
        }
        if (num > 180)
        {
            num = 180u;
            UnturnedLog.warn("Clamping popular workshop trend days to {0}", num);
        }
        UnturnedLog.info("Requesting popular workshop files from the past {0} day(s) for news feed", num);
        popularWorkshopHandle = SteamUGC.CreateQueryAllUGCRequest(EUGCQuery.k_EUGCQuery_RankedByTrend, EUGCMatchingUGCType.k_EUGCMatchingUGCType_Items_ReadyToUse, Provider.APP_ID, Provider.APP_ID, 1u);
        SteamUGC.SetRankedByTrendDays(popularWorkshopHandle, num);
        SteamUGC.SetReturnOnlyIDs(popularWorkshopHandle, bReturnOnlyIDs: true);
        SteamAPICall_t hAPICall = SteamUGC.SendQueryUGCRequest(popularWorkshopHandle);
        steamUGCQueryCompletedPopular.Set(hAPICall);
    }

    private static bool CreateItemStoreSaleNews(ItemStore itemStore)
    {
        if (!Glazier.Get().SupportsAutomaticLayout)
        {
            return false;
        }
        int[] unownedDiscountedBundleListingIndices = ItemStore.Get().GetUnownedDiscountedBundleListingIndices();
        if (unownedDiscountedBundleListingIndices == null || unownedDiscountedBundleListingIndices.Length < 3)
        {
            return false;
        }
        List<int> list = new List<int>(unownedDiscountedBundleListingIndices);
        int[] excludedListingIndices = itemStore.GetExcludedListingIndices();
        if (excludedListingIndices != null)
        {
            int[] array = excludedListingIndices;
            foreach (int item in array)
            {
                list.Remove(item);
            }
        }
        if (list.Count < 3)
        {
            return false;
        }
        int widthForLayout = ScreenEx.GetWidthForLayout();
        int num = Mathf.Max(3, (widthForLayout - 450) / 200);
        List<int> list2 = new List<int>(num);
        while (list2.Count < num && list.Count > 0)
        {
            int randomIndex = list.GetRandomIndex();
            list2.Add(list[randomIndex]);
            list.RemoveAtFast(randomIndex);
        }
        itemStoreSaleNews = Glazier.Get().CreateBox();
        itemStoreSaleNews.SizeScale_X = 1f;
        itemStoreSaleNews.UseManualLayout = false;
        itemStoreSaleNews.UseChildAutoLayout = ESleekChildLayout.Vertical;
        itemStoreSaleNews.ChildAutoLayoutPadding = 5f;
        mainScrollView.AddChild(itemStoreSaleNews);
        ReviseNewsOrder();
        ISleekLabel sleekLabel = Glazier.Get().CreateLabel();
        sleekLabel.UseManualLayout = false;
        sleekLabel.TextAlignment = TextAnchor.MiddleCenter;
        sleekLabel.FontSize = ESleekFontSize.Large;
        itemStoreSaleNews.AddChild(sleekLabel);
        if (!string.IsNullOrEmpty(LiveConfig.Get().itemStore.saleTitle))
        {
            sleekLabel.Text = LiveConfig.Get().itemStore.saleTitle;
            DateTime dateTime = LiveConfig.Get().itemStore.saleStart.ToLocalTime();
            DateTime dateTime2 = LiveConfig.Get().itemStore.saleEnd.ToLocalTime();
            ISleekLabel sleekLabel2 = Glazier.Get().CreateLabel();
            sleekLabel2.UseManualLayout = false;
            sleekLabel2.TextAlignment = TextAnchor.MiddleLeft;
            sleekLabel2.FontSize = ESleekFontSize.Medium;
            sleekLabel2.TextContrastContext = ETextContrastContext.ColorfulBackdrop;
            sleekLabel2.TextColor = new SleekColor(ESleekTint.FONT, 0.5f);
            sleekLabel2.Text = ItemStoreMenu.instance.localization.format("SaleWindowFormat", dateTime, dateTime2);
            itemStoreSaleNews.AddChild(sleekLabel2);
        }
        else
        {
            sleekLabel.Text = ItemStoreMenu.instance.localization.format("DefaultSaleTitle");
        }
        ISleekElement sleekElement = Glazier.Get().CreateFrame();
        sleekElement.UseManualLayout = false;
        sleekElement.UseChildAutoLayout = ESleekChildLayout.Horizontal;
        sleekElement.ChildAutoLayoutPadding = 5f;
        itemStoreSaleNews.AddChild(sleekElement);
        foreach (int item2 in list2)
        {
            ItemStore.Listing listing = itemStore.GetListings()[item2];
            SleekItemStoreListing sleekItemStoreListing = new SleekItemStoreListing();
            sleekItemStoreListing.UseManualLayout = false;
            sleekItemStoreListing.UseWidthLayoutOverride = true;
            sleekItemStoreListing.UseHeightLayoutOverride = true;
            sleekItemStoreListing.SizeOffset_X = 200f;
            sleekItemStoreListing.SizeOffset_Y = 200f;
            sleekItemStoreListing.canShowAsInCart = false;
            sleekItemStoreListing.SetListing(listing);
            sleekElement.AddChild(sleekItemStoreListing);
        }
        return true;
    }

    private static void OnPricesReceived()
    {
        if (hasCreatedItemStoreButton)
        {
            return;
        }
        hasCreatedItemStoreButton = true;
        ItemStore itemStore = ItemStore.Get();
        if (CreateItemStoreSaleNews(itemStore))
        {
            return;
        }
        int[] array;
        SleekItemStoreMainMenuButton.ELabelType eLabelType;
        if (itemStore.HasNewListings)
        {
            array = itemStore.GetNewListingIndices();
            eLabelType = SleekItemStoreMainMenuButton.ELabelType.New;
        }
        else if (ItemStore.Get().HasDiscountedListings)
        {
            array = itemStore.GetDiscountedListingIndices();
            eLabelType = SleekItemStoreMainMenuButton.ELabelType.Sale;
        }
        else if (ItemStore.Get().HasFeaturedListings && UnityEngine.Random.value < 0.5f)
        {
            array = itemStore.GetFeaturedListingIndices();
            eLabelType = SleekItemStoreMainMenuButton.ELabelType.None;
        }
        else
        {
            array = null;
            eLabelType = SleekItemStoreMainMenuButton.ELabelType.None;
        }
        ItemStore.Listing listing;
        if (array == null)
        {
            ItemStore.Listing[] listings = itemStore.GetListings();
            listing = listings[UnityEngine.Random.Range(0, listings.Length)];
            Guid inventoryItemGuid = Provider.provider.economyService.getInventoryItemGuid(listing.itemdefid);
            if (inventoryItemGuid != default(Guid) && Assets.find<ItemKeyAsset>(inventoryItemGuid) != null)
            {
                return;
            }
        }
        else
        {
            int[] excludedListingIndices = itemStore.GetExcludedListingIndices();
            if (excludedListingIndices != null)
            {
                List<int> list = new List<int>(array);
                int[] array2 = excludedListingIndices;
                foreach (int item in array2)
                {
                    list.Remove(item);
                }
                if (list.Count < 1)
                {
                    return;
                }
                array = list.ToArray();
            }
            int num = array.RandomOrDefault();
            listing = itemStore.GetListings()[num];
            if (eLabelType == SleekItemStoreMainMenuButton.ELabelType.New && ItemStoreSavedata.WasNewListingSeen(listing.itemdefid))
            {
                eLabelType = SleekItemStoreMainMenuButton.ELabelType.None;
            }
        }
        SleekItemStoreMainMenuButton sleekItemStoreMainMenuButton = new SleekItemStoreMainMenuButton(listing, eLabelType);
        sleekItemStoreMainMenuButton.PositionOffset_Y = 410f;
        sleekItemStoreMainMenuButton.SizeOffset_X = 200f;
        sleekItemStoreMainMenuButton.SizeOffset_Y = 50f;
        container.AddChild(sleekItemStoreMainMenuButton);
    }

    private static void PopulateAlertFromLiveConfig()
    {
        LiveConfigData liveConfigData = LiveConfig.Get();
        if (string.IsNullOrEmpty(liveConfigData.mainMenuAlert.header) || string.IsNullOrEmpty(liveConfigData.mainMenuAlert.body) || (ConvenientSavedata.get().read("MainMenuAlertSeenId", out long value) && value >= liveConfigData.mainMenuAlert.id))
        {
            return;
        }
        bool flag = false;
        if (liveConfigData.mainMenuAlert.useTimeWindow && !flag)
        {
            DateTime utcNow = DateTime.UtcNow;
            if (utcNow < liveConfigData.mainMenuAlert.startTime || utcNow > liveConfigData.mainMenuAlert.endTime)
            {
                return;
            }
        }
        if (alertBox == null)
        {
            alertBox = Glazier.Get().CreateButton();
            alertBox.OnClicked += onClickedAlertButton;
            alertBox.PositionOffset_X = 210f;
            alertBox.PositionOffset_Y = mainHeaderOffset;
            alertBox.SizeOffset_Y = 60f;
            alertBox.SizeOffset_X = -210f;
            alertBox.SizeScale_X = 1f;
            container.AddChild(alertBox);
            alertImage = Glazier.Get().CreateImage();
            alertImage.PositionOffset_X = 10f;
            alertImage.PositionOffset_Y = 10f;
            alertImage.SizeOffset_X = 40f;
            alertImage.SizeOffset_Y = 40f;
            alertImage.IsVisible = false;
            alertBox.AddChild(alertImage);
            alertWebImage = new SleekWebImage();
            alertWebImage.PositionOffset_X = 10f;
            alertWebImage.PositionOffset_Y = 10f;
            alertWebImage.SizeOffset_X = 40f;
            alertWebImage.SizeOffset_Y = 40f;
            alertWebImage.IsVisible = false;
            alertBox.AddChild(alertWebImage);
            alertHeaderLabel = Glazier.Get().CreateLabel();
            alertHeaderLabel.PositionOffset_X = 60f;
            alertHeaderLabel.SizeScale_X = 1f;
            alertHeaderLabel.SizeOffset_X = -70f;
            alertHeaderLabel.SizeOffset_Y = 30f;
            alertHeaderLabel.TextAlignment = TextAnchor.MiddleLeft;
            alertHeaderLabel.FontSize = ESleekFontSize.Medium;
            alertHeaderLabel.TextContrastContext = ETextContrastContext.ColorfulBackdrop;
            alertBox.AddChild(alertHeaderLabel);
            alertLinkLabel = Glazier.Get().CreateLabel();
            alertLinkLabel.PositionOffset_X = 60f;
            alertLinkLabel.SizeScale_X = 1f;
            alertLinkLabel.SizeOffset_X = -70f;
            alertLinkLabel.SizeOffset_Y = 30f;
            alertLinkLabel.TextAlignment = TextAnchor.MiddleRight;
            alertLinkLabel.FontSize = ESleekFontSize.Small;
            alertLinkLabel.IsVisible = false;
            alertBox.AddChild(alertLinkLabel);
            dismissAlertLabel = Glazier.Get().CreateLabel();
            dismissAlertLabel.PositionScale_Y = 1f;
            dismissAlertLabel.PositionOffset_X = 60f;
            dismissAlertLabel.PositionOffset_Y = -35f;
            dismissAlertLabel.SizeScale_X = 1f;
            dismissAlertLabel.SizeOffset_X = -65f;
            dismissAlertLabel.SizeOffset_Y = 30f;
            dismissAlertLabel.TextAlignment = TextAnchor.LowerRight;
            dismissAlertLabel.FontSize = ESleekFontSize.Small;
            dismissAlertLabel.Text = localization.format("Featured_Workshop_Dismiss");
            alertBox.AddChild(dismissAlertLabel);
            alertBodyLabel = Glazier.Get().CreateLabel();
            alertBodyLabel.PositionOffset_X = 60f;
            alertBodyLabel.PositionOffset_Y = 20f;
            alertBodyLabel.SizeOffset_X = -70f;
            alertBodyLabel.SizeOffset_Y = -20f;
            alertBodyLabel.SizeScale_X = 1f;
            alertBodyLabel.SizeScale_Y = 1f;
            alertBodyLabel.TextAlignment = TextAnchor.UpperLeft;
            alertBodyLabel.TextColor = ESleekTint.RICH_TEXT_DEFAULT;
            alertBodyLabel.TextContrastContext = ETextContrastContext.InconspicuousBackdrop;
            alertBodyLabel.AllowRichText = true;
            alertBox.AddChild(alertBodyLabel);
            mainHeaderOffset += alertBox.SizeOffset_Y + 10f;
            mainScrollView.PositionOffset_Y += alertBox.SizeOffset_Y + 10f;
            mainScrollView.SizeOffset_Y -= alertBox.SizeOffset_Y + 10f;
        }
        Color color = Palette.hex(liveConfigData.mainMenuAlert.color);
        alertHeaderLabel.Text = liveConfigData.mainMenuAlert.header;
        alertHeaderLabel.TextColor = color;
        alertBodyLabel.Text = liveConfigData.mainMenuAlert.body;
        dismissAlertLabel.TextColor = color * 0.5f;
        if (!string.IsNullOrEmpty(liveConfigData.mainMenuAlert.iconName))
        {
            alertImage.Texture = icons.load<Texture2D>(liveConfigData.mainMenuAlert.iconName);
            alertImage.IsVisible = true;
        }
        else
        {
            alertImage.IsVisible = false;
        }
        if (!string.IsNullOrEmpty(liveConfigData.mainMenuAlert.iconURL))
        {
            alertWebImage.Refresh(liveConfigData.mainMenuAlert.iconURL);
            alertWebImage.IsVisible = true;
        }
        else
        {
            alertWebImage.IsVisible = false;
        }
        Color color2 = (liveConfigData.mainMenuAlert.shouldTintIcon ? color : Color.white);
        alertImage.TintColor = color2;
        alertWebImage.color = color2;
        if (!string.IsNullOrEmpty(liveConfigData.mainMenuAlert.link))
        {
            alertLinkLabel.Text = liveConfigData.mainMenuAlert.link;
            alertLinkLabel.TextColor = color * 0.5f;
            alertLinkLabel.IsVisible = true;
            dismissAlertLabel.IsVisible = false;
        }
        else
        {
            alertLinkLabel.IsVisible = false;
            dismissAlertLabel.IsVisible = true;
        }
    }

    /// <summary>
    /// Entry point to deciding which workshop item is featured above recent announcements.
    /// </summary>
    private static void UpdateWorkshopFromLiveConfig()
    {
        if (!Glazier.Get().SupportsAutomaticLayout)
        {
            return;
        }
        MainMenuWorkshopLiveConfig mainMenuWorkshop = LiveConfig.Get().mainMenuWorkshop;
        if (mainMenuWorkshop.allowNews && SteamUser.BLoggedOn() && !hasBegunQueryingLiveConfigWorkshop)
        {
            hasBegunQueryingLiveConfigWorkshop = true;
            ulong num = 0uL;
            if (mainMenuWorkshop.featured.fileIds != null && mainMenuWorkshop.featured.fileIds.Length != 0 && mainMenuWorkshop.featured.IsNowFeaturedTimeOrBypassed())
            {
                num = mainMenuWorkshop.featured.fileIds.RandomOrDefault();
            }
            if (num != 0 && !LocalNews.wasWorkshopItemDismissed(num))
            {
                queryFeaturedItem((PublishedFileId_t)num);
            }
            else if (OptionsSettings.featuredWorkshop)
            {
                queryPopularWorkshopItems();
            }
        }
    }

    private static void OnLiveConfigRefreshed()
    {
        PopulateAlertFromLiveConfig();
        UpdateWorkshopFromLiveConfig();
    }

    public void OnDestroy()
    {
        ItemStore.Get().OnPricesReceived -= OnPricesReceived;
        LiveConfig.OnRefreshed -= OnLiveConfigRefreshed;
        playUI.OnDestroy();
        survivorsUI.OnDestroy();
        workshopUI.OnDestroy();
    }

    public MenuDashboardUI()
    {
        if (icons != null)
        {
            icons.unload();
        }
        localization = Localization.read("/Menu/MenuDashboard.dat");
        TransportBase.OnGetMessage = localization.format;
        icons = Bundles.getBundle("/Bundles/Textures/Menu/Icons/MenuDashboard/MenuDashboard.unity3d");
        MenuUI.copyNotificationButton.icon = icons.load<Texture2D>("Clipboard");
        MenuUI.copyNotificationButton.text = localization.format("Copy_Notification_Label");
        MenuUI.copyNotificationButton.tooltip = localization.format("Copy_Notification_Tooltip");
        newAnnouncement = null;
        workshopBox = null;
        itemStoreSaleNews = null;
        if (SteamUser.BLoggedOn())
        {
            MenuUI.instance.StartCoroutine(MenuUI.instance.CheckForUpdates(OnUpdateDetected));
            if (steamUGCQueryCompletedPopular == null)
            {
                steamUGCQueryCompletedPopular = CallResult<SteamUGCQueryCompleted_t>.Create(onSteamUGCQueryCompleted);
            }
            if (steamUGCQueryCompletedFeatured == null)
            {
                steamUGCQueryCompletedFeatured = CallResult<SteamUGCQueryCompleted_t>.Create(onSteamUGCQueryCompleted);
            }
            if (popularWorkshopHandle != UGCQueryHandle_t.Invalid)
            {
                SteamUGC.ReleaseQueryUGCRequest(popularWorkshopHandle);
                popularWorkshopHandle = UGCQueryHandle_t.Invalid;
            }
        }
        container = new SleekFullscreenBox();
        container.PositionOffset_X = 10f;
        container.PositionOffset_Y = 10f;
        container.SizeOffset_X = -20f;
        container.SizeOffset_Y = -20f;
        container.SizeScale_X = 1f;
        container.SizeScale_Y = 1f;
        MenuUI.container.AddChild(container);
        active = true;
        playButton = new SleekButtonIcon(icons.load<Texture2D>("Play"));
        playButton.PositionOffset_Y = 170f;
        playButton.SizeOffset_X = 200f;
        playButton.SizeOffset_Y = 50f;
        playButton.text = localization.format("PlayButtonText");
        playButton.tooltip = localization.format("PlayButtonTooltip");
        playButton.onClickedButton += onClickedPlayButton;
        playButton.fontSize = ESleekFontSize.Medium;
        playButton.iconColor = ESleekTint.FOREGROUND;
        container.AddChild(playButton);
        survivorsButton = new SleekButtonIcon(icons.load<Texture2D>("Survivors"));
        survivorsButton.PositionOffset_Y = 230f;
        survivorsButton.SizeOffset_X = 200f;
        survivorsButton.SizeOffset_Y = 50f;
        survivorsButton.text = localization.format("SurvivorsButtonText");
        survivorsButton.tooltip = localization.format("SurvivorsButtonTooltip");
        survivorsButton.onClickedButton += onClickedSurvivorsButton;
        survivorsButton.fontSize = ESleekFontSize.Medium;
        survivorsButton.iconColor = ESleekTint.FOREGROUND;
        container.AddChild(survivorsButton);
        configurationButton = new SleekButtonIcon(icons.load<Texture2D>("Configuration"));
        configurationButton.PositionOffset_Y = 290f;
        configurationButton.SizeOffset_X = 200f;
        configurationButton.SizeOffset_Y = 50f;
        configurationButton.text = localization.format("ConfigurationButtonText");
        configurationButton.tooltip = localization.format("ConfigurationButtonTooltip");
        configurationButton.onClickedButton += onClickedConfigurationButton;
        configurationButton.fontSize = ESleekFontSize.Medium;
        configurationButton.iconColor = ESleekTint.FOREGROUND;
        container.AddChild(configurationButton);
        workshopButton = new SleekButtonIcon(icons.load<Texture2D>("Workshop"));
        workshopButton.PositionOffset_Y = 350f;
        workshopButton.SizeOffset_X = 200f;
        workshopButton.SizeOffset_Y = 50f;
        workshopButton.text = localization.format("WorkshopButtonText");
        workshopButton.tooltip = localization.format("WorkshopButtonTooltip");
        workshopButton.onClickedButton += onClickedWorkshopButton;
        workshopButton.fontSize = ESleekFontSize.Medium;
        workshopButton.iconColor = ESleekTint.FOREGROUND;
        container.AddChild(workshopButton);
        exitButton = new SleekButtonIcon(icons.load<Texture2D>("Exit"));
        exitButton.PositionOffset_Y = -50f;
        exitButton.PositionScale_Y = 1f;
        exitButton.SizeOffset_X = 200f;
        exitButton.SizeOffset_Y = 50f;
        exitButton.text = localization.format("ExitButtonText");
        exitButton.tooltip = localization.format("ExitButtonTooltip");
        exitButton.onClickedButton += onClickedExitButton;
        exitButton.fontSize = ESleekFontSize.Medium;
        exitButton.iconColor = ESleekTint.FOREGROUND;
        container.AddChild(exitButton);
        mainScrollView = Glazier.Get().CreateScrollView();
        mainScrollView.PositionOffset_X = 210f;
        mainScrollView.PositionOffset_Y = 170f;
        mainScrollView.SizeScale_X = 1f;
        mainScrollView.SizeScale_Y = 1f;
        mainScrollView.SizeOffset_X = -210f;
        mainScrollView.SizeOffset_Y = -170f;
        mainScrollView.ScaleContentToWidth = true;
        container.AddChild(mainScrollView);
        if (!Glazier.Get().SupportsAutomaticLayout)
        {
            ISleekLabel sleekLabel = Glazier.Get().CreateLabel();
            sleekLabel.Text = "Featured workshop file and news feed are no longer supported when using the -Glazier=IMGUI launch option.";
            sleekLabel.FontSize = ESleekFontSize.Large;
            sleekLabel.SizeScale_X = 1f;
            sleekLabel.SizeOffset_Y = 100f;
            mainScrollView.ContentSizeOffset = new Vector2(0f, 100f);
            mainScrollView.AddChild(sleekLabel);
        }
        else
        {
            mainScrollView.ContentUseManualLayout = false;
        }
        if (!Provider.isPro)
        {
            proButton = Glazier.Get().CreateButton();
            proButton.PositionOffset_X = 210f;
            proButton.PositionOffset_Y = -100f;
            proButton.PositionScale_Y = 1f;
            proButton.SizeOffset_Y = 100f;
            proButton.SizeOffset_X = -210f;
            proButton.SizeScale_X = 1f;
            proButton.TooltipText = localization.format("Pro_Button_Tooltip");
            proButton.BackgroundColor = SleekColor.BackgroundIfLight(Palette.PRO);
            proButton.TextColor = Palette.PRO;
            proButton.OnClicked += onClickedProButton;
            container.AddChild(proButton);
            proLabel = Glazier.Get().CreateLabel();
            proLabel.SizeScale_X = 1f;
            proLabel.SizeOffset_Y = 50f;
            proLabel.Text = localization.format("Pro_Title");
            proLabel.TextColor = Palette.PRO;
            proLabel.FontSize = ESleekFontSize.Large;
            proButton.AddChild(proLabel);
            featureLabel = Glazier.Get().CreateLabel();
            featureLabel.PositionOffset_Y = 50f;
            featureLabel.SizeOffset_Y = -50f;
            featureLabel.SizeScale_X = 1f;
            featureLabel.SizeScale_Y = 1f;
            featureLabel.Text = localization.format("Pro_Button");
            featureLabel.TextColor = Palette.PRO;
            proButton.AddChild(featureLabel);
            mainScrollView.SizeOffset_Y -= 110f;
        }
        mainHeaderOffset = 170f;
        alertBox = null;
        hasCreatedItemStoreButton = false;
        if (SteamApps.GetCurrentBetaName(out var pchName, 64) && string.Equals(pchName, "preview", StringComparison.InvariantCultureIgnoreCase))
        {
            CreatePreviewBranchChangelogButton();
        }
        ItemStore.Get().OnPricesReceived += OnPricesReceived;
        hasBegunQueryingLiveConfigWorkshop = false;
        LiveConfig.OnRefreshed += OnLiveConfigRefreshed;
        OnLiveConfigRefreshed();
        pauseUI = new MenuPauseUI();
        creditsUI = new MenuCreditsUI();
        titleUI = new MenuTitleUI();
        playUI = new MenuPlayUI();
        survivorsUI = new MenuSurvivorsUI();
        configUI = new MenuConfigurationUI();
        workshopUI = new MenuWorkshopUI();
        if (Provider.connectionFailureInfo != 0)
        {
            ESteamConnectionFailureInfo eSteamConnectionFailureInfo = Provider.connectionFailureInfo;
            string connectionFailureReason = Provider.connectionFailureReason;
            uint connectionFailureDuration = Provider.connectionFailureDuration;
            int serverInvalidItemsCount = Provider.provider.workshopService.serverInvalidItemsCount;
            Provider.resetConnectionFailure();
            Provider.provider.workshopService.resetServerInvalidItems();
            if (serverInvalidItemsCount > 0 && ((eSteamConnectionFailureInfo == ESteamConnectionFailureInfo.MAP || eSteamConnectionFailureInfo == ESteamConnectionFailureInfo.HASH_LEVEL || (uint)(eSteamConnectionFailureInfo - 43) <= 2u) ? true : false))
            {
                UnturnedLog.info("Connection failure {0} is asset related and therefore probably caused by the {1} download-restricted workshop item(s)", eSteamConnectionFailureInfo, serverInvalidItemsCount);
                eSteamConnectionFailureInfo = ESteamConnectionFailureInfo.WORKSHOP_DOWNLOAD_RESTRICTION;
            }
            string text = eSteamConnectionFailureInfo switch
            {
                ESteamConnectionFailureInfo.BANNED => localization.format("Banned", connectionFailureDuration, connectionFailureReason), 
                ESteamConnectionFailureInfo.KICKED => localization.format("Kicked", connectionFailureReason), 
                ESteamConnectionFailureInfo.WHITELISTED => localization.format("Whitelisted"), 
                ESteamConnectionFailureInfo.PASSWORD => localization.format("Password"), 
                ESteamConnectionFailureInfo.FULL => localization.format("Full"), 
                ESteamConnectionFailureInfo.HASH_LEVEL => localization.format("Hash_Level"), 
                ESteamConnectionFailureInfo.HASH_ASSEMBLY => localization.format("Hash_Assembly"), 
                ESteamConnectionFailureInfo.VERSION => localization.format("Version", connectionFailureReason, Provider.APP_VERSION), 
                ESteamConnectionFailureInfo.PRO_SERVER => localization.format("Pro_Server"), 
                ESteamConnectionFailureInfo.PRO_CHARACTER => localization.format("Pro_Character"), 
                ESteamConnectionFailureInfo.PRO_DESYNC => localization.format("Pro_Desync"), 
                ESteamConnectionFailureInfo.PRO_APPEARANCE => localization.format("Pro_Appearance"), 
                ESteamConnectionFailureInfo.AUTH_VERIFICATION => localization.format("Auth_Verification"), 
                ESteamConnectionFailureInfo.AUTH_NO_STEAM => localization.format("Auth_No_Steam"), 
                ESteamConnectionFailureInfo.AUTH_LICENSE_EXPIRED => localization.format("Auth_License_Expired"), 
                ESteamConnectionFailureInfo.AUTH_VAC_BAN => localization.format("Auth_VAC_Ban"), 
                ESteamConnectionFailureInfo.AUTH_ELSEWHERE => localization.format("Auth_Elsewhere"), 
                ESteamConnectionFailureInfo.AUTH_TIMED_OUT => localization.format("Auth_Timed_Out"), 
                ESteamConnectionFailureInfo.AUTH_USED => localization.format("Auth_Used"), 
                ESteamConnectionFailureInfo.AUTH_NO_USER => localization.format("Auth_No_User"), 
                ESteamConnectionFailureInfo.AUTH_PUB_BAN => localization.format("Auth_Pub_Ban"), 
                ESteamConnectionFailureInfo.AUTH_NETWORK_IDENTITY_FAILURE => localization.format("Auth_Network_Identity_Failure"), 
                ESteamConnectionFailureInfo.AUTH_ECON_SERIALIZE => localization.format("Auth_Econ_Serialize"), 
                ESteamConnectionFailureInfo.AUTH_ECON_DESERIALIZE => localization.format("Auth_Econ_Deserialize"), 
                ESteamConnectionFailureInfo.AUTH_ECON_VERIFY => localization.format("Auth_Econ_Verify"), 
                ESteamConnectionFailureInfo.AUTH_EMPTY => localization.format("Auth_Empty"), 
                ESteamConnectionFailureInfo.ALREADY_CONNECTED => localization.format("Already_Connected"), 
                ESteamConnectionFailureInfo.ALREADY_PENDING => localization.format("Already_Pending"), 
                ESteamConnectionFailureInfo.LATE_PENDING => localization.format("Late_Pending"), 
                ESteamConnectionFailureInfo.NOT_PENDING => localization.format("Not_Pending"), 
                ESteamConnectionFailureInfo.NAME_PLAYER_SHORT => localization.format("Name_Player_Short"), 
                ESteamConnectionFailureInfo.NAME_PLAYER_LONG => localization.format("Name_Player_Long"), 
                ESteamConnectionFailureInfo.NAME_PLAYER_INVALID => localization.format("Name_Player_Invalid"), 
                ESteamConnectionFailureInfo.NAME_PLAYER_NUMBER => localization.format("Name_Player_Number"), 
                ESteamConnectionFailureInfo.NAME_CHARACTER_SHORT => localization.format("Name_Character_Short"), 
                ESteamConnectionFailureInfo.NAME_CHARACTER_LONG => localization.format("Name_Character_Long"), 
                ESteamConnectionFailureInfo.NAME_CHARACTER_INVALID => localization.format("Name_Character_Invalid"), 
                ESteamConnectionFailureInfo.NAME_CHARACTER_NUMBER => localization.format("Name_Character_Number"), 
                ESteamConnectionFailureInfo.TIMED_OUT => localization.format("Timed_Out"), 
                ESteamConnectionFailureInfo.TIMED_OUT_LOGIN => localization.format("Timed_Out_Login"), 
                ESteamConnectionFailureInfo.MAP => localization.format("Map"), 
                ESteamConnectionFailureInfo.SHUTDOWN => string.IsNullOrEmpty(connectionFailureReason) ? localization.format("Shutdown") : localization.format("Shutdown_Reason", connectionFailureReason), 
                ESteamConnectionFailureInfo.PING => connectionFailureReason, 
                ESteamConnectionFailureInfo.PLUGIN => string.IsNullOrEmpty(connectionFailureReason) ? localization.format("Plugin") : localization.format("Plugin_Reason", connectionFailureReason), 
                ESteamConnectionFailureInfo.BARRICADE => localization.format("Barricade", connectionFailureReason), 
                ESteamConnectionFailureInfo.STRUCTURE => localization.format("Structure", connectionFailureReason), 
                ESteamConnectionFailureInfo.VEHICLE => localization.format("Vehicle", connectionFailureReason), 
                ESteamConnectionFailureInfo.CLIENT_MODULE_DESYNC => localization.format("Client_Module_Desync"), 
                ESteamConnectionFailureInfo.SERVER_MODULE_DESYNC => localization.format("Server_Module_Desync"), 
                ESteamConnectionFailureInfo.BATTLEYE_BROKEN => localization.format("BattlEye_Broken"), 
                ESteamConnectionFailureInfo.BATTLEYE_UPDATE => localization.format("BattlEye_Update"), 
                ESteamConnectionFailureInfo.BATTLEYE_UNKNOWN => localization.format("BattlEye_Unknown"), 
                ESteamConnectionFailureInfo.LEVEL_VERSION => connectionFailureReason, 
                ESteamConnectionFailureInfo.ECON_HASH => localization.format("Econ_Hash"), 
                ESteamConnectionFailureInfo.HASH_MASTER_BUNDLE => localization.format("Master_Bundle_Hash", connectionFailureReason), 
                ESteamConnectionFailureInfo.REJECT_UNKNOWN => localization.format("Reject_Unknown", connectionFailureReason), 
                ESteamConnectionFailureInfo.WORKSHOP_DOWNLOAD_RESTRICTION => localization.format("Workshop_Download_Restriction", serverInvalidItemsCount), 
                ESteamConnectionFailureInfo.WORKSHOP_ADVERTISEMENT_MISMATCH => localization.format("Workshop_Advertisement_Mismatch"), 
                ESteamConnectionFailureInfo.CUSTOM => connectionFailureReason, 
                ESteamConnectionFailureInfo.LATE_PENDING_STEAM_AUTH => localization.format("Late_Pending_Steam_Auth"), 
                ESteamConnectionFailureInfo.LATE_PENDING_STEAM_ECON => localization.format("Late_Pending_Steam_Econ"), 
                ESteamConnectionFailureInfo.LATE_PENDING_STEAM_GROUPS => localization.format("Late_Pending_Steam_Groups"), 
                ESteamConnectionFailureInfo.NAME_PRIVATE_LONG => localization.format("Name_Private_Long"), 
                ESteamConnectionFailureInfo.NAME_PRIVATE_INVALID => localization.format("Name_Private_Invalid"), 
                ESteamConnectionFailureInfo.NAME_PRIVATE_NUMBER => localization.format("Name_Private_Number"), 
                ESteamConnectionFailureInfo.HASH_RESOURCES => localization.format("Hash_Resources"), 
                ESteamConnectionFailureInfo.SKIN_COLOR_WITHIN_THRESHOLD_OF_TERRAIN_COLOR => localization.format("SkinColorWithinThresholdOfTerrainColor"), 
                ESteamConnectionFailureInfo.STEAM_ID_MISMATCH => localization.format("Steam_ID_Mismatch"), 
                ESteamConnectionFailureInfo.CONNECT_RATE_LIMITING => localization.format("Connect_Rate_Limiting"), 
                ESteamConnectionFailureInfo.SERVER_MAP_ADVERTISEMENT_MISMATCH => localization.format("Server_Map_Advertisement_Mismatch"), 
                ESteamConnectionFailureInfo.SERVER_VAC_ADVERTISEMENT_MISMATCH => localization.format("Server_VAC_Advertisement_Mismatch"), 
                ESteamConnectionFailureInfo.SERVER_BATTLEYE_ADVERTISEMENT_MISMATCH => localization.format("Server_BattlEye_Advertisement_Mismatch"), 
                ESteamConnectionFailureInfo.SERVER_MAXPLAYERS_ADVERTISEMENT_MISMATCH => localization.format("Server_MaxPlayers_Advertisement_Mismatch"), 
                ESteamConnectionFailureInfo.SERVER_CAMERAMODE_ADVERTISEMENT_MISMATCH => localization.format("Server_CameraMode_Advertisement_Mismatch"), 
                ESteamConnectionFailureInfo.SERVER_PVP_ADVERTISEMENT_MISMATCH => localization.format("Server_PvP_Advertisement_Mismatch"), 
                _ => localization.format("Failure_Unknown", eSteamConnectionFailureInfo, connectionFailureReason), 
            };
            if (string.IsNullOrEmpty(text))
            {
                text = $"Error: {eSteamConnectionFailureInfo} Reason: {connectionFailureReason}";
            }
            MenuUI.alert(text);
            UnturnedLog.info(text);
        }
        if (SteamUser.BLoggedOn() && Glazier.Get().SupportsAutomaticLayout && !readNewsPreview())
        {
            MenuUI.instance.StartCoroutine(MenuUI.instance.requestSteamNews());
        }
    }

    private void OnClickedPreviewBranchChangelog(ISleekElement button)
    {
        Provider.provider.browserService.open("https://support.smartlydressedgames.com/hc/en-us/articles/12462494977172");
    }

    private void CreatePreviewBranchChangelogButton()
    {
        ISleekButton sleekButton = Glazier.Get().CreateButton();
        sleekButton.PositionOffset_X = 210f;
        sleekButton.PositionOffset_Y = mainHeaderOffset;
        sleekButton.SizeOffset_Y = 60f;
        sleekButton.SizeOffset_X = -210f;
        sleekButton.SizeScale_X = 1f;
        sleekButton.Text = "Click here to open preview branch changelog.";
        sleekButton.OnClicked += OnClickedPreviewBranchChangelog;
        container.AddChild(sleekButton);
        mainHeaderOffset += sleekButton.SizeOffset_Y + 10f;
        mainScrollView.PositionOffset_Y += sleekButton.SizeOffset_Y + 10f;
        mainScrollView.SizeOffset_Y -= sleekButton.SizeOffset_Y + 10f;
    }
}
