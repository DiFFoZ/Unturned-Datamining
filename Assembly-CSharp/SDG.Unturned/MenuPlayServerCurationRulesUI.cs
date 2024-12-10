using System;
using UnityEngine;

namespace SDG.Unturned;

internal class MenuPlayServerCurationRulesUI : SleekFullscreenBox
{
    public Local localization;

    public Bundle icons;

    public bool active;

    private ServerCurationItem item;

    private SleekList<ServerListCurationRule> list;

    private bool isAssetsReloadedBound;

    private bool isDataChangedBound;

    private ISleekLabel nameLabel;

    private ISleekLabel errorLabel;

    private ISleekLabel originLabel;

    private ISleekImage icon;

    private SleekWebImage webIcon;

    private ISleekButton activationButton;

    private SleekServerCurationRuleTester rulesTester;

    private ISleekToggle testerVisibleToggle;

    public void open(ServerCurationItem item)
    {
        if (!active)
        {
            active = true;
            BindAssetsReloaded();
            SetItem(item);
            AnimateIntoView();
        }
    }

    public void close()
    {
        if (active)
        {
            active = false;
            UnbindAssetsReloaded();
            UnbindDataChanged();
            AnimateOutOfView(0f, 1f);
        }
    }

    private ISleekElement OnCreateListElement(ServerListCurationRule rule)
    {
        return new SleekServerCurationRule(this, rule);
    }

    private void OnClickedActivationButton(ISleekElement button)
    {
        if (item != null)
        {
            item.IsActive = !item.IsActive;
            SynchronizeActivationButton();
        }
    }

    private void OnClickedReloadButton(ISleekElement button)
    {
        if (item != null)
        {
            item.Reload();
        }
    }

    private void OnClickedBackButton(ISleekElement button)
    {
        MenuPlayServersUI.serverCurationUI.open();
        close();
    }

    private void SynchronizeActivationButton()
    {
        if (item.IsActive)
        {
            activationButton.Text = localization.format("Deactivate_Label");
            activationButton.TooltipText = localization.format("Deactivate_Tooltip");
        }
        else
        {
            activationButton.Text = localization.format("Activate_Label");
            activationButton.TooltipText = localization.format("Activate_Tooltip");
        }
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        UnbindAssetsReloaded();
    }

    private void BindAssetsReloaded()
    {
        if (!isAssetsReloadedBound)
        {
            isAssetsReloadedBound = true;
            Assets.OnNewAssetsFinishedLoading = (System.Action)Delegate.Combine(Assets.OnNewAssetsFinishedLoading, new System.Action(OnNewAssetsFinishedLoading));
        }
    }

    private void UnbindAssetsReloaded()
    {
        if (isAssetsReloadedBound)
        {
            isAssetsReloadedBound = false;
            Assets.OnNewAssetsFinishedLoading = (System.Action)Delegate.Remove(Assets.OnNewAssetsFinishedLoading, new System.Action(OnNewAssetsFinishedLoading));
        }
    }

    private void UnbindDataChanged()
    {
        if (isDataChangedBound)
        {
            isDataChangedBound = false;
            item.OnDataChanged -= OnDataChanged;
        }
    }

    private void SetItem(ServerCurationItem newItem)
    {
        UnbindDataChanged();
        item = newItem;
        if (item == null)
        {
            nameLabel.Text = "null";
            nameLabel.IsVisible = true;
            errorLabel.IsVisible = false;
            originLabel.Text = "null";
            icon.IsVisible = false;
            webIcon.IsVisible = false;
            list.SetData(null);
        }
        else
        {
            item.OnDataChanged += OnDataChanged;
            OnDataChanged();
            SynchronizeActivationButton();
        }
    }

    private void OnNewAssetsFinishedLoading()
    {
        ServerListCuration.Get().RefreshIfDirty();
    }

    private void OnDataChanged()
    {
        ServerCurationItem_Web serverCurationItem_Web = item as ServerCurationItem_Web;
        string text = ((serverCurationItem_Web == null || (bool)Provider.allowWebRequests) ? item.ErrorMessage : MenuPlayServersUI.serverCurationUI.localization.format("NoWebRequests"));
        if (!string.IsNullOrEmpty(text))
        {
            errorLabel.Text = text;
            nameLabel.IsVisible = false;
            errorLabel.IsVisible = true;
        }
        else
        {
            nameLabel.IsVisible = true;
            errorLabel.IsVisible = false;
        }
        if (serverCurationItem_Web != null && serverCurationItem_Web.isWaitingForResponse)
        {
            nameLabel.Text = MenuPlayServersUI.serverCurationUI.localization.format("WebItemPending");
        }
        else
        {
            nameLabel.Text = item.DisplayName;
        }
        originLabel.Text = item.DisplayOrigin;
        if (item.Icon != null)
        {
            icon.Texture = item.Icon;
            icon.IsVisible = true;
            webIcon.IsVisible = false;
        }
        else if (!string.IsNullOrEmpty(item.IconUrl))
        {
            webIcon.Refresh(item.IconUrl);
            webIcon.IsVisible = true;
            icon.IsVisible = false;
        }
        else
        {
            icon.IsVisible = false;
            webIcon.IsVisible = false;
        }
        list.SetData(item.GetRules());
        RefreshRulesTester();
        list.ForEachElement(delegate(SleekServerCurationRule element)
        {
            element.SynchronizeBlockCount();
        });
    }

    private void RefreshRulesTester()
    {
        if (item != null && testerVisibleToggle.Value)
        {
            rulesTester.TestRules(item.GetRules(), mergedRules: false);
        }
    }

    private void OnTesterVisibleToggled(ISleekToggle toggle, bool value)
    {
        list.SizeOffset_Y = (value ? (-200f) : (-110f));
        rulesTester.IsVisible = value;
        RefreshRulesTester();
    }

    public MenuPlayServerCurationRulesUI(MenuPlayServerCurationUI curationUI)
    {
        localization = curationUI.localization;
        active = false;
        ISleekBox sleekBox = Glazier.Get().CreateBox();
        sleekBox.SizeScale_X = 1f;
        sleekBox.SizeOffset_Y = 40f;
        sleekBox.SizeOffset_X = -420f;
        ISleekButton sleekButton = Glazier.Get().CreateButton();
        sleekButton.SizeOffset_X = 200f;
        sleekButton.SizeOffset_Y = 40f;
        sleekButton.PositionScale_X = 1f;
        sleekButton.PositionOffset_X = -410f;
        sleekButton.Text = localization.format("Reload_Label");
        sleekButton.TooltipText = localization.format("Reload_Tooltip");
        sleekButton.OnClicked += OnClickedReloadButton;
        AddChild(sleekButton);
        activationButton = Glazier.Get().CreateButton();
        activationButton.SizeOffset_X = 200f;
        activationButton.SizeOffset_Y = 40f;
        activationButton.PositionScale_X = 1f;
        activationButton.PositionOffset_X = -200f;
        activationButton.OnClicked += OnClickedActivationButton;
        AddChild(activationButton);
        nameLabel = Glazier.Get().CreateLabel();
        nameLabel.PositionOffset_X = 45f;
        nameLabel.SizeScale_X = 1f;
        nameLabel.SizeOffset_X = -45f;
        nameLabel.TextAlignment = TextAnchor.MiddleLeft;
        nameLabel.SizeOffset_Y = 30f;
        sleekBox.AddChild(nameLabel);
        errorLabel = Glazier.Get().CreateLabel();
        errorLabel.PositionOffset_X = 45f;
        errorLabel.SizeScale_X = 1f;
        errorLabel.SizeOffset_X = -45f;
        errorLabel.TextAlignment = TextAnchor.MiddleLeft;
        errorLabel.SizeOffset_Y = 30f;
        errorLabel.TextColor = ESleekTint.BAD;
        sleekBox.AddChild(errorLabel);
        originLabel = Glazier.Get().CreateLabel();
        originLabel.PositionOffset_X = 45f;
        originLabel.PositionOffset_Y = 15f;
        originLabel.SizeScale_X = 1f;
        originLabel.SizeOffset_X = -45f;
        originLabel.SizeOffset_Y = 30f;
        originLabel.FontSize = ESleekFontSize.Small;
        originLabel.AllowRichText = true;
        originLabel.TextAlignment = TextAnchor.MiddleLeft;
        sleekBox.AddChild(originLabel);
        icon = Glazier.Get().CreateImage();
        icon.PositionOffset_X = 4f;
        icon.PositionOffset_Y = 4f;
        icon.SizeOffset_X = 32f;
        icon.SizeOffset_Y = 32f;
        sleekBox.AddChild(icon);
        webIcon = new SleekWebImage();
        webIcon.PositionOffset_X = 4f;
        webIcon.PositionOffset_Y = 4f;
        webIcon.SizeOffset_X = 32f;
        webIcon.SizeOffset_Y = 32f;
        sleekBox.AddChild(webIcon);
        AddChild(sleekBox);
        list = new SleekList<ServerListCurationRule>();
        list.PositionOffset_Y = 50f;
        list.SizeOffset_Y = -120f;
        list.SizeScale_X = 1f;
        list.SizeScale_Y = 1f;
        list.itemHeight = 40;
        list.onCreateElement = OnCreateListElement;
        AddChild(list);
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
        SleekButtonIcon sleekButtonIcon = new SleekButtonIcon(MenuDashboardUI.icons.load<Texture2D>("Exit"));
        sleekButtonIcon.PositionOffset_Y = -50f;
        sleekButtonIcon.PositionScale_Y = 1f;
        sleekButtonIcon.SizeOffset_X = 200f;
        sleekButtonIcon.SizeOffset_Y = 50f;
        sleekButtonIcon.text = MenuDashboardUI.localization.format("BackButtonText");
        sleekButtonIcon.tooltip = MenuDashboardUI.localization.format("BackButtonTooltip");
        sleekButtonIcon.onClickedButton += OnClickedBackButton;
        sleekButtonIcon.fontSize = ESleekFontSize.Medium;
        sleekButtonIcon.iconColor = ESleekTint.FOREGROUND;
        AddChild(sleekButtonIcon);
    }
}
