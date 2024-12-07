using System;
using System.Collections.Generic;
using SDG.Provider;
using Steamworks;
using UnityEngine;

namespace SDG.Unturned;

public class MenuSurvivorsClothingUI
{
    private enum ESortMode
    {
        Date,
        Rarity,
        Name,
        Type
    }

    public static Local localization;

    public static Bundle icons;

    private static SleekFullscreenBox container;

    public static bool active;

    public static bool isCrafting;

    private static SleekButtonIcon backButton;

    private static SleekButtonIcon itemstoreButton;

    private static ISleekLabel itemstoreNewLabel;

    private static SleekButtonIcon craftingButton;

    private static ISleekLabel craftingNewLabel;

    private static List<SteamItemDetails_t> filteredItems;

    private static ISleekConstraintFrame inventory;

    private static ISleekConstraintFrame crafting;

    private static SleekInventory[] packageButtons;

    private static ISleekBox availableBox;

    private static ISleekScrollView craftingScrollBox;

    private static ISleekButton[] craftingButtons;

    private static ISleekBox pageBox;

    private static ISleekBox infoBox;

    private static ISleekField searchField;

    private static ISleekButton searchButton;

    private static ISleekBox filterBox;

    private static ISleekButton cancelFilterButton;

    private static SleekButtonIcon leftButton;

    private static SleekButtonIcon rightButton;

    private static SleekButtonIcon refreshButton;

    /// <summary>
    /// Toggle button to open/close advanced filters panel.
    /// </summary>
    private static SleekButtonIcon optionsButton;

    /// <summary>
    /// On/off checkbox for including description text in filter.
    /// </summary>
    private static ISleekToggle searchDescriptionsToggle;

    /// <summary>
    /// Switch between sort modes.
    /// </summary>
    private static SleekButtonState sortModeButton;

    /// <summary>
    /// On/off checkbox to reverse sort results.
    /// </summary>
    private static ISleekToggle reverseSortOrderToggle;

    /// <summary>
    /// On/off checkbox to show only equipped items.
    /// </summary>
    private static ISleekToggle filterEquippedToggle;

    /// <summary>
    /// Container for advanced options.
    /// </summary>
    private static ISleekElement optionsPanel;

    private static ISleekSlider characterSlider;

    private static ISleekButton grantPackagePromoButton;

    private static int pageIndex;

    private static EEconFilterMode filterMode;

    private static ulong filterInstigator;

    /// <summary>
    /// Whether to include description text in filter.
    /// </summary>
    private static bool searchDescriptions;

    /// <summary>
    /// How to sort filtered items.
    /// </summary>
    private static ESortMode sortMode;

    /// <summary>
    /// Should sorted list be reversed?
    /// </summary>
    private static bool reverseSortOrder;

    /// <summary>
    /// Should only equipped items be shown?
    /// </summary>
    private static bool filterEquipped;

    private MenuSurvivorsClothingItemUI itemUI;

    private MenuSurvivorsClothingInspectUI inspectUI;

    private MenuSurvivorsClothingDeleteUI deleteUI;

    private MenuSurvivorsClothingBoxUI boxUI;

    private ItemStoreMenu itemStoreUI;

    private List<EconCraftOption> econCraftOptions;

    private static int numberOfPages => MathfEx.GetPageCount(filteredItems.Count, 25);

    public static void open()
    {
        if (!active)
        {
            active = true;
            Characters.RefreshPreviewCharacterModel();
            characterSlider.Value = Characters.characterYaw / 360f;
            container.AnimateIntoView();
        }
    }

    public static void close()
    {
        if (active)
        {
            active = false;
            Characters.RefreshPreviewCharacterModel();
            container.AnimateOutOfView(0f, 1f);
        }
    }

    public static void setFilter(EEconFilterMode newFilterMode, ulong newFilterInstigator)
    {
        setCrafting(isCrafting: false);
        filterMode = newFilterMode;
        filterInstigator = newFilterInstigator;
        filterBox.IsVisible = filterMode != EEconFilterMode.SEARCH;
        cancelFilterButton.IsVisible = filterMode != EEconFilterMode.SEARCH;
        if (filterMode != 0)
        {
            searchField.Text = string.Empty;
        }
        if (filterMode == EEconFilterMode.STAT_TRACKER || filterMode == EEconFilterMode.STAT_TRACKER_REMOVAL || filterMode == EEconFilterMode.RAGDOLL_EFFECT_REMOVAL || filterMode == EEconFilterMode.RAGDOLL_EFFECT)
        {
            int inventoryItem = Provider.provider.economyService.getInventoryItem(filterInstigator);
            string inventoryName = Provider.provider.economyService.getInventoryName(inventoryItem);
            Color inventoryColor = Provider.provider.economyService.getInventoryColor(inventoryItem);
            string arg = "<color=" + Palette.hex(inventoryColor) + ">" + inventoryName + "</color>";
            filterBox.Text = localization.format("Filter_Item_Target", arg);
        }
        updateFilterAndPage();
    }

    private static void updateFilterAndPage()
    {
        updateFilter();
        if (pageIndex >= numberOfPages)
        {
            pageIndex = numberOfPages - 1;
        }
        updatePage();
    }

    public static void viewPage(int newPage)
    {
        pageIndex = newPage;
        updatePage();
    }

    private static void onClickedInventory(SleekInventory button)
    {
        int num = packageButtons.Length * pageIndex;
        int num2 = inventory.FindIndexOfChild(button);
        if (num + num2 >= filteredItems.Count)
        {
            return;
        }
        int item = button.item;
        ulong instance = button.instance;
        ushort quantity = button.quantity;
        if (filterMode == EEconFilterMode.STAT_TRACKER || filterMode == EEconFilterMode.STAT_TRACKER_REMOVAL || filterMode == EEconFilterMode.RAGDOLL_EFFECT_REMOVAL || filterMode == EEconFilterMode.RAGDOLL_EFFECT)
        {
            bool flag = filterMode == EEconFilterMode.STAT_TRACKER || filterMode == EEconFilterMode.RAGDOLL_EFFECT;
            MenuSurvivorsClothingDeleteUI.viewItem(item, instance, 1, flag ? EDeleteMode.TAG_TOOL_ADD : EDeleteMode.TAG_TOOL_REMOVE, filterInstigator);
            MenuSurvivorsClothingDeleteUI.open();
            setFilter(EEconFilterMode.SEARCH, 0uL);
            close();
        }
        else if (Provider.preferenceData.Allow_Ctrl_Shift_Alt_Salvage && InputEx.GetKey(KeyCode.LeftControl) && InputEx.GetKey(KeyCode.LeftShift) && InputEx.GetKey(KeyCode.LeftAlt))
        {
            MenuSurvivorsClothingDeleteUI.salvageItem(item, instance);
        }
        else if (InputEx.GetKey(ControlsSettings.other) && packageButtons[num2].itemAsset != null)
        {
            if (button.itemAsset.type == EItemType.BOX)
            {
                MenuSurvivorsClothingItemUI.viewItem(item, quantity, instance);
                MenuSurvivorsClothingBoxUI.viewItem(item, quantity, instance);
                MenuSurvivorsClothingBoxUI.open();
                close();
            }
            else
            {
                Characters.ToggleEquipItemByInstanceId(instance);
            }
        }
        else
        {
            MenuSurvivorsClothingItemUI.viewItem(item, quantity, instance);
            MenuSurvivorsClothingItemUI.open();
            close();
        }
    }

    private static void onEnteredSearchField(ISleekField field)
    {
        updateFilterAndPage();
    }

    private static void onClickedSearchButton(ISleekElement button)
    {
        updateFilterAndPage();
    }

    private static void onClickedCancelFilterButton(ISleekElement button)
    {
        setFilter(EEconFilterMode.SEARCH, 0uL);
    }

    private static void onClickedLeftButton(ISleekElement button)
    {
        if (pageIndex > 0)
        {
            viewPage(pageIndex - 1);
        }
        else if (numberOfPages > 0)
        {
            viewPage(numberOfPages - 1);
        }
    }

    private static void onClickedRightButton(ISleekElement button)
    {
        if (pageIndex < numberOfPages - 1)
        {
            viewPage(pageIndex + 1);
        }
        else if (numberOfPages > 0)
        {
            viewPage(0);
        }
    }

    private static void onClickedOptionsButton(ISleekElement button)
    {
        optionsPanel.IsVisible = !optionsPanel.IsVisible;
        optionsButton.icon = icons.load<Texture2D>(optionsPanel.IsVisible ? "Right" : "Left");
    }

    private static void onToggledSearchDescriptions(ISleekToggle toggle, bool state)
    {
        searchDescriptions = state;
        updateFilterAndPage();
    }

    private static void onChangedSortMode(SleekButtonState button, int state)
    {
        sortMode = (ESortMode)state;
        updateFilterAndPage();
    }

    private static void onToggledReverseSortOrder(ISleekToggle toggle, bool state)
    {
        reverseSortOrder = state;
        updateFilterAndPage();
    }

    private static void onToggledFilterEquipped(ISleekToggle toggle, bool state)
    {
        filterEquipped = state;
        updateFilterAndPage();
    }

    private static void onClickedRefreshButton(ISleekElement button)
    {
        Provider.provider.economyService.refreshInventory();
    }

    private static void onClickedGrantPackagePromoButton(ISleekElement button)
    {
        button.IsVisible = false;
        GrantPackagePromo.SendRequest();
    }

    public static void prepareForCraftResult()
    {
        isCrafting = true;
        MenuUI.openAlert(localization.format("Alert_Crafting"), canBeDismissed: false);
    }

    private void onClickedCraftButton(ISleekElement button)
    {
        if (isCrafting)
        {
            return;
        }
        int num = craftingScrollBox.FindIndexOfChild(button);
        if (num != -1)
        {
            EconCraftOption econCraftOption = econCraftOptions[num];
            if (Provider.provider.economyService.getInventoryPackages(19000, econCraftOption.scrapsNeeded, out var pairs))
            {
                prepareForCraftResult();
                Provider.provider.economyService.exchangeInventory(econCraftOption.generate, pairs);
            }
        }
    }

    private static void onInventoryRefreshed()
    {
        infoBox.IsVisible = false;
        updateFilter();
        if (pageIndex >= numberOfPages)
        {
            pageIndex = numberOfPages - 1;
        }
        updatePage();
        grantPackagePromoButton.IsVisible = GrantPackagePromo.IsEligible();
    }

    public static void onInventoryDropped(int item, ushort quantity, ulong instance)
    {
        MenuUI.closeAll();
        MenuUI.alert(localization.format("Origin_Drop"), instance, item, quantity);
        MenuSurvivorsClothingItemUI.viewItem(item, quantity, instance);
        MenuSurvivorsClothingItemUI.open();
    }

    private static void onCharacterUpdated(byte index, Character character)
    {
        updatePage();
    }

    private static void OnPricesReceived()
    {
        if (ItemStore.Get().HasNewListings && !ItemStoreSavedata.WasNewListingsPageSeen())
        {
            itemstoreNewLabel = Glazier.Get().CreateLabel();
            itemstoreNewLabel.SizeScale_X = 1f;
            itemstoreNewLabel.SizeScale_Y = 1f;
            itemstoreNewLabel.TextContrastContext = ETextContrastContext.InconspicuousBackdrop;
            itemstoreNewLabel.TextAlignment = TextAnchor.UpperRight;
            itemstoreNewLabel.TextColor = Color.green;
            itemstoreNewLabel.Text = Provider.localization.format("New");
            itemstoreButton.AddChild(itemstoreNewLabel);
        }
        else if (ItemStore.Get().HasDiscountedListings)
        {
            ISleekLabel sleekLabel = Glazier.Get().CreateLabel();
            sleekLabel.SizeScale_X = 1f;
            sleekLabel.SizeScale_Y = 1f;
            sleekLabel.TextContrastContext = ETextContrastContext.InconspicuousBackdrop;
            sleekLabel.TextAlignment = TextAnchor.UpperRight;
            sleekLabel.TextColor = Color.green;
            sleekLabel.Text = localization.format("Itemstore_Sale");
            itemstoreButton.AddChild(sleekLabel);
        }
    }

    /// <summary>
    /// Remove items that do not match search text.
    /// </summary>
    private static void applySearchTextFilter()
    {
        string text = searchField.Text;
        if (string.IsNullOrEmpty(text))
        {
            return;
        }
        TokenSearchFilter? tokenSearchFilter = TokenSearchFilter.parse(text);
        if (!tokenSearchFilter.HasValue)
        {
            return;
        }
        for (int num = filteredItems.Count - 1; num >= 0; num--)
        {
            SteamItemDetails_t steamItemDetails_t = filteredItems[num];
            bool flag = false;
            string inventoryName = Provider.provider.economyService.getInventoryName(steamItemDetails_t.m_iDefinition.m_SteamItemDef);
            if (tokenSearchFilter.Value.matches(inventoryName))
            {
                flag = true;
            }
            else
            {
                string inventoryType = Provider.provider.economyService.getInventoryType(steamItemDetails_t.m_iDefinition.m_SteamItemDef);
                if (tokenSearchFilter.Value.matches(inventoryType))
                {
                    flag = true;
                }
            }
            if (!flag)
            {
                if (searchDescriptions)
                {
                    string inventoryDescription = Provider.provider.economyService.getInventoryDescription(steamItemDetails_t.m_iDefinition.m_SteamItemDef);
                    if (tokenSearchFilter.Value.matches(inventoryDescription))
                    {
                        flag = true;
                    }
                }
                if (!flag)
                {
                    filteredItems.RemoveAtFast(num);
                }
            }
        }
    }

    /// <summary>
    /// Removed items that are not equipped.
    /// </summary>
    private static void applyEquippedFilter()
    {
        if (!filterEquipped)
        {
            return;
        }
        for (int num = filteredItems.Count - 1; num >= 0; num--)
        {
            if (!Characters.isEquipped(filteredItems[num].m_itemId.m_SteamItemInstanceID))
            {
                filteredItems.RemoveAtFast(num);
            }
        }
    }

    private static void sortFilteredItems()
    {
        IComparer<SteamItemDetails_t> comparer = sortMode switch
        {
            ESortMode.Rarity => new EconSortMode_Rarity(), 
            ESortMode.Name => new EconSortMode_Name(), 
            ESortMode.Type => new EconSortMode_Type(), 
            _ => null, 
        };
        if (comparer != null)
        {
            filteredItems.Sort(comparer);
        }
        if (reverseSortOrder)
        {
            filteredItems.Reverse();
        }
    }

    private static void updateFilter()
    {
        if (filterMode == EEconFilterMode.STAT_TRACKER)
        {
            filteredItems = new List<SteamItemDetails_t>();
            foreach (SteamItemDetails_t item in Provider.provider.economyService.inventory)
            {
                Guid inventoryItemGuid = Provider.provider.economyService.getInventoryItemGuid(item.m_iDefinition.m_SteamItemDef);
                int inventorySkinID = Provider.provider.economyService.getInventorySkinID(item.m_iDefinition.m_SteamItemDef);
                if (inventoryItemGuid != default(Guid) && inventorySkinID != 0)
                {
                    filteredItems.Add(item);
                }
            }
        }
        else if (filterMode == EEconFilterMode.STAT_TRACKER_REMOVAL)
        {
            filteredItems = new List<SteamItemDetails_t>();
            foreach (SteamItemDetails_t item2 in Provider.provider.economyService.inventory)
            {
                if (Provider.provider.economyService.getInventoryStatTrackerValue(item2.m_itemId.m_SteamItemInstanceID, out var type, out var _) && type != 0)
                {
                    filteredItems.Add(item2);
                }
            }
        }
        else if (filterMode == EEconFilterMode.RAGDOLL_EFFECT_REMOVAL)
        {
            filteredItems = new List<SteamItemDetails_t>();
            foreach (SteamItemDetails_t item3 in Provider.provider.economyService.inventory)
            {
                if (Provider.provider.economyService.getInventoryRagdollEffect(item3.m_itemId.m_SteamItemInstanceID, out var effect) && effect != 0)
                {
                    filteredItems.Add(item3);
                }
            }
        }
        else if (filterMode == EEconFilterMode.RAGDOLL_EFFECT)
        {
            filteredItems = new List<SteamItemDetails_t>();
            foreach (SteamItemDetails_t item4 in Provider.provider.economyService.inventory)
            {
                Guid inventoryItemGuid2 = Provider.provider.economyService.getInventoryItemGuid(item4.m_iDefinition.m_SteamItemDef);
                int inventorySkinID2 = Provider.provider.economyService.getInventorySkinID(item4.m_iDefinition.m_SteamItemDef);
                if (inventoryItemGuid2 != default(Guid) && inventorySkinID2 != 0)
                {
                    Provider.provider.economyService.getInventoryRagdollEffect(item4.m_itemId.m_SteamItemInstanceID, out var effect2);
                    if (effect2 == ERagdollEffect.NONE)
                    {
                        filteredItems.Add(item4);
                    }
                }
            }
        }
        else
        {
            filteredItems = new List<SteamItemDetails_t>(Provider.provider.economyService.inventory);
        }
        applySearchTextFilter();
        applyEquippedFilter();
        sortFilteredItems();
    }

    public static void updatePage()
    {
        availableBox.Text = ItemTool.filterRarityRichText(localization.format("Craft_Available", Provider.provider.economyService.countInventoryPackages(19000)));
        pageBox.Text = localization.format("Page", pageIndex + 1, numberOfPages);
        if (packageButtons == null)
        {
            return;
        }
        int num = packageButtons.Length * pageIndex;
        for (int i = 0; i < packageButtons.Length; i++)
        {
            if (num + i < filteredItems.Count)
            {
                packageButtons[i].updateInventory(filteredItems[num + i].m_itemId.m_SteamItemInstanceID, filteredItems[num + i].m_iDefinition.m_SteamItemDef, filteredItems[num + i].m_unQuantity, isClickable: true, isLarge: false);
            }
            else
            {
                packageButtons[i].updateInventory(0uL, 0, 0, isClickable: false, isLarge: false);
            }
        }
    }

    private static void onDraggedCharacterSlider(ISleekSlider slider, float state)
    {
        Characters.characterYaw = state * 360f;
    }

    private static void onClickedBackButton(ISleekElement button)
    {
        MenuSurvivorsUI.open();
        close();
    }

    private static void onClickedItemstoreButton(ISleekElement button)
    {
        if (itemstoreNewLabel != null)
        {
            itemstoreButton.RemoveChild(itemstoreNewLabel);
            itemstoreNewLabel = null;
            ItemStoreSavedata.MarkNewListingsPageSeen();
            ItemStore.Get().ViewNewItems();
        }
        else
        {
            ItemStore.Get().ViewStore();
        }
    }

    private static void setCrafting(bool isCrafting)
    {
        inventory.IsVisible = !isCrafting;
        crafting.IsVisible = isCrafting;
        craftingButton.icon = (inventory.IsVisible ? icons.load<Texture2D>("Crafting") : icons.load<Texture2D>("Backpack"));
        craftingButton.text = localization.format(inventory.IsVisible ? "Crafting" : "Backpack");
        craftingButton.tooltip = localization.format(inventory.IsVisible ? "Crafting_Tooltip" : "Backpack_Tooltip");
    }

    private static void onClickedCraftingButton(ISleekElement button)
    {
        if (craftingNewLabel != null)
        {
            itemstoreButton.RemoveChild(craftingNewLabel);
            craftingNewLabel = null;
            ItemStoreSavedata.MarkNewCraftingPageSeen();
        }
        setCrafting(!crafting.IsVisible);
    }

    private static void onInventoryExchanged(List<SteamItemDetails_t> grantedItems)
    {
        if (!isCrafting)
        {
            return;
        }
        isCrafting = false;
        MenuUI.closeAlert();
        for (int num = grantedItems.Count - 1; num >= 0; num--)
        {
            if (grantedItems[num].m_iDefinition.m_SteamItemDef == 19000)
            {
                grantedItems.RemoveAtFast(num);
            }
        }
        MenuUI.alertNewItems(localization.format("Origin_Craft"), grantedItems);
        SteamItemDetails_t steamItemDetails_t = grantedItems[0];
        MenuSurvivorsClothingItemUI.viewItem(steamItemDetails_t.m_iDefinition.m_SteamItemDef, steamItemDetails_t.m_unQuantity, steamItemDetails_t.m_itemId.m_SteamItemInstanceID);
        MenuSurvivorsClothingItemUI.open();
        close();
    }

    private static void onInventoryPurchased(List<SteamItemDetails_t> grantedItems)
    {
        MenuUI.closeAlert();
        MenuUI.alertPurchasedItems(localization.format("Origin_Purchase"), grantedItems);
    }

    private static void onInventoryExchangeFailed()
    {
        if (isCrafting)
        {
            UnturnedLog.info("Crafting failed");
            isCrafting = false;
            MenuUI.closeAlert();
        }
    }

    public void OnDestroy()
    {
        boxUI.OnDestroy();
        TempSteamworksEconomy economyService = Provider.provider.economyService;
        economyService.onInventoryExchanged = (TempSteamworksEconomy.InventoryExchanged)Delegate.Remove(economyService.onInventoryExchanged, new TempSteamworksEconomy.InventoryExchanged(onInventoryExchanged));
        TempSteamworksEconomy economyService2 = Provider.provider.economyService;
        economyService2.onInventoryPurchased = (TempSteamworksEconomy.InventoryExchanged)Delegate.Remove(economyService2.onInventoryPurchased, new TempSteamworksEconomy.InventoryExchanged(onInventoryPurchased));
        TempSteamworksEconomy economyService3 = Provider.provider.economyService;
        economyService3.onInventoryExchangeFailed = (TempSteamworksEconomy.InventoryExchangeFailed)Delegate.Remove(economyService3.onInventoryExchangeFailed, new TempSteamworksEconomy.InventoryExchangeFailed(onInventoryExchangeFailed));
        TempSteamworksEconomy economyService4 = Provider.provider.economyService;
        economyService4.onInventoryRefreshed = (TempSteamworksEconomy.InventoryRefreshed)Delegate.Remove(economyService4.onInventoryRefreshed, new TempSteamworksEconomy.InventoryRefreshed(onInventoryRefreshed));
        TempSteamworksEconomy economyService5 = Provider.provider.economyService;
        economyService5.onInventoryDropped = (TempSteamworksEconomy.InventoryDropped)Delegate.Remove(economyService5.onInventoryDropped, new TempSteamworksEconomy.InventoryDropped(onInventoryDropped));
        Characters.onCharacterUpdated = (CharacterUpdated)Delegate.Remove(Characters.onCharacterUpdated, new CharacterUpdated(onCharacterUpdated));
        ItemStore.Get().OnPricesReceived -= OnPricesReceived;
    }

    public MenuSurvivorsClothingUI()
    {
        localization = Localization.read("/Menu/Survivors/MenuSurvivorsClothing.dat");
        if (icons != null)
        {
            icons.unload();
            icons = null;
        }
        icons = Bundles.getBundle("/Bundles/Textures/Menu/Icons/Survivors/MenuSurvivorsClothing/MenuSurvivorsClothing.unity3d");
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
        pageIndex = 0;
        filterMode = EEconFilterMode.SEARCH;
        inventory = Glazier.Get().CreateConstraintFrame();
        inventory.PositionOffset_Y = 80f;
        inventory.PositionScale_X = 0.5f;
        inventory.SizeScale_X = 0.5f;
        inventory.SizeScale_Y = 1f;
        inventory.SizeOffset_Y = -120f;
        inventory.Constraint = ESleekConstraint.FitInParent;
        container.AddChild(inventory);
        crafting = Glazier.Get().CreateConstraintFrame();
        crafting.PositionOffset_Y = 40f;
        crafting.PositionScale_X = 0.5f;
        crafting.SizeScale_X = 0.5f;
        crafting.SizeScale_Y = 1f;
        crafting.SizeOffset_Y = -80f;
        crafting.Constraint = ESleekConstraint.FitInParent;
        container.AddChild(crafting);
        crafting.IsVisible = false;
        packageButtons = new SleekInventory[25];
        for (int i = 0; i < packageButtons.Length; i++)
        {
            SleekInventory sleekInventory = new SleekInventory
            {
                PositionOffset_X = 5f,
                PositionOffset_Y = 5f,
                PositionScale_X = (float)(i % 5) * 0.2f,
                PositionScale_Y = (float)Mathf.FloorToInt((float)i / 5f) * 0.2f,
                SizeOffset_X = -10f,
                SizeOffset_Y = -10f,
                SizeScale_X = 0.2f,
                SizeScale_Y = 0.2f,
                onClickedInventory = onClickedInventory
            };
            inventory.AddChild(sleekInventory);
            packageButtons[i] = sleekInventory;
        }
        searchField = Glazier.Get().CreateStringField();
        searchField.PositionOffset_X = 45f;
        searchField.PositionOffset_Y = -35f;
        searchField.SizeOffset_X = -160f;
        searchField.SizeOffset_Y = 30f;
        searchField.SizeScale_X = 1f;
        searchField.PlaceholderText = localization.format("Search_Field_Hint");
        searchField.OnTextSubmitted += onEnteredSearchField;
        inventory.AddChild(searchField);
        searchButton = Glazier.Get().CreateButton();
        searchButton.PositionOffset_X = -105f;
        searchButton.PositionOffset_Y = -35f;
        searchButton.PositionScale_X = 1f;
        searchButton.SizeOffset_X = 100f;
        searchButton.SizeOffset_Y = 30f;
        searchButton.Text = localization.format("Search");
        searchButton.TooltipText = localization.format("Search_Tooltip");
        searchButton.OnClicked += onClickedSearchButton;
        inventory.AddChild(searchButton);
        filterBox = Glazier.Get().CreateBox();
        filterBox.PositionOffset_X = 5f;
        filterBox.PositionOffset_Y = -75f;
        filterBox.SizeOffset_X = -120f;
        filterBox.SizeOffset_Y = 30f;
        filterBox.SizeScale_X = 1f;
        filterBox.AllowRichText = true;
        filterBox.TextColor = ESleekTint.RICH_TEXT_DEFAULT;
        filterBox.TextContrastContext = ETextContrastContext.InconspicuousBackdrop;
        inventory.AddChild(filterBox);
        filterBox.IsVisible = false;
        cancelFilterButton = Glazier.Get().CreateButton();
        cancelFilterButton.PositionOffset_X = -105f;
        cancelFilterButton.PositionOffset_Y = -75f;
        cancelFilterButton.PositionScale_X = 1f;
        cancelFilterButton.SizeOffset_X = 100f;
        cancelFilterButton.SizeOffset_Y = 30f;
        cancelFilterButton.Text = localization.format("Cancel_Filter");
        cancelFilterButton.TooltipText = localization.format("Cancel_Filter_Tooltip");
        cancelFilterButton.OnClicked += onClickedCancelFilterButton;
        inventory.AddChild(cancelFilterButton);
        cancelFilterButton.IsVisible = false;
        pageBox = Glazier.Get().CreateBox();
        pageBox.PositionOffset_X = -145f;
        pageBox.PositionOffset_Y = 5f;
        pageBox.PositionScale_X = 1f;
        pageBox.PositionScale_Y = 1f;
        pageBox.SizeOffset_X = 100f;
        pageBox.SizeOffset_Y = 30f;
        pageBox.FontSize = ESleekFontSize.Medium;
        inventory.AddChild(pageBox);
        infoBox = Glazier.Get().CreateBox();
        infoBox.PositionOffset_X = 5f;
        infoBox.PositionOffset_Y = -25f;
        infoBox.PositionScale_Y = 0.5f;
        infoBox.SizeScale_X = 1f;
        infoBox.SizeOffset_X = -10f;
        infoBox.SizeOffset_Y = 50f;
        infoBox.Text = localization.format("No_Items");
        infoBox.FontSize = ESleekFontSize.Medium;
        inventory.AddChild(infoBox);
        infoBox.IsVisible = !Provider.provider.economyService.isInventoryAvailable;
        leftButton = new SleekButtonIcon(icons.load<Texture2D>("Left"));
        leftButton.PositionOffset_X = -185f;
        leftButton.PositionOffset_Y = 5f;
        leftButton.PositionScale_X = 1f;
        leftButton.PositionScale_Y = 1f;
        leftButton.SizeOffset_X = 30f;
        leftButton.SizeOffset_Y = 30f;
        leftButton.tooltip = localization.format("Left_Tooltip");
        leftButton.iconColor = ESleekTint.FOREGROUND;
        leftButton.onClickedButton += onClickedLeftButton;
        inventory.AddChild(leftButton);
        rightButton = new SleekButtonIcon(icons.load<Texture2D>("Right"));
        rightButton.PositionOffset_X = -35f;
        rightButton.PositionOffset_Y = 5f;
        rightButton.PositionScale_X = 1f;
        rightButton.PositionScale_Y = 1f;
        rightButton.SizeOffset_X = 30f;
        rightButton.SizeOffset_Y = 30f;
        rightButton.tooltip = localization.format("Right_Tooltip");
        rightButton.iconColor = ESleekTint.FOREGROUND;
        rightButton.onClickedButton += onClickedRightButton;
        inventory.AddChild(rightButton);
        optionsButton = new SleekButtonIcon(icons.load<Texture2D>("Left"));
        optionsButton.PositionOffset_X = 5f;
        optionsButton.PositionOffset_Y = -35f;
        optionsButton.SizeOffset_X = 30f;
        optionsButton.SizeOffset_Y = 30f;
        optionsButton.tooltip = localization.format("Advanced_Options_Tooltip");
        optionsButton.iconColor = ESleekTint.FOREGROUND;
        optionsButton.onClickedButton += onClickedOptionsButton;
        inventory.AddChild(optionsButton);
        optionsPanel = Glazier.Get().CreateFrame();
        optionsPanel.PositionOffset_X = -200f;
        optionsPanel.PositionOffset_Y = -35f;
        optionsPanel.SizeOffset_X = 200f;
        optionsPanel.SizeOffset_Y = 400f;
        optionsPanel.IsVisible = false;
        inventory.AddChild(optionsPanel);
        searchDescriptionsToggle = Glazier.Get().CreateToggle();
        searchDescriptionsToggle.SizeOffset_X = 40f;
        searchDescriptionsToggle.SizeOffset_Y = 40f;
        searchDescriptionsToggle.AddLabel(localization.format("Search_Descriptions_Label"), ESleekSide.RIGHT);
        searchDescriptionsToggle.Value = searchDescriptions;
        searchDescriptionsToggle.OnValueChanged += onToggledSearchDescriptions;
        optionsPanel.AddChild(searchDescriptionsToggle);
        sortModeButton = new SleekButtonState(new GUIContent(localization.format("Sort_Mode_Date")), new GUIContent(localization.format("Sort_Mode_Rarity")), new GUIContent(localization.format("Sort_Mode_Name")));
        sortModeButton.PositionOffset_Y = 50f;
        sortModeButton.SizeOffset_X = 100f;
        sortModeButton.SizeOffset_Y = 30f;
        sortModeButton.AddLabel(localization.format("Sort_Mode_Label"), ESleekSide.RIGHT);
        sortModeButton.tooltip = localization.format("Sort_Mode_Tooltip");
        sortModeButton.state = (int)sortMode;
        sortModeButton.onSwappedState = onChangedSortMode;
        optionsPanel.AddChild(sortModeButton);
        reverseSortOrderToggle = Glazier.Get().CreateToggle();
        reverseSortOrderToggle.PositionOffset_Y = 90f;
        reverseSortOrderToggle.SizeOffset_X = 40f;
        reverseSortOrderToggle.SizeOffset_Y = 40f;
        reverseSortOrderToggle.AddLabel(localization.format("Reverse_Sort_Order_Label"), ESleekSide.RIGHT);
        reverseSortOrderToggle.Value = reverseSortOrder;
        reverseSortOrderToggle.OnValueChanged += onToggledReverseSortOrder;
        optionsPanel.AddChild(reverseSortOrderToggle);
        filterEquippedToggle = Glazier.Get().CreateToggle();
        filterEquippedToggle.PositionOffset_Y = 140f;
        filterEquippedToggle.SizeOffset_X = 40f;
        filterEquippedToggle.SizeOffset_Y = 40f;
        filterEquippedToggle.AddLabel(localization.format("Filter_Equipped_Label"), ESleekSide.RIGHT);
        filterEquippedToggle.Value = filterEquipped;
        filterEquippedToggle.OnValueChanged += onToggledFilterEquipped;
        optionsPanel.AddChild(filterEquippedToggle);
        refreshButton = new SleekButtonIcon(icons.load<Texture2D>("Refresh"));
        refreshButton.PositionOffset_X = 5f;
        refreshButton.PositionOffset_Y = 5f;
        refreshButton.PositionScale_Y = 1f;
        refreshButton.SizeOffset_X = 30f;
        refreshButton.SizeOffset_Y = 30f;
        refreshButton.tooltip = localization.format("Refresh_Tooltip");
        refreshButton.iconColor = ESleekTint.FOREGROUND;
        refreshButton.onClickedButton += onClickedRefreshButton;
        inventory.AddChild(refreshButton);
        grantPackagePromoButton = Glazier.Get().CreateButton();
        grantPackagePromoButton.PositionOffset_Y = -280f;
        grantPackagePromoButton.PositionScale_Y = 1f;
        grantPackagePromoButton.SizeOffset_X = 200f;
        grantPackagePromoButton.SizeOffset_Y = 50f;
        grantPackagePromoButton.Text = "Claim Unturned II Access";
        grantPackagePromoButton.OnClicked += onClickedGrantPackagePromoButton;
        grantPackagePromoButton.IsVisible = false;
        container.AddChild(grantPackagePromoButton);
        characterSlider = Glazier.Get().CreateSlider();
        characterSlider.PositionOffset_X = 45f;
        characterSlider.PositionOffset_Y = 10f;
        characterSlider.PositionScale_Y = 1f;
        characterSlider.SizeOffset_X = -240f;
        characterSlider.SizeOffset_Y = 20f;
        characterSlider.SizeScale_X = 1f;
        characterSlider.Orientation = ESleekOrientation.HORIZONTAL;
        characterSlider.OnValueChanged += onDraggedCharacterSlider;
        inventory.AddChild(characterSlider);
        availableBox = Glazier.Get().CreateBox();
        availableBox.SizeScale_X = 1f;
        availableBox.SizeOffset_Y = 30f;
        availableBox.AllowRichText = true;
        availableBox.TextColor = ESleekTint.RICH_TEXT_DEFAULT;
        availableBox.TextContrastContext = ETextContrastContext.InconspicuousBackdrop;
        crafting.AddChild(availableBox);
        craftingScrollBox = Glazier.Get().CreateScrollView();
        craftingScrollBox.PositionOffset_Y = 40f;
        craftingScrollBox.SizeScale_X = 1f;
        craftingScrollBox.SizeScale_Y = 1f;
        craftingScrollBox.SizeOffset_Y = -40f;
        crafting.AddChild(craftingScrollBox);
        econCraftOptions = new List<EconCraftOption>
        {
            new EconCraftOption("Craft_Common_Cosmetic", 10003, 2),
            new EconCraftOption("Craft_Common_Skin", 10006, 2),
            new EconCraftOption("Craft_Uncommon_Cosmetic", 10004, 13),
            new EconCraftOption("Craft_Uncommon_Skin", 10007, 13),
            new EconCraftOption("Craft_Stat_Tracker_Total_Kills", 19001, 30),
            new EconCraftOption("Craft_Stat_Tracker_Player_Kills", 19002, 30),
            new EconCraftOption("Craft_Ragdoll_Effect_Zero_Kelvin", 19003, 50),
            new EconCraftOption("Craft_Ragdoll_Effect_Jaded", 19013, 50),
            new EconCraftOption("Craft_Mythical_Skin", 19043, 1000),
            new EconCraftOption("Craft_Stat_Tracker_Removal_Tool", 19004, 15),
            new EconCraftOption("Craft_Ragdoll_Effect_Removal_Tool", 19005, 15)
        };
        if (HolidayUtil.getActiveHoliday() == ENPCHoliday.PRIDE_MONTH)
        {
            econCraftOptions.Add(new EconCraftOption("Craft_ProgressPridePin", 1333, 5));
            econCraftOptions.Add(new EconCraftOption("Craft_ProgressPrideJersey", 1334, 5));
        }
        if (LiveConfig.Get().isCandyBagAvailable && LiveConfig.Get().candyBagRequiredCraftingMaterials > 0)
        {
            econCraftOptions.Add(new EconCraftOption("Craft_CandyBag", 21071, (ushort)LiveConfig.Get().candyBagRequiredCraftingMaterials));
        }
        craftingButtons = new ISleekButton[econCraftOptions.Count];
        for (int j = 0; j < craftingButtons.Length; j++)
        {
            EconCraftOption econCraftOption = econCraftOptions[j];
            ISleekButton sleekButton = Glazier.Get().CreateButton();
            sleekButton.PositionOffset_Y = j * 30;
            sleekButton.SizeScale_X = 1f;
            sleekButton.SizeOffset_Y = 30f;
            sleekButton.AllowRichText = true;
            sleekButton.TextColor = ESleekTint.RICH_TEXT_DEFAULT;
            sleekButton.TextContrastContext = ETextContrastContext.InconspicuousBackdrop;
            sleekButton.Text = ItemTool.filterRarityRichText(localization.format("Craft_Entry", localization.format(econCraftOption.token), econCraftOption.scrapsNeeded));
            sleekButton.OnClicked += onClickedCraftButton;
            craftingScrollBox.AddChild(sleekButton);
            craftingButtons[j] = sleekButton;
        }
        craftingScrollBox.ScaleContentToWidth = true;
        craftingScrollBox.ContentSizeOffset = new Vector2(0f, econCraftOptions.Count * 30);
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
        itemstoreButton = new SleekButtonIcon(icons.load<Texture2D>("ItemStore"), 40);
        itemstoreButton.PositionOffset_Y = -170f;
        itemstoreButton.PositionScale_Y = 1f;
        itemstoreButton.SizeOffset_X = 200f;
        itemstoreButton.SizeOffset_Y = 50f;
        itemstoreButton.text = localization.format("Itemstore");
        itemstoreButton.tooltip = localization.format("Itemstore_Tooltip");
        itemstoreButton.onClickedButton += onClickedItemstoreButton;
        itemstoreButton.fontSize = ESleekFontSize.Medium;
        itemstoreButton.iconColor = ESleekTint.FOREGROUND;
        container.AddChild(itemstoreButton);
        if (!Provider.provider.economyService.doesCountryAllowRandomItems && Provider.provider.economyService.hasCountryDetails)
        {
            ISleekLabel sleekLabel = Glazier.Get().CreateLabel();
            sleekLabel.PositionOffset_X = 210f;
            sleekLabel.PositionOffset_Y = -170f;
            sleekLabel.PositionScale_Y = 1f;
            sleekLabel.SizeOffset_X = 200f;
            sleekLabel.SizeOffset_Y = 50f;
            sleekLabel.TextAlignment = TextAnchor.MiddleLeft;
            sleekLabel.Text = localization.format("Itemstore_Region_Box_Disabled", Provider.provider.economyService.getCountryWarningId());
            container.AddChild(sleekLabel);
        }
        craftingButton = new SleekButtonIcon(icons.load<Texture2D>("Crafting"));
        craftingButton.PositionOffset_Y = -110f;
        craftingButton.PositionScale_Y = 1f;
        craftingButton.SizeOffset_X = 200f;
        craftingButton.SizeOffset_Y = 50f;
        craftingButton.text = localization.format("Crafting");
        craftingButton.tooltip = localization.format("Crafting_Tooltip");
        craftingButton.onClickedButton += onClickedCraftingButton;
        craftingButton.fontSize = ESleekFontSize.Medium;
        craftingButton.iconColor = ESleekTint.FOREGROUND;
        container.AddChild(craftingButton);
        if (!ItemStoreSavedata.WasNewCraftingPageSeen())
        {
            craftingNewLabel = Glazier.Get().CreateLabel();
            craftingNewLabel.SizeScale_X = 1f;
            craftingNewLabel.SizeScale_Y = 1f;
            craftingNewLabel.TextContrastContext = ETextContrastContext.InconspicuousBackdrop;
            craftingNewLabel.TextAlignment = TextAnchor.UpperRight;
            craftingNewLabel.TextColor = Color.green;
            craftingNewLabel.Text = Provider.localization.format("New");
            craftingButton.AddChild(craftingNewLabel);
        }
        TempSteamworksEconomy economyService = Provider.provider.economyService;
        economyService.onInventoryExchanged = (TempSteamworksEconomy.InventoryExchanged)Delegate.Combine(economyService.onInventoryExchanged, new TempSteamworksEconomy.InventoryExchanged(onInventoryExchanged));
        TempSteamworksEconomy economyService2 = Provider.provider.economyService;
        economyService2.onInventoryPurchased = (TempSteamworksEconomy.InventoryExchanged)Delegate.Combine(economyService2.onInventoryPurchased, new TempSteamworksEconomy.InventoryExchanged(onInventoryPurchased));
        TempSteamworksEconomy economyService3 = Provider.provider.economyService;
        economyService3.onInventoryExchangeFailed = (TempSteamworksEconomy.InventoryExchangeFailed)Delegate.Combine(economyService3.onInventoryExchangeFailed, new TempSteamworksEconomy.InventoryExchangeFailed(onInventoryExchangeFailed));
        TempSteamworksEconomy economyService4 = Provider.provider.economyService;
        economyService4.onInventoryRefreshed = (TempSteamworksEconomy.InventoryRefreshed)Delegate.Combine(economyService4.onInventoryRefreshed, new TempSteamworksEconomy.InventoryRefreshed(onInventoryRefreshed));
        TempSteamworksEconomy economyService5 = Provider.provider.economyService;
        economyService5.onInventoryDropped = (TempSteamworksEconomy.InventoryDropped)Delegate.Combine(economyService5.onInventoryDropped, new TempSteamworksEconomy.InventoryDropped(onInventoryDropped));
        Characters.onCharacterUpdated = (CharacterUpdated)Delegate.Combine(Characters.onCharacterUpdated, new CharacterUpdated(onCharacterUpdated));
        ItemStore.Get().OnPricesReceived += OnPricesReceived;
        updateFilter();
        updatePage();
        itemUI = new MenuSurvivorsClothingItemUI();
        inspectUI = new MenuSurvivorsClothingInspectUI();
        deleteUI = new MenuSurvivorsClothingDeleteUI();
        boxUI = new MenuSurvivorsClothingBoxUI();
        itemStoreUI = new ItemStoreMenu();
        MenuUI.container.AddChild(itemStoreUI);
    }
}
