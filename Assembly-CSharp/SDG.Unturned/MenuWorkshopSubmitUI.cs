using System;
using System.Collections.Generic;
using System.IO;
using SDG.Provider;
using UnityEngine;
using Unturned.SystemEx;

namespace SDG.Unturned;

public class MenuWorkshopSubmitUI
{
    private static Local localization;

    private static SleekFullscreenBox container;

    public static bool active;

    private static SleekButtonIcon backButton;

    private static ISleekField nameField;

    private static ISleekField descriptionField;

    private static ISleekField pathField;

    private static ISleekBox pathNotification;

    private static ISleekField previewField;

    private static ISleekBox previewNotification;

    private static ISleekField changeField;

    private static ISleekField allowedIPsField;

    private static SleekButtonState typeState;

    private static SleekButtonState mapTypeState;

    private static SleekButtonState itemTypeState;

    private static SleekButtonState vehicleTypeState;

    private static SleekButtonState skinTypeState;

    private static SleekButtonState objectTypeState;

    private static SleekButtonState visibilityState;

    private static SleekButtonState forState;

    private static SleekButtonIcon createButton;

    private static ISleekButton legalButton;

    private static ISleekScrollView publishedBox;

    private static List<ISleekButton> publishedButtons;

    private static string tag => (ESteamUGCType)typeState.state switch
    {
        ESteamUGCType.MAP => mapTypeState.states[mapTypeState.state].text, 
        ESteamUGCType.ITEM => itemTypeState.states[itemTypeState.state].text, 
        ESteamUGCType.VEHICLE => vehicleTypeState.states[vehicleTypeState.state].text, 
        ESteamUGCType.SKIN => skinTypeState.states[skinTypeState.state].text, 
        ESteamUGCType.OBJECT => objectTypeState.states[objectTypeState.state].text, 
        _ => "", 
    };

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

    private static void refreshPathFieldNotification()
    {
        string text = pathField.Text;
        string text2 = null;
        if (string.IsNullOrEmpty(text))
        {
            text2 = localization.format("PathFieldNotification_Empty");
        }
        else if (!ReadWrite.folderExists(text, usePath: false))
        {
            text2 = localization.format("PathFieldNotification_MissingFolder");
        }
        else if (!ReadWrite.hasDirectoryWritePermission(text))
        {
            text2 = localization.format("PathFieldNotification_NoWritePermission");
        }
        else
        {
            switch ((ESteamUGCType)typeState.state)
            {
            case ESteamUGCType.MAP:
                if (!WorkshopTool.checkMapValid(text, usePath: false))
                {
                    text2 = localization.format("PathFieldNotification_Map");
                }
                break;
            case ESteamUGCType.LOCALIZATION:
                if (!WorkshopTool.checkLocalizationValid(text, usePath: false))
                {
                    text2 = localization.format("PathFieldNotification_Localization");
                }
                break;
            default:
                if (!WorkshopTool.checkBundleValid(text, usePath: false))
                {
                    text2 = localization.format("PathFieldNotification_Bundle");
                }
                break;
            }
        }
        pathNotification.IsVisible = !string.IsNullOrEmpty(text2);
        pathNotification.TooltipText = text2;
    }

    private static void onPathFieldTyped(ISleekField field, string text)
    {
        refreshPathFieldNotification();
    }

    private static void refreshPreviewFieldNotification()
    {
        string text = previewField.Text;
        string text2 = null;
        if (string.IsNullOrEmpty(text))
        {
            text2 = localization.format("PreviewFieldNotification_Empty");
        }
        else if (!ReadWrite.fileExists(text, useCloud: false, usePath: false))
        {
            text2 = localization.format("PreviewFieldNotification_MissingFile");
        }
        else if (text.EndsWith(".png", StringComparison.InvariantCultureIgnoreCase) || text.EndsWith(".jpg", StringComparison.InvariantCultureIgnoreCase))
        {
            long length = new FileInfo(text).Length;
            if (length > 1000000)
            {
                if (localization.has("PreviewFieldNotification_FileSize_V2"))
                {
                    string arg = ByteDisplay.FileSizeToString(length);
                    string arg2 = ByteDisplay.FileSizeToString(1000000L);
                    text2 = localization.format("PreviewFieldNotification_FileSize_V2", arg, arg2);
                }
                else
                {
                    text2 = localization.format("PreviewFieldNotification_FileSize");
                }
            }
        }
        else
        {
            text2 = localization.format("PreviewFieldNotification_Extension");
        }
        previewNotification.IsVisible = !string.IsNullOrEmpty(text2);
        previewNotification.TooltipText = text2;
    }

    private static void onPreviewFieldTyped(ISleekField field, string text)
    {
        refreshPreviewFieldNotification();
    }

    private static void onClickedCreateButton(ISleekElement button)
    {
        if (checkEntered() && checkValid())
        {
            Provider.provider.workshopService.prepareUGC(nameField.Text, descriptionField.Text, pathField.Text, previewField.Text, changeField.Text, (ESteamUGCType)typeState.state, tag, allowedIPsField.Text, (ESteamUGCVisibility)visibilityState.state);
            Provider.provider.workshopService.createUGC(forState.state == 1);
            resetFields();
        }
    }

    private static void onClickedLegalButton(ISleekElement button)
    {
        if (!Provider.provider.browserService.canOpenBrowser)
        {
            MenuUI.alert(localization.format("Overlay"));
        }
        else
        {
            Provider.provider.browserService.open("http://steamcommunity.com/sharedfiles/workshoplegalagreement/?appid=304930");
        }
    }

    private static void onClickedPublished(ISleekElement button)
    {
        int index = Mathf.FloorToInt(button.PositionOffset_Y / 40f);
        if (checkValid())
        {
            Provider.provider.workshopService.prepareUGC(nameField.Text, descriptionField.Text, pathField.Text, previewField.Text, changeField.Text, (ESteamUGCType)typeState.state, tag, allowedIPsField.Text, (ESteamUGCVisibility)visibilityState.state);
            Provider.provider.workshopService.prepareUGC(Provider.provider.workshopService.published[index].id);
            Provider.provider.workshopService.updateUGC();
            resetFields();
        }
    }

    private static void onPublishedAdded()
    {
        for (int i = 0; i < Provider.provider.workshopService.published.Count; i++)
        {
            SteamPublished steamPublished = Provider.provider.workshopService.published[i];
            ISleekButton sleekButton = Glazier.Get().CreateButton();
            sleekButton.PositionOffset_Y = i * 40;
            sleekButton.SizeOffset_Y = 30f;
            sleekButton.SizeScale_X = 1f;
            sleekButton.Text = steamPublished.name;
            sleekButton.OnClicked += onClickedPublished;
            publishedBox.AddChild(sleekButton);
            publishedButtons.Add(sleekButton);
            publishedBox.ContentSizeOffset = new Vector2(0f, publishedButtons.Count * 40 - 10);
        }
    }

    private static void onPublishedRemoved()
    {
        publishedBox.RemoveAllChildren();
        publishedButtons.Clear();
    }

    private static bool checkEntered()
    {
        if (nameField.Text.Length == 0)
        {
            MenuUI.alert(localization.format("Alert_Name"));
            return false;
        }
        if (previewField.Text.Length == 0 || !ReadWrite.fileExists(previewField.Text, useCloud: false, usePath: false) || new FileInfo(previewField.Text).Length > 1000000)
        {
            MenuUI.alert(localization.format("Alert_Preview"));
            return false;
        }
        return true;
    }

    private static bool checkValid()
    {
        if (pathField.Text.Length == 0 || !ReadWrite.folderExists(pathField.Text, usePath: false))
        {
            MenuUI.alert(localization.format("Alert_Path"));
            return false;
        }
        ESteamUGCType state = (ESteamUGCType)typeState.state;
        if (forState.state == 1)
        {
            if (state != ESteamUGCType.ITEM && state != ESteamUGCType.SKIN)
            {
                MenuUI.alert(localization.format("Alert_Curated"));
                return false;
            }
        }
        else if (state == ESteamUGCType.SKIN)
        {
            MenuUI.alert(localization.format("Alert_Curated"));
            return false;
        }
        bool flag = false;
        switch (state)
        {
        case ESteamUGCType.MAP:
            flag = WorkshopTool.checkMapValid(pathField.Text, usePath: false);
            if (!flag)
            {
                MenuUI.alert(localization.format("Alert_Map"));
            }
            break;
        case ESteamUGCType.LOCALIZATION:
            flag = WorkshopTool.checkLocalizationValid(pathField.Text, usePath: false);
            if (!flag)
            {
                MenuUI.alert(localization.format("Alert_Localization"));
            }
            break;
        case ESteamUGCType.OBJECT:
        case ESteamUGCType.ITEM:
        case ESteamUGCType.VEHICLE:
        case ESteamUGCType.SKIN:
            flag = WorkshopTool.checkBundleValid(pathField.Text, usePath: false);
            if (!flag)
            {
                MenuUI.alert(localization.format("Alert_Object"));
            }
            break;
        }
        return flag;
    }

    private static void resetFields()
    {
        nameField.Text = "";
        descriptionField.Text = "";
        pathField.Text = "";
        previewField.Text = "";
        changeField.Text = "";
        allowedIPsField.Text = "";
        refreshPathFieldNotification();
        refreshPreviewFieldNotification();
    }

    private static void onSwappedTypeState(SleekButtonState button, int state)
    {
        mapTypeState.IsVisible = state == 0;
        itemTypeState.IsVisible = state == 3;
        vehicleTypeState.IsVisible = state == 4;
        skinTypeState.IsVisible = state == 5;
        objectTypeState.IsVisible = state == 2;
        refreshPathFieldNotification();
    }

    private static void onClickedBackButton(ISleekElement button)
    {
        MenuWorkshopUI.open();
        close();
    }

    public void OnDestroy()
    {
        TempSteamworksWorkshop workshopService = Provider.provider.workshopService;
        workshopService.onPublishedAdded = (TempSteamworksWorkshop.PublishedAdded)Delegate.Remove(workshopService.onPublishedAdded, new TempSteamworksWorkshop.PublishedAdded(onPublishedAdded));
        TempSteamworksWorkshop workshopService2 = Provider.provider.workshopService;
        workshopService2.onPublishedRemoved = (TempSteamworksWorkshop.PublishedRemoved)Delegate.Remove(workshopService2.onPublishedRemoved, new TempSteamworksWorkshop.PublishedRemoved(onPublishedRemoved));
    }

    public MenuWorkshopSubmitUI()
    {
        localization = Localization.read("/Menu/Workshop/MenuWorkshopSubmit.dat");
        Bundle bundle = Bundles.getBundle("/Bundles/Textures/Menu/Icons/Workshop/MenuWorkshopSubmit/MenuWorkshopSubmit.unity3d");
        publishedButtons = new List<ISleekButton>();
        TempSteamworksWorkshop workshopService = Provider.provider.workshopService;
        workshopService.onPublishedAdded = (TempSteamworksWorkshop.PublishedAdded)Delegate.Combine(workshopService.onPublishedAdded, new TempSteamworksWorkshop.PublishedAdded(onPublishedAdded));
        TempSteamworksWorkshop workshopService2 = Provider.provider.workshopService;
        workshopService2.onPublishedRemoved = (TempSteamworksWorkshop.PublishedRemoved)Delegate.Combine(workshopService2.onPublishedRemoved, new TempSteamworksWorkshop.PublishedRemoved(onPublishedRemoved));
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
        nameField = Glazier.Get().CreateStringField();
        nameField.PositionOffset_X = -200f;
        nameField.PositionOffset_Y = 140f;
        nameField.PositionScale_X = 0.5f;
        nameField.SizeOffset_X = 200f;
        nameField.SizeOffset_Y = 30f;
        nameField.MaxLength = 24;
        nameField.AddLabel(localization.format("Name_Field_Label"), ESleekSide.RIGHT);
        container.AddChild(nameField);
        descriptionField = Glazier.Get().CreateStringField();
        descriptionField.PositionOffset_X = -200f;
        descriptionField.PositionOffset_Y = 140f;
        descriptionField.PositionScale_X = 0.5f;
        descriptionField.SizeOffset_X = 400f;
        descriptionField.SizeOffset_Y = 30f;
        descriptionField.MaxLength = 128;
        descriptionField.Text = "";
        descriptionField.AddLabel(localization.format("Description_Field_Label"), ESleekSide.RIGHT);
        container.AddChild(descriptionField);
        descriptionField.IsVisible = false;
        pathField = Glazier.Get().CreateStringField();
        pathField.PositionOffset_X = -200f;
        pathField.PositionOffset_Y = 180f;
        pathField.PositionScale_X = 0.5f;
        pathField.SizeOffset_X = 400f;
        pathField.SizeOffset_Y = 30f;
        pathField.MaxLength = 128;
        pathField.AddLabel(localization.format("Path_Field_Label"), ESleekSide.RIGHT);
        pathField.OnTextChanged += onPathFieldTyped;
        container.AddChild(pathField);
        pathNotification = Glazier.Get().CreateBox();
        pathNotification.PositionOffset_X = -240f;
        pathNotification.PositionOffset_Y = 180f;
        pathNotification.PositionScale_X = 0.5f;
        pathNotification.SizeOffset_X = 30f;
        pathNotification.SizeOffset_Y = 30f;
        pathNotification.Text = "!";
        container.AddChild(pathNotification);
        pathNotification.IsVisible = false;
        previewField = Glazier.Get().CreateStringField();
        previewField.PositionOffset_X = -200f;
        previewField.PositionOffset_Y = 220f;
        previewField.PositionScale_X = 0.5f;
        previewField.SizeOffset_X = 400f;
        previewField.SizeOffset_Y = 30f;
        previewField.MaxLength = 128;
        previewField.AddLabel(localization.format("Preview_Field_Label"), ESleekSide.RIGHT);
        previewField.OnTextChanged += onPreviewFieldTyped;
        container.AddChild(previewField);
        previewNotification = Glazier.Get().CreateBox();
        previewNotification.PositionOffset_X = -240f;
        previewNotification.PositionOffset_Y = 220f;
        previewNotification.PositionScale_X = 0.5f;
        previewNotification.SizeOffset_X = 30f;
        previewNotification.SizeOffset_Y = 30f;
        previewNotification.Text = "!";
        container.AddChild(previewNotification);
        previewNotification.IsVisible = false;
        changeField = Glazier.Get().CreateStringField();
        changeField.PositionOffset_X = -200f;
        changeField.PositionOffset_Y = 260f;
        changeField.PositionScale_X = 0.5f;
        changeField.SizeOffset_X = 400f;
        changeField.SizeOffset_Y = 30f;
        changeField.MaxLength = 128;
        changeField.AddLabel(localization.format("Change_Field_Label"), ESleekSide.RIGHT);
        container.AddChild(changeField);
        typeState = new SleekButtonState(new GUIContent(localization.format("Map")), new GUIContent(localization.format("Localization")), new GUIContent(localization.format("Object")), new GUIContent(localization.format("Item")), new GUIContent(localization.format("Vehicle")), new GUIContent(localization.format("Skin")));
        typeState.PositionOffset_X = -200f;
        typeState.PositionOffset_Y = 300f;
        typeState.PositionScale_X = 0.5f;
        typeState.SizeOffset_X = 195f;
        typeState.SizeOffset_Y = 30f;
        typeState.onSwappedState = onSwappedTypeState;
        container.AddChild(typeState);
        mapTypeState = new SleekButtonState(new GUIContent(localization.format("Map_Type_Survival")), new GUIContent(localization.format("Map_Type_Horde")), new GUIContent(localization.format("Map_Type_Arena")), new GUIContent(localization.format("Map_Type_Custom")));
        mapTypeState.PositionOffset_X = 5f;
        mapTypeState.PositionOffset_Y = 300f;
        mapTypeState.PositionScale_X = 0.5f;
        mapTypeState.SizeOffset_X = 195f;
        mapTypeState.SizeOffset_Y = 30f;
        container.AddChild(mapTypeState);
        mapTypeState.IsVisible = true;
        itemTypeState = new SleekButtonState(new GUIContent(localization.format("Item_Type_Backpack")), new GUIContent(localization.format("Item_Type_Barrel")), new GUIContent(localization.format("Item_Type_Barricade")), new GUIContent(localization.format("Item_Type_Fisher")), new GUIContent(localization.format("Item_Type_Food")), new GUIContent(localization.format("Item_Type_Fuel")), new GUIContent(localization.format("Item_Type_Glasses")), new GUIContent(localization.format("Item_Type_Grip")), new GUIContent(localization.format("Item_Type_Grower")), new GUIContent(localization.format("Item_Type_Gun")), new GUIContent(localization.format("Item_Type_Hat")), new GUIContent(localization.format("Item_Type_Magazine")), new GUIContent(localization.format("Item_Type_Mask")), new GUIContent(localization.format("Item_Type_Medical")), new GUIContent(localization.format("Item_Type_Melee")), new GUIContent(localization.format("Item_Type_Optic")), new GUIContent(localization.format("Item_Type_Shirt")), new GUIContent(localization.format("Item_Type_Sight")), new GUIContent(localization.format("Item_Type_Structure")), new GUIContent(localization.format("Item_Type_Supply")), new GUIContent(localization.format("Item_Type_Tactical")), new GUIContent(localization.format("Item_Type_Throwable")), new GUIContent(localization.format("Item_Type_Tool")), new GUIContent(localization.format("Item_Type_Vest")), new GUIContent(localization.format("Item_Type_Water")));
        itemTypeState.PositionOffset_X = 5f;
        itemTypeState.PositionOffset_Y = 300f;
        itemTypeState.PositionScale_X = 0.5f;
        itemTypeState.SizeOffset_X = 195f;
        itemTypeState.SizeOffset_Y = 30f;
        container.AddChild(itemTypeState);
        itemTypeState.IsVisible = false;
        vehicleTypeState = new SleekButtonState(new GUIContent(localization.format("Vehicle_Type_Wheels_2")), new GUIContent(localization.format("Vehicle_Type_Wheels_4")), new GUIContent(localization.format("Vehicle_Type_Plane")), new GUIContent(localization.format("Vehicle_Type_Helicopter")), new GUIContent(localization.format("Vehicle_Type_Boat")), new GUIContent(localization.format("Vehicle_Type_Train")));
        vehicleTypeState.PositionOffset_X = 5f;
        vehicleTypeState.PositionOffset_Y = 300f;
        vehicleTypeState.PositionScale_X = 0.5f;
        vehicleTypeState.SizeOffset_X = 195f;
        vehicleTypeState.SizeOffset_Y = 30f;
        container.AddChild(vehicleTypeState);
        vehicleTypeState.IsVisible = false;
        skinTypeState = new SleekButtonState(new GUIContent(localization.format("Skin_Type_Generic_Pattern")), new GUIContent(localization.format("Skin_Type_Ace")), new GUIContent(localization.format("Skin_Type_Augewehr")), new GUIContent(localization.format("Skin_Type_Avenger")), new GUIContent(localization.format("Skin_Type_Bluntforce")), new GUIContent(localization.format("Skin_Type_Bulldog")), new GUIContent(localization.format("Skin_Type_Butterfly_Knife")), new GUIContent(localization.format("Skin_Type_Calling_Card")), new GUIContent(localization.format("Skin_Type_Cobra")), new GUIContent(localization.format("Skin_Type_Colt")), new GUIContent(localization.format("Skin_Type_Compound_Bow")), new GUIContent(localization.format("Skin_Type_Crossbow")), new GUIContent(localization.format("Skin_Type_Desert_Falcon")), new GUIContent(localization.format("Skin_Type_Dragonfang")), new GUIContent(localization.format("Skin_Type_Eaglefire")), new GUIContent(localization.format("Skin_Type_Ekho")), new GUIContent(localization.format("Skin_Type_Fusilaut")), new GUIContent(localization.format("Skin_Type_Grizzly")), new GUIContent(localization.format("Skin_Type_Hawkhound")), new GUIContent(localization.format("Skin_Type_Heartbreaker")), new GUIContent(localization.format("Skin_Type_Hell_Fury")), new GUIContent(localization.format("Skin_Type_Honeybadger")), new GUIContent(localization.format("Skin_Type_Katana")), new GUIContent(localization.format("Skin_Type_Kryzkarek")), new GUIContent(localization.format("Skin_Type_Machete")), new GUIContent(localization.format("Skin_Type_Maplestrike")), new GUIContent(localization.format("Skin_Type_Maschinengewehr")), new GUIContent(localization.format("Skin_Type_Masterkey")), new GUIContent(localization.format("Skin_Type_Matamorez")), new GUIContent(localization.format("Skin_Type_Military_Knife")), new GUIContent(localization.format("Skin_Type_Nightraider")), new GUIContent(localization.format("Skin_Type_Nykorev")), new GUIContent(localization.format("Skin_Type_Peacemaker")), new GUIContent(localization.format("Skin_Type_Rocket_Launcher")), new GUIContent(localization.format("Skin_Type_Sabertooth")), new GUIContent(localization.format("Skin_Type_Scalar")), new GUIContent(localization.format("Skin_Type_Schofield")), new GUIContent(localization.format("Skin_Type_Shadowstalker")), new GUIContent(localization.format("Skin_Type_Snayperskya")), new GUIContent(localization.format("Skin_Type_Sportshot")), new GUIContent(localization.format("Skin_Type_Teklowvka")), new GUIContent(localization.format("Skin_Type_Timberwolf")), new GUIContent(localization.format("Skin_Type_Viper")), new GUIContent(localization.format("Skin_Type_Vonya")), new GUIContent(localization.format("Skin_Type_Yuri")), new GUIContent(localization.format("Skin_Type_Zubeknakov")));
        skinTypeState.PositionOffset_X = 5f;
        skinTypeState.PositionOffset_Y = 300f;
        skinTypeState.PositionScale_X = 0.5f;
        skinTypeState.SizeOffset_X = 195f;
        skinTypeState.SizeOffset_Y = 30f;
        container.AddChild(skinTypeState);
        skinTypeState.IsVisible = false;
        objectTypeState = new SleekButtonState(new GUIContent(localization.format("Object_Type_Model")), new GUIContent(localization.format("Object_Type_Resource")), new GUIContent(localization.format("Object_Type_Effect")), new GUIContent(localization.format("Object_Type_Animal")));
        objectTypeState.PositionOffset_X = 5f;
        objectTypeState.PositionOffset_Y = 300f;
        objectTypeState.PositionScale_X = 0.5f;
        objectTypeState.SizeOffset_X = 195f;
        objectTypeState.SizeOffset_Y = 30f;
        container.AddChild(objectTypeState);
        objectTypeState.IsVisible = false;
        visibilityState = new SleekButtonState(new GUIContent(localization.format("Public")), new GUIContent(localization.format("Friends_Only")), new GUIContent(localization.format("Private")), new GUIContent(localization.format("Unlisted")));
        visibilityState.PositionOffset_X = -200f;
        visibilityState.PositionOffset_Y = 340f;
        visibilityState.PositionScale_X = 0.5f;
        visibilityState.SizeOffset_X = 195f;
        visibilityState.SizeOffset_Y = 30f;
        container.AddChild(visibilityState);
        forState = new SleekButtonState(new GUIContent(localization.format("Community")), new GUIContent(localization.format("Review")));
        forState.PositionOffset_X = 5f;
        forState.PositionOffset_Y = 340f;
        forState.PositionScale_X = 0.5f;
        forState.SizeOffset_X = 195f;
        forState.SizeOffset_Y = 30f;
        container.AddChild(forState);
        allowedIPsField = Glazier.Get().CreateStringField();
        allowedIPsField.PositionOffset_X = -200f;
        allowedIPsField.PositionOffset_Y = 380f;
        allowedIPsField.PositionScale_X = 0.5f;
        allowedIPsField.SizeOffset_X = 400f;
        allowedIPsField.SizeOffset_Y = 30f;
        allowedIPsField.MaxLength = 255;
        allowedIPsField.TooltipText = localization.format("Allowed_IPs_Tooltip");
        allowedIPsField.PlaceholderText = localization.format("Allowed_IPs_Hint");
        allowedIPsField.AddLabel(localization.format("Allowed_IPs_Label"), ESleekSide.RIGHT);
        container.AddChild(allowedIPsField);
        createButton = new SleekButtonIcon(bundle.load<Texture2D>("Create"));
        createButton.PositionOffset_X = -200f;
        createButton.PositionOffset_Y = 420f;
        createButton.PositionScale_X = 0.5f;
        createButton.SizeOffset_X = 195f;
        createButton.SizeOffset_Y = 30f;
        createButton.text = localization.format("Create_Button");
        createButton.tooltip = localization.format("Create_Button_Tooltip");
        createButton.onClickedButton += onClickedCreateButton;
        createButton.iconColor = ESleekTint.FOREGROUND;
        container.AddChild(createButton);
        legalButton = Glazier.Get().CreateButton();
        legalButton.PositionOffset_X = 5f;
        legalButton.PositionOffset_Y = 420f;
        legalButton.PositionScale_X = 0.5f;
        legalButton.SizeOffset_X = 195f;
        legalButton.SizeOffset_Y = 30f;
        legalButton.FontSize = ESleekFontSize.Small;
        legalButton.Text = localization.format("Legal_Button");
        legalButton.TooltipText = localization.format("Legal_Button_Tooltip");
        legalButton.OnClicked += onClickedLegalButton;
        container.AddChild(legalButton);
        publishedBox = Glazier.Get().CreateScrollView();
        publishedBox.PositionOffset_X = -200f;
        publishedBox.PositionOffset_Y = 460f;
        publishedBox.PositionScale_X = 0.5f;
        publishedBox.SizeOffset_X = 430f;
        publishedBox.SizeOffset_Y = -460f;
        publishedBox.SizeScale_Y = 1f;
        publishedBox.ScaleContentToWidth = true;
        container.AddChild(publishedBox);
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
        onPublishedAdded();
        bundle.unload();
    }
}
