using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using SDG.NetTransport;
using SDG.SteamworksProvider.Services.Store;
using Steamworks;
using UnityEngine;
using UnityEngine.UI;
using Unturned.LiveConfig;

namespace SDG.Unturned;

public class MenuDashboardUI
{
    public static Local localization;

    public static Bundle icons;

    private static SleekFullscreenBox container;

    public static bool active;

    private static bool canvasActive;

    private static SleekButtonIcon playButton;

    private static SleekButtonIcon survivorsButton;

    private static SleekButtonIcon configurationButton;

    private static SleekButtonIcon workshopButton;

    private static SleekButtonIcon exitButton;

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

    private static ISleekLabel alertBodyLabel;

    private static GameObject canvas;

    private static GameObject templateNews;

    private static GameObject templateWorkshop;

    private static GameObject templateText;

    private static GameObject templateImage;

    private static GameObject templateLink;

    private static GameObject templateReadMoreLink;

    private static GameObject templateReadMoreContent;

    private static GameObject templateWorkshopLinks;

    private static int mainHeaderOffset;

    private static NewsResponse newsResponse;

    private static bool hasNewAnnouncement;

    private static UGCQueryHandle_t popularWorkshopHandle = UGCQueryHandle_t.Invalid;

    private static UGCQueryHandle_t featuredWorkshopHandle = UGCQueryHandle_t.Invalid;

    private static CallResult<SteamUGCQueryCompleted_t> steamUGCQueryCompletedPopular;

    private static CallResult<SteamUGCQueryCompleted_t> steamUGCQueryCompletedFeatured;

    private static bool hasBegunQueryingLiveConfigWorkshop;

    private MenuPauseUI pauseUI;

    private MenuCreditsUI creditsUI;

    private MenuTitleUI titleUI;

    private MenuPlayUI playUI;

    private MenuSurvivorsUI survivorsUI;

    private MenuConfigurationUI configUI;

    private MenuWorkshopUI workshopUI;

    public static void setCanvasActive(bool isActive)
    {
        canvasActive = isActive;
        if (!(canvas == null))
        {
            canvas.GetComponent<Canvas>().enabled = active && canvasActive;
        }
    }

    public static void open()
    {
        if (!active)
        {
            active = true;
            setCanvasActive(isActive: true);
            container.AnimateIntoView();
        }
    }

    public static void close()
    {
        if (active)
        {
            active = false;
            setCanvasActive(isActive: false);
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
        global::Unturned.LiveConfig.LiveConfig liveConfig = LiveConfig.Get();
        if (!string.IsNullOrEmpty(liveConfig.MainMenuAlert.Link))
        {
            if (!Provider.provider.browserService.canOpenBrowser)
            {
                MenuUI.alert(MenuSurvivorsCharacterUI.localization.format("Overlay"));
                return;
            }
            Provider.provider.browserService.open(liveConfig.MainMenuAlert.Link);
        }
        ConvenientSavedata.get().write("MainMenuAlertSeenId", liveConfig.MainMenuAlert.Id);
    }

    private static void filterContent(string header, string source, ref string contents, ref int lines)
    {
        int num = source.IndexOf("[b]" + header + ":[/b]");
        if (num == -1)
        {
            return;
        }
        contents = contents + "<i>" + header + "</i>:\n";
        lines += 2;
        int num2 = source.IndexOf("[list]", num);
        int num3 = source.IndexOf("[/list]", num2);
        string[] array = source.Substring(num2 + 6, num3 - (num2 + 6)).Split(new string[1] { "[*]" }, StringSplitOptions.RemoveEmptyEntries);
        foreach (string text in array)
        {
            if (text.Length > 0)
            {
                contents += text;
                lines++;
            }
        }
        contents += "\n";
        lines++;
    }

    private static void insertContent(GameObject news, string contents)
    {
        if (string.IsNullOrEmpty(contents))
        {
            return;
        }
        SteamBBCodeUtils.removeYouTubePreviews(ref contents);
        SteamBBCodeUtils.removeCodeFormatting(ref contents);
        int num = 0;
        for (int i = 0; i < 1000; i++)
        {
            int num2 = contents.IndexOf("[h1]", num + 1);
            int num3 = contents.IndexOf("[b]", num + 1);
            int num4 = ((num2 == -1 && num3 == -1) ? (-1) : ((num2 == -1) ? num3 : ((num3 == -1) ? num2 : ((num2 >= num3) ? num3 : num2))));
            string text = ((num4 != -1) ? contents.Substring(num, num4 - num) : contents.Substring(num));
            List<SubcontentInfo> list = new List<SubcontentInfo>();
            int num5 = 0;
            for (int j = 0; j < 1000; j++)
            {
                int num6 = text.IndexOf("[img]", num5);
                int num7 = text.IndexOf("[url=", num5);
                if (num6 == -1 && num7 == -1)
                {
                    SubcontentInfo subcontentInfo = new SubcontentInfo();
                    subcontentInfo.content = text.Substring(num5);
                    list.Add(subcontentInfo);
                    break;
                }
                int num8;
                bool flag;
                if (num6 == -1)
                {
                    num8 = num7;
                    flag = false;
                }
                else if (num7 == -1)
                {
                    num8 = num6;
                    flag = true;
                }
                else if (num6 < num7)
                {
                    num8 = num6;
                    flag = true;
                }
                else
                {
                    num8 = num7;
                    flag = false;
                }
                SubcontentInfo subcontentInfo2 = new SubcontentInfo();
                subcontentInfo2.content = text.Substring(num5, num8 - num5);
                list.Add(subcontentInfo2);
                int num10;
                if (flag)
                {
                    int num9 = text.IndexOf("[/img]", num6);
                    string url = text.Substring(num6 + 5, num9 - num6 - 5);
                    SubcontentInfo subcontentInfo3 = new SubcontentInfo();
                    subcontentInfo3.url = url;
                    subcontentInfo3.isImage = true;
                    list.Add(subcontentInfo3);
                    num10 = num9;
                }
                else
                {
                    int num11 = text.IndexOf("[/url]", num7);
                    int num12 = text.IndexOf("]", num7);
                    string url2 = text.Substring(num7 + 5, num12 - num7 - 5);
                    string content = text.Substring(num12 + 1, num11 - num12 - 1);
                    SubcontentInfo subcontentInfo4 = new SubcontentInfo();
                    subcontentInfo4.content = content;
                    subcontentInfo4.url = url2;
                    subcontentInfo4.isLink = true;
                    list.Add(subcontentInfo4);
                    num10 = num11;
                }
                num5 = num10 + 6;
            }
            for (int k = 0; k < list.Count; k++)
            {
                SubcontentInfo subcontentInfo5 = list[k];
                if (subcontentInfo5.isImage)
                {
                    GameObject gameObject = UnityEngine.Object.Instantiate(templateImage);
                    gameObject.name = "Image";
                    gameObject.GetComponent<RectTransform>().SetParent(news.transform, worldPositionStays: true);
                    gameObject.SetActive(value: true);
                    subcontentInfo5.url = subcontentInfo5.url.Replace("{STEAM_CLAN_IMAGE}", "https://steamcdn-a.akamaihd.net/steamcommunity/public/images/clans/");
                    gameObject.transform.GetChild(0).GetComponent<WebImage>().url = subcontentInfo5.url;
                    continue;
                }
                if (subcontentInfo5.isLink)
                {
                    GameObject gameObject2 = UnityEngine.Object.Instantiate(templateLink);
                    gameObject2.name = "Link";
                    gameObject2.GetComponent<RectTransform>().SetParent(news.transform, worldPositionStays: true);
                    gameObject2.SetActive(value: true);
                    gameObject2.GetComponent<Text>().text = subcontentInfo5.content;
                    gameObject2.GetComponent<WebLink>().url = subcontentInfo5.url;
                    continue;
                }
                subcontentInfo5.content = subcontentInfo5.content.TrimStart('\r', '\n');
                subcontentInfo5.content = subcontentInfo5.content.Replace("[b]", "<b>");
                subcontentInfo5.content = subcontentInfo5.content.Replace("[/b]", "</b>");
                subcontentInfo5.content = subcontentInfo5.content.Replace("[i]", "<i>");
                subcontentInfo5.content = subcontentInfo5.content.Replace("[/i]", "</i>");
                subcontentInfo5.content = subcontentInfo5.content.Replace("[list]", "");
                subcontentInfo5.content = subcontentInfo5.content.Replace("[/list]", "");
                subcontentInfo5.content = subcontentInfo5.content.Replace("[*]", "- ");
                subcontentInfo5.content = subcontentInfo5.content.Replace("[h1]", "<color=#5aa9d6><size=18>");
                subcontentInfo5.content = subcontentInfo5.content.Replace("[/h1]", "</size></color>");
                subcontentInfo5.content = subcontentInfo5.content.TrimEnd('\r', '\n');
                if (string.IsNullOrEmpty(subcontentInfo5.content))
                {
                    continue;
                }
                string[] array = subcontentInfo5.content.Split('\r', '\n');
                string text2 = string.Empty;
                string[] array2 = array;
                for (int l = 0; l < array2.Length; l++)
                {
                    string text3 = array2[l].Trim();
                    if (string.IsNullOrEmpty(text3))
                    {
                        continue;
                    }
                    if (text3.StartsWith("- "))
                    {
                        if (!string.IsNullOrEmpty(text2))
                        {
                            text2 += "\n";
                        }
                        text2 += text3;
                        continue;
                    }
                    if (!string.IsNullOrEmpty(text2))
                    {
                        GameObject gameObject3 = UnityEngine.Object.Instantiate(templateText);
                        gameObject3.name = "Text";
                        gameObject3.GetComponent<RectTransform>().SetParent(news.transform, worldPositionStays: true);
                        gameObject3.SetActive(value: true);
                        gameObject3.GetComponent<Text>().text = text2;
                    }
                    text2 = text3;
                    GameObject gameObject4 = UnityEngine.Object.Instantiate(templateText);
                    gameObject4.name = "Text";
                    gameObject4.GetComponent<RectTransform>().SetParent(news.transform, worldPositionStays: true);
                    gameObject4.SetActive(value: true);
                    gameObject4.GetComponent<Text>().text = text2;
                    text2 = string.Empty;
                }
                if (!string.IsNullOrEmpty(text2))
                {
                    GameObject gameObject5 = UnityEngine.Object.Instantiate(templateText);
                    gameObject5.name = "Text";
                    gameObject5.GetComponent<RectTransform>().SetParent(news.transform, worldPositionStays: true);
                    gameObject5.SetActive(value: true);
                    gameObject5.GetComponent<Text>().text = text2;
                }
            }
            if (num4 != -1)
            {
                num = num4;
                continue;
            }
            break;
        }
    }

    private static void receiveNewsResponse()
    {
        if (templateNews == null)
        {
            UnturnedLog.warn("Missing templateNews to create for news web response");
            return;
        }
        for (int i = 0; i < newsResponse.AppNews.NewsItems.Length; i++)
        {
            NewsItem newsItem = newsResponse.AppNews.NewsItems[i];
            if (newsItem == null)
            {
                continue;
            }
            GameObject gameObject = UnityEngine.Object.Instantiate(templateNews);
            gameObject.name = "News_" + i;
            gameObject.GetComponent<RectTransform>().SetParent(templateNews.transform.parent, worldPositionStays: true);
            gameObject.SetActive(value: true);
            if (i == 0)
            {
                if (ConvenientSavedata.get().read("Newest_Announcement", out long value))
                {
                    hasNewAnnouncement = value != newsItem.Date;
                }
                else
                {
                    hasNewAnnouncement = true;
                }
                if (hasNewAnnouncement)
                {
                    ConvenientSavedata.get().write("Newest_Announcement", newsItem.Date);
                    gameObject.transform.SetAsFirstSibling();
                }
            }
            gameObject.transform.Find("Title").GetComponent<Text>().text = newsItem.Title;
            DateTime dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).AddSeconds(newsItem.Date).ToLocalTime();
            gameObject.transform.Find("Author").GetComponent<Text>().text = localization.format("News_Author", dateTime, newsItem.Author);
            try
            {
                insertContent(gameObject, newsItem.Contents);
            }
            catch (Exception e)
            {
                UnturnedLog.warn("Announcement description mis-formatted! Nelson messed up.");
                UnturnedLog.exception(e);
            }
            GameObject gameObject2 = UnityEngine.Object.Instantiate(templateLink);
            gameObject2.name = "Comments";
            gameObject2.GetComponent<RectTransform>().SetParent(gameObject.transform, worldPositionStays: true);
            gameObject2.SetActive(value: true);
            gameObject2.GetComponent<Text>().text = localization.format("News_Comments_Link");
            gameObject2.GetComponent<WebLink>().url = newsItem.URL;
        }
    }

    private static void OnUpdateDetected(string versionString, bool isRollback)
    {
        ISleekBox sleekBox = Glazier.Get().CreateBox();
        sleekBox.positionOffset_X = 210;
        sleekBox.positionOffset_Y = mainHeaderOffset;
        sleekBox.sizeOffset_Y = 40;
        sleekBox.sizeOffset_X = -210;
        sleekBox.sizeScale_X = 1f;
        sleekBox.fontSize = ESleekFontSize.Medium;
        container.AddChild(sleekBox);
        string key = (isRollback ? "RollbackAvailable" : "UpdateAvailable");
        string s = localization.format(key, versionString);
        RichTextUtil.replaceNewlineMarkup(ref s);
        sleekBox.text = s;
        mainHeaderOffset += sleekBox.sizeOffset_Y + 10;
        canvas.transform.Find("View").GetComponent<RectTransform>().offsetMax -= new Vector2(0f, 50f);
        canvas.transform.Find("Scrollbar").GetComponent<RectTransform>().offsetMax -= new Vector2(0f, 50f);
    }

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
        newsResponse = new NewsResponse();
        newsResponse.AppNews = new AppNews();
        newsResponse.AppNews.NewsItems = new NewsItem[1] { newsItem };
        hasNewAnnouncement = true;
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
        if (templateWorkshop == null)
        {
            UnturnedLog.warn("Missing templateWorkshop to create for featured workshop article");
            return;
        }
        GameObject gameObject = UnityEngine.Object.Instantiate(templateWorkshop);
        gameObject.name = "Workshop";
        gameObject.GetComponent<RectTransform>().SetParent(templateNews.transform.parent, worldPositionStays: true);
        gameObject.SetActive(value: true);
        if (hasNewAnnouncement)
        {
            gameObject.transform.SetSiblingIndex(1);
        }
        else
        {
            gameObject.transform.SetAsFirstSibling();
        }
        MainMenuWorkshopFeaturedLiveConfig featured = LiveConfig.Get().MainMenuWorkshop.Featured;
        bool flag = featured.IsFeatured(pDetails.m_nPublishedFileId.m_PublishedFileId);
        string key = ((!flag) ? "Featured_Workshop_Title" : (featured.Type switch
        {
            EFeaturedWorkshopType.Curated => "Curated_Workshop_Title", 
            _ => "Highlighted_Workshop_Title", 
        }));
        string text = localization.format(key, pDetails.m_rgchTitle);
        Transform transform = gameObject.transform.Find("TitleLayout");
        transform.Find("Title").GetComponent<Text>().text = text;
        Transform transform2 = transform.Find("Status");
        if (flag && featured.Status != 0)
        {
            string text2 = Provider.localization.format((featured.Status == EMapStatus.Updated) ? "Updated" : "New");
            transform2.GetComponent<Text>().text = text2;
            transform2.gameObject.SetActive(value: true);
        }
        else
        {
            transform2.gameObject.SetActive(value: false);
        }
        Transform transform3 = transform.Find("Dismiss");
        transform3.GetComponent<Text>().text = localization.format("Featured_Workshop_Dismiss");
        DismissWorkshopArticle component = transform3.GetComponent<DismissWorkshopArticle>();
        component.id = pDetails.m_nPublishedFileId.m_PublishedFileId;
        component.article = gameObject;
        if (SteamUGC.GetQueryUGCPreviewURL(featuredWorkshopHandle, 0u, out var pchURL, 1024u))
        {
            GameObject gameObject2 = UnityEngine.Object.Instantiate(templateImage);
            gameObject2.name = "Preview";
            gameObject2.GetComponent<RectTransform>().SetParent(gameObject.transform, worldPositionStays: true);
            gameObject2.SetActive(value: true);
            gameObject2.transform.GetChild(0).GetComponent<WebImage>().url = pchURL;
            gameObject2.transform.GetChild(0).gameObject.AddComponent<LayoutElement>().preferredWidth = 960f;
        }
        GameObject gameObject3 = UnityEngine.Object.Instantiate(templateReadMoreLink);
        gameObject3.name = "ReadMore_Link";
        gameObject3.GetComponent<RectTransform>().SetParent(gameObject.transform, worldPositionStays: true);
        gameObject3.SetActive(value: true);
        GameObject gameObject4 = UnityEngine.Object.Instantiate(templateReadMoreContent);
        gameObject4.name = "ReadMore_Content";
        gameObject4.GetComponent<RectTransform>().SetParent(gameObject.transform, worldPositionStays: true);
        if (flag && featured.AutoExpandDescription)
        {
            gameObject4.SetActive(value: true);
        }
        PublishedFileId_t nPublishedFileId;
        try
        {
            string contents = pDetails.m_rgchDescription;
            if (flag && !string.IsNullOrEmpty(featured.OverrideDescription))
            {
                contents = featured.OverrideDescription;
            }
            insertContent(gameObject4, contents);
        }
        catch (Exception e)
        {
            nPublishedFileId = pDetails.m_nPublishedFileId;
            UnturnedLog.warn("Workshop description mis-formatted! If you made this item I suggest you adjust it to display in-game properly: " + nPublishedFileId.ToString());
            UnturnedLog.exception(e);
        }
        gameObject3.GetComponent<ReadMore>().targetContent = gameObject4;
        gameObject3.GetComponent<ReadMore>().onText = localization.format("ReadMore_Link_On");
        gameObject3.GetComponent<ReadMore>().offText = localization.format("ReadMore_Link_Off");
        gameObject3.GetComponent<ReadMore>().Refresh();
        if (templateWorkshopLinks == null)
        {
            UnturnedLog.warn("Missing templateWorkshopLinks to create for featured workshop article");
            return;
        }
        GameObject gameObject5 = UnityEngine.Object.Instantiate(templateWorkshopLinks);
        gameObject5.GetComponent<RectTransform>().SetParent(gameObject.transform, worldPositionStays: true);
        gameObject5.SetActive(value: true);
        Transform transform4 = gameObject5.transform.Find("ViewButton");
        transform4.Find("ViewLabel").GetComponent<Text>().text = localization.format("Featured_Workshop_Link");
        WebLink component2 = transform4.GetComponent<WebLink>();
        nPublishedFileId = pDetails.m_nPublishedFileId;
        component2.url = "http://steamcommunity.com/sharedfiles/filedetails/?id=" + nPublishedFileId.ToString();
        Transform transform5 = gameObject5.transform.Find("StockpileButton");
        int[] associatedStockpileItems = featured.AssociatedStockpileItems;
        if (flag && associatedStockpileItems != null && associatedStockpileItems.Length != 0)
        {
            List<int> list = new List<int>(associatedStockpileItems.Length);
            int[] array = associatedStockpileItems;
            foreach (int num in array)
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
                    transform5.gameObject.SetActive(value: false);
                    UnturnedLog.warn("Unknown itemdefid {0} specified in featured workshop stockpile items", num2);
                }
                else
                {
                    Transform transform6 = transform5.Find("StockpileLabel");
                    string text3 = localization.format("Featured_Workshop_Stockpile_Link", inventoryName);
                    transform6.GetComponent<Text>().text = text3;
                    transform5.GetComponent<StockpileLink>().itemdefid = num2;
                    transform5.gameObject.SetActive(value: true);
                }
            }
            else
            {
                transform5.gameObject.SetActive(value: false);
            }
        }
        else
        {
            transform5.gameObject.SetActive(value: false);
        }
        Transform transform7 = gameObject5.transform.Find("NewsButton");
        Transform transform8 = transform7.Find("NewsLabel");
        if (flag && !string.IsNullOrEmpty(featured.LinkURL))
        {
            transform7.gameObject.SetActive(value: true);
            transform8.GetComponent<Text>().text = featured.LinkText;
            transform7.GetComponent<WebLink>().url = featured.LinkURL;
        }
        else
        {
            transform7.gameObject.SetActive(value: false);
        }
        ToggleWorkshopSubscription component3 = gameObject5.transform.Find("SubscribeButton").GetComponent<ToggleWorkshopSubscription>();
        component3.parentFileId = pDetails.m_nPublishedFileId;
        component3.childFileIds = new PublishedFileId_t[pDetails.m_unNumChildren];
        SteamUGC.GetQueryUGCChildren(featuredWorkshopHandle, 0u, component3.childFileIds, pDetails.m_unNumChildren);
        component3.subscribeText = localization.format("Featured_Workshop_Sub");
        component3.unsubscribeText = localization.format("Featured_Workshop_Unsub");
        component3.Refresh();
    }

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
        if (LocalNews.wasWorkshopItemDismissed(pDetails.m_nPublishedFileId.m_PublishedFileId))
        {
            return false;
        }
        queryFeaturedItem(pDetails.m_nPublishedFileId);
        return true;
    }

    private static void handlePopularItemResults(SteamUGCQueryCompleted_t callback)
    {
        UnturnedLog.info("Received popular workshop files");
        uint unNumResultsReturned = callback.m_unNumResultsReturned;
        if (unNumResultsReturned < 1)
        {
            UnturnedLog.warn("Popular workshop items response was empty");
            return;
        }
        MainMenuWorkshopPopularLiveConfig popular = LiveConfig.Get().MainMenuWorkshop.Popular;
        int num = Mathf.Min((int)unNumResultsReturned, popular.CarouselItems);
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

    private static void queryPopularWorkshopItems()
    {
        MainMenuWorkshopPopularLiveConfig popular = LiveConfig.Get().MainMenuWorkshop.Popular;
        uint num = popular.TrendDays;
        if (num < 1 || popular.CarouselItems < 1)
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

    private static void OnPricesReceived()
    {
        ItemStore itemStore = ItemStore.Get();
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
        sleekItemStoreMainMenuButton.positionOffset_Y = 410;
        sleekItemStoreMainMenuButton.sizeOffset_X = 200;
        sleekItemStoreMainMenuButton.sizeOffset_Y = 50;
        container.AddChild(sleekItemStoreMainMenuButton);
    }

    private static void PopulateAlertFromLiveConfig()
    {
        global::Unturned.LiveConfig.LiveConfig liveConfig = LiveConfig.Get();
        if (string.IsNullOrEmpty(liveConfig.MainMenuAlert.Header) || string.IsNullOrEmpty(liveConfig.MainMenuAlert.Body) || (ConvenientSavedata.get().read("MainMenuAlertSeenId", out long value) && value >= liveConfig.MainMenuAlert.Id))
        {
            return;
        }
        bool flag = false;
        if (liveConfig.MainMenuAlert.UseTimeWindow && !flag)
        {
            DateTime utcNow = DateTime.UtcNow;
            if (utcNow < liveConfig.MainMenuAlert.StartTime || utcNow > liveConfig.MainMenuAlert.EndTime)
            {
                return;
            }
        }
        if (alertBox == null)
        {
            alertBox = Glazier.Get().CreateButton();
            alertBox.onClickedButton += onClickedAlertButton;
            alertBox.positionOffset_X = 210;
            alertBox.positionOffset_Y = mainHeaderOffset;
            alertBox.sizeOffset_Y = 60;
            alertBox.sizeOffset_X = -210;
            alertBox.sizeScale_X = 1f;
            container.AddChild(alertBox);
            alertImage = Glazier.Get().CreateImage();
            alertImage.positionOffset_X = 10;
            alertImage.positionOffset_Y = 10;
            alertImage.sizeOffset_X = 40;
            alertImage.sizeOffset_Y = 40;
            alertImage.isVisible = false;
            alertBox.AddChild(alertImage);
            alertWebImage = new SleekWebImage();
            alertWebImage.positionOffset_X = 10;
            alertWebImage.positionOffset_Y = 10;
            alertWebImage.sizeOffset_X = 40;
            alertWebImage.sizeOffset_Y = 40;
            alertWebImage.isVisible = false;
            alertBox.AddChild(alertWebImage);
            alertHeaderLabel = Glazier.Get().CreateLabel();
            alertHeaderLabel.positionOffset_X = 60;
            alertHeaderLabel.sizeScale_X = 1f;
            alertHeaderLabel.sizeOffset_X = -70;
            alertHeaderLabel.sizeOffset_Y = 30;
            alertHeaderLabel.fontAlignment = TextAnchor.MiddleLeft;
            alertHeaderLabel.fontSize = ESleekFontSize.Medium;
            alertHeaderLabel.shadowStyle = ETextContrastContext.ColorfulBackdrop;
            alertBox.AddChild(alertHeaderLabel);
            alertLinkLabel = Glazier.Get().CreateLabel();
            alertLinkLabel.positionOffset_X = 60;
            alertLinkLabel.sizeScale_X = 1f;
            alertLinkLabel.sizeOffset_X = -70;
            alertLinkLabel.sizeOffset_Y = 30;
            alertLinkLabel.fontAlignment = TextAnchor.MiddleRight;
            alertLinkLabel.fontSize = ESleekFontSize.Small;
            alertLinkLabel.isVisible = false;
            alertBox.AddChild(alertLinkLabel);
            alertBodyLabel = Glazier.Get().CreateLabel();
            alertBodyLabel.positionOffset_X = 60;
            alertBodyLabel.positionOffset_Y = 20;
            alertBodyLabel.sizeOffset_X = -70;
            alertBodyLabel.sizeOffset_Y = -20;
            alertBodyLabel.sizeScale_X = 1f;
            alertBodyLabel.sizeScale_Y = 1f;
            alertBodyLabel.fontAlignment = TextAnchor.UpperLeft;
            alertBodyLabel.textColor = ESleekTint.RICH_TEXT_DEFAULT;
            alertBodyLabel.shadowStyle = ETextContrastContext.InconspicuousBackdrop;
            alertBodyLabel.enableRichText = true;
            alertBox.AddChild(alertBodyLabel);
            mainHeaderOffset += alertBox.sizeOffset_Y + 10;
            canvas.transform.Find("View").GetComponent<RectTransform>().offsetMax -= new Vector2(0f, 70f);
            canvas.transform.Find("Scrollbar").GetComponent<RectTransform>().offsetMax -= new Vector2(0f, 70f);
        }
        Color color = Palette.hex(liveConfig.MainMenuAlert.Color);
        alertHeaderLabel.text = liveConfig.MainMenuAlert.Header;
        alertHeaderLabel.textColor = color;
        alertBodyLabel.text = liveConfig.MainMenuAlert.Body;
        if (!string.IsNullOrEmpty(liveConfig.MainMenuAlert.IconName))
        {
            alertImage.texture = icons.load<Texture2D>(liveConfig.MainMenuAlert.IconName);
            alertImage.color = color;
            alertImage.isVisible = true;
        }
        else
        {
            alertImage.isVisible = false;
        }
        if (!string.IsNullOrEmpty(liveConfig.MainMenuAlert.IconURL))
        {
            alertWebImage.Refresh(liveConfig.MainMenuAlert.IconURL);
            alertWebImage.isVisible = true;
        }
        else
        {
            alertWebImage.isVisible = false;
        }
        if (!string.IsNullOrEmpty(liveConfig.MainMenuAlert.Link))
        {
            alertLinkLabel.text = liveConfig.MainMenuAlert.Link;
            alertLinkLabel.textColor = color * 0.5f;
            alertLinkLabel.isVisible = true;
        }
        else
        {
            alertLinkLabel.isVisible = false;
        }
    }

    private static void UpdateWorkshopFromLiveConfig()
    {
        MainMenuWorkshopLiveConfig mainMenuWorkshop = LiveConfig.Get().MainMenuWorkshop;
        if (mainMenuWorkshop.AllowNews && SteamUser.BLoggedOn() && !hasBegunQueryingLiveConfigWorkshop)
        {
            hasBegunQueryingLiveConfigWorkshop = true;
            ulong num = 0uL;
            if (mainMenuWorkshop.Featured.FileIds != null && mainMenuWorkshop.Featured.FileIds.Length != 0)
            {
                num = mainMenuWorkshop.Featured.FileIds.RandomOrDefault();
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
        canvas = GameObject.Find("Canvas");
        templateNews = canvas.transform.Find("View").Find("List").Find("Template_News")
            .gameObject;
        templateWorkshop = canvas.transform.Find("View").Find("List").Find("Template_Workshop")
            .gameObject;
        templateText = templateNews.transform.parent.Find("Template_Text").gameObject;
        templateImage = templateNews.transform.parent.Find("Template_Image").gameObject;
        templateLink = templateNews.transform.parent.Find("Template_Link").gameObject;
        templateReadMoreLink = templateNews.transform.parent.Find("Template_ReadMore_Link").gameObject;
        templateReadMoreContent = templateNews.transform.parent.Find("Template_ReadMore_Content").gameObject;
        templateWorkshopLinks = templateNews.transform.parent.Find("Template_WorkshopLinks").gameObject;
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
        if (SteamUser.BLoggedOn())
        {
            hasNewAnnouncement = false;
            if (!readNewsPreview())
            {
                MenuUI.instance.StartCoroutine(MenuUI.instance.requestSteamNews());
            }
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
        container.positionOffset_X = 10;
        container.positionOffset_Y = 10;
        container.sizeOffset_X = -20;
        container.sizeOffset_Y = -20;
        container.sizeScale_X = 1f;
        container.sizeScale_Y = 1f;
        MenuUI.container.AddChild(container);
        active = true;
        playButton = new SleekButtonIcon(icons.load<Texture2D>("Play"));
        playButton.positionOffset_Y = 170;
        playButton.sizeOffset_X = 200;
        playButton.sizeOffset_Y = 50;
        playButton.text = localization.format("PlayButtonText");
        playButton.tooltip = localization.format("PlayButtonTooltip");
        playButton.onClickedButton += onClickedPlayButton;
        playButton.fontSize = ESleekFontSize.Medium;
        playButton.iconColor = ESleekTint.FOREGROUND;
        container.AddChild(playButton);
        survivorsButton = new SleekButtonIcon(icons.load<Texture2D>("Survivors"));
        survivorsButton.positionOffset_Y = 230;
        survivorsButton.sizeOffset_X = 200;
        survivorsButton.sizeOffset_Y = 50;
        survivorsButton.text = localization.format("SurvivorsButtonText");
        survivorsButton.tooltip = localization.format("SurvivorsButtonTooltip");
        survivorsButton.onClickedButton += onClickedSurvivorsButton;
        survivorsButton.fontSize = ESleekFontSize.Medium;
        survivorsButton.iconColor = ESleekTint.FOREGROUND;
        container.AddChild(survivorsButton);
        configurationButton = new SleekButtonIcon(icons.load<Texture2D>("Configuration"));
        configurationButton.positionOffset_Y = 290;
        configurationButton.sizeOffset_X = 200;
        configurationButton.sizeOffset_Y = 50;
        configurationButton.text = localization.format("ConfigurationButtonText");
        configurationButton.tooltip = localization.format("ConfigurationButtonTooltip");
        configurationButton.onClickedButton += onClickedConfigurationButton;
        configurationButton.fontSize = ESleekFontSize.Medium;
        configurationButton.iconColor = ESleekTint.FOREGROUND;
        container.AddChild(configurationButton);
        workshopButton = new SleekButtonIcon(icons.load<Texture2D>("Workshop"));
        workshopButton.positionOffset_Y = 350;
        workshopButton.sizeOffset_X = 200;
        workshopButton.sizeOffset_Y = 50;
        workshopButton.text = localization.format("WorkshopButtonText");
        workshopButton.tooltip = localization.format("WorkshopButtonTooltip");
        workshopButton.onClickedButton += onClickedWorkshopButton;
        workshopButton.fontSize = ESleekFontSize.Medium;
        workshopButton.iconColor = ESleekTint.FOREGROUND;
        container.AddChild(workshopButton);
        exitButton = new SleekButtonIcon(icons.load<Texture2D>("Exit"));
        exitButton.positionOffset_Y = -50;
        exitButton.positionScale_Y = 1f;
        exitButton.sizeOffset_X = 200;
        exitButton.sizeOffset_Y = 50;
        exitButton.text = localization.format("ExitButtonText");
        exitButton.tooltip = localization.format("ExitButtonTooltip");
        exitButton.onClickedButton += onClickedExitButton;
        exitButton.fontSize = ESleekFontSize.Medium;
        exitButton.iconColor = ESleekTint.FOREGROUND;
        container.AddChild(exitButton);
        if (!Provider.isPro)
        {
            proButton = Glazier.Get().CreateButton();
            proButton.positionOffset_X = 210;
            proButton.positionOffset_Y = -100;
            proButton.positionScale_Y = 1f;
            proButton.sizeOffset_Y = 100;
            proButton.sizeOffset_X = -210;
            proButton.sizeScale_X = 1f;
            proButton.tooltipText = localization.format("Pro_Button_Tooltip");
            proButton.backgroundColor = SleekColor.BackgroundIfLight(Palette.PRO);
            proButton.textColor = Palette.PRO;
            proButton.onClickedButton += onClickedProButton;
            container.AddChild(proButton);
            proLabel = Glazier.Get().CreateLabel();
            proLabel.sizeScale_X = 1f;
            proLabel.sizeOffset_Y = 50;
            proLabel.text = localization.format("Pro_Title");
            proLabel.textColor = Palette.PRO;
            proLabel.fontSize = ESleekFontSize.Large;
            proButton.AddChild(proLabel);
            featureLabel = Glazier.Get().CreateLabel();
            featureLabel.positionOffset_Y = 50;
            featureLabel.sizeOffset_Y = -50;
            featureLabel.sizeScale_X = 1f;
            featureLabel.sizeScale_Y = 1f;
            featureLabel.text = localization.format("Pro_Button");
            featureLabel.textColor = Palette.PRO;
            proButton.AddChild(featureLabel);
            canvas.transform.Find("View").GetComponent<RectTransform>().offsetMin += new Vector2(0f, 110f);
            canvas.transform.Find("Scrollbar").GetComponent<RectTransform>().offsetMin += new Vector2(0f, 110f);
        }
        mainHeaderOffset = 170;
        alertBox = null;
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
                _ => localization.format("Failure_Unknown", eSteamConnectionFailureInfo, connectionFailureReason), 
            };
            if (string.IsNullOrEmpty(text))
            {
                text = $"Error: {eSteamConnectionFailureInfo} Reason: {connectionFailureReason}";
            }
            MenuUI.alert(text);
            UnturnedLog.info(text);
        }
    }

    private void OnClickedPreviewBranchChangelog(ISleekElement button)
    {
        Provider.provider.browserService.open("https://support.smartlydressedgames.com/hc/en-us/articles/12462494977172");
    }

    private void CreatePreviewBranchChangelogButton()
    {
        ISleekButton sleekButton = Glazier.Get().CreateButton();
        sleekButton.positionOffset_X = 210;
        sleekButton.positionOffset_Y = mainHeaderOffset;
        sleekButton.sizeOffset_Y = 60;
        sleekButton.sizeOffset_X = -210;
        sleekButton.sizeScale_X = 1f;
        sleekButton.text = "Click here to open preview branch changelog.";
        sleekButton.onClickedButton += OnClickedPreviewBranchChangelog;
        container.AddChild(sleekButton);
        mainHeaderOffset += sleekButton.sizeOffset_Y + 10;
        canvas.transform.Find("View").GetComponent<RectTransform>().offsetMax -= new Vector2(0f, 70f);
        canvas.transform.Find("Scrollbar").GetComponent<RectTransform>().offsetMax -= new Vector2(0f, 70f);
    }
}
