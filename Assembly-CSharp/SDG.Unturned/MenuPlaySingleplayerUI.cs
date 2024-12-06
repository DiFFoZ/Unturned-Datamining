using System;
using System.Collections.Generic;
using UnityEngine;

namespace SDG.Unturned;

public class MenuPlaySingleplayerUI
{
    public static Local localization;

    private static SleekFullscreenBox container;

    public static bool active;

    private static SleekButtonIcon backButton;

    private static LevelInfo[] levels;

    private static ISleekBox previewBox;

    private static ISleekImage previewImage;

    private static ISleekScrollView levelScrollBox;

    private static SleekLevel[] levelButtons;

    private static SleekButtonIcon playButton;

    private static SleekButtonState modeButtonState;

    private static ISleekButton configButton;

    private static SleekButtonIconConfirm resetButton;

    private static ISleekButton browseServersButton;

    private static ISleekBox selectedBox;

    private static ISleekBox descriptionBox;

    private static ISleekToggle cheatsToggle;

    private static ISleekBox creditsBox;

    private static ISleekButton itemButton;

    private static ISleekButton feedbackButton;

    private static ISleekButton newsButton;

    private static ISleekButton officalMapsButton;

    private static ISleekButton curatedMapsButton;

    private static ISleekButton workshopMapsButton;

    private static ISleekButton miscMapsButton;

    private static SleekNew curatedStatusLabel;

    /// <summary>
    /// Stockpile item definition id with rev-share for the level creators.
    /// Randomly selected from associated items list.
    /// </summary>
    private static int featuredItemDefId;

    private bool hasCreatedFeaturedMapLabel;

    public static void open()
    {
        if (!active)
        {
            active = true;
            browseServersButton.IsVisible = !OptionsSettings.ShouldShowOnlineSafetyMenu;
            container.AnimateIntoView();
        }
    }

    public static void close()
    {
        if (active)
        {
            active = false;
            MenuSettings.save();
            container.AnimateOutOfView(0f, 1f);
        }
    }

    private static void updateSelection()
    {
        if (string.IsNullOrEmpty(PlaySettings.singleplayerMap))
        {
            UnturnedLog.warn("Singleplayer map selection empty");
            return;
        }
        LevelInfo levelInfo = null;
        LevelInfo[] array = levels;
        foreach (LevelInfo levelInfo2 in array)
        {
            if (string.Equals(levelInfo2.name, PlaySettings.singleplayerMap, StringComparison.InvariantCultureIgnoreCase))
            {
                levelInfo = levelInfo2;
                break;
            }
        }
        if (levelInfo == null)
        {
            UnturnedLog.warn("Unable to find singleplayer selected map '{0}'", PlaySettings.singleplayerMap);
            return;
        }
        Local local = levelInfo.getLocalization();
        if (local != null)
        {
            string desc = local.format("Description");
            desc = ItemTool.filterRarityRichText(desc);
            RichTextUtil.replaceNewlineMarkup(ref desc);
            descriptionBox.Text = desc;
        }
        if (local != null && local.has("Name"))
        {
            selectedBox.Text = local.format("Name");
        }
        else
        {
            selectedBox.Text = PlaySettings.singleplayerMap;
        }
        if (previewImage.Texture != null && previewImage.ShouldDestroyTexture)
        {
            UnityEngine.Object.Destroy(previewImage.Texture);
            previewImage.Texture = null;
        }
        string previewImageFilePath = levelInfo.GetPreviewImageFilePath();
        if (!string.IsNullOrEmpty(previewImageFilePath))
        {
            previewImage.Texture = ReadWrite.readTextureFromFile(previewImageFilePath);
        }
        float num = creditsBox.PositionOffset_Y;
        if (levelInfo.configData.Creators.Length != 0 || levelInfo.configData.Collaborators.Length != 0 || levelInfo.configData.Thanks.Length != 0)
        {
            int num2 = 0;
            string text = string.Empty;
            if (levelInfo.configData.Creators.Length != 0)
            {
                text += localization.format("Creators");
                num2 += 20;
                for (int j = 0; j < levelInfo.configData.Creators.Length; j++)
                {
                    text = text + "\n" + levelInfo.configData.Creators[j];
                    num2 += 20;
                }
            }
            if (levelInfo.configData.Collaborators.Length != 0)
            {
                if (text.Length > 0)
                {
                    text += "\n\n";
                    num2 += 30;
                }
                text += localization.format("Collaborators");
                num2 += 20;
                for (int k = 0; k < levelInfo.configData.Collaborators.Length; k++)
                {
                    text = text + "\n" + levelInfo.configData.Collaborators[k];
                    num2 += 20;
                }
            }
            if (levelInfo.configData.Thanks.Length != 0)
            {
                if (text.Length > 0)
                {
                    text += "\n\n";
                    num2 += 30;
                }
                text += localization.format("Thanks");
                num2 += 20;
                for (int l = 0; l < levelInfo.configData.Thanks.Length; l++)
                {
                    text = text + "\n" + levelInfo.configData.Thanks[l];
                    num2 += 20;
                }
            }
            num2 = Mathf.Max(num2, 40);
            creditsBox.SizeOffset_Y = num2;
            creditsBox.Text = text;
            creditsBox.IsVisible = true;
            num += (float)(num2 + 10);
        }
        else
        {
            creditsBox.IsVisible = false;
        }
        List<int> list = new List<int>(4);
        if (levelInfo.configData.Item > 0 && !Provider.provider.economyService.isItemHiddenByCountryRestrictions(levelInfo.configData.Item))
        {
            list.Add(levelInfo.configData.Item);
        }
        if (levelInfo.configData.Associated_Stockpile_Items.Length != 0)
        {
            int[] associated_Stockpile_Items = levelInfo.configData.Associated_Stockpile_Items;
            foreach (int num3 in associated_Stockpile_Items)
            {
                if (num3 > 0 && !Provider.provider.economyService.isItemHiddenByCountryRestrictions(num3))
                {
                    list.Add(num3);
                }
            }
        }
        featuredItemDefId = list.RandomOrDefault();
        if (featuredItemDefId > 0)
        {
            itemButton.PositionOffset_Y = num;
            itemButton.Text = localization.format("Credits_Text", selectedBox.Text, "<color=" + Palette.hex(Provider.provider.economyService.getInventoryColor(featuredItemDefId)) + ">" + Provider.provider.economyService.getInventoryName(featuredItemDefId) + "</color>");
            itemButton.TooltipText = localization.format("Credits_Tooltip");
            itemButton.IsVisible = true;
            num += itemButton.SizeOffset_Y + 10f;
        }
        else
        {
            itemButton.IsVisible = false;
        }
        string text2 = levelInfo.feedbackUrl;
        if (!string.IsNullOrEmpty(text2) && !WebUtils.CanParseThirdPartyUrl(text2))
        {
            UnturnedLog.warn("Ignoring potentially unsafe level feedback url {0}", text2);
            text2 = null;
        }
        if (string.IsNullOrEmpty(text2))
        {
            feedbackButton.IsVisible = false;
        }
        else
        {
            feedbackButton.PositionOffset_Y = num;
            feedbackButton.IsVisible = true;
            num += feedbackButton.SizeOffset_Y + 10f;
        }
        MainMenuWorkshopFeaturedLiveConfig featured = LiveConfig.Get().mainMenuWorkshop.featured;
        if (featured.IsNowFeaturedTimeOrBypassed() && featured.IsFeatured(levelInfo.publishedFileId) && !string.IsNullOrEmpty(featured.linkURL))
        {
            newsButton.Text = featured.linkText;
            newsButton.PositionOffset_Y = num;
            newsButton.IsVisible = true;
            _ = num + (newsButton.SizeOffset_Y + 10f);
        }
        else
        {
            newsButton.IsVisible = false;
        }
    }

    private static void onClickedLevel(SleekLevel level, byte index)
    {
        if (index < levels.Length && levels[index] != null)
        {
            PlaySettings.singleplayerMap = levels[index].name;
            updateSelection();
        }
    }

    private static void onClickedPlayButton(ISleekElement button)
    {
        if (!string.IsNullOrEmpty(PlaySettings.singleplayerMap))
        {
            Provider.map = PlaySettings.singleplayerMap;
            MenuSettings.save();
            Provider.singleplayer(PlaySettings.singleplayerMode, PlaySettings.singleplayerCheats);
        }
    }

    private static void onClickedOfficialMapsButton(ISleekElement button)
    {
        PlaySettings.singleplayerCategory = ESingleplayerMapCategory.OFFICIAL;
        refreshLevels();
    }

    private static void onClickedCuratedMapsButton(ISleekElement button)
    {
        if (curatedStatusLabel != null)
        {
            curatedMapsButton.RemoveChild(curatedStatusLabel);
            curatedStatusLabel = null;
            MainMenuWorkshopFeaturedLiveConfig featured = LiveConfig.Get().mainMenuWorkshop.featured;
            ConvenientSavedata.get().write("SingleplayerCuratedSeenId", featured.id);
        }
        PlaySettings.singleplayerCategory = ESingleplayerMapCategory.CURATED;
        refreshLevels();
    }

    private static void onClickedWorkshopMapsButton(ISleekElement button)
    {
        PlaySettings.singleplayerCategory = ESingleplayerMapCategory.WORKSHOP;
        refreshLevels();
    }

    private static void onClickedMiscMapsButton(ISleekElement button)
    {
        PlaySettings.singleplayerCategory = ESingleplayerMapCategory.MISC;
        refreshLevels();
    }

    private static void onClickedManageSubscriptionsButton(ISleekElement button)
    {
        MenuUI.closeAll();
        MenuWorkshopSubscriptionsUI.instance.open();
    }

    private static void onSwappedModeState(SleekButtonState button, int index)
    {
        PlaySettings.singleplayerMode = (EGameMode)index;
    }

    private static void onClickedConfigButton(ISleekElement button)
    {
        if (PlaySettings.singleplayerMap != null && PlaySettings.singleplayerMap.Length != 0)
        {
            MenuPlayConfigUI.open();
            close();
        }
    }

    private static void onClickedBrowseServersButton(ISleekElement button)
    {
        if (!string.IsNullOrEmpty(PlaySettings.singleplayerMap))
        {
            MenuPlayServersUI.serverListFiltersUI.OpenForMap(PlaySettings.singleplayerMap);
            close();
        }
    }

    private static void onClickedResetButton(SleekButtonIconConfirm button)
    {
        if (PlaySettings.singleplayerMap != null && PlaySettings.singleplayerMap.Length != 0)
        {
            if (ReadWrite.folderExists("/Worlds/Singleplayer_" + Characters.selected + "/Level/" + PlaySettings.singleplayerMap))
            {
                ReadWrite.deleteFolder("/Worlds/Singleplayer_" + Characters.selected + "/Level/" + PlaySettings.singleplayerMap);
            }
            if (ReadWrite.folderExists("/Worlds/Singleplayer_" + Characters.selected + "/Players/" + Provider.user.ToString() + "_" + Characters.selected + "/" + PlaySettings.singleplayerMap))
            {
                ReadWrite.deleteFolder("/Worlds/Singleplayer_" + Characters.selected + "/Players/" + Provider.user.ToString() + "_" + Characters.selected + "/" + PlaySettings.singleplayerMap);
            }
        }
    }

    private static void onToggledCheatsToggle(ISleekToggle toggle, bool state)
    {
        PlaySettings.singleplayerCheats = state;
    }

    private static void refreshLevels()
    {
        if (levelScrollBox == null)
        {
            return;
        }
        levelScrollBox.RemoveAllChildren();
        levels = Level.getLevels(PlaySettings.singleplayerCategory);
        bool flag = false;
        int num = 0;
        levelButtons = new SleekLevel[levels.Length];
        for (int i = 0; i < levels.Length; i++)
        {
            if (levels[i] != null)
            {
                SleekLevel sleekLevel = new SleekLevel(levels[i]);
                sleekLevel.PositionOffset_Y = num;
                sleekLevel.onClickedLevel = onClickedLevel;
                levelScrollBox.AddChild(sleekLevel);
                num += 110;
                levelButtons[i] = sleekLevel;
                if (!flag && string.Equals(levels[i].name, PlaySettings.singleplayerMap, StringComparison.InvariantCultureIgnoreCase))
                {
                    flag = true;
                }
            }
        }
        if (levels.Length == 0)
        {
            PlaySettings.singleplayerMap = "";
        }
        else if (!flag || PlaySettings.singleplayerMap == null || PlaySettings.singleplayerMap.Length == 0)
        {
            PlaySettings.singleplayerMap = levels[0].name;
        }
        updateSelection();
        if (PlaySettings.singleplayerCategory == ESingleplayerMapCategory.CURATED)
        {
            foreach (CuratedMapLink curated_Map_Link in Provider.statusData.Maps.Curated_Map_Links)
            {
                if (!Provider.provider.workshopService.getSubscribed(curated_Map_Link.Workshop_File_Id) && curated_Map_Link.Visible_In_Singleplayer_Recommendations_List)
                {
                    SleekCuratedLevelLink sleekCuratedLevelLink = new SleekCuratedLevelLink(curated_Map_Link);
                    sleekCuratedLevelLink.PositionOffset_Y = num;
                    levelScrollBox.AddChild(sleekCuratedLevelLink);
                    num += 110;
                }
            }
        }
        if (PlaySettings.singleplayerCategory == ESingleplayerMapCategory.WORKSHOP)
        {
            ISleekButton sleekButton = Glazier.Get().CreateButton();
            sleekButton.PositionOffset_Y = num;
            sleekButton.SizeOffset_X = 400f;
            sleekButton.SizeOffset_Y = 30f;
            sleekButton.Text = localization.format("Manage_Workshop_Label");
            sleekButton.TooltipText = localization.format("Manage_Workshop_Tooltip");
            sleekButton.OnClicked += onClickedManageSubscriptionsButton;
            levelScrollBox.AddChild(sleekButton);
            num += 40;
        }
        levelScrollBox.ContentSizeOffset = new Vector2(0f, num - 10);
    }

    private static void onLevelsRefreshed()
    {
        refreshLevels();
    }

    private static void onClickedItemButton(ISleekElement button)
    {
        if (PlaySettings.singleplayerMap != null && PlaySettings.singleplayerMap.Length != 0 && Level.getLevel(PlaySettings.singleplayerMap) != null && featuredItemDefId > 0)
        {
            ItemStore.Get().ViewItem(featuredItemDefId);
        }
    }

    private static void onClickedFeedbackButton(ISleekElement button)
    {
        if (PlaySettings.singleplayerMap == null || PlaySettings.singleplayerMap.Length == 0)
        {
            return;
        }
        LevelInfo level = Level.getLevel(PlaySettings.singleplayerMap);
        if (level != null)
        {
            if (WebUtils.ParseThirdPartyUrl(level.feedbackUrl, out var result))
            {
                Provider.provider.browserService.open(result);
                return;
            }
            UnturnedLog.warn("Ignoring potentially unsafe level feedback url {0}", level.feedbackUrl);
        }
    }

    private static void onClickedNewsButton(ISleekElement button)
    {
        MainMenuWorkshopFeaturedLiveConfig featured = LiveConfig.Get().mainMenuWorkshop.featured;
        Provider.provider.browserService.open(featured.linkURL);
    }

    private static void onClickedBackButton(ISleekElement button)
    {
        MenuPlayUI.open();
        close();
    }

    private void OnLiveConfigRefreshed()
    {
        if (!hasCreatedFeaturedMapLabel)
        {
            MainMenuWorkshopFeaturedLiveConfig featured = LiveConfig.Get().mainMenuWorkshop.featured;
            if (featured.status != 0 && featured.type == EFeaturedWorkshopType.Curated && (!ConvenientSavedata.get().read("SingleplayerCuratedSeenId", out long value) || value < featured.id))
            {
                curatedStatusLabel = new SleekNew(featured.status == EMapStatus.Updated);
                curatedMapsButton.AddChild(curatedStatusLabel);
                hasCreatedFeaturedMapLabel = true;
            }
        }
    }

    public void OnDestroy()
    {
        Level.onLevelsRefreshed = (LevelsRefreshed)Delegate.Remove(Level.onLevelsRefreshed, new LevelsRefreshed(onLevelsRefreshed));
        LiveConfig.OnRefreshed -= OnLiveConfigRefreshed;
    }

    public MenuPlaySingleplayerUI()
    {
        localization = Localization.read("/Menu/Play/MenuPlaySingleplayer.dat");
        Bundle bundle = Bundles.getBundle("/Bundles/Textures/Menu/Icons/Play/MenuPlaySingleplayer/MenuPlaySingleplayer.unity3d");
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
        previewBox = Glazier.Get().CreateBox();
        previewBox.PositionOffset_X = -305f;
        previewBox.PositionOffset_Y = 80f;
        previewBox.PositionScale_X = 0.5f;
        previewBox.SizeOffset_X = 340f;
        previewBox.SizeOffset_Y = 200f;
        container.AddChild(previewBox);
        previewImage = Glazier.Get().CreateImage();
        previewImage.PositionOffset_X = 10f;
        previewImage.PositionOffset_Y = 10f;
        previewImage.SizeOffset_X = -20f;
        previewImage.SizeOffset_Y = -20f;
        previewImage.SizeScale_X = 1f;
        previewImage.SizeScale_Y = 1f;
        previewImage.ShouldDestroyTexture = true;
        previewBox.AddChild(previewImage);
        levelScrollBox = Glazier.Get().CreateScrollView();
        levelScrollBox.PositionOffset_X = -95f;
        levelScrollBox.PositionOffset_Y = 340f;
        levelScrollBox.PositionScale_X = 0.5f;
        levelScrollBox.SizeOffset_X = 430f;
        levelScrollBox.SizeOffset_Y = -440f;
        levelScrollBox.SizeScale_Y = 1f;
        levelScrollBox.ScaleContentToWidth = true;
        container.AddChild(levelScrollBox);
        officalMapsButton = Glazier.Get().CreateButton();
        officalMapsButton.PositionOffset_X = -95f;
        officalMapsButton.PositionOffset_Y = 290f;
        officalMapsButton.PositionScale_X = 0.5f;
        officalMapsButton.SizeOffset_X = 100f;
        officalMapsButton.SizeOffset_Y = 50f;
        officalMapsButton.Text = localization.format("Maps_Official");
        officalMapsButton.TooltipText = localization.format("Maps_Official_Tooltip");
        officalMapsButton.OnClicked += onClickedOfficialMapsButton;
        officalMapsButton.FontSize = ESleekFontSize.Medium;
        container.AddChild(officalMapsButton);
        curatedMapsButton = Glazier.Get().CreateButton();
        curatedMapsButton.PositionOffset_X = 5f;
        curatedMapsButton.PositionOffset_Y = 290f;
        curatedMapsButton.PositionScale_X = 0.5f;
        curatedMapsButton.SizeOffset_X = 100f;
        curatedMapsButton.SizeOffset_Y = 50f;
        curatedMapsButton.Text = localization.format("Maps_Curated");
        curatedMapsButton.TooltipText = localization.format("Maps_Curated_Tooltip");
        curatedMapsButton.OnClicked += onClickedCuratedMapsButton;
        curatedMapsButton.FontSize = ESleekFontSize.Medium;
        container.AddChild(curatedMapsButton);
        hasCreatedFeaturedMapLabel = false;
        LiveConfig.OnRefreshed -= OnLiveConfigRefreshed;
        OnLiveConfigRefreshed();
        workshopMapsButton = Glazier.Get().CreateButton();
        workshopMapsButton.PositionOffset_X = 105f;
        workshopMapsButton.PositionOffset_Y = 290f;
        workshopMapsButton.PositionScale_X = 0.5f;
        workshopMapsButton.SizeOffset_X = 100f;
        workshopMapsButton.SizeOffset_Y = 50f;
        workshopMapsButton.Text = localization.format("Maps_Workshop");
        workshopMapsButton.TooltipText = localization.format("Maps_Workshop_Tooltip");
        workshopMapsButton.OnClicked += onClickedWorkshopMapsButton;
        workshopMapsButton.FontSize = ESleekFontSize.Medium;
        container.AddChild(workshopMapsButton);
        miscMapsButton = Glazier.Get().CreateButton();
        miscMapsButton.PositionOffset_X = 205f;
        miscMapsButton.PositionOffset_Y = 290f;
        miscMapsButton.PositionScale_X = 0.5f;
        miscMapsButton.SizeOffset_X = 100f;
        miscMapsButton.SizeOffset_Y = 50f;
        miscMapsButton.Text = localization.format("Maps_Misc");
        miscMapsButton.TooltipText = localization.format("Maps_Misc_Tooltip");
        miscMapsButton.OnClicked += onClickedMiscMapsButton;
        miscMapsButton.FontSize = ESleekFontSize.Medium;
        container.AddChild(miscMapsButton);
        selectedBox = Glazier.Get().CreateBox();
        selectedBox.PositionOffset_X = 45f;
        selectedBox.PositionOffset_Y = 80f;
        selectedBox.PositionScale_X = 0.5f;
        selectedBox.SizeOffset_X = 260f;
        selectedBox.SizeOffset_Y = 30f;
        container.AddChild(selectedBox);
        descriptionBox = Glazier.Get().CreateBox();
        descriptionBox.PositionOffset_X = 45f;
        descriptionBox.PositionOffset_Y = 120f;
        descriptionBox.PositionScale_X = 0.5f;
        descriptionBox.SizeOffset_X = 260f;
        descriptionBox.SizeOffset_Y = 160f;
        descriptionBox.TextAlignment = TextAnchor.UpperCenter;
        descriptionBox.AllowRichText = true;
        descriptionBox.TextColor = ESleekTint.RICH_TEXT_DEFAULT;
        container.AddChild(descriptionBox);
        creditsBox = Glazier.Get().CreateBox();
        creditsBox.PositionOffset_X = 345f;
        creditsBox.PositionOffset_Y = 100f;
        creditsBox.PositionScale_X = 0.5f;
        creditsBox.SizeOffset_X = 250f;
        container.AddChild(creditsBox);
        creditsBox.IsVisible = false;
        itemButton = Glazier.Get().CreateButton();
        itemButton.AllowRichText = true;
        itemButton.PositionOffset_X = 345f;
        itemButton.PositionOffset_Y = 100f;
        itemButton.PositionScale_X = 0.5f;
        itemButton.SizeOffset_X = 250f;
        itemButton.SizeOffset_Y = 100f;
        itemButton.TextColor = ESleekTint.RICH_TEXT_DEFAULT;
        itemButton.TextContrastContext = ETextContrastContext.InconspicuousBackdrop;
        itemButton.OnClicked += onClickedItemButton;
        container.AddChild(itemButton);
        itemButton.IsVisible = false;
        feedbackButton = Glazier.Get().CreateButton();
        feedbackButton.PositionOffset_X = 345f;
        feedbackButton.PositionOffset_Y = 100f;
        feedbackButton.PositionScale_X = 0.5f;
        feedbackButton.SizeOffset_X = 250f;
        feedbackButton.SizeOffset_Y = 30f;
        feedbackButton.Text = localization.format("Feedback_Button");
        feedbackButton.TooltipText = localization.format("Feedback_Button_Tooltip");
        feedbackButton.OnClicked += onClickedFeedbackButton;
        container.AddChild(feedbackButton);
        feedbackButton.IsVisible = false;
        newsButton = Glazier.Get().CreateButton();
        newsButton.PositionOffset_X = 345f;
        newsButton.PositionOffset_Y = 100f;
        newsButton.PositionScale_X = 0.5f;
        newsButton.SizeOffset_X = 250f;
        newsButton.SizeOffset_Y = 30f;
        newsButton.OnClicked += onClickedNewsButton;
        container.AddChild(newsButton);
        newsButton.IsVisible = false;
        playButton = new SleekButtonIcon(bundle.load<Texture2D>("Play"));
        playButton.PositionOffset_X = -305f;
        playButton.PositionOffset_Y = 290f;
        playButton.PositionScale_X = 0.5f;
        playButton.SizeOffset_X = 200f;
        playButton.SizeOffset_Y = 30f;
        playButton.text = localization.format("Play_Button");
        playButton.tooltip = localization.format("Play_Button_Tooltip");
        playButton.iconColor = ESleekTint.FOREGROUND;
        playButton.onClickedButton += onClickedPlayButton;
        container.AddChild(playButton);
        browseServersButton = Glazier.Get().CreateButton();
        browseServersButton.PositionOffset_X = -305f;
        browseServersButton.PositionOffset_Y = 420f;
        browseServersButton.PositionScale_X = 0.5f;
        browseServersButton.SizeOffset_X = 200f;
        browseServersButton.SizeOffset_Y = 30f;
        browseServersButton.Text = localization.format("Browse_Servers_Label");
        browseServersButton.TooltipText = localization.format("Browse_Servers_Tooltip");
        browseServersButton.OnClicked += onClickedBrowseServersButton;
        container.AddChild(browseServersButton);
        modeButtonState = new SleekButtonState(new GUIContent(localization.format("Easy_Button"), bundle.load<Texture>("Easy")), new GUIContent(localization.format("Normal_Button"), bundle.load<Texture>("Normal")), new GUIContent(localization.format("Hard_Button"), bundle.load<Texture>("Hard")));
        modeButtonState.PositionOffset_X = -305f;
        modeButtonState.PositionOffset_Y = 330f;
        modeButtonState.PositionScale_X = 0.5f;
        modeButtonState.SizeOffset_X = 105f;
        modeButtonState.SizeOffset_Y = 30f;
        modeButtonState.state = (int)PlaySettings.singleplayerMode;
        modeButtonState.onSwappedState = onSwappedModeState;
        container.AddChild(modeButtonState);
        configButton = Glazier.Get().CreateButton();
        configButton.PositionOffset_X = -195f;
        configButton.PositionOffset_Y = 330f;
        configButton.PositionScale_X = 0.5f;
        configButton.SizeOffset_X = 85f;
        configButton.SizeOffset_Y = 30f;
        configButton.Text = localization.format("Config_Button");
        configButton.TooltipText = localization.format("Config_Button_Tooltip");
        configButton.OnClicked += onClickedConfigButton;
        container.AddChild(configButton);
        cheatsToggle = Glazier.Get().CreateToggle();
        cheatsToggle.PositionOffset_X = -305f;
        cheatsToggle.PositionOffset_Y = 370f;
        cheatsToggle.PositionScale_X = 0.5f;
        cheatsToggle.SizeOffset_X = 40f;
        cheatsToggle.SizeOffset_Y = 40f;
        cheatsToggle.AddLabel(localization.format("Cheats_Label"), ESleekSide.RIGHT);
        cheatsToggle.Value = PlaySettings.singleplayerCheats;
        cheatsToggle.OnValueChanged += onToggledCheatsToggle;
        container.AddChild(cheatsToggle);
        resetButton = new SleekButtonIconConfirm(null, localization.format("Reset_Button_Confirm"), localization.format("Reset_Button_Confirm_Tooltip"), localization.format("Reset_Button_Deny"), localization.format("Reset_Button_Deny_Tooltip"));
        resetButton.PositionOffset_X = -305f;
        resetButton.PositionOffset_Y = 480f;
        resetButton.PositionScale_X = 0.5f;
        resetButton.SizeOffset_X = 200f;
        resetButton.SizeOffset_Y = 30f;
        resetButton.text = localization.format("Reset_Button");
        resetButton.tooltip = localization.format("Reset_Button_Tooltip");
        resetButton.onConfirmed = onClickedResetButton;
        container.AddChild(resetButton);
        bundle.unload();
        refreshLevels();
        Level.onLevelsRefreshed = (LevelsRefreshed)Delegate.Combine(Level.onLevelsRefreshed, new LevelsRefreshed(onLevelsRefreshed));
        backButton = new SleekButtonIcon(MenuDashboardUI.icons.load<Texture2D>("Exit"));
        backButton.PositionOffset_Y = -50f;
        backButton.PositionScale_Y = 1f;
        backButton.SizeOffset_X = 200f;
        backButton.SizeOffset_Y = 50f;
        backButton.text = MenuDashboardUI.localization.format("BackButtonText");
        backButton.tooltip = MenuDashboardUI.localization.format("BackButtonTooltip");
        backButton.onClickedButton += onClickedBackButton;
        backButton.fontSize = ESleekFontSize.Medium;
        backButton.iconColor = ESleekTint.FOREGROUND;
        container.AddChild(backButton);
        new MenuPlayConfigUI();
    }
}
