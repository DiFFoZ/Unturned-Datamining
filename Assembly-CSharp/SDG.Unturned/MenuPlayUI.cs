using System;
using UnityEngine;

namespace SDG.Unturned;

public class MenuPlayUI
{
    private static SleekFullscreenBox container;

    public static bool active;

    private static SleekButtonIcon connectButton;

    private static SleekButtonIcon serversButton;

    private static SleekButtonIcon serverBookmarksButton;

    private static SleekButtonIcon singleplayerButton;

    private static SleekButtonIcon lobbiesButton;

    private static SleekButtonIconConfirm tutorialButton;

    private static SleekButtonIcon backButton;

    private MenuPlayConnectUI connectUI;

    public static MenuPlayServersUI serverListUI;

    public static MenuPlayServerBookmarksUI serverBookmarksUI;

    public static MenuPlayOnlineSafetyUI onlineSafetyUI;

    private MenuPlayServerInfoUI serverInfoUI;

    private MenuPlaySingleplayerUI singleplayerUI;

    private MenuPlayLobbiesUI lobbiesUI;

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
            tutorialButton.reset();
            container.AnimateOutOfView(0f, -1f);
        }
    }

    private static void onClickedConnectButton(ISleekElement button)
    {
        onlineSafetyUI.OpenIfNecessary(EOnlineSafetyDestination.Connect);
        close();
    }

    private static void onClickedServersButton(ISleekElement button)
    {
        onlineSafetyUI.OpenIfNecessary(EOnlineSafetyDestination.ServerList);
        close();
    }

    private static void OnClickedServerBookmarksButton(ISleekElement button)
    {
        onlineSafetyUI.OpenIfNecessary(EOnlineSafetyDestination.Bookmarks);
        close();
    }

    private static void onClickedSingleplayerButton(ISleekElement button)
    {
        MenuPlaySingleplayerUI.open();
        close();
    }

    private static void onClickedLobbiesButton(ISleekElement button)
    {
        onlineSafetyUI.OpenIfNecessary(EOnlineSafetyDestination.Lobby);
        close();
    }

    private static void onClickedTutorialButton(ISleekElement button)
    {
        if (ReadWrite.folderExists("/Worlds/Singleplayer_" + Characters.selected + "/Level/Tutorial"))
        {
            ReadWrite.deleteFolder("/Worlds/Singleplayer_" + Characters.selected + "/Level/Tutorial");
        }
        if (ReadWrite.folderExists("/Worlds/Singleplayer_" + Characters.selected + "/Players/" + Provider.user.ToString() + "_" + Characters.selected + "/Tutorial"))
        {
            ReadWrite.deleteFolder("/Worlds/Singleplayer_" + Characters.selected + "/Players/" + Provider.user.ToString() + "_" + Characters.selected + "/Tutorial");
        }
        Provider.map = "Tutorial";
        Provider.singleplayer(EGameMode.TUTORIAL, singleplayerCheats: false);
    }

    private static void onClickedBackButton(ISleekElement button)
    {
        MenuDashboardUI.open();
        MenuTitleUI.open();
        close();
    }

    public void OnDestroy()
    {
        connectUI.OnDestroy();
        serverInfoUI.OnDestroy();
        singleplayerUI.OnDestroy();
        lobbiesUI.OnDestroy();
    }

    public MenuPlayUI()
    {
        Local local = Localization.read("/Menu/Play/MenuPlay.dat");
        Bundle bundle = Bundles.getBundle("/Bundles/Textures/Menu/Icons/Play/MenuPlay/MenuPlay.unity3d");
        container = new SleekFullscreenBox();
        container.PositionOffset_X = 10f;
        container.PositionOffset_Y = 10f;
        container.PositionScale_Y = -1f;
        container.SizeOffset_X = -20f;
        container.SizeOffset_Y = -20f;
        container.SizeScale_X = 1f;
        container.SizeScale_Y = 1f;
        MenuUI.container.AddChild(container);
        active = false;
        float num = 0f;
        ISleekElement sleekElement = Glazier.Get().CreateFrame();
        sleekElement.PositionOffset_X = -100f;
        sleekElement.PositionScale_X = 0.5f;
        sleekElement.PositionScale_Y = 0.5f;
        sleekElement.SizeOffset_X = 200f;
        container.AddChild(sleekElement);
        tutorialButton = new SleekButtonIconConfirm(bundle.load<Texture2D>("Tutorial"), local.format("Tutorial_Confirm_Label"), local.format("Tutorial_Confirm_Tooltip"), local.format("Tutorial_Deny_Label"), local.format("Tutorial_Deny_Tooltip"), 40);
        tutorialButton.PositionOffset_Y = num;
        tutorialButton.SizeOffset_X = 200f;
        tutorialButton.SizeOffset_Y = 50f;
        tutorialButton.text = local.format("TutorialButtonText");
        tutorialButton.tooltip = local.format("TutorialButtonTooltip");
        SleekButtonIconConfirm sleekButtonIconConfirm = tutorialButton;
        sleekButtonIconConfirm.onConfirmed = (Confirm)Delegate.Combine(sleekButtonIconConfirm.onConfirmed, new Confirm(onClickedTutorialButton));
        tutorialButton.fontSize = ESleekFontSize.Medium;
        tutorialButton.iconColor = ESleekTint.FOREGROUND;
        sleekElement.AddChild(tutorialButton);
        num += tutorialButton.SizeOffset_Y;
        num += 10f;
        singleplayerButton = new SleekButtonIcon(bundle.load<Texture2D>("Singleplayer"));
        singleplayerButton.PositionOffset_Y = num;
        singleplayerButton.SizeOffset_X = 200f;
        singleplayerButton.SizeOffset_Y = 50f;
        singleplayerButton.text = local.format("SingleplayerButtonText");
        singleplayerButton.tooltip = local.format("SingleplayerButtonTooltip");
        singleplayerButton.onClickedButton += onClickedSingleplayerButton;
        singleplayerButton.iconColor = ESleekTint.FOREGROUND;
        singleplayerButton.fontSize = ESleekFontSize.Medium;
        sleekElement.AddChild(singleplayerButton);
        num += singleplayerButton.SizeOffset_Y;
        num += 10f;
        serversButton = new SleekButtonIcon(bundle.load<Texture2D>("Servers"));
        serversButton.PositionOffset_Y = num;
        serversButton.SizeOffset_X = 200f;
        serversButton.SizeOffset_Y = 50f;
        serversButton.text = local.format("ServersButtonText");
        serversButton.tooltip = local.format("ServersButtonTooltip");
        serversButton.iconColor = ESleekTint.FOREGROUND;
        serversButton.onClickedButton += onClickedServersButton;
        serversButton.fontSize = ESleekFontSize.Medium;
        sleekElement.AddChild(serversButton);
        num += serversButton.SizeOffset_Y;
        num += 10f;
        connectButton = new SleekButtonIcon(bundle.load<Texture2D>("Connect"));
        connectButton.PositionOffset_Y = num;
        connectButton.SizeOffset_X = 200f;
        connectButton.SizeOffset_Y = 50f;
        connectButton.text = local.format("ConnectButtonText");
        connectButton.tooltip = local.format("ConnectButtonTooltip");
        connectButton.iconColor = ESleekTint.FOREGROUND;
        connectButton.onClickedButton += onClickedConnectButton;
        connectButton.fontSize = ESleekFontSize.Medium;
        sleekElement.AddChild(connectButton);
        num += connectButton.SizeOffset_Y;
        num += 10f;
        serverBookmarksButton = new SleekButtonIcon(bundle.load<Texture2D>("Bookmarks"), 40);
        serverBookmarksButton.PositionOffset_Y = num;
        serverBookmarksButton.SizeOffset_X = 200f;
        serverBookmarksButton.SizeOffset_Y = 50f;
        serverBookmarksButton.text = local.format("ServerBookmarksButtonText");
        serverBookmarksButton.tooltip = local.format("ServerBookmarksButtonTooltip");
        serverBookmarksButton.iconColor = ESleekTint.FOREGROUND;
        serverBookmarksButton.onClickedButton += OnClickedServerBookmarksButton;
        serverBookmarksButton.fontSize = ESleekFontSize.Medium;
        sleekElement.AddChild(serverBookmarksButton);
        num += serverBookmarksButton.SizeOffset_Y;
        num += 10f;
        lobbiesButton = new SleekButtonIcon(bundle.load<Texture2D>("Lobbies"));
        lobbiesButton.PositionOffset_Y = num;
        lobbiesButton.SizeOffset_X = 200f;
        lobbiesButton.SizeOffset_Y = 50f;
        lobbiesButton.text = local.format("LobbiesButtonText");
        lobbiesButton.tooltip = local.format("LobbiesButtonTooltip");
        lobbiesButton.onClickedButton += onClickedLobbiesButton;
        lobbiesButton.iconColor = ESleekTint.FOREGROUND;
        lobbiesButton.fontSize = ESleekFontSize.Medium;
        sleekElement.AddChild(lobbiesButton);
        num += lobbiesButton.SizeOffset_Y;
        num += 10f;
        backButton = new SleekButtonIcon(MenuDashboardUI.icons.load<Texture2D>("Exit"));
        backButton.PositionOffset_Y = num;
        backButton.SizeOffset_X = 200f;
        backButton.SizeOffset_Y = 50f;
        backButton.text = MenuDashboardUI.localization.format("BackButtonText");
        backButton.tooltip = MenuDashboardUI.localization.format("BackButtonTooltip");
        backButton.onClickedButton += onClickedBackButton;
        backButton.fontSize = ESleekFontSize.Medium;
        backButton.iconColor = ESleekTint.FOREGROUND;
        sleekElement.AddChild(backButton);
        sleekElement.PositionOffset_Y = 0f - (sleekElement.SizeOffset_Y = num + backButton.SizeOffset_Y) / 2f;
        bundle.unload();
        connectUI = new MenuPlayConnectUI();
        serverListUI = new MenuPlayServersUI();
        serverListUI.PositionOffset_X = 10f;
        serverListUI.PositionOffset_Y = 10f;
        serverListUI.PositionScale_Y = 1f;
        serverListUI.SizeOffset_X = -20f;
        serverListUI.SizeOffset_Y = -20f;
        serverListUI.SizeScale_X = 1f;
        serverListUI.SizeScale_Y = 1f;
        MenuUI.container.AddChild(serverListUI);
        serverBookmarksUI = new MenuPlayServerBookmarksUI();
        serverBookmarksUI.PositionOffset_X = 10f;
        serverBookmarksUI.PositionOffset_Y = 10f;
        serverBookmarksUI.PositionScale_Y = 1f;
        serverBookmarksUI.SizeOffset_X = -20f;
        serverBookmarksUI.SizeOffset_Y = -20f;
        serverBookmarksUI.SizeScale_X = 1f;
        serverBookmarksUI.SizeScale_Y = 1f;
        MenuUI.container.AddChild(serverBookmarksUI);
        onlineSafetyUI = new MenuPlayOnlineSafetyUI();
        onlineSafetyUI.PositionOffset_X = 10f;
        onlineSafetyUI.PositionOffset_Y = 10f;
        onlineSafetyUI.PositionScale_Y = 1f;
        onlineSafetyUI.SizeOffset_X = -20f;
        onlineSafetyUI.SizeOffset_Y = -20f;
        onlineSafetyUI.SizeScale_X = 1f;
        onlineSafetyUI.SizeScale_Y = 1f;
        MenuUI.container.AddChild(onlineSafetyUI);
        serverInfoUI = new MenuPlayServerInfoUI();
        singleplayerUI = new MenuPlaySingleplayerUI();
        lobbiesUI = new MenuPlayLobbiesUI();
    }
}
