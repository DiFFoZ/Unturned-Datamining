using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace SDG.Unturned;

public class PlayerDashboardInventoryUI
{
    private static List<InteractableItem> pendingItemsInRadius;

    private static SleekFullscreenBox container;

    public static Local localization;

    public static Bundle icons;

    public static bool active;

    private static ISleekBox backdropBox;

    /// <summary>
    /// Added during the UI refactor to catch unhandled mouse clicks during drag.
    /// </summary>
    private static ISleekImage dragOutsideHandler;

    private static ISleekImage dragOutsideClothingHandler;

    private static ISleekImage dragOutsideAreaHandler;

    private static bool hasDragOutsideHandlers;

    private static ItemJar dragJar;

    private static SleekItem dragSource;

    private static SleekItem dragItem;

    private static Vector2 dragOffset;

    private static Vector2 dragPivot;

    private static byte dragFromPage;

    private static byte dragFrom_x;

    private static byte dragFrom_y;

    private static byte dragFromRot;

    private static SleekCameraImage characterImage;

    private static ISleekSlider characterSlider;

    private static SleekButtonIcon swapCosmeticsButton;

    private static SleekButtonIcon swapSkinsButton;

    private static SleekButtonIcon swapMythicsButton;

    private static SleekPlayer characterPlayer;

    private static SleekSlot[] slots;

    private static ISleekElement box;

    private static ISleekScrollView clothingBox;

    private static ISleekScrollView areaBox;

    private static ISleekButton[] headers;

    private static SleekItemIcon[] headerItemIcons;

    private static SleekItems[] items;

    /// <summary>
    /// Contains inspect item box and invisible button.
    /// </summary>
    private static ISleekElement selectionFrame;

    /// <summary>
    /// Added during the UI refactor to catch mouse clicks outside the selection box.
    /// </summary>
    private static ISleekImage outsideSelectionInvisibleButton;

    private static ISleekBox selectionBackdropBox;

    private static ISleekBox selectionIconBox;

    private static SleekItemIcon selectionIconImage;

    private static ISleekScrollView selectionDescriptionScrollView;

    private static ISleekBox selectionDescriptionBox;

    private static ISleekLabel selectionDescriptionLabel;

    private static ISleekLabel selectionNameLabel;

    private static ISleekLabel selectionHotkeyLabel;

    private static ISleekBox vehicleBox;

    private static ISleekLabel vehicleNameLabel;

    private static ISleekElement vehicleActionsBox;

    private static ISleekElement vehiclePassengersBox;

    private static ISleekButton vehicleLockButton;

    private static ISleekButton vehicleHornButton;

    private static ISleekButton vehicleHeadlightsButton;

    private static ISleekButton vehicleSirensButton;

    private static ISleekButton vehicleBlimpButton;

    private static ISleekButton vehicleHookButton;

    private static ISleekButton vehicleStealBatteryButton;

    private static ISleekButton vehicleSkinButton;

    private static ISleekScrollView selectionActionsBox;

    private static ISleekButton selectionEquipButton;

    private static ISleekButton selectionContextButton;

    private static ISleekButton selectionDropButton;

    private static ISleekButton selectionStorageButton;

    private static ISleekElement selectionExtraActionsBox;

    private static ISleekButton rot_xButton;

    private static ISleekButton rot_yButton;

    private static ISleekButton rot_zButton;

    private static byte _selectedPage;

    private static byte _selected_x;

    private static byte _selected_y;

    private static ItemJar _selectedJar;

    private static ItemAsset _selectedAsset;

    private static Items areaItems;

    private static List<Action> actions;

    private static OncePerFrameGuard eventGuard;

    public static bool isDragging { get; private set; }

    public static byte selectedPage => _selectedPage;

    public static byte selected_x => _selected_x;

    public static byte selected_y => _selected_y;

    public static ItemJar selectedJar => _selectedJar;

    public static ItemAsset selectedAsset => _selectedAsset;

    private static bool isSplitClothingArea => Screen.width >= 1350;

    /// <summary>
    /// Was ConsumeEvent called during this frame?
    /// This is a hack to prevent firing when clicking in the UI on the same frame it closes.
    /// Moved from SleekWindow and Event.current.Use() during UI refactor.
    /// </summary>
    public static bool WasEventConsumed => eventGuard.HasBeenConsumed;

    public static void open()
    {
        if (!active)
        {
            active = true;
            Player.player.animator.sendGesture(EPlayerGesture.INVENTORY_START, all: false);
            Player.player.character.Find("Camera").gameObject.SetActive(value: true);
            if (isSplitClothingArea)
            {
                clothingBox.SizeOffset_X = -5f;
                clothingBox.SizeScale_X = 0.5f;
                areaBox.IsVisible = true;
            }
            else
            {
                clothingBox.SizeOffset_X = 0f;
                clothingBox.SizeScale_X = 1f;
                areaBox.IsVisible = false;
            }
            updateVehicle();
            resetNearbyDrops();
            updateHotkeys();
            if (characterPlayer != null)
            {
                backdropBox.RemoveChild(characterPlayer);
                characterPlayer = null;
            }
            if (Player.player != null)
            {
                characterPlayer = new SleekPlayer(Player.player.channel.owner, isButton: true, SleekPlayer.ESleekPlayerDisplayContext.NONE);
                characterPlayer.PositionOffset_X = 10f;
                characterPlayer.PositionOffset_Y = 10f;
                characterPlayer.SizeOffset_X = 410f;
                characterPlayer.SizeOffset_Y = 50f;
                backdropBox.AddChild(characterPlayer);
            }
            container.AnimateIntoView();
        }
    }

    public static void close()
    {
        if (active)
        {
            active = false;
            Player.player.animator.sendGesture(EPlayerGesture.INVENTORY_STOP, all: false);
            Player.player.character.Find("Camera").gameObject.SetActive(value: false);
            stopDrag();
            closeSelection();
            container.AnimateOutOfView(0f, 1f);
        }
    }

    private static void startDrag()
    {
        if (!isDragging)
        {
            isDragging = true;
            setItemsEnabled(enabled: false);
            dragItem.IsVisible = true;
            if (hasDragOutsideHandlers)
            {
                dragOutsideHandler.IsVisible = true;
                dragOutsideClothingHandler.IsVisible = true;
                dragOutsideAreaHandler.IsVisible = true;
            }
            PlayInventoryAudio(dragJar.GetAsset());
        }
    }

    public static void stopDrag()
    {
        if (isDragging)
        {
            isDragging = false;
            PlayInventoryAudio(dragJar.GetAsset());
            dragJar.rot = dragFromRot;
            setItemsEnabled(enabled: true);
            dragItem.IsVisible = false;
            if (hasDragOutsideHandlers)
            {
                dragOutsideHandler.IsVisible = false;
                dragOutsideClothingHandler.IsVisible = false;
                dragOutsideAreaHandler.IsVisible = false;
            }
        }
    }

    private static void setItemsEnabled(bool enabled)
    {
        SleekSlot[] array = slots;
        for (int i = 0; i < array.Length; i++)
        {
            array[i].isItemEnabled = enabled;
        }
        SleekItems[] array2 = items;
        for (int i = 0; i < array2.Length; i++)
        {
            array2[i].areItemsEnabled = enabled;
        }
    }

    /// <summary>
    /// Annoying frustrating workaround for IMGUI. Disable inventory headers, grids and slots while selection is open
    /// to prevent them from interfering with selection menu.
    /// </summary>
    private static void setMiscButtonsEnabled(bool enabled)
    {
        ISleekButton[] array = headers;
        for (int i = 0; i < array.Length; i++)
        {
            array[i].IsRaycastTarget = enabled;
        }
        SleekSlot[] array2 = slots;
        for (int i = 0; i < array2.Length; i++)
        {
            array2[i].isImageRaycastTarget = enabled;
        }
        SleekItems[] array3 = items;
        for (int i = 0; i < array3.Length; i++)
        {
            array3[i].isGridRaycastTarget = enabled;
        }
    }

    private static void onDraggedCharacterSlider(ISleekSlider slider, float state)
    {
        PlayerLook.characterYaw = state * 360f;
    }

    private static void onClickedSwapCosmeticsButton(ISleekElement button)
    {
        Player.player.clothing.sendVisualToggle(EVisualToggleType.COSMETIC);
    }

    private static void onClickedSwapSkinsButton(ISleekElement button)
    {
        Player.player.clothing.sendVisualToggle(EVisualToggleType.SKIN);
    }

    private static void onClickedSwapMythicsButton(ISleekElement button)
    {
        Player.player.clothing.sendVisualToggle(EVisualToggleType.MYTHIC);
    }

    private static void onClickedVehicleLockButton(ISleekElement button)
    {
        VehicleManager.sendVehicleLock();
    }

    private static void onClickedVehicleHornButton(ISleekElement button)
    {
        VehicleManager.sendVehicleHorn();
    }

    private static void onClickedVehicleHeadlightsButton(ISleekElement button)
    {
        VehicleManager.sendVehicleHeadlights();
    }

    private static void onClickedVehicleSirensButton(ISleekElement button)
    {
        VehicleManager.sendVehicleBonus();
    }

    private static void onClickedVehicleBlimpButton(ISleekElement button)
    {
        VehicleManager.sendVehicleBonus();
    }

    private static void onClickedVehicleHookButton(ISleekElement button)
    {
        VehicleManager.sendVehicleBonus();
    }

    private static void onClickedVehicleStealBatteryButton(ISleekElement button)
    {
        VehicleManager.sendVehicleStealBattery();
    }

    private static void onClickedVehicleSkinButton(ISleekElement button)
    {
        VehicleManager.sendVehicleSkin();
    }

    private static void onClickedVehiclePassengerButton(ISleekElement button)
    {
        int num = vehiclePassengersBox.FindIndexOfChild(button);
        if (num >= 0)
        {
            VehicleManager.swapVehicle((byte)num);
        }
    }

    private static void ConsumeEvent()
    {
        eventGuard.Consume();
    }

    private static void onClickedEquip(ISleekElement button)
    {
        if (selectedPage != byte.MaxValue)
        {
            checkEquip(selectedPage, selected_x, selected_y, Player.player.inventory.getItem(selectedPage, Player.player.inventory.getIndex(selectedPage, selected_x, selected_y)), byte.MaxValue);
            ConsumeEvent();
        }
    }

    private static void onClickedContext(ISleekElement button)
    {
        if (selectedPage != byte.MaxValue)
        {
            if (selectedAsset.type == EItemType.GUN)
            {
                Player.player.crafting.sendStripAttachments(selectedPage, selected_x, selected_y);
            }
            ConsumeEvent();
            closeSelection();
        }
    }

    private static void onClickedDrop(ISleekElement button)
    {
        if (selectedPage == byte.MaxValue)
        {
            return;
        }
        if (selectedPage == PlayerInventory.AREA)
        {
            if (selectedJar.interactableItem != null)
            {
                ItemManager.takeItem(selectedJar.interactableItem.transform.parent, byte.MaxValue, byte.MaxValue, 0, byte.MaxValue);
            }
            closeSelection();
        }
        else
        {
            Player.player.inventory.sendDropItem(selectedPage, selected_x, selected_y);
        }
        ConsumeEvent();
    }

    private static void onClickedStore(ISleekElement button)
    {
        if (selectedPage == byte.MaxValue)
        {
            return;
        }
        byte x2;
        byte y2;
        byte rot2;
        if (selectedPage == PlayerInventory.AREA)
        {
            if (selectedJar.interactableItem != null)
            {
                ItemManager.takeItem(selectedJar.interactableItem.transform.parent, byte.MaxValue, byte.MaxValue, 0, PlayerInventory.STORAGE);
            }
            closeSelection();
        }
        else if (selectedPage == PlayerInventory.STORAGE)
        {
            if (Player.player.inventory.tryFindSpace(selectedJar.size_x, selectedJar.size_y, out var page, out var x, out var y, out var rot))
            {
                Player.player.inventory.sendDragItem(selectedPage, selected_x, selected_y, page, x, y, rot);
            }
        }
        else if (Player.player.inventory.tryFindSpace(PlayerInventory.STORAGE, selectedJar.size_x, selectedJar.size_y, out x2, out y2, out rot2))
        {
            Player.player.inventory.sendDragItem(selectedPage, selected_x, selected_y, PlayerInventory.STORAGE, x2, y2, rot2);
        }
        ConsumeEvent();
    }

    private static void onClickedAction(ISleekElement button)
    {
        int index = selectionExtraActionsBox.FindIndexOfChild(button);
        Action action = actions[index];
        if (!(Assets.find(EAssetType.ITEM, action.source) is ItemAsset itemAsset))
        {
            return;
        }
        Blueprint[] array = new Blueprint[action.blueprints.Length];
        bool flag = false;
        for (byte b = 0; b < array.Length; b++)
        {
            array[b] = itemAsset.blueprints[action.blueprints[b].id];
            if (action.blueprints[b].isLink)
            {
                flag = true;
            }
        }
        flag &= !InputEx.GetKey(ControlsSettings.other);
        PlayerDashboardCraftingUI.filteredBlueprintsOverride = array;
        if (!flag)
        {
            PlayerDashboardCraftingUI.updateSelection();
            foreach (Blueprint blueprint in array)
            {
                if (!blueprint.hasSupplies)
                {
                    flag = true;
                    break;
                }
                if (!blueprint.hasTool)
                {
                    flag = true;
                    break;
                }
                if (!blueprint.hasItem)
                {
                    flag = true;
                    break;
                }
                if (!blueprint.hasSkills)
                {
                    flag = true;
                    break;
                }
                if (Player.player.equipment.isBusy)
                {
                    flag = true;
                    break;
                }
            }
        }
        if (flag)
        {
            close();
            PlayerDashboardCraftingUI.open();
            return;
        }
        foreach (Blueprint blueprint2 in array)
        {
            Player.player.crafting.sendCraft(blueprint2.sourceItem.id, blueprint2.id, force: false);
        }
        PlayerDashboardCraftingUI.filteredBlueprintsOverride = null;
        closeSelection();
    }

    private static void onClickedRot_XButton(ISleekElement button)
    {
        InteractableStorage interactableStorage = PlayerInteract.interactable as InteractableStorage;
        if (!(interactableStorage == null) && interactableStorage.isDisplay)
        {
            byte rot_x = interactableStorage.rot_x;
            rot_x++;
            if (rot_x > 3)
            {
                rot_x = 0;
            }
            byte rotation = interactableStorage.getRotation(rot_x, interactableStorage.rot_y, interactableStorage.rot_z);
            interactableStorage.ClientSetDisplayRotation(rotation);
        }
    }

    private static void onClickedRot_YButton(ISleekElement button)
    {
        InteractableStorage interactableStorage = PlayerInteract.interactable as InteractableStorage;
        if (!(interactableStorage == null) && interactableStorage.isDisplay)
        {
            byte rot_y = interactableStorage.rot_y;
            rot_y++;
            if (rot_y > 3)
            {
                rot_y = 0;
            }
            byte rotation = interactableStorage.getRotation(interactableStorage.rot_x, rot_y, interactableStorage.rot_z);
            interactableStorage.ClientSetDisplayRotation(rotation);
        }
    }

    private static void onClickedRot_ZButton(ISleekElement button)
    {
        InteractableStorage interactableStorage = PlayerInteract.interactable as InteractableStorage;
        if (!(interactableStorage == null) && interactableStorage.isDisplay)
        {
            byte b = (byte)(interactableStorage.rot_z++ + 1);
            if (b > 3)
            {
                b = 0;
            }
            byte rotation = interactableStorage.getRotation(interactableStorage.rot_x, interactableStorage.rot_y, b);
            interactableStorage.ClientSetDisplayRotation(rotation);
        }
    }

    private static void openSelection(byte page, byte x, byte y)
    {
        _selectedPage = page;
        _selected_x = x;
        _selected_y = y;
        if (!Glazier.Get().SupportsDepth)
        {
            setItemsEnabled(enabled: false);
            setMiscButtonsEnabled(enabled: false);
        }
        selectionFrame.IsVisible = true;
        _selectedJar = Player.player.inventory.getItem(page, Player.player.inventory.getIndex(page, x, y));
        if (selectedJar == null)
        {
            return;
        }
        _selectedAsset = selectedJar.GetAsset();
        selectionIconImage.Clear();
        if (selectedAsset == null)
        {
            return;
        }
        int num;
        int num2;
        if (selectedAsset.size_x <= selectedAsset.size_y)
        {
            selectionBackdropBox.SizeOffset_X = 490f;
            selectionBackdropBox.SizeOffset_Y = 330f;
            selectionIconBox.SizeOffset_X = 210f;
            selectionIconBox.SizeOffset_Y = 310f;
            if (selectionDescriptionScrollView != null)
            {
                selectionDescriptionScrollView.PositionOffset_X = 230f;
                selectionDescriptionScrollView.PositionOffset_Y = 10f;
                selectionDescriptionScrollView.SizeOffset_X = 250f;
                selectionDescriptionScrollView.SizeOffset_Y = 150f;
            }
            else
            {
                selectionDescriptionBox.PositionOffset_X = 230f;
                selectionDescriptionBox.PositionOffset_Y = 10f;
                selectionDescriptionBox.SizeOffset_X = 250f;
                selectionDescriptionBox.SizeOffset_Y = 150f;
            }
            selectionActionsBox.PositionOffset_X = 230f;
            selectionActionsBox.PositionOffset_Y = 170f;
            selectionActionsBox.SizeOffset_Y = 150f;
            if (selectedAsset.size_x == selectedAsset.size_y)
            {
                num = 200;
                num2 = 200;
            }
            else
            {
                num = 200;
                num2 = 300;
            }
        }
        else
        {
            selectionBackdropBox.SizeOffset_X = 530f;
            selectionBackdropBox.SizeOffset_Y = 390f;
            selectionIconBox.SizeOffset_X = 510f;
            selectionIconBox.SizeOffset_Y = 210f;
            if (selectionDescriptionScrollView != null)
            {
                selectionDescriptionScrollView.PositionOffset_X = 10f;
                selectionDescriptionScrollView.PositionOffset_Y = 230f;
                selectionDescriptionScrollView.SizeOffset_X = 250f;
                selectionDescriptionScrollView.SizeOffset_Y = 150f;
            }
            else
            {
                selectionDescriptionBox.PositionOffset_X = 10f;
                selectionDescriptionBox.PositionOffset_Y = 230f;
                selectionDescriptionBox.SizeOffset_X = 250f;
                selectionDescriptionBox.SizeOffset_Y = 150f;
            }
            selectionActionsBox.PositionOffset_X = 270f;
            selectionActionsBox.PositionOffset_Y = 230f;
            selectionActionsBox.SizeOffset_Y = 150f;
            num = 500;
            num2 = 200;
        }
        selectionIconImage.PositionOffset_X = -num / 2;
        selectionIconImage.PositionOffset_Y = -num2 / 2;
        selectionIconImage.SizeOffset_X = num;
        selectionIconImage.SizeOffset_Y = num2;
        selectionIconImage.Refresh(selectedJar.item.id, selectedJar.item.quality, selectedJar.item.state, selectedAsset, num, num2);
        Vector2 vector = Input.mousePosition;
        vector.y = (float)Screen.height - vector.y;
        vector /= GraphicsSettings.userInterfaceScale;
        selectionBackdropBox.PositionOffset_X = (int)Mathf.Clamp(vector.x - selectionBackdropBox.SizeOffset_X / 2f, 0f, (float)Screen.width / GraphicsSettings.userInterfaceScale - selectionBackdropBox.SizeOffset_X);
        selectionBackdropBox.PositionOffset_Y = (int)Mathf.Clamp(vector.y - selectionBackdropBox.SizeOffset_Y / 2f, 0f, (float)Screen.height / GraphicsSettings.userInterfaceScale - selectionBackdropBox.SizeOffset_Y);
        StringBuilder stringBuilder = new StringBuilder(512);
        ItemDescriptionBuilder builder = default(ItemDescriptionBuilder);
        builder.stringBuilder = stringBuilder;
        builder.shouldRestrictToLegacyContent = !Glazier.Get().SupportsAutomaticLayout || !selectedAsset.isEligibleForAutoStatDescriptions;
        builder.lines = new List<ItemDescriptionLine>();
        selectedAsset.BuildDescription(builder, selectedJar.item);
        builder.lines.Sort();
        int num3 = 0;
        stringBuilder.Clear();
        foreach (ItemDescriptionLine line in builder.lines)
        {
            if (line.sortOrder - num3 > 100)
            {
                stringBuilder.AppendLine();
            }
            stringBuilder.AppendLine(line.text);
            num3 = line.sortOrder;
        }
        selectionDescriptionLabel.Text = stringBuilder.ToString();
        if (selectionDescriptionScrollView != null)
        {
            selectionDescriptionScrollView.ScrollToTop();
        }
        selectionNameLabel.Text = selectedAsset.itemName;
        if (selectedPage < PlayerInventory.SLOTS)
        {
            selectionHotkeyLabel.Text = localization.format("Hotkey_Set", ControlsSettings.getEquipmentHotkeyText(selectedPage));
            selectionHotkeyLabel.IsVisible = true;
        }
        else if (selectedPage < PlayerInventory.STORAGE && ItemTool.checkUseable(selectedPage, selectedJar.item.id))
        {
            selectionHotkeyLabel.Text = localization.format("Hotkey_Unset");
            selectionHotkeyLabel.IsVisible = true;
            for (byte b = 0; b < Player.player.equipment.hotkeys.Length; b++)
            {
                HotkeyInfo hotkeyInfo = Player.player.equipment.hotkeys[b];
                if (hotkeyInfo.page == selectedPage && hotkeyInfo.x == selected_x && hotkeyInfo.y == selected_y)
                {
                    selectionHotkeyLabel.Text = localization.format("Hotkey_Set", ControlsSettings.getEquipmentHotkeyText(b + 2));
                    break;
                }
            }
        }
        else
        {
            selectionHotkeyLabel.IsVisible = false;
        }
        if (Player.player.equipment.checkSelection(page, x, y))
        {
            selectionEquipButton.Text = localization.format("Dequip_Button");
            selectionEquipButton.TooltipText = localization.format("Dequip_Button_Tooltip");
        }
        else
        {
            selectionEquipButton.Text = localization.format("Equip_Button");
            selectionEquipButton.TooltipText = localization.format("Equip_Button_Tooltip");
        }
        if (selectedAsset.type == EItemType.GUN)
        {
            selectionContextButton.Text = localization.format("Attachments_Button");
            selectionContextButton.TooltipText = localization.format("Attachments_Button_Tooltip");
            selectionContextButton.IsVisible = selectedPage >= PlayerInventory.SLOTS && selectedPage < PlayerInventory.AREA;
        }
        else
        {
            selectionContextButton.IsVisible = false;
        }
        bool flag = page == PlayerInventory.AREA;
        if (flag)
        {
            selectionDropButton.Text = localization.format("Pickup_Button");
            selectionDropButton.TooltipText = localization.format("Pickup_Button_Tooltip");
        }
        else
        {
            selectionDropButton.Text = localization.format("Drop_Button");
            selectionDropButton.TooltipText = localization.format("Drop_Button_Tooltip");
        }
        if (page == PlayerInventory.STORAGE)
        {
            selectionStorageButton.Text = localization.format("Take_Button");
            selectionStorageButton.TooltipText = localization.format("Take_Button_Tooltip");
        }
        else
        {
            selectionStorageButton.Text = localization.format("Store_Button");
            selectionStorageButton.TooltipText = localization.format("Store_Button_Tooltip");
        }
        selectionEquipButton.IsVisible = selectedAsset.canPlayerEquip && page < PlayerInventory.PAGES - 2;
        selectionDropButton.IsVisible = flag || selectedAsset.allowManualDrop;
        selectionStorageButton.IsVisible = Player.player.inventory.isStoring;
        int num4 = 0;
        if (selectionEquipButton.IsVisible)
        {
            selectionEquipButton.PositionOffset_Y = num4;
            num4 += 40;
        }
        if (selectionContextButton.IsVisible)
        {
            selectionContextButton.PositionOffset_Y = num4;
            num4 += 40;
        }
        if (selectionDropButton.IsVisible)
        {
            selectionDropButton.PositionOffset_Y = num4;
            num4 += 40;
        }
        if (selectionStorageButton.IsVisible)
        {
            selectionStorageButton.PositionOffset_Y = num4;
            num4 += 40;
        }
        selectionExtraActionsBox.RemoveAllChildren();
        selectionExtraActionsBox.PositionOffset_Y = num4;
        int num5 = 0;
        if (page != PlayerInventory.AREA)
        {
            actions.Clear();
            for (int i = 0; i < selectedAsset.actions.Count; i++)
            {
                Action action = selectedAsset.actions[i];
                if (action.type == EActionType.BLUEPRINT)
                {
                    if (page < PlayerInventory.SLOTS || page >= PlayerInventory.STORAGE)
                    {
                        continue;
                    }
                    Blueprint blueprint = (Assets.find(EAssetType.ITEM, action.source) as ItemAsset).blueprints[action.blueprints[0].id];
                    if ((blueprint.skill == EBlueprintSkill.REPAIR && blueprint.level > Provider.modeConfigData.Gameplay.Repair_Level_Max) || (blueprint.type == EBlueprintType.REPAIR && selectedJar.item.quality == 100) || !blueprint.areConditionsMet(Player.player) || Player.player.crafting.isBlueprintBlacklisted(blueprint))
                    {
                        continue;
                    }
                }
                actions.Add(action);
                ISleekButton sleekButton = Glazier.Get().CreateButton();
                sleekButton.PositionOffset_Y = num5;
                sleekButton.SizeScale_X = 1f;
                sleekButton.SizeOffset_Y = 30f;
                if (!string.IsNullOrEmpty(action.key))
                {
                    sleekButton.Text = localization.format(action.key + "_Button");
                    sleekButton.TooltipText = localization.format(action.key + "_Button_Tooltip");
                }
                else
                {
                    sleekButton.Text = action.text;
                    sleekButton.TooltipText = action.tooltip;
                }
                sleekButton.OnClicked += onClickedAction;
                selectionExtraActionsBox.AddChild(sleekButton);
                num5 += 40;
                num4 += 40;
            }
        }
        selectionExtraActionsBox.SizeOffset_Y = num5 - 10;
        selectionActionsBox.ContentSizeOffset = new Vector2(0f, num4 - 10);
        selectionNameLabel.TextColor = ItemTool.getRarityColorUI(selectedAsset.rarity);
    }

    public static void closeSelection()
    {
        if (selectedPage != byte.MaxValue)
        {
            _selectedPage = byte.MaxValue;
            _selected_x = byte.MaxValue;
            _selected_y = byte.MaxValue;
            if (!Glazier.Get().SupportsDepth)
            {
                setItemsEnabled(enabled: true);
                setMiscButtonsEnabled(enabled: true);
            }
            selectionFrame.IsVisible = false;
        }
    }

    private static void onSelectedItem(byte page, byte x, byte y)
    {
        if (page == byte.MaxValue || (page == selectedPage && x == selected_x && y == selected_y))
        {
            closeSelection();
        }
        else if (InputEx.GetKey(ControlsSettings.other))
        {
            ItemJar item = Player.player.inventory.getItem(page, Player.player.inventory.getIndex(page, x, y));
            if (item == null)
            {
                return;
            }
            if (Player.player.inventory.isStoring)
            {
                byte x3;
                byte y3;
                byte rot2;
                if (page == PlayerInventory.AREA)
                {
                    if (item.interactableItem != null)
                    {
                        ItemManager.takeItem(item.interactableItem.transform.parent, byte.MaxValue, byte.MaxValue, 0, PlayerInventory.STORAGE);
                    }
                }
                else if (page == PlayerInventory.STORAGE)
                {
                    if (Player.player.inventory.tryFindSpace(item.size_x, item.size_y, out var page2, out var x2, out var y2, out var rot))
                    {
                        Player.player.inventory.sendDragItem(page, x, y, page2, x2, y2, rot);
                    }
                }
                else if (Player.player.inventory.tryFindSpace(PlayerInventory.STORAGE, item.size_x, item.size_y, out x3, out y3, out rot2))
                {
                    Player.player.inventory.sendDragItem(page, x, y, PlayerInventory.STORAGE, x3, y3, rot2);
                }
            }
            else
            {
                checkAction(page, x, y, item);
            }
        }
        else
        {
            openSelection(page, x, y);
        }
    }

    private static bool checkSlot(byte page, byte x, byte y, ItemJar jar, byte slot)
    {
        if (Player.player.inventory.checkSpaceEmpty(slot, byte.MaxValue, byte.MaxValue, 0, 0, 0))
        {
            Player.player.inventory.sendDragItem(page, x, y, slot, 0, 0, 0);
            Player.player.equipment.ClientEquipAfterItemDrag(slot, 0, 0);
            PlayerDashboardUI.close();
            PlayerLifeUI.open();
            return true;
        }
        ItemJar item = Player.player.inventory.getItem(slot, 0);
        byte b = item.rot;
        if (!Player.player.inventory.checkSpaceSwap(page, x, y, jar.size_x, jar.size_y, jar.rot, item.size_x, item.size_y, b))
        {
            b = (byte)((b + 1) % 4);
            if (!Player.player.inventory.checkSpaceSwap(page, x, y, jar.size_x, jar.size_y, jar.rot, item.size_x, item.size_y, b))
            {
                return false;
            }
        }
        Player.player.inventory.sendSwapItem(page, x, y, b, slot, 0, 0, jar.rot);
        Player.player.equipment.ClientEquipAfterItemDrag(slot, 0, 0);
        PlayerDashboardUI.close();
        PlayerLifeUI.open();
        return true;
    }

    private static void checkEquip(byte page, byte x, byte y, ItemJar jar, byte slot)
    {
        if (page == PlayerInventory.AREA)
        {
            if (page == selectedPage && x == selected_x && y == selected_y)
            {
                closeSelection();
            }
            if (jar.interactableItem != null)
            {
                ItemManager.takeItem(jar.interactableItem.transform.parent, byte.MaxValue, byte.MaxValue, 0, byte.MaxValue);
            }
        }
        else if (!Player.player.equipment.checkSelection(page, x, y))
        {
            ItemAsset asset = jar.GetAsset();
            if (asset == null)
            {
                return;
            }
            if (asset.canPlayerEquip && asset.slot.canEquipInPage(page))
            {
                Player.player.equipment.equip(page, x, y);
                PlayerDashboardUI.close();
                PlayerLifeUI.open();
            }
            else if (asset.slot == ESlotType.PRIMARY)
            {
                checkSlot(page, x, y, jar, 0);
            }
            else
            {
                if (asset.slot != ESlotType.SECONDARY)
                {
                    return;
                }
                if (slot == byte.MaxValue)
                {
                    if (!checkSlot(page, x, y, jar, 1))
                    {
                        checkSlot(page, x, y, jar, 0);
                    }
                }
                else
                {
                    checkSlot(page, x, y, jar, slot);
                }
            }
        }
        else if (Player.player.equipment.HasValidUseable && !Player.player.equipment.isBusy && Player.player.equipment.IsEquipAnimationFinished)
        {
            Player.player.equipment.dequip();
            if (page == selectedPage && x == selected_x && y == selected_y)
            {
                closeSelection();
            }
        }
    }

    private static void checkAction(byte page, byte x, byte y, ItemJar jar)
    {
        if (page == PlayerInventory.AREA)
        {
            if (jar.interactableItem != null)
            {
                ItemManager.takeItem(jar.interactableItem.transform.parent, byte.MaxValue, byte.MaxValue, 0, byte.MaxValue);
            }
            return;
        }
        ItemAsset asset = jar.GetAsset();
        if (asset != null)
        {
            if (asset.type == EItemType.HAT)
            {
                Player.player.clothing.sendSwapHat(page, x, y);
            }
            else if (asset.type == EItemType.SHIRT)
            {
                Player.player.clothing.sendSwapShirt(page, x, y);
            }
            else if (asset.type == EItemType.PANTS)
            {
                Player.player.clothing.sendSwapPants(page, x, y);
            }
            else if (asset.type == EItemType.BACKPACK)
            {
                Player.player.clothing.sendSwapBackpack(page, x, y);
            }
            else if (asset.type == EItemType.VEST)
            {
                Player.player.clothing.sendSwapVest(page, x, y);
            }
            else if (asset.type == EItemType.MASK)
            {
                Player.player.clothing.sendSwapMask(page, x, y);
            }
            else if (asset.type == EItemType.GLASSES)
            {
                Player.player.clothing.sendSwapGlasses(page, x, y);
            }
            else if (asset.canPlayerEquip)
            {
                checkEquip(page, x, y, jar, byte.MaxValue);
            }
        }
    }

    private static void onGrabbedItem(byte page, byte x, byte y, SleekItem item)
    {
        if (InputEx.GetKey(ControlsSettings.other))
        {
            if (page == PlayerInventory.AREA)
            {
                if (item.jar.interactableItem == null)
                {
                    UnturnedLog.warn("onGrabbedItem nearby without interactable");
                }
                else
                {
                    ItemManager.takeItem(item.jar.interactableItem.transform.parent, byte.MaxValue, byte.MaxValue, 0, byte.MaxValue);
                }
            }
            else
            {
                Player.player.inventory.sendDropItem(page, x, y);
            }
            return;
        }
        dragJar = Player.player.inventory.getItem(page, Player.player.inventory.getIndex(page, x, y));
        if (dragJar != null)
        {
            dragSource = item;
            dragFromPage = page;
            dragFrom_x = x;
            dragFrom_y = y;
            dragFromRot = dragJar.rot;
            dragOffset = -item.GetNormalizedCursorPosition();
            dragOffset.x *= item.SizeOffset_X;
            dragOffset.y *= item.SizeOffset_Y;
            if (dragJar.rot == 1)
            {
                float x2 = dragOffset.x;
                dragOffset.x = dragOffset.y;
                dragOffset.y = 0f - ((float)(dragJar.size_y * 50) + x2);
            }
            else if (dragJar.rot == 2)
            {
                dragOffset.x = 0f - ((float)(dragJar.size_x * 50) + dragOffset.x);
                dragOffset.y = 0f - ((float)(dragJar.size_y * 50) + dragOffset.y);
            }
            else if (dragJar.rot == 3)
            {
                float x3 = dragOffset.x;
                dragOffset.x = 0f - ((float)(dragJar.size_x * 50) + dragOffset.y);
                dragOffset.y = x3;
            }
            updatePivot();
            dragItem.updateItem(dragJar);
            refreshDraggedVisualPosition();
            startDrag();
        }
    }

    private static void onPlacedItem(byte page, byte x, byte y)
    {
        ConsumeEvent();
        if (dragSource == null || !isDragging)
        {
            return;
        }
        if (page >= PlayerInventory.SLOTS)
        {
            int num = x + (int)(dragPivot.x / 50f);
            int num2 = y + (int)(dragPivot.y / 50f);
            if (num < 0)
            {
                num = 0;
            }
            if (num2 < 0)
            {
                num2 = 0;
            }
            byte b = dragJar.size_x;
            byte b2 = dragJar.size_y;
            if (dragJar.rot % 2 == 1)
            {
                b = dragJar.size_y;
                b2 = dragJar.size_x;
            }
            if (num >= Player.player.inventory.getWidth(page) - b)
            {
                num = (byte)(Player.player.inventory.getWidth(page) - b);
            }
            if (num2 >= Player.player.inventory.getHeight(page) - b2)
            {
                num2 = (byte)(Player.player.inventory.getHeight(page) - b2);
            }
            x = (byte)num;
            y = (byte)num2;
        }
        ItemAsset asset = dragJar.GetAsset();
        if (asset == null || (page < PlayerInventory.SLOTS && !asset.slot.canEquipInPage(page)))
        {
            return;
        }
        if (dragFromPage == page && dragFrom_x == x && dragFrom_y == y && dragFromRot == dragJar.rot)
        {
            stopDrag();
            return;
        }
        if (page == PlayerInventory.AREA)
        {
            stopDrag();
            if (page != dragFromPage)
            {
                Player.player.inventory.sendDropItem(dragFromPage, dragFrom_x, dragFrom_y);
            }
            return;
        }
        if (dragFromPage == PlayerInventory.AREA)
        {
            byte rot = dragJar.rot;
            stopDrag();
            if (page != dragFromPage && Player.player.inventory.checkSpaceEmpty(page, x, y, dragJar.size_x, dragJar.size_y, rot) && dragItem.jar != null && dragItem.jar.interactableItem != null)
            {
                ItemManager.takeItem(dragItem.jar.interactableItem.transform.parent, x, y, rot, page);
            }
            return;
        }
        if (Player.player.inventory.checkSpaceDrag(page, dragFrom_x, dragFrom_y, dragFromRot, x, y, dragJar.rot, dragJar.size_x, dragJar.size_y, page == dragFromPage))
        {
            byte rot2 = dragJar.rot;
            stopDrag();
            Player.player.inventory.sendDragItem(dragFromPage, dragFrom_x, dragFrom_y, page, x, y, rot2);
            if (page < PlayerInventory.SLOTS)
            {
                Player.player.equipment.equip(page, 0, 0);
                PlayerDashboardUI.close();
                PlayerLifeUI.open();
            }
            return;
        }
        if (page < PlayerInventory.SLOTS)
        {
            stopDrag();
            checkEquip(dragFromPage, dragFrom_x, dragFrom_y, dragJar, page);
            return;
        }
        byte find_x;
        byte find_y;
        byte b3 = Player.player.inventory.findIndex(page, x, y, out find_x, out find_y);
        if (b3 == byte.MaxValue)
        {
            return;
        }
        if (dragFromPage == page && dragFrom_x == find_x && dragFrom_y == find_y)
        {
            stopDrag();
            return;
        }
        ItemJar item = Player.player.inventory.getItem(page, b3);
        if (!Player.player.inventory.checkSpaceSwap(page, find_x, find_y, item.size_x, item.size_y, item.rot, dragJar.size_x, dragJar.size_y, dragJar.rot))
        {
            return;
        }
        byte b4 = item.rot;
        if (!Player.player.inventory.checkSpaceSwap(dragFromPage, dragFrom_x, dragFrom_y, dragJar.size_x, dragJar.size_y, dragFromRot, item.size_x, item.size_y, b4))
        {
            b4 = (byte)((b4 + 1) % 4);
            if (!Player.player.inventory.checkSpaceSwap(dragFromPage, dragFrom_x, dragFrom_y, dragJar.size_x, dragJar.size_y, dragFromRot, item.size_x, item.size_y, b4))
            {
                return;
            }
        }
        ItemAsset asset2 = item.GetAsset();
        if (asset2 != null && (dragFromPage >= PlayerInventory.SLOTS || asset2.slot.canEquipInPage(dragFromPage)))
        {
            byte rot3 = dragJar.rot;
            stopDrag();
            Player.player.inventory.sendSwapItem(page, find_x, find_y, rot3, dragFromPage, dragFrom_x, dragFrom_y, b4);
            if (dragFromPage < PlayerInventory.SLOTS)
            {
                checkEquip(dragFromPage, dragFrom_x, dragFrom_y, dragJar, page);
            }
        }
    }

    private static void onClickedCharacter()
    {
        if (dragSource != null && isDragging)
        {
            byte page = dragFromPage;
            byte x = dragFrom_x;
            byte y = dragFrom_y;
            ItemJar jar = dragJar;
            stopDrag();
            checkAction(page, x, y, jar);
        }
        else
        {
            Vector2 normalizedCursorPosition = characterImage.GetNormalizedCursorPosition();
            Vector3 pos = new Vector3(normalizedCursorPosition.x, 1f - normalizedCursorPosition.y, 0f);
            Physics.Raycast(Player.player.look.characterCamera.ViewportPointToRay(pos), out var hitInfo, 8f, RayMasks.CLOTHING_INTERACT);
            if (hitInfo.collider != null)
            {
                Transform transform = hitInfo.collider.transform;
                if (transform.CompareTag("Player"))
                {
                    switch (DamageTool.getLimb(transform))
                    {
                    case ELimb.LEFT_FOOT:
                    case ELimb.LEFT_LEG:
                    case ELimb.RIGHT_FOOT:
                    case ELimb.RIGHT_LEG:
                        Player.player.clothing.sendSwapPants(byte.MaxValue, byte.MaxValue, byte.MaxValue);
                        break;
                    case ELimb.LEFT_HAND:
                    case ELimb.LEFT_ARM:
                    case ELimb.RIGHT_HAND:
                    case ELimb.RIGHT_ARM:
                    case ELimb.SPINE:
                        Player.player.clothing.sendSwapShirt(byte.MaxValue, byte.MaxValue, byte.MaxValue);
                        break;
                    }
                }
                else if (transform.CompareTag("Enemy"))
                {
                    if (transform.name == "Hat")
                    {
                        Player.player.clothing.sendSwapHat(byte.MaxValue, byte.MaxValue, byte.MaxValue);
                    }
                    else if (transform.name == "Glasses")
                    {
                        Player.player.clothing.sendSwapGlasses(byte.MaxValue, byte.MaxValue, byte.MaxValue);
                    }
                    else if (transform.name == "Mask")
                    {
                        Player.player.clothing.sendSwapMask(byte.MaxValue, byte.MaxValue, byte.MaxValue);
                    }
                    else if (transform.name == "Vest")
                    {
                        Player.player.clothing.sendSwapVest(byte.MaxValue, byte.MaxValue, byte.MaxValue);
                    }
                    else if (transform.name == "Backpack")
                    {
                        Player.player.clothing.sendSwapBackpack(byte.MaxValue, byte.MaxValue, byte.MaxValue);
                    }
                }
                else if (transform.CompareTag("Item"))
                {
                    Player.player.equipment.dequip();
                }
            }
        }
        ConsumeEvent();
    }

    private static void onClickedOutsideSelection()
    {
        closeSelection();
    }

    private static void onClickedDuringDrag()
    {
        if (dragSource != null && isDragging)
        {
            byte b = dragFromPage;
            byte x = dragFrom_x;
            byte y = dragFrom_y;
            stopDrag();
            if (b != PlayerInventory.AREA)
            {
                Player.player.inventory.sendDropItem(b, x, y);
            }
            ConsumeEvent();
        }
    }

    private static void onRightClickedDuringDrag()
    {
        if (dragSource != null && isDragging)
        {
            stopDrag();
        }
    }

    private static void onItemDropAdded(Transform model, InteractableItem interactableItem)
    {
        if (active && PlayerDashboardUI.active && !(Player.player == null) && areaItems.getItemCount() < 200)
        {
            Vector3 eyesPositionWithoutLeaning = Player.player.look.GetEyesPositionWithoutLeaning();
            if (!((model.position - eyesPositionWithoutLeaning).sqrMagnitude > 16f))
            {
                pendingItemsInRadius.Add(interactableItem);
            }
        }
    }

    private static void onItemDropRemoved(Transform model, InteractableItem interactableItem)
    {
        if (!active || !PlayerDashboardUI.active)
        {
            return;
        }
        ItemJar jar = interactableItem.jar;
        if (jar != null)
        {
            int num = areaItems.FindIndexOfJar(jar);
            if (num < 0)
            {
                pendingItemsInRadius.RemoveFast(interactableItem);
                return;
            }
            areaItems.removeItem((byte)num);
            items[PlayerInventory.AREA - PlayerInventory.SLOTS].removeItem(jar);
        }
    }

    private static void onSeated(bool isDriver, bool inVehicle, bool wasVehicle, InteractableVehicle oldVehicle, InteractableVehicle newVehicle)
    {
        if (oldVehicle != null)
        {
            oldVehicle.onPassengersUpdated -= updateVehicle;
            oldVehicle.onLockUpdated -= onVehicleLockUpdated;
            oldVehicle.onHeadlightsUpdated -= updateVehicle;
            oldVehicle.onSirensUpdated -= updateVehicle;
            oldVehicle.onBlimpUpdated -= updateVehicle;
            oldVehicle.batteryChanged -= updateVehicle;
            oldVehicle.skinChanged -= updateVehicle;
        }
        if (newVehicle != null)
        {
            newVehicle.onPassengersUpdated += updateVehicle;
            newVehicle.onLockUpdated += onVehicleLockUpdated;
            newVehicle.onHeadlightsUpdated += updateVehicle;
            newVehicle.onSirensUpdated += updateVehicle;
            newVehicle.onBlimpUpdated += updateVehicle;
            newVehicle.batteryChanged += updateVehicle;
            newVehicle.skinChanged += updateVehicle;
        }
        updateVehicle();
    }

    private static void onVehicleLockUpdated()
    {
        updateVehicle();
        InteractableVehicle vehicle = Player.player.movement.getVehicle();
        if (!(vehicle == null))
        {
            PlayerUI.message(vehicle.isLocked ? EPlayerMessage.VEHICLE_LOCKED : EPlayerMessage.VEHICLE_UNLOCKED, string.Empty);
        }
    }

    private static void updateVehicle()
    {
        if (!active)
        {
            return;
        }
        InteractableVehicle vehicle = Player.player.movement.getVehicle();
        if (vehicle != null && vehicle.asset != null)
        {
            VehicleAsset asset = vehicle.asset;
            vehicleNameLabel.Text = asset.vehicleName;
            vehicleNameLabel.TextColor = ItemTool.getRarityColorUI(asset.rarity);
            int num = 0;
            int num2 = 0;
            if (asset.canBeLocked)
            {
                vehicleLockButton.Text = localization.format(vehicle.isLocked ? "Vehicle_Lock_Off" : "Vehicle_Lock_On", MenuConfigurationControlsUI.getKeyCodeText(ControlsSettings.locker));
                vehicleLockButton.TooltipText = localization.format("Vehicle_Lock_Tooltip");
                vehicleLockButton.IsVisible = true;
                vehicleLockButton.PositionOffset_Y = num;
                num += 40;
            }
            else
            {
                vehicleLockButton.IsVisible = false;
            }
            if (asset.hasHorn)
            {
                vehicleHornButton.Text = localization.format("Vehicle_Horn", MenuConfigurationControlsUI.getKeyCodeText(ControlsSettings.primary));
                vehicleHornButton.TooltipText = localization.format("Vehicle_Horn_Tooltip");
                vehicleHornButton.IsVisible = true;
                vehicleHornButton.PositionOffset_Y = num;
                num += 40;
            }
            else
            {
                vehicleHornButton.IsVisible = false;
            }
            if (asset.hasHeadlights)
            {
                vehicleHeadlightsButton.Text = localization.format(vehicle.headlightsOn ? "Vehicle_Headlights_Off" : "Vehicle_Headlights_On", MenuConfigurationControlsUI.getKeyCodeText(ControlsSettings.secondary));
                vehicleHeadlightsButton.TooltipText = localization.format("Vehicle_Headlights_Tooltip");
                vehicleHeadlightsButton.IsVisible = true;
                vehicleHeadlightsButton.PositionOffset_Y = num;
                num += 40;
            }
            else
            {
                vehicleHeadlightsButton.IsVisible = false;
            }
            if (asset.hasSirens)
            {
                vehicleSirensButton.Text = localization.format(vehicle.sirensOn ? "Vehicle_Sirens_Off" : "Vehicle_Sirens_On", MenuConfigurationControlsUI.getKeyCodeText(ControlsSettings.other));
                vehicleSirensButton.TooltipText = localization.format("Vehicle_Sirens_Tooltip");
                vehicleSirensButton.IsVisible = true;
                vehicleSirensButton.PositionOffset_Y = num;
                num += 40;
            }
            else
            {
                vehicleSirensButton.IsVisible = false;
            }
            if (asset.engine == EEngine.BLIMP)
            {
                vehicleBlimpButton.Text = localization.format(vehicle.isBlimpFloating ? "Vehicle_Blimp_Off" : "Vehicle_Blimp_On", MenuConfigurationControlsUI.getKeyCodeText(ControlsSettings.other));
                vehicleBlimpButton.TooltipText = localization.format("Vehicle_Blimp_Tooltip");
                vehicleBlimpButton.IsVisible = true;
                vehicleBlimpButton.PositionOffset_Y = num;
                num += 40;
            }
            else
            {
                vehicleBlimpButton.IsVisible = false;
            }
            if (asset.hasHook)
            {
                vehicleHookButton.Text = localization.format("Vehicle_Hook", MenuConfigurationControlsUI.getKeyCodeText(ControlsSettings.other));
                vehicleHookButton.TooltipText = localization.format("Vehicle_Hook_Tooltip");
                vehicleHookButton.IsVisible = true;
                vehicleHookButton.PositionOffset_Y = num;
                num += 40;
            }
            else
            {
                vehicleHookButton.IsVisible = false;
            }
            if (vehicle.usesBattery && vehicle.ContainsBatteryItem && vehicle.asset.canStealBattery)
            {
                vehicleStealBatteryButton.Text = localization.format("Vehicle_Steal_Battery");
                vehicleStealBatteryButton.TooltipText = localization.format("Vehicle_Steal_Battery_Tooltip");
                vehicleStealBatteryButton.IsVisible = true;
                vehicleStealBatteryButton.PositionOffset_Y = num;
                num += 40;
            }
            else
            {
                vehicleStealBatteryButton.IsVisible = false;
            }
            int itemdefid = 0;
            ushort num3 = 0;
            ushort num4 = 0;
            if (Player.player.channel.owner.skinItems != null && Player.player.channel.owner.GetVehicleSkinItemDefId(vehicle, out itemdefid))
            {
                num3 = Provider.provider.economyService.getInventorySkinID(itemdefid);
                num4 = Provider.provider.economyService.getInventoryMythicID(itemdefid);
            }
            bool flag;
            bool flag2;
            if (num3 != 0)
            {
                if (num3 == vehicle.skinID && num4 == vehicle.mythicID)
                {
                    flag = true;
                    flag2 = true;
                }
                else
                {
                    flag = false;
                    flag2 = true;
                }
            }
            else if (vehicle.isSkinned)
            {
                flag = true;
                flag2 = true;
            }
            else
            {
                flag = false;
                flag2 = false;
            }
            if (flag2)
            {
                vehicleSkinButton.Text = localization.format(flag ? "Vehicle_Skin_Off" : "Vehicle_Skin_On");
                vehicleSkinButton.TooltipText = localization.format("Vehicle_Skin_Tooltip");
                vehicleSkinButton.IsVisible = true;
                vehicleSkinButton.PositionOffset_Y = num;
                num += 40;
            }
            else
            {
                vehicleSkinButton.IsVisible = false;
            }
            if (Player.player.stance.stance == EPlayerStance.DRIVING)
            {
                vehiclePassengersBox.PositionOffset_X = 270f;
                vehiclePassengersBox.SizeOffset_X = -280f;
                vehicleActionsBox.IsVisible = true;
            }
            else
            {
                vehiclePassengersBox.PositionOffset_X = 10f;
                vehiclePassengersBox.SizeOffset_X = -20f;
                vehicleActionsBox.IsVisible = false;
            }
            vehiclePassengersBox.RemoveAllChildren();
            for (int i = 0; i < vehicle.passengers.Length; i++)
            {
                Passenger passenger = vehicle.passengers[i];
                ISleekButton sleekButton = Glazier.Get().CreateButton();
                sleekButton.PositionOffset_Y = num2;
                sleekButton.SizeOffset_Y = 30f;
                sleekButton.SizeScale_X = 1f;
                sleekButton.OnClicked += onClickedVehiclePassengerButton;
                vehiclePassengersBox.AddChild(sleekButton);
                if (passenger.player != null)
                {
                    string localDisplayName = passenger.player.GetLocalDisplayName();
                    if (i < 12)
                    {
                        sleekButton.Text = localization.format("Vehicle_Seat_Slot", localDisplayName, "F" + (i + 1));
                    }
                    else
                    {
                        sleekButton.Text = localDisplayName;
                    }
                }
                else if (i < 12)
                {
                    sleekButton.Text = localization.format("Vehicle_Seat_Slot", localization.format("Vehicle_Seat_Empty"), "F" + (i + 1));
                }
                else
                {
                    sleekButton.Text = localization.format("Vehicle_Seat_Empty");
                }
                num2 += 40;
            }
            vehicleActionsBox.SizeOffset_Y = num - 10;
            vehiclePassengersBox.SizeOffset_Y = num2 - 10;
            vehicleBox.IsVisible = true;
            int num5 = Mathf.Max(num, num2);
            vehicleBox.SizeOffset_Y = 60 + num5;
            headers[PlayerInventory.STORAGE - PlayerInventory.SLOTS].TextColor = vehicleNameLabel.TextColor;
            headers[PlayerInventory.STORAGE - PlayerInventory.SLOTS].Text = localization.format("Storage_Trunk", vehicleNameLabel.Text);
        }
        else
        {
            vehicleBox.IsVisible = false;
            headers[PlayerInventory.STORAGE - PlayerInventory.SLOTS].TextColor = ESleekTint.FONT;
            headers[PlayerInventory.STORAGE - PlayerInventory.SLOTS].Text = localization.format("Storage");
        }
        updateBoxAreas();
    }

    private static void resetNearbyDrops()
    {
        Vector3 eyesPositionWithoutLeaning = Player.player.look.GetEyesPositionWithoutLeaning();
        pendingItemsInRadius.Clear();
        ItemManager.findSimulatedItemsInRadius(eyesPositionWithoutLeaning, 16f, pendingItemsInRadius);
        areaItems.clear();
        areaItems.resize(8, 3);
        Player.player.inventory.replaceItems(PlayerInventory.AREA, areaItems);
        SleekItems obj = items[PlayerInventory.AREA - PlayerInventory.SLOTS];
        obj.clear();
        obj.resize(areaItems.width, areaItems.height);
        updateBoxAreas();
    }

    private static void onInventoryResized(byte page, byte newWidth, byte newHeight)
    {
        if (page >= PlayerInventory.SLOTS)
        {
            page -= PlayerInventory.SLOTS;
            items[page].resize(newWidth, newHeight);
            if (page > 0)
            {
                headers[page].IsVisible = newHeight > 0;
            }
            items[page].IsVisible = newHeight > 0;
            if (page == PlayerInventory.STORAGE - PlayerInventory.SLOTS && newHeight == 0)
            {
                items[page].clear();
            }
            updateBoxAreas();
        }
    }

    private static void updateBoxAreas()
    {
        float num = 0f;
        float num2 = 0f;
        bool flag = isSplitClothingArea;
        if (vehicleBox.IsVisible)
        {
            if (flag)
            {
                if (vehicleBox.Parent != areaBox)
                {
                    areaBox.AddChild(vehicleBox);
                }
                vehicleBox.PositionOffset_Y = num2;
                num2 += vehicleBox.SizeOffset_Y + 10f;
            }
            else
            {
                if (vehicleBox.Parent != clothingBox)
                {
                    clothingBox.AddChild(vehicleBox);
                }
                vehicleBox.PositionOffset_Y = num;
                num += vehicleBox.SizeOffset_Y + 10f;
            }
        }
        for (byte b = 0; b < items.Length; b++)
        {
            if (headers[b].IsVisible)
            {
                if (flag && (b == PlayerInventory.STORAGE - PlayerInventory.SLOTS || b == PlayerInventory.AREA - PlayerInventory.SLOTS))
                {
                    if (headers[b].Parent != areaBox)
                    {
                        areaBox.AddChild(headers[b]);
                    }
                    if (items[b].Parent != areaBox)
                    {
                        areaBox.AddChild(items[b]);
                    }
                    headers[b].PositionOffset_Y = num2;
                    items[b].PositionOffset_Y = num2 + 70f;
                    num2 += items[b].SizeOffset_Y + 80f;
                }
                else
                {
                    if (headers[b].Parent != clothingBox)
                    {
                        clothingBox.AddChild(headers[b]);
                    }
                    if (items[b].Parent != clothingBox)
                    {
                        clothingBox.AddChild(items[b]);
                    }
                    headers[b].PositionOffset_Y = num;
                    items[b].PositionOffset_Y = num + 70f;
                    num += items[b].SizeOffset_Y + 80f;
                }
            }
        }
        headers[7].IsVisible = Player.player.clothing.hatAsset != null;
        if (headers[7].IsVisible)
        {
            headers[7].PositionOffset_Y = num;
            num += 70f;
        }
        headers[8].IsVisible = Player.player.clothing.maskAsset != null;
        if (headers[8].IsVisible)
        {
            headers[8].PositionOffset_Y = num;
            num += 70f;
        }
        headers[9].IsVisible = Player.player.clothing.glassesAsset != null;
        if (headers[9].IsVisible)
        {
            headers[9].PositionOffset_Y = num;
            num += 70f;
        }
        clothingBox.ContentSizeOffset = new Vector2(0f, num - 10f);
        areaBox.ContentSizeOffset = new Vector2(0f, num2 - 10f);
        InteractableStorage interactableStorage = PlayerInteract.interactable as InteractableStorage;
        if (interactableStorage != null && interactableStorage.isDisplay)
        {
            headers[PlayerInventory.STORAGE - PlayerInventory.SLOTS].SizeOffset_X = -180f;
            rot_xButton.IsVisible = true;
            rot_yButton.IsVisible = true;
            rot_zButton.IsVisible = true;
        }
        else
        {
            headers[PlayerInventory.STORAGE - PlayerInventory.SLOTS].SizeOffset_X = 0f;
            rot_xButton.IsVisible = false;
            rot_yButton.IsVisible = false;
            rot_zButton.IsVisible = false;
        }
    }

    private static void updateHotkeys()
    {
        for (byte b = 0; b < PlayerInventory.STORAGE - PlayerInventory.SLOTS; b++)
        {
            items[b].resetHotkeyDisplay();
        }
        for (byte b2 = 0; b2 < Player.player.equipment.hotkeys.Length; b2++)
        {
            HotkeyInfo hotkeyInfo = Player.player.equipment.hotkeys[b2];
            byte button = (byte)(b2 + 2);
            byte b3 = (byte)(hotkeyInfo.page - 2);
            if (hotkeyInfo.id != 0)
            {
                byte index = Player.player.inventory.getIndex(hotkeyInfo.page, hotkeyInfo.x, hotkeyInfo.y);
                ItemJar item = Player.player.inventory.getItem(hotkeyInfo.page, index);
                if (item == null || item.item.id != hotkeyInfo.id)
                {
                    hotkeyInfo.id = 0;
                    hotkeyInfo.page = byte.MaxValue;
                    hotkeyInfo.x = byte.MaxValue;
                    hotkeyInfo.y = byte.MaxValue;
                }
                else
                {
                    items[b3].updateHotkey(item, button);
                }
            }
        }
    }

    private static void onHotkeysUpdated()
    {
        updateHotkeys();
    }

    private static void onInventoryUpdated(byte page, byte index, ItemJar jar)
    {
        if (page < PlayerInventory.SLOTS)
        {
            slots[page].updateItem(jar);
            return;
        }
        page -= PlayerInventory.SLOTS;
        items[page].updateItem(jar);
    }

    private static void onInventoryAdded(byte page, byte index, ItemJar jar)
    {
        if (page < PlayerInventory.SLOTS)
        {
            slots[page].applyItem(jar);
            return;
        }
        page -= PlayerInventory.SLOTS;
        items[page].addItem(jar);
    }

    private static void onInventoryRemoved(byte page, byte index, ItemJar jar)
    {
        if (page == selectedPage && jar.x == selected_x && jar.y == selected_y)
        {
            closeSelection();
        }
        if (page < PlayerInventory.SLOTS)
        {
            slots[page].applyItem(null);
            return;
        }
        page -= PlayerInventory.SLOTS;
        items[page].removeItem(jar);
    }

    private static void onInventoryStored()
    {
        if (Player.player.inventory.shouldStorageOpenDashboard)
        {
            PlayerLifeUI.close();
            PlayerPauseUI.close();
            if (PlayerDashboardUI.active)
            {
                PlayerDashboardCraftingUI.close();
                PlayerDashboardSkillsUI.close();
                PlayerDashboardInformationUI.close();
                open();
            }
            else
            {
                active = true;
                PlayerDashboardCraftingUI.active = false;
                PlayerDashboardSkillsUI.active = false;
                PlayerDashboardInformationUI.active = false;
                PlayerDashboardUI.open();
            }
        }
        if (!isSplitClothingArea)
        {
            clothingBox.ScrollToBottom();
        }
    }

    private static void onShirtUpdated(ushort newShirtObsolete, byte newShirtQuality, byte[] newShirtState)
    {
        ItemAsset shirtAsset = Player.player.clothing.shirtAsset;
        if (shirtAsset != null)
        {
            headers[3].Text = shirtAsset.itemName;
            headers[3].GetChildAtIndex(0).SizeOffset_X = shirtAsset.size_x * 25;
            headers[3].GetChildAtIndex(0).SizeOffset_Y = shirtAsset.size_y * 25;
            headers[3].GetChildAtIndex(0).PositionOffset_Y = (0f - headers[3].GetChildAtIndex(0).SizeOffset_Y) / 2f;
            headerItemIcons[3].Refresh(shirtAsset.id, newShirtQuality, newShirtState);
            ((ISleekLabel)headers[3].GetChildAtIndex(2)).Text = newShirtQuality + "%";
            Color rarityColorUI = ItemTool.getRarityColorUI(shirtAsset.rarity);
            headers[3].BackgroundColor = SleekColor.BackgroundIfLight(rarityColorUI);
            headers[3].TextColor = rarityColorUI;
            Color qualityColor = ItemTool.getQualityColor((float)(int)newShirtQuality / 100f);
            ((ISleekImage)headers[3].GetChildAtIndex(1)).TintColor = qualityColor;
            ((ISleekLabel)headers[3].GetChildAtIndex(2)).TextColor = qualityColor;
        }
    }

    private static void onPantsUpdated(ushort newPantsObsolete, byte newPantsQuality, byte[] newPantsState)
    {
        if (headers != null)
        {
            ItemAsset pantsAsset = Player.player.clothing.pantsAsset;
            if (pantsAsset != null)
            {
                headers[4].Text = pantsAsset.itemName;
                headers[4].GetChildAtIndex(0).SizeOffset_X = pantsAsset.size_x * 25;
                headers[4].GetChildAtIndex(0).SizeOffset_Y = pantsAsset.size_y * 25;
                headers[4].GetChildAtIndex(0).PositionOffset_Y = (0f - headers[4].GetChildAtIndex(0).SizeOffset_Y) / 2f;
                headerItemIcons[4].Refresh(pantsAsset.id, newPantsQuality, newPantsState);
                ((ISleekLabel)headers[4].GetChildAtIndex(2)).Text = newPantsQuality + "%";
                Color rarityColorUI = ItemTool.getRarityColorUI(pantsAsset.rarity);
                headers[4].BackgroundColor = SleekColor.BackgroundIfLight(rarityColorUI);
                headers[4].TextColor = rarityColorUI;
                Color qualityColor = ItemTool.getQualityColor((float)(int)newPantsQuality / 100f);
                ((ISleekImage)headers[4].GetChildAtIndex(1)).TintColor = qualityColor;
                ((ISleekLabel)headers[4].GetChildAtIndex(2)).TextColor = qualityColor;
            }
        }
    }

    private static void onHatUpdated(ushort newHatObsolete, byte newHatQuality, byte[] newHatState)
    {
        if (headers != null)
        {
            ItemAsset hatAsset = Player.player.clothing.hatAsset;
            if (hatAsset != null)
            {
                headers[7].Text = hatAsset.itemName;
                headers[7].GetChildAtIndex(0).SizeOffset_X = hatAsset.size_x * 25;
                headers[7].GetChildAtIndex(0).SizeOffset_Y = hatAsset.size_y * 25;
                headers[7].GetChildAtIndex(0).PositionOffset_Y = (0f - headers[7].GetChildAtIndex(0).SizeOffset_Y) / 2f;
                headerItemIcons[7].Refresh(newHatObsolete, newHatQuality, newHatState);
                ((ISleekLabel)headers[7].GetChildAtIndex(2)).Text = newHatQuality + "%";
                Color rarityColorUI = ItemTool.getRarityColorUI(hatAsset.rarity);
                headers[7].BackgroundColor = SleekColor.BackgroundIfLight(rarityColorUI);
                headers[7].TextColor = rarityColorUI;
                Color qualityColor = ItemTool.getQualityColor((float)(int)newHatQuality / 100f);
                ((ISleekImage)headers[7].GetChildAtIndex(1)).TintColor = qualityColor;
                ((ISleekLabel)headers[7].GetChildAtIndex(2)).TextColor = qualityColor;
            }
            headers[7].IsVisible = hatAsset != null;
            updateBoxAreas();
        }
    }

    private static void onBackpackUpdated(ushort newBackpackObsolete, byte newBackpackQuality, byte[] newBackpackState)
    {
        ItemAsset backpackAsset = Player.player.clothing.backpackAsset;
        if (backpackAsset != null)
        {
            headers[1].Text = backpackAsset.itemName;
            headers[1].GetChildAtIndex(0).SizeOffset_X = backpackAsset.size_x * 25;
            headers[1].GetChildAtIndex(0).SizeOffset_Y = backpackAsset.size_y * 25;
            headers[1].GetChildAtIndex(0).PositionOffset_Y = (0f - headers[1].GetChildAtIndex(0).SizeOffset_Y) / 2f;
            headerItemIcons[1].Refresh(backpackAsset.id, newBackpackQuality, newBackpackState);
            ((ISleekLabel)headers[1].GetChildAtIndex(2)).Text = newBackpackQuality + "%";
            Color rarityColorUI = ItemTool.getRarityColorUI(backpackAsset.rarity);
            headers[1].BackgroundColor = SleekColor.BackgroundIfLight(rarityColorUI);
            headers[1].TextColor = rarityColorUI;
            Color qualityColor = ItemTool.getQualityColor((float)(int)newBackpackQuality / 100f);
            ((ISleekImage)headers[1].GetChildAtIndex(1)).TintColor = qualityColor;
            ((ISleekLabel)headers[1].GetChildAtIndex(2)).TextColor = qualityColor;
        }
    }

    private static void onVestUpdated(ushort newVestObsolete, byte newVestQuality, byte[] newVestState)
    {
        ItemAsset vestAsset = Player.player.clothing.vestAsset;
        if (vestAsset != null)
        {
            headers[2].Text = vestAsset.itemName;
            headers[2].GetChildAtIndex(0).SizeOffset_X = vestAsset.size_x * 25;
            headers[2].GetChildAtIndex(0).SizeOffset_Y = vestAsset.size_y * 25;
            headers[2].GetChildAtIndex(0).PositionOffset_Y = (0f - headers[2].GetChildAtIndex(0).SizeOffset_Y) / 2f;
            headerItemIcons[2].Refresh(vestAsset.id, newVestQuality, newVestState);
            ((ISleekLabel)headers[2].GetChildAtIndex(2)).Text = newVestQuality + "%";
            Color rarityColorUI = ItemTool.getRarityColorUI(vestAsset.rarity);
            headers[2].BackgroundColor = SleekColor.BackgroundIfLight(rarityColorUI);
            headers[2].TextColor = rarityColorUI;
            Color qualityColor = ItemTool.getQualityColor((float)(int)newVestQuality / 100f);
            ((ISleekImage)headers[2].GetChildAtIndex(1)).TintColor = qualityColor;
            ((ISleekLabel)headers[2].GetChildAtIndex(2)).TextColor = qualityColor;
        }
    }

    private static void onMaskUpdated(ushort newMaskObsolete, byte newMaskQuality, byte[] newMaskState)
    {
        if (headers != null)
        {
            ItemAsset maskAsset = Player.player.clothing.maskAsset;
            if (maskAsset != null)
            {
                headers[8].Text = maskAsset.itemName;
                headers[8].GetChildAtIndex(0).SizeOffset_X = maskAsset.size_x * 25;
                headers[8].GetChildAtIndex(0).SizeOffset_Y = maskAsset.size_y * 25;
                headers[8].GetChildAtIndex(0).PositionOffset_Y = (0f - headers[8].GetChildAtIndex(0).SizeOffset_Y) / 2f;
                headerItemIcons[8].Refresh(maskAsset.id, newMaskQuality, newMaskState);
                ((ISleekLabel)headers[8].GetChildAtIndex(2)).Text = newMaskQuality + "%";
                Color rarityColorUI = ItemTool.getRarityColorUI(maskAsset.rarity);
                headers[8].BackgroundColor = SleekColor.BackgroundIfLight(rarityColorUI);
                headers[8].TextColor = rarityColorUI;
                Color qualityColor = ItemTool.getQualityColor((float)(int)newMaskQuality / 100f);
                ((ISleekImage)headers[8].GetChildAtIndex(1)).TintColor = qualityColor;
                ((ISleekLabel)headers[8].GetChildAtIndex(2)).TextColor = qualityColor;
            }
            headers[8].IsVisible = maskAsset != null;
            updateBoxAreas();
        }
    }

    private static void onGlassesUpdated(ushort newGlassesObsolete, byte newGlassesQuality, byte[] newGlassesState)
    {
        if (headers != null)
        {
            ItemAsset glassesAsset = Player.player.clothing.glassesAsset;
            if (glassesAsset != null)
            {
                headers[9].Text = glassesAsset.itemName;
                headers[9].GetChildAtIndex(0).SizeOffset_X = glassesAsset.size_x * 25;
                headers[9].GetChildAtIndex(0).SizeOffset_Y = glassesAsset.size_y * 25;
                headers[9].GetChildAtIndex(0).PositionOffset_Y = (0f - headers[9].GetChildAtIndex(0).SizeOffset_Y) / 2f;
                headerItemIcons[9].Refresh(glassesAsset.id, newGlassesQuality, newGlassesState);
                ((ISleekLabel)headers[9].GetChildAtIndex(2)).Text = newGlassesQuality + "%";
                Color rarityColorUI = ItemTool.getRarityColorUI(glassesAsset.rarity);
                headers[9].BackgroundColor = SleekColor.BackgroundIfLight(rarityColorUI);
                headers[9].TextColor = rarityColorUI;
                Color qualityColor = ItemTool.getQualityColor((float)(int)newGlassesQuality / 100f);
                ((ISleekImage)headers[9].GetChildAtIndex(1)).TintColor = qualityColor;
                ((ISleekLabel)headers[9].GetChildAtIndex(2)).TextColor = qualityColor;
            }
            headers[9].IsVisible = glassesAsset != null;
            updateBoxAreas();
        }
    }

    private static void onClickedHeader(ISleekElement button)
    {
        int i;
        for (i = 0; i < headers.Length && headers[i] != button; i++)
        {
        }
        switch (i)
        {
        case 0:
            if (Player.player.equipment.HasValidUseable && !Player.player.equipment.isBusy && Player.player.equipment.IsEquipAnimationFinished)
            {
                Player.player.equipment.dequip();
            }
            break;
        case 1:
            Player.player.clothing.sendSwapBackpack(byte.MaxValue, byte.MaxValue, byte.MaxValue);
            break;
        case 2:
            Player.player.clothing.sendSwapVest(byte.MaxValue, byte.MaxValue, byte.MaxValue);
            break;
        case 3:
            Player.player.clothing.sendSwapShirt(byte.MaxValue, byte.MaxValue, byte.MaxValue);
            break;
        case 4:
            Player.player.clothing.sendSwapPants(byte.MaxValue, byte.MaxValue, byte.MaxValue);
            break;
        case 5:
            PlayerDashboardUI.close();
            PlayerLifeUI.open();
            break;
        case 6:
            PlayerDashboardUI.close();
            PlayerLifeUI.open();
            break;
        case 7:
            Player.player.clothing.sendSwapHat(byte.MaxValue, byte.MaxValue, byte.MaxValue);
            break;
        case 8:
            Player.player.clothing.sendSwapMask(byte.MaxValue, byte.MaxValue, byte.MaxValue);
            break;
        case 9:
            Player.player.clothing.sendSwapGlasses(byte.MaxValue, byte.MaxValue, byte.MaxValue);
            break;
        }
    }

    private static void updatePivot()
    {
        if (dragJar.rot == 0)
        {
            dragPivot.x = dragOffset.x;
            dragPivot.y = dragOffset.y;
        }
        else if (dragJar.rot == 1)
        {
            dragPivot.x = 0f - ((float)(dragJar.size_y * 50) + dragOffset.y);
            dragPivot.y = dragOffset.x;
        }
        else if (dragJar.rot == 2)
        {
            dragPivot.x = 0f - ((float)(dragJar.size_x * 50) + dragOffset.x);
            dragPivot.y = 0f - ((float)(dragJar.size_y * 50) + dragOffset.y);
        }
        else if (dragJar.rot == 3)
        {
            dragPivot.x = dragOffset.y;
            dragPivot.y = 0f - ((float)(dragJar.size_x * 50) + dragOffset.x);
        }
    }

    /// <summary>
    /// Move item drag visual to the cursor's position.
    /// </summary>
    private static void refreshDraggedVisualPosition()
    {
        dragItem.PositionOffset_X = (int)dragPivot.x;
        dragItem.PositionOffset_Y = (int)dragPivot.y;
        Vector2 vector = PlayerUI.container.ViewportToNormalizedPosition(InputEx.NormalizedMousePosition);
        dragItem.PositionScale_X = vector.x;
        dragItem.PositionScale_Y = vector.y;
    }

    public static void updateDraggedItem()
    {
        if (active && PlayerDashboardUI.active && dragFromPage != byte.MaxValue && dragJar != null && isDragging)
        {
            if (InputEx.GetKeyDown(ControlsSettings.rotate))
            {
                dragJar.rot++;
                dragJar.rot %= 4;
                updatePivot();
                dragItem.updateItem(dragJar);
                PlayInventoryAudio(dragJar.GetAsset());
            }
            refreshDraggedVisualPosition();
        }
    }

    private static void createElementForNearbyDrop(InteractableItem interactableItem)
    {
        while (!areaItems.tryAddItem(interactableItem.item))
        {
            if (areaItems.height < 200)
            {
                areaItems.resize(areaItems.width, (byte)(areaItems.height + 1));
                continue;
            }
            return;
        }
        ItemJar item = areaItems.getItem((byte)(areaItems.getItemCount() - 1));
        item.interactableItem = interactableItem;
        interactableItem.jar = item;
        byte b = (byte)(areaItems.height - (item.y + ((item.rot % 2 == 0) ? item.size_y : item.size_x)));
        if (b < 3 && areaItems.height + b <= 200)
        {
            areaItems.resize(areaItems.width, (byte)(areaItems.height + (3 - b)));
        }
        SleekItems obj = items[PlayerInventory.AREA - PlayerInventory.SLOTS];
        obj.resize(areaItems.width, areaItems.height);
        obj.addItem(item);
    }

    public static void updateNearbyDrops()
    {
        if (!active || pendingItemsInRadius.Count < 1)
        {
            return;
        }
        int height = areaItems.height;
        Vector3 eyesPositionWithoutLeaning = Player.player.look.GetEyesPositionWithoutLeaning();
        int num = Mathf.Max(0, pendingItemsInRadius.Count - 20);
        for (int num2 = pendingItemsInRadius.Count - 1; num2 >= num; num2--)
        {
            InteractableItem interactableItem = pendingItemsInRadius[num2];
            pendingItemsInRadius.RemoveAt(num2);
            if (!(interactableItem == null) && interactableItem.item != null)
            {
                Renderer componentInChildren = interactableItem.transform.GetComponentInChildren<Renderer>();
                if (!(componentInChildren == null))
                {
                    Vector3 center = componentInChildren.bounds.center;
                    if (!Physics.Linecast(eyesPositionWithoutLeaning, center, out var _, RayMasks.BLOCK_PICKUP, QueryTriggerInteraction.Ignore))
                    {
                        createElementForNearbyDrop(interactableItem);
                    }
                }
            }
        }
        if (areaItems.height > height)
        {
            updateBoxAreas();
        }
    }

    private static void PlayInventoryAudio(ItemAsset item)
    {
        if (item != null && !item.inventoryAudio.IsNullOrEmpty)
        {
            float pitchMultiplier;
            float volumeMultiplier;
            AudioClip audioClip = item.inventoryAudio.LoadAudioClip(out volumeMultiplier, out pitchMultiplier);
            if (!(audioClip == null))
            {
                volumeMultiplier *= 0.2f;
                OneShotAudioParameters oneShotAudioParameters = new OneShotAudioParameters(audioClip);
                oneShotAudioParameters.volume = volumeMultiplier;
                oneShotAudioParameters.pitch = pitchMultiplier;
                oneShotAudioParameters.Play();
            }
        }
    }

    public PlayerDashboardInventoryUI()
    {
        if (icons != null)
        {
            icons.unload();
        }
        pendingItemsInRadius = new List<InteractableItem>();
        localization = Localization.read("/Player/PlayerDashboardInventory.dat");
        icons = Bundles.getBundle("/Bundles/Textures/Player/Icons/PlayerDashboardInventory/PlayerDashboardInventory.unity3d");
        _selectedPage = byte.MaxValue;
        _selected_x = byte.MaxValue;
        _selected_y = byte.MaxValue;
        isDragging = false;
        container = new SleekFullscreenBox();
        container.PositionScale_Y = 1f;
        container.PositionOffset_X = 10f;
        container.PositionOffset_Y = 10f;
        container.SizeOffset_X = -20f;
        container.SizeOffset_Y = -20f;
        container.SizeScale_X = 1f;
        container.SizeScale_Y = 1f;
        PlayerUI.container.AddChild(container);
        active = true;
        backdropBox = Glazier.Get().CreateBox();
        backdropBox.PositionOffset_Y = 60f;
        backdropBox.SizeOffset_Y = -60f;
        backdropBox.SizeScale_X = 1f;
        backdropBox.SizeScale_Y = 1f;
        backdropBox.BackgroundColor = new SleekColor(ESleekTint.BACKGROUND, 0.5f);
        container.AddChild(backdropBox);
        characterPlayer = null;
        hasDragOutsideHandlers = Glazier.Get().SupportsDepth;
        if (hasDragOutsideHandlers)
        {
            dragOutsideHandler = Glazier.Get().CreateImage();
            dragOutsideHandler.SizeScale_X = 1f;
            dragOutsideHandler.SizeScale_Y = 1f;
            dragOutsideHandler.OnClicked += onClickedDuringDrag;
            dragOutsideHandler.OnRightClicked += onRightClickedDuringDrag;
            dragOutsideHandler.IsVisible = false;
            backdropBox.AddChild(dragOutsideHandler);
        }
        else
        {
            dragOutsideHandler = null;
        }
        ISleekBox sleekBox = Glazier.Get().CreateBox();
        sleekBox.PositionOffset_X = 10f;
        sleekBox.PositionOffset_Y = 70f;
        sleekBox.SizeOffset_X = 410f;
        sleekBox.SizeOffset_Y = -280f;
        sleekBox.SizeScale_Y = 1f;
        backdropBox.AddChild(sleekBox);
        ISleekConstraintFrame sleekConstraintFrame = Glazier.Get().CreateConstraintFrame();
        sleekConstraintFrame.SizeScale_Y = 1f;
        sleekConstraintFrame.Constraint = ESleekConstraint.FitInParent;
        if (Glazier.Get().SupportsDepth)
        {
            sleekConstraintFrame.PositionScale_X = -0.5f;
            sleekConstraintFrame.SizeScale_X = 2f;
        }
        else
        {
            sleekConstraintFrame.PositionScale_X = 0f;
            sleekConstraintFrame.SizeScale_X = 1f;
        }
        sleekBox.AddChild(sleekConstraintFrame);
        characterImage = new SleekCameraImage();
        characterImage.SizeScale_X = 1f;
        characterImage.SizeScale_Y = 1f;
        characterImage.internalImage.OnClicked += onClickedCharacter;
        characterImage.SetCamera(Player.player.look.characterCamera);
        sleekConstraintFrame.AddChild(characterImage);
        slots = new SleekSlot[PlayerInventory.SLOTS];
        for (byte b = 0; b < slots.Length; b++)
        {
            slots[b] = new SleekSlot(b);
            slots[b].onSelectedItem = onSelectedItem;
            slots[b].onGrabbedItem = onGrabbedItem;
            slots[b].onPlacedItem = onPlacedItem;
            backdropBox.AddChild(slots[b]);
        }
        slots[0].PositionOffset_X = 10f;
        slots[0].PositionOffset_Y = -160f;
        slots[0].PositionScale_Y = 1f;
        slots[1].PositionOffset_X = 270f;
        slots[1].PositionOffset_Y = -160f;
        slots[1].PositionScale_Y = 1f;
        slots[1].SizeOffset_X = 150f;
        characterSlider = Glazier.Get().CreateSlider();
        characterSlider.SizeOffset_Y = 20f;
        characterSlider.SizeScale_X = 1f;
        characterSlider.SizeOffset_X = -120f;
        characterSlider.PositionOffset_X = 120f;
        characterSlider.PositionOffset_Y = 15f;
        characterSlider.PositionScale_Y = 1f;
        characterSlider.Orientation = ESleekOrientation.HORIZONTAL;
        characterSlider.OnValueChanged += onDraggedCharacterSlider;
        sleekBox.AddChild(characterSlider);
        swapCosmeticsButton = new SleekButtonIcon(icons.load<Texture2D>("Swap_Cosmetics"));
        swapCosmeticsButton.PositionOffset_Y = 10f;
        swapCosmeticsButton.PositionScale_Y = 1f;
        swapCosmeticsButton.SizeOffset_X = 30f;
        swapCosmeticsButton.SizeOffset_Y = 30f;
        swapCosmeticsButton.tooltip = localization.format("Swap_Cosmetics_Tooltip");
        swapCosmeticsButton.iconColor = ESleekTint.FOREGROUND;
        swapCosmeticsButton.onClickedButton += onClickedSwapCosmeticsButton;
        sleekBox.AddChild(swapCosmeticsButton);
        swapSkinsButton = new SleekButtonIcon(icons.load<Texture2D>("Swap_Skins"));
        swapSkinsButton.PositionOffset_X = 40f;
        swapSkinsButton.PositionOffset_Y = 10f;
        swapSkinsButton.PositionScale_Y = 1f;
        swapSkinsButton.SizeOffset_X = 30f;
        swapSkinsButton.SizeOffset_Y = 30f;
        swapSkinsButton.tooltip = localization.format("Swap_Skins_Tooltip");
        swapSkinsButton.iconColor = ESleekTint.FOREGROUND;
        swapSkinsButton.onClickedButton += onClickedSwapSkinsButton;
        sleekBox.AddChild(swapSkinsButton);
        swapMythicsButton = new SleekButtonIcon(icons.load<Texture2D>("Swap_Mythics"));
        swapMythicsButton.PositionOffset_X = 80f;
        swapMythicsButton.PositionOffset_Y = 10f;
        swapMythicsButton.PositionScale_Y = 1f;
        swapMythicsButton.SizeOffset_X = 30f;
        swapMythicsButton.SizeOffset_Y = 30f;
        swapMythicsButton.tooltip = localization.format("Swap_Mythics_Tooltip");
        swapMythicsButton.iconColor = ESleekTint.FOREGROUND;
        swapMythicsButton.onClickedButton += onClickedSwapMythicsButton;
        sleekBox.AddChild(swapMythicsButton);
        box = Glazier.Get().CreateFrame();
        box.PositionOffset_X = 430f;
        box.PositionOffset_Y = 10f;
        box.SizeOffset_X = -440f;
        box.SizeOffset_Y = -20f;
        box.SizeScale_X = 1f;
        box.SizeScale_Y = 1f;
        backdropBox.AddChild(box);
        clothingBox = Glazier.Get().CreateScrollView();
        clothingBox.SizeScale_X = 1f;
        clothingBox.SizeScale_Y = 1f;
        clothingBox.ContentSizeOffset = new Vector2(0f, 1000f);
        clothingBox.ScaleContentToWidth = true;
        box.AddChild(clothingBox);
        areaBox = Glazier.Get().CreateScrollView();
        areaBox.PositionOffset_X = 5f;
        areaBox.PositionScale_X = 0.5f;
        areaBox.SizeOffset_X = -5f;
        areaBox.SizeScale_X = 0.5f;
        areaBox.SizeScale_Y = 1f;
        areaBox.ContentSizeOffset = new Vector2(0f, 1000f);
        areaBox.ScaleContentToWidth = true;
        box.AddChild(areaBox);
        if (hasDragOutsideHandlers)
        {
            dragOutsideClothingHandler = Glazier.Get().CreateImage();
            dragOutsideClothingHandler.SizeScale_X = 1f;
            dragOutsideClothingHandler.SizeScale_Y = 1f;
            dragOutsideClothingHandler.OnClicked += onClickedDuringDrag;
            dragOutsideClothingHandler.OnRightClicked += onRightClickedDuringDrag;
            dragOutsideClothingHandler.IsVisible = false;
            clothingBox.AddChild(dragOutsideClothingHandler);
            dragOutsideAreaHandler = Glazier.Get().CreateImage();
            dragOutsideAreaHandler.SizeScale_X = 1f;
            dragOutsideAreaHandler.SizeScale_Y = 1f;
            dragOutsideAreaHandler.OnClicked += onClickedDuringDrag;
            dragOutsideAreaHandler.OnRightClicked += onRightClickedDuringDrag;
            dragOutsideAreaHandler.IsVisible = false;
            areaBox.AddChild(dragOutsideAreaHandler);
        }
        else
        {
            dragOutsideClothingHandler = null;
            dragOutsideAreaHandler = null;
        }
        headers = new ISleekButton[PlayerInventory.PAGES - PlayerInventory.SLOTS + 3];
        for (byte b2 = 0; b2 < headers.Length; b2++)
        {
            headers[b2] = Glazier.Get().CreateButton();
            headers[b2].SizeOffset_Y = 60f;
            headers[b2].SizeScale_X = 1f;
            headers[b2].FontSize = ESleekFontSize.Medium;
            headers[b2].OnClicked += onClickedHeader;
            headers[b2].TextContrastContext = ETextContrastContext.InconspicuousBackdrop;
            clothingBox.AddChild(headers[b2]);
            headers[b2].IsVisible = false;
        }
        headers[0].IsVisible = true;
        headers[PlayerInventory.AREA - PlayerInventory.SLOTS].IsVisible = true;
        headerItemIcons = new SleekItemIcon[headers.Length];
        for (byte b3 = 1; b3 < headers.Length; b3++)
        {
            if (b3 != PlayerInventory.STORAGE - PlayerInventory.SLOTS && b3 != PlayerInventory.AREA - PlayerInventory.SLOTS)
            {
                SleekItemIcon sleekItemIcon = new SleekItemIcon
                {
                    PositionOffset_X = 5f,
                    PositionScale_Y = 0.5f
                };
                headerItemIcons[b3] = sleekItemIcon;
                headers[b3].AddChild(sleekItemIcon);
                ISleekImage sleekImage = Glazier.Get().CreateImage();
                sleekImage.PositionOffset_X = -25f;
                sleekImage.PositionOffset_Y = -25f;
                sleekImage.PositionScale_X = 1f;
                sleekImage.PositionScale_Y = 1f;
                sleekImage.SizeOffset_X = 20f;
                sleekImage.SizeOffset_Y = 20f;
                sleekImage.Texture = icons.load<Texture2D>("Quality_0");
                headers[b3].AddChild(sleekImage);
                ISleekLabel sleekLabel = Glazier.Get().CreateLabel();
                sleekLabel.PositionOffset_X = -105f;
                sleekLabel.PositionOffset_Y = 5f;
                sleekLabel.PositionScale_X = 1f;
                sleekLabel.SizeOffset_X = 100f;
                sleekLabel.SizeOffset_Y = -10f;
                sleekLabel.SizeScale_Y = 1f;
                sleekLabel.TextAlignment = TextAnchor.UpperRight;
                sleekLabel.TextContrastContext = ETextContrastContext.InconspicuousBackdrop;
                headers[b3].AddChild(sleekLabel);
            }
        }
        headers[0].Text = localization.format("Hands");
        headers[PlayerInventory.AREA - PlayerInventory.SLOTS].Text = localization.format("Area");
        onShirtUpdated(0, Player.player.clothing.shirtQuality, Player.player.clothing.shirtState);
        onPantsUpdated(0, Player.player.clothing.pantsQuality, Player.player.clothing.pantsState);
        onBackpackUpdated(0, Player.player.clothing.backpackQuality, Player.player.clothing.backpackState);
        onVestUpdated(0, Player.player.clothing.vestQuality, Player.player.clothing.vestState);
        items = new SleekItems[PlayerInventory.PAGES - PlayerInventory.SLOTS];
        for (byte b4 = 0; b4 < items.Length; b4++)
        {
            items[b4] = new SleekItems((byte)(PlayerInventory.SLOTS + b4));
            items[b4].onSelectedItem = onSelectedItem;
            items[b4].onGrabbedItem = onGrabbedItem;
            items[b4].onPlacedItem = onPlacedItem;
            clothingBox.AddChild(items[b4]);
        }
        areaItems = new Items(PlayerInventory.AREA);
        actions = new List<Action>();
        selectionFrame = Glazier.Get().CreateFrame();
        selectionFrame.SizeScale_X = 1f;
        selectionFrame.SizeScale_Y = 1f;
        selectionFrame.IsVisible = false;
        PlayerUI.container.AddChild(selectionFrame);
        if (hasDragOutsideHandlers)
        {
            outsideSelectionInvisibleButton = Glazier.Get().CreateImage();
            outsideSelectionInvisibleButton.SizeScale_X = 1f;
            outsideSelectionInvisibleButton.SizeScale_Y = 1f;
            outsideSelectionInvisibleButton.OnClicked += onClickedOutsideSelection;
            outsideSelectionInvisibleButton.OnRightClicked += onClickedOutsideSelection;
            outsideSelectionInvisibleButton.Texture = (Texture2D)GlazierResources.PixelTexture;
            outsideSelectionInvisibleButton.TintColor = new Color(0f, 0f, 0f, 0.5f);
            selectionFrame.AddChild(outsideSelectionInvisibleButton);
        }
        else
        {
            outsideSelectionInvisibleButton = null;
        }
        selectionBackdropBox = Glazier.Get().CreateBox();
        selectionBackdropBox.SizeOffset_X = 530f;
        selectionBackdropBox.SizeOffset_Y = 440f;
        selectionFrame.AddChild(selectionBackdropBox);
        selectionIconBox = Glazier.Get().CreateBox();
        selectionIconBox.PositionOffset_X = 10f;
        selectionIconBox.PositionOffset_Y = 10f;
        selectionIconBox.SizeOffset_X = 510f;
        selectionIconBox.SizeOffset_Y = 310f;
        selectionBackdropBox.AddChild(selectionIconBox);
        selectionIconImage = new SleekItemIcon();
        selectionIconImage.PositionScale_X = 0.5f;
        selectionIconImage.PositionScale_Y = 0.5f;
        selectionIconBox.AddChild(selectionIconImage);
        if (Glazier.Get().SupportsAutomaticLayout)
        {
            selectionDescriptionScrollView = Glazier.Get().CreateScrollView();
            selectionDescriptionScrollView.PositionOffset_X = 10f;
            selectionDescriptionScrollView.PositionOffset_Y = 330f;
            selectionDescriptionScrollView.SizeOffset_X = 250f;
            selectionDescriptionScrollView.SizeOffset_Y = 100f;
            selectionDescriptionScrollView.ScaleContentToWidth = true;
            selectionDescriptionScrollView.ContentUseManualLayout = false;
            selectionBackdropBox.AddChild(selectionDescriptionScrollView);
            selectionDescriptionLabel = Glazier.Get().CreateLabel();
            selectionDescriptionLabel.UseManualLayout = false;
            selectionDescriptionLabel.AllowRichText = true;
            selectionDescriptionLabel.TextAlignment = TextAnchor.UpperLeft;
            selectionDescriptionLabel.TextColor = ESleekTint.RICH_TEXT_DEFAULT;
            selectionDescriptionLabel.TextContrastContext = ETextContrastContext.InconspicuousBackdrop;
            selectionDescriptionScrollView.AddChild(selectionDescriptionLabel);
        }
        else
        {
            selectionDescriptionBox = Glazier.Get().CreateBox();
            selectionDescriptionBox.PositionOffset_X = 10f;
            selectionDescriptionBox.PositionOffset_Y = 330f;
            selectionDescriptionBox.SizeOffset_X = 250f;
            selectionDescriptionBox.SizeOffset_Y = 100f;
            selectionBackdropBox.AddChild(selectionDescriptionBox);
            selectionDescriptionLabel = Glazier.Get().CreateLabel();
            selectionDescriptionLabel.AllowRichText = true;
            selectionDescriptionLabel.PositionOffset_X = 5f;
            selectionDescriptionLabel.PositionOffset_Y = 5f;
            selectionDescriptionLabel.SizeOffset_X = -10f;
            selectionDescriptionLabel.SizeOffset_Y = -10f;
            selectionDescriptionLabel.SizeScale_X = 1f;
            selectionDescriptionLabel.SizeScale_Y = 1f;
            selectionDescriptionLabel.TextAlignment = TextAnchor.UpperLeft;
            selectionDescriptionLabel.TextColor = ESleekTint.RICH_TEXT_DEFAULT;
            selectionDescriptionLabel.TextContrastContext = ETextContrastContext.InconspicuousBackdrop;
            selectionDescriptionBox.AddChild(selectionDescriptionLabel);
        }
        selectionNameLabel = Glazier.Get().CreateLabel();
        selectionNameLabel.PositionOffset_Y = -70f;
        selectionNameLabel.PositionScale_Y = 1f;
        selectionNameLabel.SizeOffset_Y = 70f;
        selectionNameLabel.SizeScale_X = 1f;
        selectionNameLabel.FontSize = ESleekFontSize.Large;
        selectionNameLabel.TextContrastContext = ETextContrastContext.InconspicuousBackdrop;
        selectionIconBox.AddChild(selectionNameLabel);
        selectionHotkeyLabel = Glazier.Get().CreateLabel();
        selectionHotkeyLabel.PositionOffset_X = 5f;
        selectionHotkeyLabel.PositionOffset_Y = 5f;
        selectionHotkeyLabel.SizeOffset_X = -10f;
        selectionHotkeyLabel.SizeOffset_Y = 30f;
        selectionHotkeyLabel.SizeScale_X = 1f;
        selectionHotkeyLabel.TextAlignment = TextAnchor.UpperRight;
        selectionIconBox.AddChild(selectionHotkeyLabel);
        selectionActionsBox = Glazier.Get().CreateScrollView();
        selectionActionsBox.PositionOffset_X = 270f;
        selectionActionsBox.PositionOffset_Y = 330f;
        selectionActionsBox.SizeOffset_X = -280f;
        selectionActionsBox.SizeOffset_Y = 100f;
        selectionActionsBox.SizeScale_X = 1f;
        selectionActionsBox.ScaleContentToWidth = true;
        selectionBackdropBox.AddChild(selectionActionsBox);
        selectionEquipButton = Glazier.Get().CreateButton();
        selectionEquipButton.SizeScale_X = 1f;
        selectionEquipButton.SizeOffset_Y = 30f;
        selectionEquipButton.OnClicked += onClickedEquip;
        selectionActionsBox.AddChild(selectionEquipButton);
        selectionContextButton = Glazier.Get().CreateButton();
        selectionContextButton.SizeScale_X = 1f;
        selectionContextButton.SizeOffset_Y = 30f;
        selectionContextButton.OnClicked += onClickedContext;
        selectionActionsBox.AddChild(selectionContextButton);
        selectionDropButton = Glazier.Get().CreateButton();
        selectionDropButton.SizeScale_X = 1f;
        selectionDropButton.SizeOffset_Y = 30f;
        selectionDropButton.OnClicked += onClickedDrop;
        selectionActionsBox.AddChild(selectionDropButton);
        selectionStorageButton = Glazier.Get().CreateButton();
        selectionStorageButton.SizeScale_X = 1f;
        selectionStorageButton.SizeOffset_Y = 30f;
        selectionStorageButton.OnClicked += onClickedStore;
        selectionActionsBox.AddChild(selectionStorageButton);
        selectionExtraActionsBox = Glazier.Get().CreateFrame();
        selectionExtraActionsBox.SizeScale_X = 1f;
        selectionActionsBox.AddChild(selectionExtraActionsBox);
        vehicleBox = Glazier.Get().CreateBox();
        vehicleBox.SizeScale_X = 1f;
        clothingBox.AddChild(vehicleBox);
        vehicleNameLabel = Glazier.Get().CreateLabel();
        vehicleNameLabel.SizeOffset_Y = 60f;
        vehicleNameLabel.SizeScale_X = 1f;
        vehicleNameLabel.FontSize = ESleekFontSize.Medium;
        vehicleNameLabel.TextContrastContext = ETextContrastContext.InconspicuousBackdrop;
        vehicleBox.AddChild(vehicleNameLabel);
        vehicleActionsBox = Glazier.Get().CreateFrame();
        vehicleActionsBox.PositionOffset_X = 10f;
        vehicleActionsBox.PositionOffset_Y = 60f;
        vehicleActionsBox.SizeOffset_X = 250f;
        vehicleBox.AddChild(vehicleActionsBox);
        vehicleLockButton = Glazier.Get().CreateButton();
        vehicleLockButton.SizeOffset_Y = 30f;
        vehicleLockButton.SizeScale_X = 1f;
        vehicleLockButton.OnClicked += onClickedVehicleLockButton;
        vehicleActionsBox.AddChild(vehicleLockButton);
        vehicleLockButton.IsVisible = false;
        vehicleHornButton = Glazier.Get().CreateButton();
        vehicleHornButton.SizeOffset_Y = 30f;
        vehicleHornButton.SizeScale_X = 1f;
        vehicleHornButton.OnClicked += onClickedVehicleHornButton;
        vehicleActionsBox.AddChild(vehicleHornButton);
        vehicleHornButton.IsVisible = false;
        vehicleHeadlightsButton = Glazier.Get().CreateButton();
        vehicleHeadlightsButton.SizeOffset_Y = 30f;
        vehicleHeadlightsButton.SizeScale_X = 1f;
        vehicleHeadlightsButton.OnClicked += onClickedVehicleHeadlightsButton;
        vehicleActionsBox.AddChild(vehicleHeadlightsButton);
        vehicleHeadlightsButton.IsVisible = false;
        vehicleSirensButton = Glazier.Get().CreateButton();
        vehicleSirensButton.SizeOffset_Y = 30f;
        vehicleSirensButton.SizeScale_X = 1f;
        vehicleSirensButton.OnClicked += onClickedVehicleSirensButton;
        vehicleActionsBox.AddChild(vehicleSirensButton);
        vehicleSirensButton.IsVisible = false;
        vehicleBlimpButton = Glazier.Get().CreateButton();
        vehicleBlimpButton.SizeOffset_Y = 30f;
        vehicleBlimpButton.SizeScale_X = 1f;
        vehicleBlimpButton.OnClicked += onClickedVehicleBlimpButton;
        vehicleActionsBox.AddChild(vehicleBlimpButton);
        vehicleBlimpButton.IsVisible = false;
        vehicleHookButton = Glazier.Get().CreateButton();
        vehicleHookButton.SizeOffset_Y = 30f;
        vehicleHookButton.SizeScale_X = 1f;
        vehicleHookButton.OnClicked += onClickedVehicleHookButton;
        vehicleActionsBox.AddChild(vehicleHookButton);
        vehicleHookButton.IsVisible = false;
        vehicleStealBatteryButton = Glazier.Get().CreateButton();
        vehicleStealBatteryButton.SizeOffset_Y = 30f;
        vehicleStealBatteryButton.SizeScale_X = 1f;
        vehicleStealBatteryButton.OnClicked += onClickedVehicleStealBatteryButton;
        vehicleActionsBox.AddChild(vehicleStealBatteryButton);
        vehicleStealBatteryButton.IsVisible = false;
        vehicleSkinButton = Glazier.Get().CreateButton();
        vehicleSkinButton.SizeOffset_Y = 30f;
        vehicleSkinButton.SizeScale_X = 1f;
        vehicleSkinButton.OnClicked += onClickedVehicleSkinButton;
        vehicleActionsBox.AddChild(vehicleSkinButton);
        vehicleSkinButton.IsVisible = false;
        vehiclePassengersBox = Glazier.Get().CreateFrame();
        vehiclePassengersBox.PositionOffset_Y = 60f;
        vehiclePassengersBox.SizeScale_X = 1f;
        vehicleBox.AddChild(vehiclePassengersBox);
        rot_xButton = Glazier.Get().CreateButton();
        rot_xButton.PositionScale_X = 1f;
        rot_xButton.SizeOffset_X = 60f;
        rot_xButton.SizeOffset_Y = 60f;
        rot_xButton.OnClicked += onClickedRot_XButton;
        rot_xButton.Text = localization.format("Rot_X");
        headers[PlayerInventory.STORAGE - PlayerInventory.SLOTS].AddChild(rot_xButton);
        rot_xButton.IsVisible = false;
        rot_yButton = Glazier.Get().CreateButton();
        rot_yButton.PositionScale_X = 1f;
        rot_yButton.PositionOffset_X = 60f;
        rot_yButton.SizeOffset_X = 60f;
        rot_yButton.SizeOffset_Y = 60f;
        rot_yButton.OnClicked += onClickedRot_YButton;
        rot_yButton.Text = localization.format("Rot_Y");
        headers[PlayerInventory.STORAGE - PlayerInventory.SLOTS].AddChild(rot_yButton);
        rot_yButton.IsVisible = false;
        rot_zButton = Glazier.Get().CreateButton();
        rot_zButton.PositionScale_X = 1f;
        rot_zButton.PositionOffset_X = 120f;
        rot_zButton.SizeOffset_X = 60f;
        rot_zButton.SizeOffset_Y = 60f;
        rot_zButton.OnClicked += onClickedRot_ZButton;
        rot_zButton.Text = localization.format("Rot_Z");
        headers[PlayerInventory.STORAGE - PlayerInventory.SLOTS].AddChild(rot_zButton);
        rot_zButton.IsVisible = false;
        dragItem = new SleekItem();
        PlayerUI.container.AddChild(dragItem);
        dragItem.IsVisible = false;
        dragItem.SetIsDragItem();
        dragOffset = Vector2.zero;
        dragPivot = Vector2.zero;
        dragFromPage = byte.MaxValue;
        dragFrom_x = byte.MaxValue;
        dragFrom_y = byte.MaxValue;
        dragFromRot = 0;
        PlayerInventory inventory = Player.player.inventory;
        inventory.onInventoryResized = (InventoryResized)Delegate.Combine(inventory.onInventoryResized, new InventoryResized(onInventoryResized));
        PlayerInventory inventory2 = Player.player.inventory;
        inventory2.onInventoryUpdated = (InventoryUpdated)Delegate.Combine(inventory2.onInventoryUpdated, new InventoryUpdated(onInventoryUpdated));
        PlayerInventory inventory3 = Player.player.inventory;
        inventory3.onInventoryAdded = (InventoryAdded)Delegate.Combine(inventory3.onInventoryAdded, new InventoryAdded(onInventoryAdded));
        PlayerInventory inventory4 = Player.player.inventory;
        inventory4.onInventoryRemoved = (InventoryRemoved)Delegate.Combine(inventory4.onInventoryRemoved, new InventoryRemoved(onInventoryRemoved));
        PlayerInventory inventory5 = Player.player.inventory;
        inventory5.onInventoryStored = (InventoryStored)Delegate.Combine(inventory5.onInventoryStored, new InventoryStored(onInventoryStored));
        PlayerEquipment equipment = Player.player.equipment;
        equipment.onHotkeysUpdated = (HotkeysUpdated)Delegate.Combine(equipment.onHotkeysUpdated, new HotkeysUpdated(onHotkeysUpdated));
        ItemManager.onItemDropAdded = onItemDropAdded;
        ItemManager.onItemDropRemoved = onItemDropRemoved;
        PlayerMovement movement = Player.player.movement;
        movement.onSeated = (Seated)Delegate.Combine(movement.onSeated, new Seated(onSeated));
        PlayerClothing clothing = Player.player.clothing;
        clothing.onShirtUpdated = (ShirtUpdated)Delegate.Combine(clothing.onShirtUpdated, new ShirtUpdated(onShirtUpdated));
        PlayerClothing clothing2 = Player.player.clothing;
        clothing2.onPantsUpdated = (PantsUpdated)Delegate.Combine(clothing2.onPantsUpdated, new PantsUpdated(onPantsUpdated));
        PlayerClothing clothing3 = Player.player.clothing;
        clothing3.onHatUpdated = (HatUpdated)Delegate.Combine(clothing3.onHatUpdated, new HatUpdated(onHatUpdated));
        PlayerClothing clothing4 = Player.player.clothing;
        clothing4.onBackpackUpdated = (BackpackUpdated)Delegate.Combine(clothing4.onBackpackUpdated, new BackpackUpdated(onBackpackUpdated));
        PlayerClothing clothing5 = Player.player.clothing;
        clothing5.onVestUpdated = (VestUpdated)Delegate.Combine(clothing5.onVestUpdated, new VestUpdated(onVestUpdated));
        PlayerClothing clothing6 = Player.player.clothing;
        clothing6.onMaskUpdated = (MaskUpdated)Delegate.Combine(clothing6.onMaskUpdated, new MaskUpdated(onMaskUpdated));
        PlayerClothing clothing7 = Player.player.clothing;
        clothing7.onGlassesUpdated = (GlassesUpdated)Delegate.Combine(clothing7.onGlassesUpdated, new GlassesUpdated(onGlassesUpdated));
    }

    internal static string FormatStatColor(string text, bool isBeneficial)
    {
        Color32 color = (isBeneficial ? OptionsSettings.fontColor : OptionsSettings.badColor);
        return "<color=" + Palette.hex(color) + ">" + text + "</color>";
    }

    internal static string FormatStatModifier(float modifier, bool higherIsPositive, bool higherIsBeneficial)
    {
        char c = ((!higherIsPositive) ? ((modifier > 1f) ? '-' : '+') : ((modifier > 1f) ? '+' : '-'));
        bool isBeneficial = (higherIsBeneficial ? (modifier > 1f) : (modifier < 1f));
        float num = ((modifier > 1f) ? (modifier - 1f) : (1f - modifier));
        return FormatStatColor($"{c}{num:P}", isBeneficial);
    }
}
