using System;
using System.Collections.Generic;
using UnityEngine;

namespace SDG.Unturned;

public class PlayerNPCVendorUI
{
    private static SleekFullscreenBox container;

    public static Local localization;

    public static bool active;

    private static VendorAsset vendor;

    private static DialogueAsset dialogue;

    private static DialogueMessage nextMessage;

    private static bool hasNextDialogue;

    private static List<VendorBuying> buying;

    private static List<VendorSellingBase> selling;

    private static List<SleekVendor> buyingButtons;

    private static List<SleekVendor> sellingButtons;

    private static VendorBuyingNameAscendingComparator buyingComparator = new VendorBuyingNameAscendingComparator();

    private static VendorSellingNameAscendingComparator sellingComparator = new VendorSellingNameAscendingComparator();

    private static ISleekBox vendorBox;

    private static ISleekLabel nameLabel;

    private static ISleekLabel descriptionLabel;

    private static ISleekLabel sellingLabel;

    private static ISleekScrollView sellingBox;

    private static ISleekLabel buyingLabel;

    private static ISleekScrollView buyingBox;

    private static ISleekBox experienceBox;

    private static ISleekBox currencyBox;

    private static ISleekElement currencyPanel;

    private static ISleekLabel currencyLabel;

    private static ISleekButton returnButton;

    private static bool needsRefresh;

    public static void open(VendorAsset newVendor, DialogueAsset newDialogue, DialogueMessage newNextMessage, bool newHasNextDialogue)
    {
        if (!active)
        {
            active = true;
            updateVendor(newVendor, newDialogue, newNextMessage, newHasNextDialogue);
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

    public static void closeNicely()
    {
        close();
        PlayerNPCDialogueUI.open(dialogue, nextMessage, hasNextDialogue);
    }

    /// <summary>
    /// Update currency and owned items if inventory has changed and menu is open.
    /// </summary>
    public static void MaybeRefresh()
    {
        if (!active || !needsRefresh || vendor == null)
        {
            return;
        }
        Player player = Player.player;
        if (player == null || player.inventory == null)
        {
            return;
        }
        needsRefresh = false;
        RefreshExperienceOrCurrencyBoxAmount();
        RefreshButtonVisibility();
        foreach (SleekVendor buyingButton in buyingButtons)
        {
            buyingButton.updateAmount();
        }
        foreach (SleekVendor sellingButton in sellingButtons)
        {
            sellingButton.updateAmount();
        }
    }

    private static void RefreshButtonVisibility()
    {
        Player player = Player.player;
        float num = 0f;
        for (int i = 0; i < buying.Count; i++)
        {
            bool flag = buying[i].areConditionsMet(player);
            buyingButtons[i].IsVisible = flag;
            if (flag)
            {
                buyingButtons[i].PositionOffset_Y = num;
                num += buyingButtons[i].SizeOffset_Y;
            }
        }
        buyingBox.IsVisible = num > 0f;
        buyingBox.ContentSizeOffset = new Vector2(0f, num);
        float num2 = 0f;
        for (int j = 0; j < selling.Count; j++)
        {
            bool flag2 = selling[j].areConditionsMet(player);
            sellingButtons[j].IsVisible = flag2;
            if (flag2)
            {
                sellingButtons[j].PositionOffset_Y = num2;
                num2 += sellingButtons[j].SizeOffset_Y;
            }
        }
        sellingBox.IsVisible = num2 > 0f;
        sellingBox.ContentSizeOffset = new Vector2(0f, num2);
    }

    private static void RefreshExperienceOrCurrencyBoxAmount()
    {
        if (experienceBox.IsVisible)
        {
            experienceBox.Text = localization.format("Experience", Player.player.skills.experience.ToString());
        }
        else
        {
            if (!currencyBox.IsVisible)
            {
                return;
            }
            ItemCurrencyAsset itemCurrencyAsset = vendor.currency.Find();
            if (itemCurrencyAsset != null)
            {
                uint inventoryValue = itemCurrencyAsset.getInventoryValue(Player.player);
                if (string.IsNullOrEmpty(itemCurrencyAsset.valueFormat))
                {
                    currencyLabel.Text = inventoryValue.ToString("N");
                }
                else
                {
                    currencyLabel.Text = string.Format(itemCurrencyAsset.valueFormat, inventoryValue);
                }
            }
        }
    }

    /// <summary>
    /// Update currency or experience depending what the vendor accepts.
    /// </summary>
    private static void updateCurrencyOrExperienceBox()
    {
        currencyBox.IsVisible = vendor.currency.isValid;
        experienceBox.IsVisible = !currencyBox.IsVisible;
        if (!currencyBox.IsVisible)
        {
            return;
        }
        currencyPanel.RemoveAllChildren();
        ItemCurrencyAsset itemCurrencyAsset = vendor.currency.Find();
        if (itemCurrencyAsset == null)
        {
            Assets.ReportError(vendor, "unable to find currency");
            currencyLabel.Text = "Invalid";
            return;
        }
        float num = 5f;
        ItemCurrencyAsset.Entry[] entries = itemCurrencyAsset.entries;
        for (int i = 0; i < entries.Length; i++)
        {
            ItemCurrencyAsset.Entry entry = entries[i];
            AssetReference<ItemAsset> item = entry.item;
            ItemAsset itemAsset = item.Find();
            if (itemAsset == null)
            {
                Assets.ReportError(vendor, "unable to find entry item {0}", entry.item);
            }
            else if (entry.isVisibleInVendorMenu)
            {
                SleekItemIcon sleekItemIcon = new SleekItemIcon();
                sleekItemIcon.PositionOffset_X = num;
                sleekItemIcon.PositionOffset_Y = 5f;
                float num2 = (float)(int)itemAsset.size_x / (float)(int)itemAsset.size_y;
                sleekItemIcon.SizeOffset_X = Mathf.RoundToInt(num2 * 40f);
                sleekItemIcon.SizeOffset_Y = 40f;
                currencyPanel.AddChild(sleekItemIcon);
                sleekItemIcon.Refresh(itemAsset.id, 100, itemAsset.getState(isFull: false), itemAsset, Mathf.RoundToInt(sleekItemIcon.SizeOffset_X), Mathf.RoundToInt(sleekItemIcon.SizeOffset_Y));
                ISleekLabel sleekLabel = Glazier.Get().CreateLabel();
                sleekLabel.PositionOffset_X = sleekItemIcon.PositionOffset_X;
                sleekLabel.PositionOffset_Y = 0f;
                sleekLabel.SizeOffset_X = sleekItemIcon.SizeOffset_X;
                sleekLabel.SizeScale_Y = 1f;
                uint value = entry.value;
                sleekLabel.Text = value.ToString();
                sleekLabel.TextAlignment = TextAnchor.LowerCenter;
                sleekLabel.TextContrastContext = ETextContrastContext.InconspicuousBackdrop;
                currencyPanel.AddChild(sleekLabel);
                num += sleekItemIcon.SizeOffset_X + 2f;
            }
        }
    }

    private static void updateVendor(VendorAsset newVendor, DialogueAsset newDialogue, DialogueMessage newNextMessage, bool newHasNextDialogue)
    {
        vendor = newVendor;
        dialogue = newDialogue;
        nextMessage = newNextMessage;
        hasNextDialogue = newHasNextDialogue;
        if (vendor == null)
        {
            return;
        }
        if (PlayerLifeUI.npc != null)
        {
            PlayerLifeUI.npc.SetFaceOverride(vendor.faceOverride);
        }
        nameLabel.Text = vendor.vendorName;
        descriptionLabel.Text = vendor.vendorDescription;
        buyingButtons.Clear();
        sellingButtons.Clear();
        buying.Clear();
        buying.AddRange(vendor.buying);
        if (vendor.enableSorting)
        {
            buying.Sort(buyingComparator);
        }
        buyingBox.RemoveAllChildren();
        foreach (VendorBuying item in buying)
        {
            SleekVendor sleekVendor = new SleekVendor(item);
            sleekVendor.SizeScale_X = 1f;
            sleekVendor.onClickedButton += onClickedBuyingButton;
            buyingBox.AddChild(sleekVendor);
            buyingButtons.Add(sleekVendor);
        }
        selling.Clear();
        selling.AddRange(vendor.selling);
        if (vendor.enableSorting)
        {
            selling.Sort(sellingComparator);
        }
        sellingBox.RemoveAllChildren();
        foreach (VendorSellingBase item2 in selling)
        {
            SleekVendor sleekVendor2 = new SleekVendor(item2);
            sleekVendor2.SizeScale_X = 1f;
            sleekVendor2.onClickedButton += onClickedSellingButton;
            sellingBox.AddChild(sleekVendor2);
            sellingButtons.Add(sleekVendor2);
        }
        needsRefresh = false;
        updateCurrencyOrExperienceBox();
        RefreshExperienceOrCurrencyBoxAmount();
        RefreshButtonVisibility();
    }

    private static void onInventoryStateUpdated()
    {
        needsRefresh = true;
    }

    private static void onExperienceUpdated(uint newExperience)
    {
        needsRefresh = true;
    }

    private static void onReputationUpdated(int newReputation)
    {
        needsRefresh = true;
    }

    private static void onFlagsUpdated()
    {
        needsRefresh = true;
    }

    private static void onFlagUpdated(ushort id)
    {
        needsRefresh = true;
    }

    private static void onClickedBuyingButton(ISleekElement button)
    {
        byte index = (byte)buyingBox.FindIndexOfChild(button);
        VendorBuying vendorBuying = buying[index];
        if (vendorBuying.canSell(Player.player))
        {
            Player.player.quests.sendSellToVendor(vendor.GUID, vendorBuying.index, InputEx.GetKey(ControlsSettings.other));
        }
    }

    private static void onClickedSellingButton(ISleekElement button)
    {
        byte index = (byte)sellingBox.FindIndexOfChild(button);
        VendorSellingBase vendorSellingBase = selling[index];
        if (vendorSellingBase.canBuy(Player.player))
        {
            Player.player.quests.sendBuyFromVendor(vendor.GUID, vendorSellingBase.index, InputEx.GetKey(ControlsSettings.other));
        }
    }

    private static void onClickedReturnButton(ISleekElement button)
    {
        closeNicely();
    }

    public PlayerNPCVendorUI()
    {
        localization = Localization.read("/Player/PlayerNPCVendor.dat");
        container = new SleekFullscreenBox();
        container.PositionScale_Y = 1f;
        container.PositionOffset_X = 10f;
        container.PositionOffset_Y = 10f;
        container.SizeOffset_X = -20f;
        container.SizeOffset_Y = -20f;
        container.SizeScale_X = 1f;
        container.SizeScale_Y = 1f;
        PlayerUI.container.AddChild(container);
        active = false;
        buying = new List<VendorBuying>();
        selling = new List<VendorSellingBase>();
        buyingButtons = new List<SleekVendor>();
        sellingButtons = new List<SleekVendor>();
        vendorBox = Glazier.Get().CreateBox();
        vendorBox.SizeOffset_Y = -60f;
        vendorBox.SizeScale_X = 1f;
        vendorBox.SizeScale_Y = 1f;
        container.AddChild(vendorBox);
        nameLabel = Glazier.Get().CreateLabel();
        nameLabel.PositionOffset_X = 5f;
        nameLabel.PositionOffset_Y = 5f;
        nameLabel.SizeOffset_X = -10f;
        nameLabel.SizeOffset_Y = 40f;
        nameLabel.SizeScale_X = 1f;
        nameLabel.TextColor = ESleekTint.RICH_TEXT_DEFAULT;
        nameLabel.TextContrastContext = ETextContrastContext.InconspicuousBackdrop;
        nameLabel.AllowRichText = true;
        nameLabel.FontSize = ESleekFontSize.Large;
        vendorBox.AddChild(nameLabel);
        descriptionLabel = Glazier.Get().CreateLabel();
        descriptionLabel.PositionOffset_X = 5f;
        descriptionLabel.PositionOffset_Y = 40f;
        descriptionLabel.SizeOffset_X = -10f;
        descriptionLabel.SizeOffset_Y = 40f;
        descriptionLabel.SizeScale_X = 1f;
        descriptionLabel.TextColor = ESleekTint.RICH_TEXT_DEFAULT;
        descriptionLabel.TextContrastContext = ETextContrastContext.InconspicuousBackdrop;
        descriptionLabel.AllowRichText = true;
        vendorBox.AddChild(descriptionLabel);
        buyingLabel = Glazier.Get().CreateLabel();
        buyingLabel.PositionOffset_X = 5f;
        buyingLabel.PositionOffset_Y = 80f;
        buyingLabel.SizeOffset_X = -40f;
        buyingLabel.SizeOffset_Y = 30f;
        buyingLabel.SizeScale_X = 0.5f;
        buyingLabel.FontSize = ESleekFontSize.Medium;
        buyingLabel.Text = localization.format("Buying");
        vendorBox.AddChild(buyingLabel);
        buyingBox = Glazier.Get().CreateScrollView();
        buyingBox.PositionOffset_X = 5f;
        buyingBox.PositionOffset_Y = 115f;
        buyingBox.SizeOffset_X = -10f;
        buyingBox.SizeOffset_Y = -120f;
        buyingBox.SizeScale_X = 0.5f;
        buyingBox.SizeScale_Y = 1f;
        buyingBox.ScaleContentToWidth = true;
        buyingBox.ContentSizeOffset = new Vector2(0f, 1024f);
        vendorBox.AddChild(buyingBox);
        sellingLabel = Glazier.Get().CreateLabel();
        sellingLabel.PositionOffset_X = 5f;
        sellingLabel.PositionOffset_Y = 80f;
        sellingLabel.PositionScale_X = 0.5f;
        sellingLabel.SizeOffset_X = -40f;
        sellingLabel.SizeOffset_Y = 30f;
        sellingLabel.SizeScale_X = 0.5f;
        sellingLabel.FontSize = ESleekFontSize.Medium;
        sellingLabel.Text = localization.format("Selling");
        vendorBox.AddChild(sellingLabel);
        sellingBox = Glazier.Get().CreateScrollView();
        sellingBox.PositionOffset_X = 5f;
        sellingBox.PositionOffset_Y = 115f;
        sellingBox.PositionScale_X = 0.5f;
        sellingBox.SizeOffset_X = -10f;
        sellingBox.SizeOffset_Y = -120f;
        sellingBox.SizeScale_X = 0.5f;
        sellingBox.SizeScale_Y = 1f;
        sellingBox.ScaleContentToWidth = true;
        sellingBox.ContentSizeOffset = new Vector2(0f, 1024f);
        vendorBox.AddChild(sellingBox);
        experienceBox = Glazier.Get().CreateBox();
        experienceBox.PositionOffset_Y = 10f;
        experienceBox.PositionScale_Y = 1f;
        experienceBox.SizeOffset_X = -5f;
        experienceBox.SizeOffset_Y = 50f;
        experienceBox.SizeScale_X = 0.5f;
        experienceBox.FontSize = ESleekFontSize.Medium;
        experienceBox.IsVisible = false;
        vendorBox.AddChild(experienceBox);
        currencyBox = Glazier.Get().CreateBox();
        currencyBox.PositionOffset_Y = 10f;
        currencyBox.PositionScale_Y = 1f;
        currencyBox.SizeOffset_X = -5f;
        currencyBox.SizeOffset_Y = 50f;
        currencyBox.SizeScale_X = 0.5f;
        currencyBox.IsVisible = false;
        vendorBox.AddChild(currencyBox);
        currencyPanel = Glazier.Get().CreateFrame();
        currencyPanel.SizeScale_X = 1f;
        currencyPanel.SizeScale_Y = 1f;
        currencyBox.AddChild(currencyPanel);
        currencyLabel = Glazier.Get().CreateLabel();
        currencyLabel.PositionOffset_X = -160f;
        currencyLabel.PositionScale_X = 1f;
        currencyLabel.SizeOffset_X = 150f;
        currencyLabel.SizeScale_Y = 1f;
        currencyLabel.TextAlignment = TextAnchor.MiddleRight;
        currencyLabel.FontSize = ESleekFontSize.Medium;
        currencyBox.AddChild(currencyLabel);
        returnButton = Glazier.Get().CreateButton();
        returnButton.PositionOffset_X = 5f;
        returnButton.PositionOffset_Y = 10f;
        returnButton.PositionScale_X = 0.5f;
        returnButton.PositionScale_Y = 1f;
        returnButton.SizeOffset_X = -5f;
        returnButton.SizeOffset_Y = 50f;
        returnButton.SizeScale_X = 0.5f;
        returnButton.FontSize = ESleekFontSize.Medium;
        returnButton.Text = localization.format("Return");
        returnButton.TooltipText = localization.format("Return_Tooltip");
        returnButton.OnClicked += onClickedReturnButton;
        vendorBox.AddChild(returnButton);
        PlayerInventory inventory = Player.player.inventory;
        inventory.onInventoryStateUpdated = (InventoryStateUpdated)Delegate.Combine(inventory.onInventoryStateUpdated, new InventoryStateUpdated(onInventoryStateUpdated));
        PlayerSkills skills = Player.player.skills;
        skills.onExperienceUpdated = (ExperienceUpdated)Delegate.Combine(skills.onExperienceUpdated, new ExperienceUpdated(onExperienceUpdated));
        PlayerSkills skills2 = Player.player.skills;
        skills2.onReputationUpdated = (ReputationUpdated)Delegate.Combine(skills2.onReputationUpdated, new ReputationUpdated(onReputationUpdated));
        PlayerQuests quests = Player.player.quests;
        quests.onFlagsUpdated = (FlagsUpdated)Delegate.Combine(quests.onFlagsUpdated, new FlagsUpdated(onFlagsUpdated));
        PlayerQuests quests2 = Player.player.quests;
        quests2.onFlagUpdated = (FlagUpdated)Delegate.Combine(quests2.onFlagUpdated, new FlagUpdated(onFlagUpdated));
        needsRefresh = true;
    }
}
