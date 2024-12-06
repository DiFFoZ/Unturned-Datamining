using SDG.HostBans;
using SDG.SteamworksProvider.Services.Store;
using UnityEngine;

namespace SDG.Unturned;

public class SleekServer : SleekWrapper
{
    private SteamServerAdvertisement info;

    private SleekButtonIcon favoriteButton;

    private ISleekButton button;

    private ISleekBox mapBox;

    private ISleekBox playersBox;

    private ISleekBox maxPlayersBox;

    private ISleekBox fullnessBox;

    private ISleekBox pingBox;

    private ISleekBox anticheatBox;

    private ISleekBox perspectiveBox;

    private ISleekBox combatBox;

    private ISleekBox passwordBox;

    private ISleekBox workshopBox;

    private ISleekBox goldBox;

    private ISleekBox cheatsBox;

    private ISleekBox monetizationBox;

    private ISleekBox pluginsBox;

    private SleekWebImage thumbnail;

    private ISleekLabel nameLabel;

    public ClickedServer onClickedServer;

    /// <summary>
    /// Is the server this widget represents currently favorited?
    /// Can be false on the favorites list.
    /// </summary>
    public bool isCurrentlyFavorited => Provider.GetServerIsFavorited(info.ip, info.queryPort);

    public void SynchronizeVisibleColumns()
    {
        float num = 0f;
        if (FilterSettings.columns.anticheat)
        {
            num -= anticheatBox.SizeOffset_X;
            anticheatBox.PositionOffset_X = num;
            anticheatBox.IsVisible = true;
            num -= 0f;
        }
        else
        {
            anticheatBox.IsVisible = false;
        }
        if (FilterSettings.columns.cheats)
        {
            num -= cheatsBox.SizeOffset_X;
            cheatsBox.PositionOffset_X = num;
            cheatsBox.IsVisible = true;
            num -= 0f;
        }
        else
        {
            cheatsBox.IsVisible = false;
        }
        if (FilterSettings.columns.plugins)
        {
            num -= pluginsBox.SizeOffset_X;
            pluginsBox.PositionOffset_X = num;
            pluginsBox.IsVisible = true;
            num -= 0f;
        }
        else
        {
            pluginsBox.IsVisible = false;
        }
        if (FilterSettings.columns.workshop)
        {
            num -= workshopBox.SizeOffset_X;
            workshopBox.PositionOffset_X = num;
            workshopBox.IsVisible = true;
            num -= 0f;
        }
        else
        {
            workshopBox.IsVisible = false;
        }
        if (FilterSettings.columns.monetization)
        {
            num -= monetizationBox.SizeOffset_X;
            monetizationBox.PositionOffset_X = num;
            monetizationBox.IsVisible = true;
            num -= 0f;
        }
        else
        {
            monetizationBox.IsVisible = false;
        }
        if (FilterSettings.columns.gold)
        {
            num -= goldBox.SizeOffset_X;
            goldBox.PositionOffset_X = num;
            goldBox.IsVisible = true;
            num -= 0f;
        }
        else
        {
            goldBox.IsVisible = false;
        }
        if (FilterSettings.columns.perspective)
        {
            num -= perspectiveBox.SizeOffset_X;
            perspectiveBox.PositionOffset_X = num;
            perspectiveBox.IsVisible = true;
            num -= 0f;
        }
        else
        {
            perspectiveBox.IsVisible = false;
        }
        if (FilterSettings.columns.combat)
        {
            num -= combatBox.SizeOffset_X;
            combatBox.PositionOffset_X = num;
            combatBox.IsVisible = true;
            num -= 0f;
        }
        else
        {
            combatBox.IsVisible = false;
        }
        if (FilterSettings.columns.password)
        {
            num -= passwordBox.SizeOffset_X;
            passwordBox.PositionOffset_X = num;
            passwordBox.IsVisible = true;
            num -= 0f;
        }
        else
        {
            passwordBox.IsVisible = false;
        }
        if (FilterSettings.columns.fullnessPercentage)
        {
            num -= fullnessBox.SizeOffset_X;
            fullnessBox.PositionOffset_X = num;
            fullnessBox.IsVisible = true;
            num -= 0f;
        }
        else
        {
            fullnessBox.IsVisible = false;
        }
        if (FilterSettings.columns.maxPlayers)
        {
            num -= maxPlayersBox.SizeOffset_X;
            maxPlayersBox.PositionOffset_X = num;
            maxPlayersBox.IsVisible = true;
            num -= 0f;
        }
        else
        {
            maxPlayersBox.IsVisible = false;
        }
        if (FilterSettings.columns.players)
        {
            if (FilterSettings.columns.maxPlayers)
            {
                playersBox.SizeOffset_X = 80f;
                playersBox.Text = info.players.ToString();
            }
            else
            {
                playersBox.SizeOffset_X = 120f;
                playersBox.Text = MenuPlayUI.serverListUI.localization.format("Server_Players", info.players, info.maxPlayers);
            }
            num -= playersBox.SizeOffset_X;
            playersBox.PositionOffset_X = num;
            playersBox.IsVisible = true;
            num -= 0f;
        }
        else
        {
            playersBox.IsVisible = false;
        }
        if (FilterSettings.columns.ping)
        {
            num -= pingBox.SizeOffset_X;
            pingBox.PositionOffset_X = num;
            pingBox.IsVisible = true;
            num -= 0f;
        }
        else
        {
            pingBox.IsVisible = false;
        }
        if (FilterSettings.columns.map)
        {
            num -= mapBox.SizeOffset_X;
            mapBox.PositionOffset_X = num;
            mapBox.IsVisible = true;
            num -= 0f;
        }
        else
        {
            mapBox.IsVisible = false;
        }
        num -= button.PositionOffset_X;
        button.SizeOffset_X = num;
    }

    private void onClickedFavoriteOffButton(ISleekElement button)
    {
        Provider.SetServerIsFavorited(info.ip, info.connectionPort, info.queryPort, !isCurrentlyFavorited);
        refreshFavoriteButton();
    }

    private void refreshFavoriteButton()
    {
        if (isCurrentlyFavorited)
        {
            button.IsClickable = true;
            favoriteButton.tooltip = MenuPlayUI.serverListUI.localization.format("Favorite_Off_Button_Tooltip");
            favoriteButton.icon = MenuPlayUI.serverListUI.icons.load<Texture2D>("Favorite_Off");
        }
        else
        {
            button.IsClickable = false;
            favoriteButton.tooltip = MenuPlayUI.serverListUI.localization.format("Favorite_On_Button_Tooltip");
            favoriteButton.icon = MenuPlayUI.serverListUI.icons.load<Texture2D>("Favorite_On");
        }
    }

    private void onClickedButton(ISleekElement button)
    {
        if (!Provider.isPro && info.isPro)
        {
            Provider.provider.storeService.open(new SteamworksStorePackageID(Provider.PRO_ID.m_AppId));
        }
        else
        {
            onClickedServer?.Invoke(this, info);
        }
    }

    public SleekServer(ESteamServerList list, SteamServerAdvertisement newInfo)
    {
        info = newInfo;
        button = Glazier.Get().CreateButton();
        button.SizeScale_X = 1f;
        button.SizeScale_Y = 1f;
        button.OnClicked += onClickedButton;
        if (info.deniedByRule != null)
        {
            button.TooltipText = MenuPlayUI.serverListUI.localization.format("BlockedByCurator_Tooltip", info.deniedByRule.owner.Name, info.deniedByRule.description);
        }
        if (info.isDeniedByServerCurationRule)
        {
            button.BackgroundColor = new SleekColor(ESleekTint.BACKGROUND, 0.5f);
        }
        nameLabel = Glazier.Get().CreateLabel();
        nameLabel.PositionOffset_X = 45f;
        nameLabel.SizeScale_X = 1f;
        nameLabel.SizeOffset_X = -45f;
        nameLabel.TextAlignment = TextAnchor.MiddleLeft;
        nameLabel.Text = info.name;
        button.AddChild(nameLabel);
        if (info.isDeniedByServerCurationRule)
        {
            nameLabel.TextColor = new SleekColor(ESleekTint.FONT, 0.5f);
        }
        if (string.IsNullOrEmpty(info.descText) && string.IsNullOrEmpty(info.serverCurationLabels))
        {
            nameLabel.SizeOffset_Y = 40f;
        }
        else
        {
            nameLabel.SizeOffset_Y = 30f;
        }
        if (!string.IsNullOrEmpty(info.descText))
        {
            ISleekLabel sleekLabel = Glazier.Get().CreateLabel();
            sleekLabel.PositionOffset_X = 45f;
            sleekLabel.PositionOffset_Y = 15f;
            sleekLabel.SizeScale_X = 1f;
            sleekLabel.SizeOffset_X = -50f;
            sleekLabel.SizeOffset_Y = 30f;
            sleekLabel.FontSize = ESleekFontSize.Small;
            sleekLabel.AllowRichText = true;
            if (info.isDeniedByServerCurationRule)
            {
                sleekLabel.TextColor = new SleekColor(ESleekTint.RICH_TEXT_DEFAULT, 0.5f);
            }
            else
            {
                sleekLabel.TextColor = ESleekTint.RICH_TEXT_DEFAULT;
            }
            sleekLabel.TextContrastContext = ETextContrastContext.InconspicuousBackdrop;
            sleekLabel.TextAlignment = TextAnchor.MiddleLeft;
            sleekLabel.Text = info.descText;
            button.AddChild(sleekLabel);
        }
        if (!string.IsNullOrEmpty(info.serverCurationLabels))
        {
            ISleekLabel sleekLabel2 = Glazier.Get().CreateLabel();
            sleekLabel2.PositionOffset_X = 45f;
            sleekLabel2.PositionOffset_Y = 15f;
            sleekLabel2.SizeScale_X = 1f;
            sleekLabel2.SizeOffset_X = -50f;
            sleekLabel2.SizeOffset_Y = 30f;
            sleekLabel2.FontSize = ESleekFontSize.Small;
            sleekLabel2.AllowRichText = true;
            sleekLabel2.TextColor = ESleekTint.RICH_TEXT_DEFAULT;
            sleekLabel2.TextContrastContext = ETextContrastContext.InconspicuousBackdrop;
            sleekLabel2.TextAlignment = TextAnchor.MiddleRight;
            sleekLabel2.Text = info.serverCurationLabels;
            button.AddChild(sleekLabel2);
        }
        mapBox = Glazier.Get().CreateBox();
        mapBox.PositionScale_X = 1f;
        mapBox.SizeOffset_X = 153f;
        mapBox.SizeScale_Y = 1f;
        Texture2D orLoadIcon = LevelIconCache.GetOrLoadIcon(info.map);
        if (orLoadIcon != null)
        {
            ISleekImage sleekImage = Glazier.Get().CreateImage(orLoadIcon);
            sleekImage.PositionOffset_X = 5f;
            sleekImage.PositionOffset_Y = 5f;
            sleekImage.SizeOffset_X = 143f;
            sleekImage.SizeOffset_Y = 30f;
            mapBox.AddChild(sleekImage);
            ISleekLabel sleekLabel3 = Glazier.Get().CreateLabel();
            sleekLabel3.SizeScale_X = 1f;
            sleekLabel3.SizeScale_Y = 1f;
            sleekLabel3.TextAlignment = TextAnchor.MiddleCenter;
            sleekLabel3.TextContrastContext = ETextContrastContext.ColorfulBackdrop;
            sleekLabel3.Text = info.map;
            mapBox.AddChild(sleekLabel3);
        }
        else
        {
            mapBox.Text = info.map;
        }
        playersBox = Glazier.Get().CreateBox();
        playersBox.PositionScale_X = 1f;
        playersBox.SizeOffset_X = 80f;
        playersBox.SizeScale_Y = 1f;
        maxPlayersBox = Glazier.Get().CreateBox();
        maxPlayersBox.PositionScale_X = 1f;
        maxPlayersBox.SizeOffset_X = 80f;
        maxPlayersBox.SizeScale_Y = 1f;
        maxPlayersBox.Text = info.maxPlayers.ToString();
        fullnessBox = Glazier.Get().CreateBox();
        fullnessBox.PositionScale_X = 1f;
        fullnessBox.SizeOffset_X = 80f;
        fullnessBox.SizeScale_Y = 1f;
        fullnessBox.Text = info.NormalizedPlayerCount.ToString("P0");
        fullnessBox.TooltipText = MenuPlayUI.serverListUI.localization.format("Server_Players", info.players, info.maxPlayers);
        pingBox = Glazier.Get().CreateBox();
        pingBox.PositionScale_X = 1f;
        pingBox.SizeOffset_X = 80f;
        pingBox.SizeScale_Y = 1f;
        pingBox.Text = $"{info.ping} ms";
        if (info.hostBanFlags.HasFlag(EHostBanFlags.QueryPingWarning))
        {
            pingBox.TextColor = ESleekTint.BAD;
            if (MenuPlayServerInfoUI.localization != null)
            {
                pingBox.TooltipText = MenuPlayServerInfoUI.localization.format("HostBan_QueryPingWarning");
            }
        }
        anticheatBox = Glazier.Get().CreateBox();
        anticheatBox.PositionScale_X = 1f;
        anticheatBox.SizeOffset_X = 80f;
        anticheatBox.SizeScale_Y = 1f;
        ISleekImage sleekImage2 = Glazier.Get().CreateImage();
        sleekImage2.PositionOffset_X = 15f;
        sleekImage2.PositionOffset_Y = 10f;
        sleekImage2.SizeOffset_X = 20f;
        sleekImage2.SizeOffset_Y = 20f;
        sleekImage2.TintColor = ESleekTint.FOREGROUND;
        anticheatBox.AddChild(sleekImage2);
        if (info.IsBattlEyeSecure)
        {
            sleekImage2.Texture = MenuPlayUI.serverListUI.icons.load<Texture2D>("BattlEye");
        }
        else
        {
            sleekImage2.Texture = MenuPlayUI.serverListUI.icons.load<Texture2D>("BattlEye_Off");
        }
        ISleekImage sleekImage3 = Glazier.Get().CreateImage();
        sleekImage3.PositionOffset_X = 45f;
        sleekImage3.PositionOffset_Y = 10f;
        sleekImage3.SizeOffset_X = 20f;
        sleekImage3.SizeOffset_Y = 20f;
        sleekImage3.TintColor = ESleekTint.FOREGROUND;
        anticheatBox.AddChild(sleekImage3);
        if (info.IsVACSecure)
        {
            sleekImage3.Texture = MenuPlayUI.serverListUI.icons.load<Texture2D>("VAC");
        }
        else
        {
            sleekImage3.Texture = MenuPlayUI.serverListUI.icons.load<Texture2D>("VAC_Off");
        }
        if (info.IsBattlEyeSecure && info.IsVACSecure)
        {
            anticheatBox.TooltipText = MenuPlayUI.serverListUI.localization.format("Anticheat_Column_Both_Tooltip");
        }
        else if (info.IsBattlEyeSecure)
        {
            anticheatBox.TooltipText = MenuPlayUI.serverListUI.localization.format("Anticheat_Column_BattlEye_Tooltip");
        }
        else if (info.IsVACSecure)
        {
            anticheatBox.TooltipText = MenuPlayUI.serverListUI.localization.format("Anticheat_Column_VAC_Tooltip");
        }
        else
        {
            anticheatBox.TooltipText = MenuPlayUI.serverListUI.localization.format("Anticheat_Column_None_Tooltip");
        }
        perspectiveBox = Glazier.Get().CreateBox();
        perspectiveBox.PositionScale_X = 1f;
        perspectiveBox.SizeOffset_X = 40f;
        perspectiveBox.SizeScale_Y = 1f;
        ISleekImage sleekImage4 = Glazier.Get().CreateImage();
        sleekImage4.PositionOffset_X = 10f;
        sleekImage4.PositionOffset_Y = 10f;
        sleekImage4.SizeOffset_X = 20f;
        sleekImage4.SizeOffset_Y = 20f;
        sleekImage4.TintColor = ESleekTint.FOREGROUND;
        switch (info.cameraMode)
        {
        case ECameraMode.FIRST:
            sleekImage4.Texture = MenuPlayUI.serverListUI.icons.load<Texture2D>("Perspective_FirstPerson");
            perspectiveBox.TooltipText = MenuPlayUI.serverListUI.localization.format("Perspective_Column_First_Tooltip");
            break;
        case ECameraMode.THIRD:
            sleekImage4.Texture = MenuPlayUI.serverListUI.icons.load<Texture2D>("Perspective_ThirdPerson");
            perspectiveBox.TooltipText = MenuPlayUI.serverListUI.localization.format("Perspective_Column_Third_Tooltip");
            break;
        case ECameraMode.BOTH:
            sleekImage4.Texture = MenuPlayUI.serverListUI.icons.load<Texture2D>("Perspective_Both");
            perspectiveBox.TooltipText = MenuPlayUI.serverListUI.localization.format("Perspective_Column_Both_Tooltip");
            break;
        case ECameraMode.VEHICLE:
            sleekImage4.Texture = MenuPlayUI.serverListUI.icons.load<Texture2D>("Perspective_Vehicle");
            perspectiveBox.TooltipText = MenuPlayUI.serverListUI.localization.format("Perspective_Column_Vehicle_Tooltip");
            break;
        }
        perspectiveBox.AddChild(sleekImage4);
        combatBox = Glazier.Get().CreateBox();
        combatBox.PositionScale_X = 1f;
        combatBox.SizeOffset_X = 40f;
        combatBox.SizeScale_Y = 1f;
        if (info.isPvP)
        {
            combatBox.TooltipText = MenuPlayUI.serverListUI.localization.format("Combat_Column_PvP_Tooltip");
        }
        else
        {
            combatBox.TooltipText = MenuPlayUI.serverListUI.localization.format("Combat_Column_PvE_Tooltip");
        }
        ISleekImage sleekImage5 = Glazier.Get().CreateImage();
        sleekImage5.PositionOffset_X = 10f;
        sleekImage5.PositionOffset_Y = 10f;
        sleekImage5.SizeOffset_X = 20f;
        sleekImage5.SizeOffset_Y = 20f;
        sleekImage5.TintColor = ESleekTint.FOREGROUND;
        sleekImage5.Texture = MenuPlayUI.serverListUI.icons.load<Texture2D>(info.isPvP ? "PvP" : "PvE");
        combatBox.AddChild(sleekImage5);
        passwordBox = Glazier.Get().CreateBox();
        passwordBox.PositionScale_X = 1f;
        passwordBox.SizeOffset_X = 40f;
        passwordBox.SizeScale_Y = 1f;
        ISleekImage sleekImage6 = Glazier.Get().CreateImage();
        sleekImage6.PositionOffset_X = 10f;
        sleekImage6.PositionOffset_Y = 10f;
        sleekImage6.SizeOffset_X = 20f;
        sleekImage6.SizeOffset_Y = 20f;
        sleekImage6.TintColor = ESleekTint.FOREGROUND;
        passwordBox.AddChild(sleekImage6);
        if (info.isPassworded)
        {
            sleekImage6.Texture = MenuPlayUI.serverListUI.icons.load<Texture2D>("PasswordProtected");
            passwordBox.TooltipText = MenuPlayUI.serverListUI.localization.format("Password_Column_Yes_Tooltip");
        }
        else
        {
            sleekImage6.Texture = MenuPlayUI.serverListUI.icons.load<Texture2D>("NotPasswordProtected");
            passwordBox.TooltipText = MenuPlayUI.serverListUI.localization.format("Password_Column_No_Tooltip");
        }
        workshopBox = Glazier.Get().CreateBox();
        workshopBox.PositionScale_X = 1f;
        workshopBox.SizeOffset_X = 40f;
        workshopBox.SizeScale_Y = 1f;
        ISleekImage sleekImage7 = Glazier.Get().CreateImage();
        sleekImage7.PositionOffset_X = 10f;
        sleekImage7.PositionOffset_Y = 10f;
        sleekImage7.SizeOffset_X = 20f;
        sleekImage7.SizeOffset_Y = 20f;
        sleekImage7.TintColor = ESleekTint.FOREGROUND;
        workshopBox.AddChild(sleekImage7);
        if (info.isWorkshop)
        {
            sleekImage7.Texture = MenuPlayUI.serverListUI.icons.load<Texture2D>("HasMods");
            workshopBox.TooltipText = MenuPlayUI.serverListUI.localization.format("Workshop_Column_Yes_Tooltip");
        }
        else
        {
            sleekImage7.Texture = MenuPlayUI.serverListUI.icons.load<Texture2D>("NoMods");
            workshopBox.TooltipText = MenuPlayUI.serverListUI.localization.format("Workshop_Column_No_Tooltip");
        }
        goldBox = Glazier.Get().CreateBox();
        goldBox.PositionScale_X = 1f;
        goldBox.SizeOffset_X = 40f;
        goldBox.SizeScale_Y = 1f;
        goldBox.BackgroundColor = SleekColor.BackgroundIfLight(Palette.PRO);
        goldBox.TextColor = Palette.PRO;
        ISleekImage sleekImage8 = Glazier.Get().CreateImage();
        sleekImage8.PositionOffset_X = 10f;
        sleekImage8.PositionOffset_Y = 10f;
        sleekImage8.SizeOffset_X = 20f;
        sleekImage8.SizeOffset_Y = 20f;
        sleekImage8.TintColor = Palette.PRO;
        goldBox.AddChild(sleekImage8);
        if (info.isPro)
        {
            sleekImage8.Texture = MenuPlayUI.serverListUI.icons.load<Texture2D>("GoldRequired");
            goldBox.TooltipText = MenuPlayUI.serverListUI.localization.format("Gold_Column_Yes_Tooltip");
        }
        else
        {
            sleekImage8.Texture = MenuPlayUI.serverListUI.icons.load<Texture2D>("GoldNotRequired");
            goldBox.TooltipText = MenuPlayUI.serverListUI.localization.format("Gold_Column_No_Tooltip");
        }
        cheatsBox = Glazier.Get().CreateBox();
        cheatsBox.PositionScale_X = 1f;
        cheatsBox.SizeOffset_X = 40f;
        cheatsBox.SizeScale_Y = 1f;
        ISleekImage sleekImage9 = Glazier.Get().CreateImage();
        sleekImage9.PositionOffset_X = 10f;
        sleekImage9.PositionOffset_Y = 10f;
        sleekImage9.SizeOffset_X = 20f;
        sleekImage9.SizeOffset_Y = 20f;
        sleekImage9.TintColor = ESleekTint.FOREGROUND;
        cheatsBox.AddChild(sleekImage9);
        if (info.hasCheats)
        {
            sleekImage9.Texture = MenuPlayUI.serverListUI.icons.load<Texture2D>("CheatCodes");
            cheatsBox.TooltipText = MenuPlayUI.serverListUI.localization.format("Cheats_Column_Yes_Tooltip");
        }
        else
        {
            sleekImage9.Texture = MenuPlayUI.serverListUI.icons.load<Texture2D>("CheatCodes_None");
            cheatsBox.TooltipText = MenuPlayUI.serverListUI.localization.format("Cheats_Column_No_Tooltip");
        }
        monetizationBox = Glazier.Get().CreateBox();
        monetizationBox.PositionScale_X = 1f;
        monetizationBox.SizeOffset_X = 40f;
        monetizationBox.SizeScale_Y = 1f;
        ISleekImage sleekImage10 = Glazier.Get().CreateImage();
        sleekImage10.PositionOffset_X = 10f;
        sleekImage10.PositionOffset_Y = 10f;
        sleekImage10.SizeOffset_X = 20f;
        sleekImage10.SizeOffset_Y = 20f;
        sleekImage10.TintColor = ESleekTint.FOREGROUND;
        monetizationBox.AddChild(sleekImage10);
        switch (info.monetization)
        {
        case EServerMonetizationTag.Unspecified:
            sleekImage10.Texture = MenuPlayUI.serverListUI.icons.load<Texture2D>("Unknown");
            monetizationBox.TooltipText = MenuPlayUI.serverListUI.localization.format("Monetization_Column_Unspecified_Tooltip");
            break;
        case EServerMonetizationTag.NonGameplay:
            sleekImage10.Texture = MenuPlayUI.serverListUI.icons.load<Texture2D>("NonGameplayMonetization");
            monetizationBox.TooltipText = MenuPlayUI.serverListUI.localization.format("Monetization_Column_NonGameplay_Tooltip");
            break;
        case EServerMonetizationTag.Monetized:
            sleekImage10.Texture = MenuPlayUI.serverListUI.icons.load<Texture2D>("Monetized");
            monetizationBox.TooltipText = MenuPlayUI.serverListUI.localization.format("Monetization_Column_Monetized_Tooltip");
            break;
        case EServerMonetizationTag.None:
            sleekImage10.Texture = MenuPlayUI.serverListUI.icons.load<Texture2D>("Monetization_None");
            monetizationBox.TooltipText = MenuPlayUI.serverListUI.localization.format("Monetization_Column_None_Tooltip");
            break;
        }
        pluginsBox = Glazier.Get().CreateBox();
        pluginsBox.PositionScale_X = 1f;
        pluginsBox.SizeOffset_X = 40f;
        pluginsBox.SizeScale_Y = 1f;
        ISleekImage sleekImage11 = Glazier.Get().CreateImage();
        sleekImage11.PositionOffset_X = 10f;
        sleekImage11.PositionOffset_Y = 10f;
        sleekImage11.SizeOffset_X = 20f;
        sleekImage11.SizeOffset_Y = 20f;
        sleekImage11.TintColor = ESleekTint.FOREGROUND;
        pluginsBox.AddChild(sleekImage11);
        switch (info.pluginFramework)
        {
        case SteamServerAdvertisement.EPluginFramework.None:
            sleekImage11.Texture = MenuPlayUI.serverListUI.icons.load<Texture2D>("Plugins_None");
            pluginsBox.TooltipText = MenuPlayUI.serverListUI.localization.format("Plugins_Column_None_Tooltip");
            break;
        case SteamServerAdvertisement.EPluginFramework.Rocket:
            sleekImage11.Texture = MenuPlayUI.serverListUI.icons.load<Texture2D>("RocketMod");
            pluginsBox.TooltipText = MenuPlayUI.serverListUI.localization.format("Plugins_Column_Rocket_Tooltip");
            break;
        case SteamServerAdvertisement.EPluginFramework.OpenMod:
            sleekImage11.Texture = MenuPlayUI.serverListUI.icons.load<Texture2D>("OpenMod");
            pluginsBox.TooltipText = MenuPlayUI.serverListUI.localization.format("Plugins_Column_OpenMod_Tooltip");
            break;
        case SteamServerAdvertisement.EPluginFramework.Unknown:
            sleekImage11.Texture = MenuPlayUI.serverListUI.icons.load<Texture2D>("Unknown");
            pluginsBox.TooltipText = MenuPlayUI.serverListUI.localization.format("Plugins_Column_Unknown_Tooltip");
            break;
        }
        SynchronizeVisibleColumns();
        AddChild(button);
        AddChild(mapBox);
        AddChild(playersBox);
        AddChild(maxPlayersBox);
        AddChild(fullnessBox);
        AddChild(pingBox);
        AddChild(anticheatBox);
        AddChild(perspectiveBox);
        AddChild(combatBox);
        AddChild(passwordBox);
        AddChild(workshopBox);
        AddChild(goldBox);
        AddChild(cheatsBox);
        AddChild(monetizationBox);
        AddChild(pluginsBox);
        if (!string.IsNullOrEmpty(info.thumbnailURL))
        {
            thumbnail = new SleekWebImage();
            thumbnail.PositionOffset_X = 4f;
            thumbnail.PositionOffset_Y = 4f;
            thumbnail.SizeOffset_X = 32f;
            thumbnail.SizeOffset_Y = 32f;
            thumbnail.Refresh(info.thumbnailURL);
            if (info.isDeniedByServerCurationRule)
            {
                thumbnail.color = new Color(1f, 1f, 1f, 0.5f);
            }
            button.AddChild(thumbnail);
        }
        if (info.isPro && !Provider.isPro)
        {
            if (!info.isDeniedByServerCurationRule)
            {
                button.TextColor = Palette.PRO;
                button.TooltipText = MenuPlayUI.serverListUI.localization.format("Gold_Column_Yes_Tooltip");
            }
            ISleekImage sleekImage12 = Glazier.Get().CreateImage();
            sleekImage12.PositionOffset_X = 10f;
            sleekImage12.PositionOffset_Y = 10f;
            sleekImage12.SizeOffset_X = 20f;
            sleekImage12.SizeOffset_Y = 20f;
            sleekImage12.TintColor = Palette.PRO;
            sleekImage12.Texture = MenuPlayUI.serverListUI.icons.load<Texture2D>("GoldRequired");
            button.AddChild(sleekImage12);
        }
        if (list == ESteamServerList.FAVORITES)
        {
            button.PositionOffset_X += 40f;
            button.SizeOffset_X -= 40f;
            favoriteButton = new SleekButtonIcon(null);
            favoriteButton.SizeOffset_X = 40f;
            favoriteButton.SizeScale_Y = 1f;
            favoriteButton.iconPositionOffset = 10;
            favoriteButton.iconColor = ESleekTint.FOREGROUND;
            favoriteButton.onClickedButton += onClickedFavoriteOffButton;
            AddChild(favoriteButton);
            refreshFavoriteButton();
        }
    }
}
