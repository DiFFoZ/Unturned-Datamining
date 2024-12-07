using System;
using System.Collections.Generic;
using UnityEngine;

namespace SDG.Unturned;

internal class ItemStoreMenu : SleekFullscreenBox
{
    private enum ECategoryFilter
    {
        None,
        /// <summary>
        /// Collections of multiple items. 
        /// </summary>
        Bundles,
        /// <summary>
        /// Discounted items.
        /// </summary>
        Specials,
        /// <summary>
        /// Items marked as new in the Status.json file.
        /// </summary>
        New,
        /// <summary>
        /// Items marked as featured in the Status.json file.
        /// </summary>
        Featured
    }

    public static ItemStoreMenu instance;

    private ItemStoreCartMenu cartMenu;

    private ItemStoreDetailsMenu detailsMenu;

    private ItemStoreBundleContentsMenu bundleContentsMenu;

    private SleekItemStoreListing[] listingButtons;

    private ISleekElement categoryButtonsFrame;

    private ISleekField searchField;

    /// <summary>
    /// Toggle button to open/close advanced filters panel.
    /// </summary>
    private static SleekButtonIcon optionsButton;

    /// <summary>
    /// On/off checkbox for including already-owned items in filter.
    /// </summary>
    private static ISleekToggle showOwnedToggle;

    /// <summary>
    /// Container for advanced options.
    /// </summary>
    private static ISleekElement optionsPanel;

    /// <summary>
    /// Displays the current page number.
    /// </summary>
    private ISleekBox pageBox;

    /// <summary>
    /// Only visible when cart is not empty.
    /// </summary>
    private ISleekButton viewCartButton;

    private List<ItemStore.Listing> filteredListings;

    /// <summary>
    /// [0, pageCount)
    /// </summary>
    private int pageIndex;

    private int pageCount;

    /// <summary>
    /// If true, listings should be re-filtered when opening the menu.
    /// </summary>
    private bool areListingsDirty;

    private ECategoryFilter categoryFilter;

    public Local localization { get; private set; }

    public Bundle icons { get; private set; }

    public bool IsOpen { get; private set; }

    public void Open()
    {
        IsOpen = true;
        AnimateIntoView();
        viewCartButton.IsVisible = !ItemStore.Get().IsCartEmpty;
        if (areListingsDirty)
        {
            areListingsDirty = false;
            FilterListings();
        }
        else
        {
            RefreshListingsInCart();
        }
    }

    public void OpenNewItems()
    {
        searchField.Text = string.Empty;
        categoryFilter = ECategoryFilter.New;
        areListingsDirty = true;
        Open();
    }

    public void Close()
    {
        if (IsOpen)
        {
            IsOpen = false;
            AnimateOutOfView(0f, 1f);
        }
    }

    public ItemStoreMenu()
    {
        localization = Localization.read("/Menu/Survivors/ItemStoreMenu.dat");
        icons = Bundles.getBundle("/Bundles/Textures/Menu/Icons/Survivors/ItemStore/ItemStore.unity3d");
        instance = this;
        base.PositionScale_Y = 1f;
        base.PositionOffset_X = 10f;
        base.PositionOffset_Y = 10f;
        base.SizeOffset_X = -20f;
        base.SizeOffset_Y = -20f;
        base.SizeScale_X = 1f;
        base.SizeScale_Y = 1f;
        ISleekConstraintFrame sleekConstraintFrame = Glazier.Get().CreateConstraintFrame();
        sleekConstraintFrame.PositionOffset_Y = 70f;
        sleekConstraintFrame.PositionScale_X = 0.5f;
        sleekConstraintFrame.SizeScale_X = 0.5f;
        sleekConstraintFrame.SizeScale_Y = 1f;
        sleekConstraintFrame.SizeOffset_Y = -105f;
        sleekConstraintFrame.Constraint = ESleekConstraint.FitInParent;
        AddChild(sleekConstraintFrame);
        listingButtons = new SleekItemStoreListing[25];
        for (int i = 0; i < 25; i++)
        {
            SleekItemStoreListing sleekItemStoreListing = new SleekItemStoreListing
            {
                PositionOffset_X = 5f,
                PositionOffset_Y = 5f,
                PositionScale_X = (float)(i % 5) * 0.2f,
                PositionScale_Y = (float)Mathf.FloorToInt((float)i / 5f) * 0.2f,
                SizeOffset_X = -10f,
                SizeOffset_Y = -10f,
                SizeScale_X = 0.2f,
                SizeScale_Y = 0.2f
            };
            sleekConstraintFrame.AddChild(sleekItemStoreListing);
            listingButtons[i] = sleekItemStoreListing;
        }
        categoryButtonsFrame = Glazier.Get().CreateFrame();
        categoryButtonsFrame.PositionOffset_Y = -70f;
        categoryButtonsFrame.SizeScale_X = 1f;
        categoryButtonsFrame.SizeOffset_Y = 30f;
        sleekConstraintFrame.AddChild(categoryButtonsFrame);
        searchField = Glazier.Get().CreateStringField();
        searchField.PositionOffset_X = 40f;
        searchField.PositionOffset_Y = -35f;
        searchField.SizeOffset_X = -150f;
        searchField.SizeOffset_Y = 30f;
        searchField.SizeScale_X = 1f;
        searchField.PlaceholderText = MenuSurvivorsClothingUI.localization.format("Search_Field_Hint");
        searchField.OnTextSubmitted += OnEnteredSearchField;
        sleekConstraintFrame.AddChild(searchField);
        ISleekButton sleekButton = Glazier.Get().CreateButton();
        sleekButton.PositionOffset_X = -100f;
        sleekButton.PositionOffset_Y = -35f;
        sleekButton.PositionScale_X = 1f;
        sleekButton.SizeOffset_X = 100f;
        sleekButton.SizeOffset_Y = 30f;
        sleekButton.Text = MenuSurvivorsClothingUI.localization.format("Search");
        sleekButton.TooltipText = MenuSurvivorsClothingUI.localization.format("Search_Tooltip");
        sleekButton.OnClicked += OnClickedSearchButton;
        sleekConstraintFrame.AddChild(sleekButton);
        optionsButton = new SleekButtonIcon(MenuSurvivorsClothingUI.icons.load<Texture2D>("Left"));
        optionsButton.PositionOffset_Y = -35f;
        optionsButton.SizeOffset_X = 30f;
        optionsButton.SizeOffset_Y = 30f;
        optionsButton.tooltip = MenuSurvivorsClothingUI.localization.format("Advanced_Options_Tooltip");
        optionsButton.iconColor = ESleekTint.FOREGROUND;
        optionsButton.onClickedButton += OnClickedOptionsButton;
        sleekConstraintFrame.AddChild(optionsButton);
        optionsPanel = Glazier.Get().CreateFrame();
        optionsPanel.PositionOffset_X = -205f;
        optionsPanel.PositionOffset_Y = -35f;
        optionsPanel.SizeOffset_X = 200f;
        optionsPanel.SizeOffset_Y = 400f;
        optionsPanel.IsVisible = false;
        sleekConstraintFrame.AddChild(optionsPanel);
        showOwnedToggle = Glazier.Get().CreateToggle();
        showOwnedToggle.SizeOffset_X = 40f;
        showOwnedToggle.SizeOffset_Y = 40f;
        showOwnedToggle.AddLabel(localization.format("FilterShowOwned_Label"), ESleekSide.RIGHT);
        showOwnedToggle.OnValueChanged += OnShowOwnedToggled;
        optionsPanel.AddChild(showOwnedToggle);
        pageBox = Glazier.Get().CreateBox();
        pageBox.PositionOffset_X = -50f;
        pageBox.PositionOffset_Y = 5f;
        pageBox.PositionScale_X = 0.5f;
        pageBox.PositionScale_Y = 1f;
        pageBox.SizeOffset_X = 100f;
        pageBox.SizeOffset_Y = 30f;
        pageBox.FontSize = ESleekFontSize.Medium;
        sleekConstraintFrame.AddChild(pageBox);
        SleekButtonIcon sleekButtonIcon = new SleekButtonIcon(MenuSurvivorsClothingUI.icons.load<Texture2D>("Left"));
        sleekButtonIcon.PositionOffset_X = -85f;
        sleekButtonIcon.PositionOffset_Y = 5f;
        sleekButtonIcon.PositionScale_X = 0.5f;
        sleekButtonIcon.PositionScale_Y = 1f;
        sleekButtonIcon.SizeOffset_X = 30f;
        sleekButtonIcon.SizeOffset_Y = 30f;
        sleekButtonIcon.tooltip = MenuSurvivorsClothingUI.localization.format("Left_Tooltip");
        sleekButtonIcon.iconColor = ESleekTint.FOREGROUND;
        sleekButtonIcon.onClickedButton += OnClickedLeftPageButton;
        sleekConstraintFrame.AddChild(sleekButtonIcon);
        SleekButtonIcon sleekButtonIcon2 = new SleekButtonIcon(MenuSurvivorsClothingUI.icons.load<Texture2D>("Right"));
        sleekButtonIcon2.PositionOffset_X = 55f;
        sleekButtonIcon2.PositionOffset_Y = 5f;
        sleekButtonIcon2.PositionScale_X = 0.5f;
        sleekButtonIcon2.PositionScale_Y = 1f;
        sleekButtonIcon2.SizeOffset_X = 30f;
        sleekButtonIcon2.SizeOffset_Y = 30f;
        sleekButtonIcon2.tooltip = MenuSurvivorsClothingUI.localization.format("Right_Tooltip");
        sleekButtonIcon2.iconColor = ESleekTint.FOREGROUND;
        sleekButtonIcon2.onClickedButton += OnClickedRightPageButton;
        sleekConstraintFrame.AddChild(sleekButtonIcon2);
        viewCartButton = Glazier.Get().CreateButton();
        viewCartButton.PositionOffset_Y = -110f;
        viewCartButton.PositionScale_Y = 1f;
        viewCartButton.SizeOffset_X = 200f;
        viewCartButton.SizeOffset_Y = 50f;
        viewCartButton.Text = localization.format("ViewCart_Label");
        viewCartButton.TooltipText = localization.format("ViewCart_Tooltip");
        viewCartButton.OnClicked += OnClickedViewCartButton;
        viewCartButton.FontSize = ESleekFontSize.Medium;
        AddChild(viewCartButton);
        ISleekSprite sleekSprite = Glazier.Get().CreateSprite(icons.load<Sprite>("Cart"));
        sleekSprite.PositionOffset_X = 5f;
        sleekSprite.PositionOffset_Y = 5f;
        sleekSprite.SizeOffset_X = 40f;
        sleekSprite.SizeOffset_Y = 40f;
        sleekSprite.TintColor = ESleekTint.FOREGROUND;
        sleekSprite.DrawMethod = ESleekSpriteType.Regular;
        viewCartButton.AddChild(sleekSprite);
        SleekButtonIcon sleekButtonIcon3 = new SleekButtonIcon(MenuDashboardUI.icons.load<Texture2D>("Exit"));
        sleekButtonIcon3.PositionOffset_Y = -50f;
        sleekButtonIcon3.PositionScale_Y = 1f;
        sleekButtonIcon3.SizeOffset_X = 200f;
        sleekButtonIcon3.SizeOffset_Y = 50f;
        sleekButtonIcon3.text = MenuDashboardUI.localization.format("BackButtonText");
        sleekButtonIcon3.tooltip = MenuDashboardUI.localization.format("BackButtonTooltip");
        sleekButtonIcon3.onClickedButton += OnClickedBackButton;
        sleekButtonIcon3.fontSize = ESleekFontSize.Medium;
        sleekButtonIcon3.iconColor = ESleekTint.FOREGROUND;
        AddChild(sleekButtonIcon3);
        cartMenu = new ItemStoreCartMenu();
        MenuUI.container.AddChild(cartMenu);
        detailsMenu = new ItemStoreDetailsMenu();
        MenuUI.container.AddChild(detailsMenu);
        bundleContentsMenu = new ItemStoreBundleContentsMenu();
        MenuUI.container.AddChild(bundleContentsMenu);
        ItemStore.Get().OnPricesReceived += OnPricesReceived;
        ItemStore.Get().OnPurchaseResult += OnPurchaseResult;
        ItemStore.Get().RequestPrices();
    }

    public override void OnDestroy()
    {
        ItemStore.Get().OnPricesReceived -= OnPricesReceived;
        ItemStore.Get().OnPurchaseResult -= OnPurchaseResult;
        base.OnDestroy();
    }

    private void OnPricesReceived()
    {
        filteredListings = new List<ItemStore.Listing>(ItemStore.Get().GetListings().Length);
        CreateFilterCategoryButtons();
        areListingsDirty = true;
    }

    private void OnPurchaseResult(ItemStore.EPurchaseResult result)
    {
        switch (result)
        {
        case ItemStore.EPurchaseResult.UnableToInitialize:
            MenuUI.alert(localization.format("PurchaseResult_UnableToInitialize"));
            break;
        case ItemStore.EPurchaseResult.Denied:
            MenuUI.alert(localization.format("PurchaseResult_Denied"));
            break;
        }
    }

    private void BuildListingsFromIndices(int[] listingIndices)
    {
        ItemStore.Listing[] listings = ItemStore.Get().GetListings();
        foreach (int num in listingIndices)
        {
            filteredListings.Add(listings[num]);
        }
    }

    private void BuildCategoryListings()
    {
        if (categoryFilter == ECategoryFilter.Specials)
        {
            BuildListingsFromIndices(ItemStore.Get().GetDiscountedListingIndices());
            return;
        }
        if (categoryFilter == ECategoryFilter.New)
        {
            BuildListingsFromIndices(ItemStore.Get().GetNewListingIndices());
            return;
        }
        if (categoryFilter == ECategoryFilter.Featured)
        {
            BuildListingsFromIndices(ItemStore.Get().GetFeaturedListingIndices());
            return;
        }
        ItemStore.Listing[] listings = ItemStore.Get().GetListings();
        filteredListings.AddRange(listings);
        if (categoryFilter != ECategoryFilter.Bundles)
        {
            return;
        }
        for (int num = filteredListings.Count - 1; num >= 0; num--)
        {
            if (!Provider.provider.economyService.IsItemBundle(filteredListings[num].itemdefid))
            {
                filteredListings.RemoveAtFast(num);
            }
        }
    }

    /// <summary>
    /// Remove items that do not match search text.
    /// </summary>
    private void ApplySearchTextFilter()
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
        for (int num = filteredListings.Count - 1; num >= 0; num--)
        {
            int itemdefid = filteredListings[num].itemdefid;
            string inventoryName = Provider.provider.economyService.getInventoryName(itemdefid);
            if (!tokenSearchFilter.Value.matches(inventoryName))
            {
                string inventoryType = Provider.provider.economyService.getInventoryType(itemdefid);
                if (!tokenSearchFilter.Value.matches(inventoryType))
                {
                    filteredListings.RemoveAtFast(num);
                }
            }
        }
    }

    private void ApplyOwnedFilter()
    {
        if (!string.IsNullOrEmpty(searchField.Text) || showOwnedToggle.Value)
        {
            return;
        }
        HashSet<int> hashSet = Provider.provider.economyService.GatherOwnedItemDefIds();
        for (int num = filteredListings.Count - 1; num >= 0; num--)
        {
            int itemdefid = filteredListings[num].itemdefid;
            if (Provider.provider.economyService.IsItemBundle(itemdefid))
            {
                List<int> bundleContents = Provider.provider.economyService.GetBundleContents(itemdefid);
                if (bundleContents != null && bundleContents.Count > 0)
                {
                    bool flag = false;
                    foreach (int item in bundleContents)
                    {
                        if (hashSet.Contains(item))
                        {
                            flag = true;
                            break;
                        }
                    }
                    if (flag)
                    {
                        filteredListings.RemoveAtFast(num);
                        continue;
                    }
                }
            }
            if (hashSet.Contains(itemdefid))
            {
                filteredListings.RemoveAtFast(num);
            }
        }
    }

    private void SortListings()
    {
        filteredListings.Sort(delegate(ItemStore.Listing lhs, ItemStore.Listing rhs)
        {
            DateTime creationTime = Provider.provider.economyService.GetCreationTime(lhs.itemdefid);
            DateTime creationTime2 = Provider.provider.economyService.GetCreationTime(rhs.itemdefid);
            int num = -creationTime.CompareTo(creationTime2);
            if (num == 0)
            {
                string inventoryName = Provider.provider.economyService.getInventoryName(lhs.itemdefid);
                string inventoryName2 = Provider.provider.economyService.getInventoryName(rhs.itemdefid);
                num = inventoryName.CompareTo(inventoryName2);
            }
            return num;
        });
    }

    private void FilterListings()
    {
        filteredListings.Clear();
        BuildCategoryListings();
        ApplySearchTextFilter();
        ApplyOwnedFilter();
        SortListings();
        pageCount = MathfEx.GetPageCount(filteredListings.Count, listingButtons.Length);
        if (pageIndex >= pageCount)
        {
            pageIndex = pageCount - 1;
        }
        RefreshPage();
    }

    private void RefreshListingsInCart()
    {
        SleekItemStoreListing[] array = listingButtons;
        for (int i = 0; i < array.Length; i++)
        {
            array[i].RefreshInCart();
        }
    }

    /// <summary>
    /// Note SetListing also calls RefreshInCart.
    /// </summary>
    private void RefreshPage()
    {
        pageBox.Text = MenuSurvivorsClothingUI.localization.format("Page", pageIndex + 1, pageCount);
        int num = pageIndex * listingButtons.Length;
        int num2 = Mathf.Min(filteredListings.Count - num, listingButtons.Length);
        for (int i = 0; i < num2; i++)
        {
            int index = num + i;
            listingButtons[i].SetListing(filteredListings[index]);
        }
        for (int j = num2; j < listingButtons.Length; j++)
        {
            listingButtons[j].ClearListing();
        }
    }

    /// <summary>
    /// Cannot be created until store data is available.
    /// </summary>
    private void CreateFilterCategoryButtons()
    {
        ItemStore itemStore = ItemStore.Get();
        bool hasDiscountedListings = itemStore.HasDiscountedListings;
        bool hasNewListings = itemStore.HasNewListings;
        bool hasFeaturedListings = itemStore.HasFeaturedListings;
        int num = 2 + (hasNewListings ? 1 : 0) + (hasDiscountedListings ? 1 : 0) + (hasFeaturedListings ? 1 : 0);
        float num2 = 0f;
        float num3 = 1f / (float)num;
        if (hasNewListings)
        {
            ISleekButton sleekButton = Glazier.Get().CreateButton();
            sleekButton.PositionScale_X = num2;
            sleekButton.SizeScale_X = num3;
            sleekButton.SizeScale_Y = 1f;
            sleekButton.Text = localization.format("FilterNewButton_Label") + " x" + itemStore.GetNewListingIndices().Length;
            sleekButton.TooltipText = localization.format("FilterNewButton_Tooltip");
            sleekButton.OnClicked += OnClickedFilterNew;
            categoryButtonsFrame.AddChild(sleekButton);
            num2 += num3;
        }
        if (hasFeaturedListings)
        {
            ISleekButton sleekButton2 = Glazier.Get().CreateButton();
            sleekButton2.PositionScale_X = num2;
            sleekButton2.SizeScale_X = num3;
            sleekButton2.SizeScale_Y = 1f;
            sleekButton2.Text = localization.format("FilterFeaturedButton_Label") + " x" + itemStore.GetFeaturedListingIndices().Length;
            sleekButton2.TooltipText = localization.format("FilterFeaturedButton_Label");
            sleekButton2.OnClicked += OnClickedFilterFeatured;
            categoryButtonsFrame.AddChild(sleekButton2);
            num2 += num3;
        }
        ISleekButton sleekButton3 = Glazier.Get().CreateButton();
        sleekButton3.PositionScale_X = num2;
        sleekButton3.SizeScale_X = num3;
        sleekButton3.SizeScale_Y = 1f;
        sleekButton3.Text = localization.format("FilterAllButton_Label");
        sleekButton3.TooltipText = localization.format("FilterAllButton_Tooltip");
        sleekButton3.OnClicked += OnClickedFilterAll;
        categoryButtonsFrame.AddChild(sleekButton3);
        num2 += num3;
        ISleekButton sleekButton4 = Glazier.Get().CreateButton();
        sleekButton4.PositionScale_X = num2;
        sleekButton4.SizeScale_X = num3;
        sleekButton4.SizeScale_Y = 1f;
        sleekButton4.Text = localization.format("FilterBundlesButton_Label");
        sleekButton4.TooltipText = localization.format("FilterBundlesButton_Tooltip");
        sleekButton4.OnClicked += OnClickedFilterBundles;
        categoryButtonsFrame.AddChild(sleekButton4);
        num2 += num3;
        if (hasDiscountedListings)
        {
            ISleekButton sleekButton5 = Glazier.Get().CreateButton();
            sleekButton5.PositionScale_X = num2;
            sleekButton5.SizeScale_X = num3;
            sleekButton5.SizeScale_Y = 1f;
            sleekButton5.Text = localization.format("FilterSpecialsButton_Label") + " x" + itemStore.GetDiscountedListingIndices().Length;
            sleekButton5.TooltipText = localization.format("FilterSpecialsButton_Tooltip");
            sleekButton5.OnClicked += OnClickedFilterSpecials;
            categoryButtonsFrame.AddChild(sleekButton5);
        }
        if (hasDiscountedListings)
        {
            categoryFilter = ECategoryFilter.Specials;
        }
        else if (hasNewListings)
        {
            categoryFilter = ECategoryFilter.New;
        }
        else if (hasFeaturedListings)
        {
            categoryFilter = ECategoryFilter.Featured;
        }
    }

    private void OnClickedLeftPageButton(ISleekElement button)
    {
        if (pageIndex > 0)
        {
            pageIndex--;
        }
        else
        {
            pageIndex = pageCount - 1;
        }
        RefreshPage();
    }

    private void OnClickedRightPageButton(ISleekElement button)
    {
        if (pageIndex < pageCount - 1)
        {
            pageIndex++;
        }
        else
        {
            pageIndex = 0;
        }
        RefreshPage();
    }

    private void OnClickedFilterAll(ISleekElement button)
    {
        categoryFilter = ECategoryFilter.None;
        FilterListings();
    }

    private void OnClickedFilterBundles(ISleekElement button)
    {
        categoryFilter = ECategoryFilter.Bundles;
        FilterListings();
    }

    private void OnClickedFilterSpecials(ISleekElement button)
    {
        categoryFilter = ECategoryFilter.Specials;
        FilterListings();
    }

    private void OnClickedFilterNew(ISleekElement button)
    {
        categoryFilter = ECategoryFilter.New;
        FilterListings();
    }

    private void OnClickedFilterFeatured(ISleekElement button)
    {
        categoryFilter = ECategoryFilter.Featured;
        FilterListings();
    }

    private void OnEnteredSearchField(ISleekField field)
    {
        FilterListings();
    }

    private void OnClickedSearchButton(ISleekElement button)
    {
        FilterListings();
    }

    private void OnClickedOptionsButton(ISleekElement button)
    {
        optionsPanel.IsVisible = !optionsPanel.IsVisible;
        optionsButton.icon = MenuSurvivorsClothingUI.icons.load<Texture2D>(optionsPanel.IsVisible ? "Right" : "Left");
    }

    private void OnShowOwnedToggled(ISleekToggle toggle, bool state)
    {
        FilterListings();
    }

    private void OnClickedViewCartButton(ISleekElement button)
    {
        cartMenu.Open();
        Close();
    }

    private void OnClickedBackButton(ISleekElement button)
    {
        MenuSurvivorsClothingUI.open();
        Close();
    }
}
