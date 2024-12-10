using System;
using UnityEngine;

namespace SDG.Unturned;

public class MenuPlayServerCurationUI : SleekFullscreenBox
{
    public Local localization;

    public Bundle icons;

    public bool active;

    private SleekButtonIcon backButton;

    internal MenuPlayServerCurationRulesUI rulesUI;

    private ISleekField urlField;

    private SleekList<ServerCurationItem> list;

    private ISleekLabel tutorialBox;

    private SleekButtonState denyModeButton;

    private SleekServerCurationRuleTester rulesTester;

    private ISleekToggle testerVisibleToggle;

    public void open()
    {
        if (!active)
        {
            active = true;
            SynchronizeSortedItems();
            AnimateIntoView();
        }
    }

    public void close()
    {
        if (active)
        {
            active = false;
            AnimateOutOfView(0f, 1f);
        }
    }

    private void SynchronizeSortedItems()
    {
        ServerListCuration serverListCuration = ServerListCuration.Get();
        serverListCuration.RefreshIfDirty();
        list.NotifyDataChanged();
        tutorialBox.IsVisible = serverListCuration.GetItems().Count < 1;
        RefreshRulesTester();
        list.ForEachElement(delegate(SleekServerCurationItem element)
        {
            element.SynchronizeBlockCount();
        });
    }

    private void OnUrlSubmitted(ISleekField field)
    {
        string text = field.Text;
        if (!WebUtils.ParseThirdPartyUrl(text, out var result, autoPrefix: true, useLinkFiltering: false))
        {
            UnturnedLog.info("Unable to parse curation URL \"" + text + "\"");
            return;
        }
        urlField.Text = string.Empty;
        ServerListCuration.Get().AddUrl(result);
        SynchronizeSortedItems();
    }

    private void OnAddUrlButtonClicked(ISleekElement button)
    {
        OnUrlSubmitted(urlField);
    }

    private void OnClickedItem(ServerCurationItem item)
    {
        rulesUI.open(item);
        close();
    }

    private void OnDeletedItem(ServerCurationItem item)
    {
        item.Delete();
        list.NotifyDataChanged();
        RefreshRulesTester();
    }

    private void OnMovedItem(ServerCurationItem item, int direction)
    {
        ServerListCuration.Get().MoveItem(item, direction);
        list.NotifyDataChanged();
        RefreshRulesTester();
    }

    private ISleekElement OnCreateListElement(ServerCurationItem item)
    {
        SleekServerCurationItem sleekServerCurationItem = new SleekServerCurationItem(localization, icons, item);
        sleekServerCurationItem.OnClickedItem += OnClickedItem;
        sleekServerCurationItem.OnDeletedItem += OnDeletedItem;
        sleekServerCurationItem.OnMovedItem += OnMovedItem;
        return sleekServerCurationItem;
    }

    private void OnClickedBackButton(ISleekElement button)
    {
        MenuPlayUI.serverListUI.open(shouldRefresh: true);
        close();
    }

    private void OnChangedDenyMode(SleekButtonState button, int value)
    {
        ServerListCuration.Get().DenyMode = (EServerListCurationDenyMode)value;
    }

    private void RefreshRulesTester()
    {
        if (testerVisibleToggle.Value)
        {
            rulesTester.TestRules(null, mergedRules: true);
        }
    }

    private void OnTesterVisibleToggled(ISleekToggle toggle, bool value)
    {
        list.SizeOffset_Y = (value ? (-260f) : (-170f));
        rulesTester.IsVisible = value;
        RefreshRulesTester();
    }

    public MenuPlayServerCurationUI(MenuPlayServersUI serverListUI)
    {
        localization = Localization.read("/Menu/Play/MenuPlayServerCuration.dat");
        icons = serverListUI.icons;
        active = false;
        rulesUI = new MenuPlayServerCurationRulesUI(this);
        rulesUI.PositionOffset_X = 10f;
        rulesUI.PositionOffset_Y = 10f;
        rulesUI.PositionScale_Y = 1f;
        rulesUI.SizeOffset_X = -20f;
        rulesUI.SizeOffset_Y = -20f;
        rulesUI.SizeScale_X = 1f;
        rulesUI.SizeScale_Y = 1f;
        MenuUI.container.AddChild(rulesUI);
        ISleekBox sleekBox = Glazier.Get().CreateBox();
        sleekBox.SizeScale_X = 1f;
        sleekBox.SizeOffset_Y = 60f;
        AddChild(sleekBox);
        ISleekLabel sleekLabel = Glazier.Get().CreateLabel();
        sleekLabel.SizeScale_X = 1f;
        sleekLabel.SizeOffset_Y = 40f;
        sleekLabel.Text = localization.format("Title");
        sleekLabel.FontSize = ESleekFontSize.Large;
        sleekBox.AddChild(sleekLabel);
        ISleekLabel sleekLabel2 = Glazier.Get().CreateLabel();
        sleekLabel2.PositionOffset_Y = 20f;
        sleekLabel2.SizeScale_X = 1f;
        sleekLabel2.SizeOffset_Y = 40f;
        sleekLabel2.Text = localization.format("TitleInfo");
        sleekBox.AddChild(sleekLabel2);
        urlField = Glazier.Get().CreateStringField();
        urlField.PositionOffset_Y = 70f;
        urlField.SizeOffset_X = -200f;
        urlField.SizeScale_X = 1f;
        urlField.SizeOffset_Y = 30f;
        urlField.PlaceholderText = localization.format("URL_Placeholder");
        urlField.TooltipText = localization.format("URL_Tooltip");
        urlField.OnTextSubmitted += OnUrlSubmitted;
        AddChild(urlField);
        SleekButtonIcon sleekButtonIcon = new SleekButtonIcon(icons.load<Texture2D>("NewPreset"), 20);
        sleekButtonIcon.PositionOffset_Y = 70f;
        sleekButtonIcon.PositionOffset_X = -200f;
        sleekButtonIcon.PositionScale_X = 1f;
        sleekButtonIcon.SizeOffset_X = 200f;
        sleekButtonIcon.SizeOffset_Y = 30f;
        sleekButtonIcon.text = localization.format("AddURLButton_Label");
        sleekButtonIcon.tooltip = localization.format("AddURLButton_Tooltip");
        sleekButtonIcon.onClickedButton += OnAddUrlButtonClicked;
        AddChild(sleekButtonIcon);
        list = new SleekList<ServerCurationItem>();
        list.PositionOffset_Y = 110f;
        list.SizeOffset_Y = -180f;
        list.SizeScale_X = 1f;
        list.SizeScale_Y = 1f;
        list.itemHeight = 40;
        list.onCreateElement = OnCreateListElement;
        list.SetData(ServerListCuration.Get().GetItems());
        AddChild(list);
        tutorialBox = Glazier.Get().CreateBox();
        tutorialBox.SizeOffset_Y = 80f;
        tutorialBox.SizeScale_X = 0.8f;
        tutorialBox.PositionScale_X = 0.1f;
        tutorialBox.PositionScale_Y = 0.5f;
        tutorialBox.PositionOffset_Y = -40f;
        tutorialBox.Text = localization.format("Tutorial");
        tutorialBox.FontSize = ESleekFontSize.Medium;
        tutorialBox.TextContrastContext = ETextContrastContext.ColorfulBackdrop;
        AddChild(tutorialBox);
        tutorialBox.IsVisible = false;
        denyModeButton = new SleekButtonState(new GUIContent(localization.format("DenyMode_Hide_Label", "DenyMode_Hide_Tooltip")), new GUIContent(localization.format("DenyMode_MoveToBottom_Label", "DenyMode_MoveToBottom_Label")));
        denyModeButton.PositionOffset_X = -650f;
        denyModeButton.PositionOffset_Y = -40f;
        denyModeButton.PositionScale_X = 1f;
        denyModeButton.PositionScale_Y = 1f;
        denyModeButton.SizeOffset_X = 200f;
        denyModeButton.SizeOffset_Y = 30f;
        denyModeButton.AddLabel(localization.format("DenyMode_Label"), ESleekSide.RIGHT);
        denyModeButton.state = (int)ServerListCuration.Get().DenyMode;
        SleekButtonState sleekButtonState = denyModeButton;
        sleekButtonState.onSwappedState = (SwappedState)Delegate.Combine(sleekButtonState.onSwappedState, new SwappedState(OnChangedDenyMode));
        AddChild(denyModeButton);
        rulesTester = new SleekServerCurationRuleTester(localization);
        rulesTester.PositionOffset_Y = -140f;
        rulesTester.PositionScale_Y = 1f;
        rulesTester.SizeScale_X = 1f;
        rulesTester.SizeOffset_Y = 80f;
        rulesTester.OnInputChanged += RefreshRulesTester;
        rulesTester.IsVisible = false;
        AddChild(rulesTester);
        testerVisibleToggle = Glazier.Get().CreateToggle();
        testerVisibleToggle.PositionOffset_X = -245f;
        testerVisibleToggle.PositionScale_X = 1f;
        testerVisibleToggle.PositionScale_Y = 1f;
        testerVisibleToggle.PositionOffset_Y = -45f;
        testerVisibleToggle.AddLabel(localization.format("Test_Visible_Label"), ESleekSide.RIGHT);
        testerVisibleToggle.TooltipText = localization.format("Test_Visible_Tooltip");
        testerVisibleToggle.Value = false;
        testerVisibleToggle.OnValueChanged += OnTesterVisibleToggled;
        AddChild(testerVisibleToggle);
        backButton = new SleekButtonIcon(MenuDashboardUI.icons.load<Texture2D>("Exit"));
        backButton.PositionOffset_Y = -50f;
        backButton.PositionScale_Y = 1f;
        backButton.SizeOffset_X = 200f;
        backButton.SizeOffset_Y = 50f;
        backButton.text = MenuDashboardUI.localization.format("BackButtonText");
        backButton.tooltip = MenuDashboardUI.localization.format("BackButtonTooltip");
        backButton.onClickedButton += OnClickedBackButton;
        backButton.fontSize = ESleekFontSize.Medium;
        backButton.iconColor = ESleekTint.FOREGROUND;
        AddChild(backButton);
    }
}
