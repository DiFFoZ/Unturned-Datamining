using System;
using UnityEngine;

namespace SDG.Unturned;

/// <summary>
/// Entry in the MenuPlayServerCurationUI list.
/// </summary>
public class SleekServerCurationItem : SleekWrapper
{
    private Local localization;

    private ServerCurationItem item;

    private ISleekToggle toggle;

    private ISleekButton button;

    private ISleekImage icon;

    private SleekWebImage webIcon;

    private SleekButtonIcon moveUpButton;

    private SleekButtonIcon moveDownButton;

    private ISleekLabel nameLabel;

    private ISleekLabel errorLabel;

    private ISleekLabel originLabel;

    internal event Action<ServerCurationItem> OnClickedItem;

    internal event Action<ServerCurationItem> OnDeletedItem;

    internal event Action<ServerCurationItem, int> OnMovedItem;

    private void OnActiveToggled(ISleekToggle toggle, bool value)
    {
        item.IsActive = value;
        RefreshIsActive();
    }

    private void OnClickedButton(ISleekElement button)
    {
        this.OnClickedItem?.Invoke(item);
    }

    private void OnClickedRemoveButton(ISleekElement button)
    {
        this.OnDeletedItem?.Invoke(item);
    }

    private void OnClickedMoveUpButton(ISleekElement button)
    {
        this.OnMovedItem?.Invoke(item, -1);
    }

    private void OnClickedMoveDownButton(ISleekElement button)
    {
        this.OnMovedItem?.Invoke(item, 1);
    }

    private void RefreshIsActive()
    {
        if (item.IsActive)
        {
            nameLabel.TextColor = ESleekTint.FONT;
            originLabel.TextColor = ESleekTint.FONT;
            icon.TintColor = ESleekTint.NONE;
            webIcon.internalImage.TintColor = ESleekTint.NONE;
            toggle.TooltipText = localization.format("Deactivate_Tooltip");
        }
        else
        {
            nameLabel.TextColor = new SleekColor(ESleekTint.FONT, 0.5f);
            originLabel.TextColor = new SleekColor(ESleekTint.FONT, 0.5f);
            icon.TintColor = new Color(1f, 1f, 1f, 0.5f);
            webIcon.internalImage.TintColor = new Color(1f, 1f, 1f, 0.5f);
            toggle.TooltipText = localization.format("Activate_Tooltip");
        }
    }

    private void SynchronizeSortOrder()
    {
        bool flag = !item.IsAtFrontOfList;
        moveUpButton.isClickable = flag;
        moveUpButton.iconColor = new SleekColor(ESleekTint.FOREGROUND, flag ? 1f : 0.5f);
        bool flag2 = !item.IsAtBackOfList;
        moveDownButton.isClickable = flag2;
        moveDownButton.iconColor = new SleekColor(ESleekTint.FOREGROUND, flag2 ? 1f : 0.5f);
    }

    private void SynchronizeDetails()
    {
        ServerCurationItem_Web serverCurationItem_Web = item as ServerCurationItem_Web;
        string text = ((serverCurationItem_Web == null || (bool)Provider.allowWebRequests) ? item.ErrorMessage : localization.format("NoWebRequests"));
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
            nameLabel.Text = localization.format("WebItemPending");
        }
        else
        {
            nameLabel.Text = item.DisplayName;
        }
        originLabel.Text = item.DisplayOrigin;
        if (item.Icon != null)
        {
            icon.IsVisible = true;
            icon.Texture = item.Icon;
            webIcon.IsVisible = false;
        }
        else if (!string.IsNullOrEmpty(item.IconUrl))
        {
            icon.IsVisible = false;
            webIcon.Refresh(item.IconUrl);
            webIcon.IsVisible = true;
        }
        else
        {
            icon.IsVisible = false;
            webIcon.IsVisible = false;
        }
    }

    public override void OnDestroy()
    {
        base.OnDestroy();
        item.OnSortOrderChanged -= SynchronizeSortOrder;
        item.OnDataChanged -= SynchronizeDetails;
    }

    internal SleekServerCurationItem(Local localization, Bundle icons, ServerCurationItem item)
    {
        this.localization = localization;
        this.item = item;
        item.OnSortOrderChanged += SynchronizeSortOrder;
        item.OnDataChanged += SynchronizeDetails;
        toggle = Glazier.Get().CreateToggle();
        toggle.SizeOffset_X = 40f;
        toggle.SizeOffset_Y = 40f;
        toggle.Value = item.IsActive;
        toggle.OnValueChanged += OnActiveToggled;
        AddChild(toggle);
        button = Glazier.Get().CreateButton();
        button.PositionOffset_X = 40f;
        button.SizeScale_X = 1f;
        button.SizeScale_Y = 1f;
        button.OnClicked += OnClickedButton;
        nameLabel = Glazier.Get().CreateLabel();
        nameLabel.PositionOffset_X = 45f;
        nameLabel.SizeScale_X = 1f;
        nameLabel.SizeOffset_X = -45f;
        nameLabel.TextAlignment = TextAnchor.MiddleLeft;
        nameLabel.SizeOffset_Y = 30f;
        button.AddChild(nameLabel);
        errorLabel = Glazier.Get().CreateLabel();
        errorLabel.PositionOffset_X = 45f;
        errorLabel.SizeScale_X = 1f;
        errorLabel.SizeOffset_X = -45f;
        errorLabel.TextAlignment = TextAnchor.MiddleLeft;
        errorLabel.SizeOffset_Y = 30f;
        errorLabel.TextColor = ESleekTint.BAD;
        button.AddChild(errorLabel);
        originLabel = Glazier.Get().CreateLabel();
        originLabel.PositionOffset_X = 45f;
        originLabel.PositionOffset_Y = 15f;
        originLabel.SizeScale_X = 1f;
        originLabel.SizeOffset_X = -45f;
        originLabel.SizeOffset_Y = 30f;
        originLabel.FontSize = ESleekFontSize.Small;
        originLabel.TextAlignment = TextAnchor.MiddleLeft;
        button.AddChild(originLabel);
        icon = Glazier.Get().CreateImage();
        icon.PositionOffset_X = 4f;
        icon.PositionOffset_Y = 4f;
        icon.SizeOffset_X = 32f;
        icon.SizeOffset_Y = 32f;
        icon.IsVisible = false;
        button.AddChild(icon);
        webIcon = new SleekWebImage();
        webIcon.PositionOffset_X = 4f;
        webIcon.PositionOffset_Y = 4f;
        webIcon.SizeOffset_X = 32f;
        webIcon.SizeOffset_Y = 32f;
        webIcon.IsVisible = false;
        button.AddChild(webIcon);
        if (item.IsDeletable)
        {
            button.SizeOffset_X = -160f;
            SleekButtonIcon sleekButtonIcon = new SleekButtonIcon(icons.load<Texture2D>("DeletePreset"), 20);
            sleekButtonIcon.PositionScale_X = 1f;
            sleekButtonIcon.PositionOffset_X = -120f;
            sleekButtonIcon.SizeOffset_X = 40f;
            sleekButtonIcon.SizeScale_Y = 1f;
            sleekButtonIcon.iconPositionOffset = 10;
            sleekButtonIcon.iconColor = ESleekTint.FOREGROUND;
            sleekButtonIcon.onClickedButton += OnClickedRemoveButton;
            sleekButtonIcon.tooltip = localization.format("Remove_Tooltip");
            AddChild(sleekButtonIcon);
        }
        else
        {
            button.SizeOffset_X = -120f;
        }
        moveUpButton = new SleekButtonIcon(icons.load<Texture2D>("MoveCurationItemUp"), 20);
        moveUpButton.PositionScale_X = 1f;
        moveUpButton.PositionOffset_X = -80f;
        moveUpButton.SizeOffset_X = 40f;
        moveUpButton.SizeScale_Y = 1f;
        moveUpButton.iconPositionOffset = 10;
        moveUpButton.iconColor = ESleekTint.FOREGROUND;
        moveUpButton.onClickedButton += OnClickedMoveUpButton;
        moveUpButton.tooltip = localization.format("MoveUp_Tooltip");
        AddChild(moveUpButton);
        moveDownButton = new SleekButtonIcon(icons.load<Texture2D>("MoveCurationItemDown"), 20);
        moveDownButton.PositionScale_X = 1f;
        moveDownButton.PositionOffset_X = -40f;
        moveDownButton.SizeOffset_X = 40f;
        moveDownButton.SizeScale_Y = 1f;
        moveDownButton.iconPositionOffset = 10;
        moveDownButton.iconColor = ESleekTint.FOREGROUND;
        moveDownButton.onClickedButton += OnClickedMoveDownButton;
        moveDownButton.tooltip = localization.format("MoveDown_Tooltip");
        AddChild(moveDownButton);
        SynchronizeSortOrder();
        SynchronizeDetails();
        RefreshIsActive();
        AddChild(button);
    }
}
