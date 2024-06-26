using System;
using System.Net;
using Steamworks;
using UnityEngine;
using UnityEngine.Networking;
using Unturned.SystemEx;

namespace SDG.Unturned;

public class MenuPlayConnectUI
{
    public static Bundle icons;

    public static Local localization;

    private static SleekFullscreenBox container;

    public static bool active;

    /// <summary>
    /// These server relay variables redirect the client to another server when the menu opens
    /// similar to how Steam sets the +connect string on game startup. Allows plugin to redirect
    /// player to another server on the same network.
    /// </summary>
    public static bool hasPendingServerRelay;

    public static uint serverRelayIP;

    public static ushort serverRelayPort;

    public static CSteamID serverRelayServerCode;

    public static string serverRelayPassword;

    public static bool serverRelayWaitOnMenu;

    private static SleekButtonIcon backButton;

    private static ISleekField hostField;

    private static ISleekUInt16Field portField;

    private static ISleekField passwordField;

    private static SleekButtonIcon connectButton;

    private static ISleekBox addressInfoBox;

    private static ISleekBox serverCodeInfoBox;

    private static ISleekImage serverCodeIcon;

    private static bool isLaunched;

    /// <param name="shouldAutoJoin">If true the server is immediately joined, otherwise show server details beforehand.</param>
    public static void connect(SteamConnectionInfo info, bool shouldAutoJoin, MenuPlayServerInfoUI.EServerInfoOpenContext openContext)
    {
        Provider.provider.matchmakingService.connect(info);
        Provider.provider.matchmakingService.autoJoinServerQuery = shouldAutoJoin;
        Provider.provider.matchmakingService.serverQueryContext = openContext;
    }

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
            MenuSettings.save();
            container.AnimateOutOfView(0f, 1f);
        }
    }

    internal static bool TryParseHostString(string input, out IPv4Address address, out CSteamID steamId, out ushort queryPortOverride)
    {
        address = default(IPv4Address);
        steamId = default(CSteamID);
        queryPortOverride = 0;
        if (string.IsNullOrEmpty(input))
        {
            UnturnedLog.info("Unable to parse empty host string");
            return false;
        }
        input = input.Trim();
        if (string.IsNullOrEmpty(input))
        {
            UnturnedLog.info("Unable to parse host string empty after trimming");
            return false;
        }
        bool autoPrefix = input.Contains('/');
        if (WebUtils.ParseThirdPartyUrl(input, out var result, autoPrefix))
        {
            if (!Provider.allowWebRequests)
            {
                UnturnedLog.warn("Unable to request host details because web requests are disabled");
                return false;
            }
            UnturnedLog.info("Requesting host details from " + result + "...");
            using UnityWebRequest unityWebRequest = UnityWebRequest.Get(result);
            unityWebRequest.timeout = 2;
            unityWebRequest.SendWebRequest();
            while (!unityWebRequest.isDone)
            {
            }
            if (unityWebRequest.result != UnityWebRequest.Result.Success)
            {
                UnturnedLog.warn("Network error requesting host details: \"" + unityWebRequest.error + "\"");
                return false;
            }
            string text = unityWebRequest.downloadHandler.text.Trim();
            if (string.IsNullOrEmpty(text))
            {
                UnturnedLog.info("Unable to parse empty host details response");
                return false;
            }
            int num = text.IndexOf(':');
            if (num < 0)
            {
                input = text;
            }
            else
            {
                input = text.Substring(0, num);
                ushort.TryParse(text.Substring(num + 1), out queryPortOverride);
            }
            UnturnedLog.info($"Received host details ({input}:{queryPortOverride}) from {result}");
        }
        if (input.Length >= 6 && ulong.TryParse(input, out var result2))
        {
            steamId = new CSteamID(result2);
            if (steamId.BGameServerAccount())
            {
                return true;
            }
            steamId = CSteamID.Nil;
        }
        if (string.Equals(input, "localhost", StringComparison.OrdinalIgnoreCase))
        {
            address = new IPv4Address("127.0.0.1");
            return true;
        }
        if (IPv4Address.TryParse(input, out address))
        {
            return true;
        }
        string text2;
        try
        {
            IPAddress[] hostAddresses = Dns.GetHostAddresses(input);
            text2 = ((hostAddresses.Length == 0 || hostAddresses[0] == null) ? null : hostAddresses[0].ToString());
        }
        catch (Exception e)
        {
            text2 = input;
            UnturnedLog.exception(e, "Caught exception while resolving host string \"" + input + "\":");
        }
        if (string.IsNullOrEmpty(text2))
        {
            UnturnedLog.info("Resolved address was empty");
            return false;
        }
        if (!IPv4Address.TryParse(text2, out address))
        {
            UnturnedLog.info("Unable to parse resolved address \"" + text2 + "\"");
            return false;
        }
        return true;
    }

    private static void onClickedConnectButton(ISleekElement button)
    {
        if (!TryParseHostString(hostField.Text, out var address, out var steamId, out var queryPortOverride))
        {
            UnturnedLog.info("Cannot connect because unable to parse host string");
            return;
        }
        if (steamId.BGameServerAccount())
        {
            MenuSettings.save();
            Provider.connect(new ServerConnectParameters(steamId, passwordField.Text), null, null);
            return;
        }
        ushort num = ((queryPortOverride > 0) ? queryPortOverride : portField.Value);
        if (num == 0)
        {
            UnturnedLog.info("Cannot connect because port field is empty");
            return;
        }
        SteamConnectionInfo info = new SteamConnectionInfo(address.value, num, passwordField.Text);
        MenuSettings.save();
        connect(info, shouldAutoJoin: false, MenuPlayServerInfoUI.EServerInfoOpenContext.CONNECT);
    }

    private static void onTypedHostField(ISleekField field, string text)
    {
        PlaySettings.connectHost = text;
        addressInfoBox.IsVisible = false;
        RefreshServerCodeInfo();
    }

    private static void SplitHostIntoAddressAndPort()
    {
        string text = hostField.Text;
        int num = text.LastIndexOf(':');
        if (num >= 0 && ushort.TryParse(text.Substring(num + 1), out var result))
        {
            PlaySettings.connectHost = text.Substring(0, num);
            PlaySettings.connectPort = result;
            hostField.Text = PlaySettings.connectHost;
            portField.Value = PlaySettings.connectPort;
        }
    }

    private static void OnIpFieldCommitted(ISleekField field)
    {
        SplitHostIntoAddressAndPort();
        RefreshAddressInfo();
        RefreshServerCodeInfo();
    }

    private static void onTypedPortField(ISleekUInt16Field field, ushort state)
    {
        PlaySettings.connectPort = state;
    }

    private static void onTypedPasswordField(ISleekField field, string text)
    {
        PlaySettings.connectPassword = text;
    }

    private static void onAttemptUpdated(int attempt)
    {
        MenuUI.openAlert(localization.format("Connecting", attempt));
    }

    private static void onTimedOut()
    {
        if (Provider.connectionFailureInfo != 0)
        {
            ESteamConnectionFailureInfo connectionFailureInfo = Provider.connectionFailureInfo;
            Provider.resetConnectionFailure();
            switch (connectionFailureInfo)
            {
            case ESteamConnectionFailureInfo.PRO_SERVER:
                MenuUI.alert(localization.format("Pro_Server"));
                break;
            case ESteamConnectionFailureInfo.PASSWORD:
                MenuUI.alert(localization.format("Password"));
                break;
            case ESteamConnectionFailureInfo.FULL:
                MenuUI.alert(localization.format("Full"));
                break;
            case ESteamConnectionFailureInfo.TIMED_OUT:
                MenuUI.alert(localization.format("Timed_Out"));
                break;
            }
        }
    }

    private static void RefreshAddressInfo()
    {
        addressInfoBox.IsVisible = false;
        string text = hostField.Text.ToLower();
        text = text.Trim();
        if (string.IsNullOrEmpty(text) || (text.Length >= 6 && ulong.TryParse(text, out var _)))
        {
            return;
        }
        string text2;
        if (text == "localhost")
        {
            text2 = "127.0.0.1";
        }
        else
        {
            try
            {
                IPAddress[] hostAddresses = Dns.GetHostAddresses(text);
                text2 = ((hostAddresses.Length == 0 || hostAddresses[0] == null) ? null : hostAddresses[0].ToString());
            }
            catch (Exception e)
            {
                UnturnedLog.exception(e, "Caught exception while resolving \"" + text + "\" for address info box:");
                text2 = text;
            }
        }
        if (!string.IsNullOrEmpty(text2) && IPv4Address.TryParse(text2, out IPv4Address address))
        {
            if (address.IsLoopback)
            {
                addressInfoBox.Text = localization.format("Address_Loopback_Label");
                addressInfoBox.TooltipText = localization.format("Address_Loopback_Tooltip");
                addressInfoBox.IsVisible = true;
            }
            else if (address.IsLocalPrivate)
            {
                addressInfoBox.Text = localization.format("Address_LocalPrivate_Label");
                addressInfoBox.TooltipText = localization.format("Address_LocalPrivate_Tooltip");
                addressInfoBox.IsVisible = true;
            }
        }
    }

    private static void RefreshServerCodeInfo()
    {
        serverCodeInfoBox.IsVisible = false;
        portField.IsVisible = true;
        string text = hostField.Text.ToLower();
        text = text.Trim();
        if (!string.IsNullOrEmpty(text) && text.Length >= 6 && ulong.TryParse(text, out var result))
        {
            CSteamID cSteamID = new CSteamID(result);
            if (cSteamID.BGameServerAccount())
            {
                serverCodeInfoBox.Text = localization.format("ServerCode_Valid_Label");
                serverCodeInfoBox.TooltipText = localization.format("ServerCode_Valid_Tooltip");
                serverCodeIcon.Texture = icons.load<Texture2D>("ValidServerCode");
                serverCodeIcon.TintColor = ESleekTint.FOREGROUND;
            }
            else if (cSteamID.BIndividualAccount())
            {
                serverCodeInfoBox.Text = localization.format("ServerCode_Invalid_Label");
                serverCodeInfoBox.TooltipText = localization.format("ServerCode_Friend_Tooltip");
                serverCodeIcon.Texture = icons.load<Texture2D>("InvalidServerCode");
                serverCodeIcon.TintColor = ESleekTint.BAD;
            }
            else
            {
                serverCodeInfoBox.Text = localization.format("ServerCode_Invalid_Label");
                serverCodeInfoBox.TooltipText = localization.format("ServerCode_Invalid_Tooltip");
                serverCodeIcon.Texture = icons.load<Texture2D>("InvalidServerCode");
                serverCodeIcon.TintColor = ESleekTint.BAD;
            }
            serverCodeInfoBox.IsVisible = true;
            portField.IsVisible = false;
        }
    }

    private static void onClickedBackButton(ISleekElement button)
    {
        MenuPlayUI.open();
        close();
    }

    public void OnDestroy()
    {
        Provider.provider.matchmakingService.onAttemptUpdated -= onAttemptUpdated;
        Provider.provider.matchmakingService.onTimedOut -= onTimedOut;
    }

    public MenuPlayConnectUI()
    {
        if (icons != null)
        {
            icons.unload();
        }
        localization = Localization.read("/Menu/Play/MenuPlayConnect.dat");
        icons = Bundles.getBundle("/Bundles/Textures/Menu/Icons/Play/MenuPlayConnect/MenuPlayConnect.unity3d");
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
        hostField = Glazier.Get().CreateStringField();
        hostField.PositionOffset_X = -300f;
        hostField.PositionOffset_Y = -75f;
        hostField.PositionScale_X = 0.5f;
        hostField.PositionScale_Y = 0.5f;
        hostField.SizeOffset_X = 600f;
        hostField.SizeOffset_Y = 30f;
        hostField.MaxLength = 64;
        hostField.AddLabel(localization.format("Host_Field_Label"), ESleekSide.RIGHT);
        hostField.TooltipText = localization.format("Host_Field_Tooltip");
        hostField.Text = PlaySettings.connectHost;
        hostField.OnTextChanged += onTypedHostField;
        hostField.OnTextSubmitted += OnIpFieldCommitted;
        container.AddChild(hostField);
        addressInfoBox = Glazier.Get().CreateBox();
        addressInfoBox.PositionOffset_X = -410f;
        addressInfoBox.PositionOffset_Y = -75f;
        addressInfoBox.PositionScale_X = 0.5f;
        addressInfoBox.PositionScale_Y = 0.5f;
        addressInfoBox.SizeOffset_X = 100f;
        addressInfoBox.SizeOffset_Y = 30f;
        addressInfoBox.IsVisible = false;
        container.AddChild(addressInfoBox);
        serverCodeInfoBox = Glazier.Get().CreateBox();
        serverCodeInfoBox.PositionOffset_X = -300f;
        serverCodeInfoBox.PositionOffset_Y = -35f;
        serverCodeInfoBox.PositionScale_X = 0.5f;
        serverCodeInfoBox.PositionScale_Y = 0.5f;
        serverCodeInfoBox.SizeOffset_X = 600f;
        serverCodeInfoBox.SizeOffset_Y = 30f;
        serverCodeInfoBox.IsVisible = false;
        container.AddChild(serverCodeInfoBox);
        serverCodeIcon = Glazier.Get().CreateImage();
        serverCodeIcon.PositionOffset_X = 5f;
        serverCodeIcon.PositionOffset_Y = 5f;
        serverCodeIcon.SizeOffset_X = 20f;
        serverCodeIcon.SizeOffset_Y = 20f;
        serverCodeInfoBox.AddChild(serverCodeIcon);
        portField = Glazier.Get().CreateUInt16Field();
        portField.PositionOffset_X = -300f;
        portField.PositionOffset_Y = -35f;
        portField.PositionScale_X = 0.5f;
        portField.PositionScale_Y = 0.5f;
        portField.SizeOffset_X = 600f;
        portField.SizeOffset_Y = 30f;
        portField.AddLabel(localization.format("Port_Field_Label"), ESleekSide.RIGHT);
        portField.TooltipText = localization.format("Port_Field_Tooltip");
        portField.Value = PlaySettings.connectPort;
        portField.OnValueChanged += onTypedPortField;
        container.AddChild(portField);
        passwordField = Glazier.Get().CreateStringField();
        passwordField.PositionOffset_X = -300f;
        passwordField.PositionOffset_Y = 5f;
        passwordField.PositionScale_X = 0.5f;
        passwordField.PositionScale_Y = 0.5f;
        passwordField.SizeOffset_X = 600f;
        passwordField.SizeOffset_Y = 30f;
        passwordField.AddLabel(localization.format("Password_Field_Label"), ESleekSide.RIGHT);
        passwordField.IsPasswordField = true;
        passwordField.MaxLength = 0;
        passwordField.Text = PlaySettings.connectPassword;
        passwordField.OnTextChanged += onTypedPasswordField;
        container.AddChild(passwordField);
        connectButton = new SleekButtonIcon(icons.load<Texture2D>("Connect"));
        connectButton.PositionOffset_X = -300f;
        connectButton.PositionOffset_Y = 45f;
        connectButton.PositionScale_X = 0.5f;
        connectButton.PositionScale_Y = 0.5f;
        connectButton.SizeOffset_X = 600f;
        connectButton.SizeOffset_Y = 30f;
        connectButton.text = localization.format("Connect_Button");
        connectButton.tooltip = localization.format("Connect_Button_Tooltip");
        connectButton.iconColor = ESleekTint.FOREGROUND;
        connectButton.onClickedButton += onClickedConnectButton;
        container.AddChild(connectButton);
        RefreshAddressInfo();
        RefreshServerCodeInfo();
        Provider.provider.matchmakingService.onAttemptUpdated += onAttemptUpdated;
        Provider.provider.matchmakingService.onTimedOut += onTimedOut;
        if (!isLaunched)
        {
            isLaunched = true;
            ulong lobby;
            if (CommandLine.TryGetSteamConnect(Environment.CommandLine, out var ip, out var queryPort, out var pass))
            {
                SteamConnectionInfo steamConnectionInfo = new SteamConnectionInfo(ip, queryPort, pass);
                UnturnedLog.info("Command-line connect IP: {0} Port: {1} Password: '{2}'", Parser.getIPFromUInt32(steamConnectionInfo.ip), steamConnectionInfo.port, steamConnectionInfo.password);
                connect(steamConnectionInfo, shouldAutoJoin: false, MenuPlayServerInfoUI.EServerInfoOpenContext.CONNECT);
            }
            else if (CommandLine.tryGetLobby(Environment.CommandLine, out lobby))
            {
                UnturnedLog.info("Lobby: " + lobby);
                Lobbies.joinLobby(new CSteamID(lobby));
            }
        }
        else if (hasPendingServerRelay)
        {
            hasPendingServerRelay = false;
            UnturnedLog.info("Relay connect IP: {0} Port: {1} Code: {2} Password: \"{3}\"", Parser.getIPFromUInt32(serverRelayIP), serverRelayPort, serverRelayServerCode, serverRelayPassword);
            bool shouldAutoJoin = !serverRelayWaitOnMenu;
            if (serverRelayServerCode != CSteamID.Nil)
            {
                if (serverRelayServerCode.BGameServerAccount())
                {
                    Provider.connect(new ServerConnectParameters(serverRelayServerCode, serverRelayPassword), null, null);
                }
                else
                {
                    UnturnedLog.warn($"Unable to join non-gameserver code ({serverRelayServerCode.GetEAccountType()})");
                }
            }
            else
            {
                connect(new SteamConnectionInfo(serverRelayIP, serverRelayPort, serverRelayPassword), shouldAutoJoin, MenuPlayServerInfoUI.EServerInfoOpenContext.CONNECT);
            }
        }
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
    }
}
